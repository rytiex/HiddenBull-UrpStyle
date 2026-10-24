#ifndef HB_STYLE_CAMERA_INCLUDED
#define HB_STYLE_CAMERA_INCLUDED

bool HB_IsPerspective()
{
    return unity_OrthoParams.w == 0.0;
}

float HB_ViewDepth(float3 positionWS)
{
    return -TransformWorldToView(positionWS).z;
}

float HB_PixelsPerMetre(float viewDepth)
{
    float perspective = abs(UNITY_MATRIX_P[1][1]) * 0.5 * _ScreenParams.y / max(viewDepth, HB_EPSILON);
    float orthographic = 0.5 * _ScreenParams.y / max(unity_OrthoParams.y, HB_EPSILON);

    return HB_IsPerspective() ? perspective : orthographic;
}

#endif
