#ifndef HB_STYLE_SCREEN_REFLECTION_INCLUDED
#define HB_STYLE_SCREEN_REFLECTION_INCLUDED

TEXTURE2D(_HB_ReflectHistory);
TEXTURE2D_FLOAT(_HB_ReflectDepth);
float4x4 _HB_ReflectPast;
float4 _HB_ReflectSet;

#define HB_REFLECT_SKY_EYE 1e7
#define HB_REFLECT_THICK   0.05
#define HB_REFLECT_SLACK   0.1
#define HB_REFLECT_REFINE  4
#define HB_REFLECT_EDGE    0.08
#define HB_REFLECT_FAR     0.2
#define HB_REFLECT_BLUR    4.0
#define HB_REFLECT_ROUGH   0.6h
#define HB_REFLECT_FADE    0.3h
#define HB_REFLECT_LIFT    0.002
#define HB_REFLECT_KEEP    0.25
#define HB_REFLECT_NEAR    0.05

float4 HB_ReflectClip(float3 positionWS)
{
    return mul(_HB_ReflectPast, float4(positionWS, 1.0));
}

float2 HB_ReflectUV(float4 clip)
{
    float2 uv = clip.xy * (0.5 / clip.w) + 0.5;

#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
#endif

    return uv;
}

float HB_ReflectEye(float2 uv)
{
    return SAMPLE_TEXTURE2D_LOD(_HB_ReflectDepth, sampler_PointClamp, uv, 0).r;
}

float HB_ReflectThick(float scene)
{
    return scene * (1.0 + HB_REFLECT_THICK) + HB_REFLECT_SLACK;
}

bool HB_ReflectOnScreen(float2 uv)
{
    return all(uv >= 0.0) && all(uv <= 1.0);
}

half4 HB_ReflectTrace(float3 positionWS, float3 heading, half perceptualRoughness)
{
    half4 found = half4(0.0h, 0.0h, 0.0h, 0.0h);
    int steps = (int)_HB_ReflectSet.x;
    float3 eyeWS = GetCameraPositionWS();
    float3 viewWS = normalize(eyeWS - positionWS);
    float facing = saturate(dot(normalize(heading + viewWS), viewWS));
    float2 texels = _ScaledScreenParams.xy * 0.5;
    float2 home = floor(HB_ReflectUV(HB_ReflectClip(positionWS)) * texels);

    float3 origin = eyeWS + (positionWS - eyeWS) * max(1.0 - HB_REFLECT_LIFT / max(facing, 1e-4), HB_REFLECT_KEEP);
    float reach = _HB_ReflectSet.z;
    float4 startClip = HB_ReflectClip(origin);
    float4 endClip = HB_ReflectClip(origin + heading * reach);

    if (endClip.w < HB_REFLECT_NEAR)
    {
        reach *= (startClip.w - HB_REFLECT_NEAR) / max(startClip.w - endClip.w, 1e-5);
        endClip = HB_ReflectClip(origin + heading * reach);
    }

    float nearStart = rcp(startClip.w);
    float nearEnd = rcp(endClip.w);
    float2 start = HB_ReflectUV(startClip);
    float2 span = HB_ReflectUV(endClip) - start;
    float2 bound = span > 0.0 ? (1.0 - start) / max(span, 1e-7) : (span < 0.0 ? start / max(-span, 1e-7) : 1e9);
    float limit = min(1.0, min(bound.x, bound.y));

    float before = 0.0;
    float beforeEye = startClip.w;
    float now = 0.0;
    bool hit = false;
    bool inside = startClip.w > HB_REFLECT_NEAR && HB_ReflectOnScreen(start);

    UNITY_BRANCH
    if (inside && length(span * limit * texels) >= 1.0)
    {
        UNITY_LOOP
        for (int hop = 1; hop <= steps; hop++)
        {
            now = (float)hop / steps * limit;

            float2 uv = start + span * now;
            float eye = rcp(lerp(nearStart, nearEnd, now));
            float scene = HB_ReflectEye(uv);

            UNITY_BRANCH
            if (eye >= scene && min(beforeEye, eye) <= HB_ReflectThick(scene)
                && any(floor(uv * texels) != home))
            {
                float front = before;
                float behind = now;

                UNITY_UNROLL
                for (int refine = 0; refine < HB_REFLECT_REFINE; refine++)
                {
                    float middle = (front + behind) * 0.5;
                    bool past = rcp(lerp(nearStart, nearEnd, middle)) >= HB_ReflectEye(start + span * middle);

                    behind = past ? middle : behind;
                    front = past ? front : middle;
                }

                if (rcp(lerp(nearStart, nearEnd, behind)) <= HB_ReflectThick(HB_ReflectEye(start + span * behind)))
                {
                    now = behind;
                    hit = true;
                    break;
                }
            }

            before = now;
            beforeEye = eye;
        }
    }

    UNITY_BRANCH
    if (hit)
    {
        float2 seen = start + span * now;
        float2 border = min(seen, 1.0 - seen);
        float travel = reach * now * nearEnd / lerp(nearStart, nearEnd, now);

        found.rgb = SAMPLE_TEXTURE2D_LOD(_HB_ReflectHistory, sampler_LinearClamp, seen,
                                         perceptualRoughness * HB_REFLECT_BLUR).rgb;
        found.a = half(saturate(min(border.x, border.y) / HB_REFLECT_EDGE)
                     * saturate((_HB_ReflectSet.z - travel) / (_HB_ReflectSet.z * HB_REFLECT_FAR)));
    }

    return found;
}

half4 HB_ScreenReflect(float3 positionWS, half3 reflectWS, half perceptualRoughness)
{
    half4 found = half4(0.0h, 0.0h, 0.0h, 0.0h);

    UNITY_BRANCH
    if (_HB_ReflectSet.x > 0.5 && _HB_ReflectSet.w > 0.5 && perceptualRoughness < HB_REFLECT_ROUGH)
    {
        found = HB_ReflectTrace(positionWS, float3(reflectWS), perceptualRoughness);
        found.a *= 1.0h - smoothstep(HB_REFLECT_ROUGH - HB_REFLECT_FADE, HB_REFLECT_ROUGH, perceptualRoughness);
    }

    return found;
}

#endif
