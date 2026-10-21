using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace HiddenBull.UrpStyle.Editor
{
    public class LitShaderGUI : StyleShaderGUI
    {
        static class Styles
        {
            public static readonly GUIContent Surface = new GUIContent("Surface");
            public static readonly GUIContent Shading = new GUIContent("Shading");
            public static readonly GUIContent Textures = new GUIContent("Maps");
            public static readonly GUIContent Advanced = new GUIContent("Advanced");
            public static readonly GUIContent Brush = new GUIContent("Brush");
            public static readonly GUIContent Weather = new GUIContent("Weather");

            public static readonly GUIContent Emission = new GUIContent(
                "Emission",
                "Light the surface gives off by itself — lamps, screens, embers, glowing runes. It is " +
                "added on top of the lit colour, so it shows the same in the dark as in daylight, and " +
                "it follows the brush strokes like the Base Map does. Snow lying on it hides it.");

            public static readonly GUIContent EmissionMap = new GUIContent(
                "Emission Map",
                "Colour and strength of the glow. The map is read with the Base Map's layout (UVs or " +
                "Triplanar) and multiplied by the colour; raise the colour's intensity above 1 for a " +
                "glow strong enough to bloom later. Set Global Illumination to Baked for it to light " +
                "its surroundings in the lightmap.");
            public static readonly GUIContent Drops = new GUIContent("Rain Drops");
            public static readonly GUIContent Flakes = new GUIContent("Snow Flakes");

            public static readonly GUIContent Rim = new GUIContent(
                "Rim Light",
                "A glow along the silhouette where the surface turns away from the view, tinted by " +
                "the sky's ambient colour.");

            public const string RestPoseMissing =
                "Rest Pose needs a skinned mesh whose rest pose was written at import (Project " +
                "Settings > HiddenBull URP Style). A renderer in this selection has none, so its " +
                "strokes, drops and triplanar maps will not hold to the surface.";

            public static readonly GUIContent BrushSpace = new GUIContent(
                "Brush Space",
                "World for static geometry. Object for anything that moves — a world-anchored " +
                "brush slides across a carried object. Both use the same stroke size, so the two " +
                "can sit side by side.\n\n" +
                "Rest Pose is for skinned meshes: object space is tied to the transform rather " +
                "than the surface, so skinning drags every point through it and the strokes swim " +
                "across the character. Rest Pose reads a bind-pose position written into the mesh " +
                "at import instead, which skinning cannot move. Use it only on skinned meshes — " +
                "give a shared material its own copy for the character. See Project Settings > " +
                "HiddenBull URP Style.");

            public static readonly GUIContent BrushSize = new GUIContent(
                "Size",
                "Stroke size relative to the project's, which is set once on the renderer feature. " +
                "1 matches every other material; 2 paints this one with strokes twice as large. " +
                "Keep it close to 1 on surfaces that sit next to each other, or the seam between " +
                "them reads as two different brushes.");

            public static readonly GUIContent BrushEdgeKeep = new GUIContent(
                "Edge Keep",
                "How firmly Paint stops at hard edges in the texture. At 1 a stroke that would drag " +
                "in a colour very unlike the one underneath is held back, so regions are painted " +
                "from the inside and a logo, a seam line or a stripe stays crisp. At 0 strokes run " +
                "straight across them and break them up like everything else.\n\n" +
                "Colour is all the shader can go on, so it cannot tell a logo's edge from a " +
                "neighbouring UV island packed close by. If lowering this lets a foreign colour " +
                "creep in somewhere, that is the island showing through.");

            public static readonly GUIContent ToneStrokes = new GUIContent(
                "Tone Strokes",
                "How far light and shade are decided stroke by stroke. Light is read at the middle " +
                "of each stroke instead of at every point, so a whole stroke falls in the light, " +
                "the half tone or the shadow together — the edge between them becomes a run of " +
                "strokes rather than a line, the way a painter lays it. It moves the edge of the " +
                "object's own shadow side and of every shadow cast onto it, along the same strokes " +
                "that repaint the texture.\n\n" +
                "Above 1 a stroke reaches past its own middle and the edge breaks up further. On a " +
                "brushed material this replaces the renderer feature's shadow brush.");

            public static readonly GUIContent BakedStrokes = new GUIContent(
                "Baked Strokes (Bake Only)",
                "The same for lightmapped light: the lightmap is read at the middle of each stroke, " +
                "so the steps of Baked Tones break along strokes too. Turn it down if a lightmap " +
                "chart edge shows through as a stray line.");

            public static readonly GUIContent BrushPaint = new GUIContent(
                "Paint",
                "How far the texture is repainted by the strokes. Each point reads its colour from " +
                "the middle of the stroke it sits under, so a stroke carries one colour across its " +
                "width and drags it along its length, the way a loaded brush does. Nothing slides: " +
                "the texture is resampled, not moved.\n\n" +
                "Strokes keep the same size in the world whatever the UV layout — a densely " +
                "unwrapped face and a stretched one paint with the same brush.\n\n" +
                "0 leaves the texture as it is. Does nothing on a material with no maps. On a mesh " +
                "without usable UVs turn on Triplanar Mapping and Paint works there too.");

            public static readonly GUIContent Triplanar = new GUIContent(
                "Triplanar Mapping",
                "Lays the Base, Normal and Mask maps on from three sides in space instead of by the " +
                "mesh's UVs, so a mesh with no UVs, or with UVs never laid out, is still textured " +
                "and Paint still repaints it. Mapping Space picks the space: World, Object, " +
                "or Rest Pose, which keeps the texture fixed to an animated character's limbs.\n\n" +
                "Tiling X on the Base Map is how many times the texture repeats per metre; Offset " +
                "slides it. The Brush Mask still follows the UVs.");

            public static readonly GUIContent TriplanarSpace = new GUIContent(
                "Mapping Space",
                "World for static geometry, Object for anything that moves, Rest Pose for skinned " +
                "characters. It only places the Base, Normal and Mask maps; the brush has its own Brush Space.");

            public static readonly GUIContent BrushRelief = new GUIContent(
                "Relief",
                "How far each stroke raises the surface along its middle, the way thick paint " +
                "stands proud of the canvas. This is what carries the brush into the ambient, the " +
                "rim and the specular — without it a face that is entirely lit or entirely " +
                "shadowed receives nothing but a flat brightness multiply.");


            public static readonly GUIContent BrushMask = new GUIContent(
                "Mask",
                "Red channel gates the whole brush — paint, relief, break-up and albedo all fall " +
                "silent where it is black. Leave it off and the brush covers everything.");

            public static readonly GUIContent BrushAmbient = new GUIContent(
                "Ambient Break-up",
                "How far the brush shifts what the ambient gradient reads. This is the only brush " +
                "term that still acts where the direct light has saturated — Relief and Shading " +
                "Break-up both work on the terminator, so they fall silent on a face that is " +
                "entirely lit or entirely in shadow.");

            public static readonly GUIContent BrushShading = new GUIContent(
                "Brush Shading Break-up",
                "How far the brush offsets the terminator, breaking the light-to-shadow transition " +
                "into painted patches.");

            public static readonly GUIContent BrushAlbedo = new GUIContent(
                "Brush Albedo Variation",
                "How far the gaps between strokes darken the surface. Only the gaps act, so the " +
                "brush reads as bare patches in the paint rather than as pale strokes laid over " +
                "the top — which is what made it look like scribble.");

            public const string BrushAtlasHint =
                "The atlas and its scale are set once on the HiddenBull Style renderer feature, not " +
                "per material — stroke size has to match across the whole project.";

            public static readonly GUIContent DiffuseWrap = new GUIContent(
                "Diffuse Wrap",
                "How softly light falls off toward the terminator. Low reads as rock, high reads as " +
                "skin. It never carries light onto a face turned away from the light — that side is " +
                "in the surface's own shadow, and reading it from the shadow map only speckles it.");

            public static readonly GUIContent DiffuseSoftness = new GUIContent(
                "Diffuse Softness",
                "Width of each step between the tones of a realtime light. Near zero gives a hard " +
                "cel-like cut; high values blend the tones into a smooth gradient. Lightmapped " +
                "light has its own softness under Baked Tones.");

            public static readonly GUIContent HalfTone = new GUIContent(
                "Half Tone (Realtime Only)",
                "Width of the middle tone between light and shadow. A painter lays the lit side, " +
                "a half tone and the shadow as flat values rather than as a gradient, and this is " +
                "that half tone. 0 leaves two tones.\n\n" +
                "Cast shadows land on the same tones as the form's own shadow side, since a painter " +
                "does not tell them apart: the soft edge of a shadow becomes the half tone band, " +
                "narrow where the caster touches the ground and wide where it lifts away.\n\n" +
                "It follows realtime light — Realtime and Mixed — whether the object is static or " +
                "not. A fully Baked light puts no realtime light on anything, so it does nothing " +
                "there; Baked Tones covers that case.");

            public static readonly GUIContent BakedTones = new GUIContent(
                "Baked Tones (Bake Only)",
                "How far lightmapped light is stepped into tones as well. A fully Baked light lives " +
                "inside the lightmap, so without this its light and shadow are Unity's smooth " +
                "falloff and none of the tones above apply.\n\n" +
                "The steps are a stop apart — each tone half as bright as the one above — so a " +
                "dark interior stays dark rather than being lifted onto a step. 0 leaves the " +
                "lightmap as it was baked.");

            public static readonly GUIContent BakedSoftness = new GUIContent(
                "Softness",
                "Width of each step between lightmapped tones. Near zero gives crisp flat steps; " +
                "1 blends them back into the lightmap's own falloff.");

            public static readonly GUIContent SurfaceType = new GUIContent(
                "Surface Type",
                "Transparent surfaces are blended without sorting: every layer lands in the right " +
                "order whatever the draw order, so hair, foliage, glass and liquid can overlap and " +
                "intersect without popping. They receive light, shadow, brush and fog like any " +
                "other surface. They do not write depth. They cast an ordinary solid shadow, " +
                "following the renderer's Cast Shadows setting, unless Cast Colored Shadows is on.");

            public static readonly GUIContent CastColoredShadows = new GUIContent(
                "Cast Colored Shadows",
                "Lets sunlight through in colour: light passing the surface takes on Base Color and " +
                "the Base Map, and dims with alpha until a fully solid surface casts a full shadow. " +
                "Overlapping surfaces stack their colours, so light through a red pane and a blue " +
                "one comes out dark purple. Glass keeps its tint over a faint shade.\n\n" +
                "Off, the surface casts an ordinary solid shadow like an opaque one, and the " +
                "renderer's Cast Shadows setting decides whether it casts at all.\n\n" +
                "On, the colour follows the sun and moon only; other lights see the surface as clear. " +
                "The renderer's Cast Shadows setting cannot reach the coloured shadow, so turn this " +
                "off for anything that should not shadow.");

            public static readonly GUIContent BlendMode = new GUIContent(
                "Blend Mode",
                "Alpha fades the whole surface, highlights included — for hair, cloth, leaves and " +
                "smoke. Premultiply fades only the colour and keeps highlights at full strength, so " +
                "clear glass or water still shows a bright reflection where it is almost invisible. " +
                "Additive adds its colour on top of what is behind it and never darkens it — for fire, " +
                "glows and light beams. Additive surfaces cast no shadow and do not refract.");

            public static readonly GUIContent Refraction = new GUIContent(
                "Refraction",
                "Bends what is seen through the surface. The bend follows the surface's slope " +
                "against the view, so it is strongest at the edges of glass and liquid and fades to " +
                "nothing where the surface faces you. Transparent surfaces behind this one bend with " +
                "the background, those in front do not, and overlapping panes add up.\n\n" +
                "With it on, the surface stops hiding what is behind it: Base Color tints the view " +
                "through it instead, and its alpha sets how strongly — 0 is clear glass, 1 the full " +
                "colour, like a green bottle or red wine. Highlights, rim and the brush still sit on " +
                "top. Bending, blur and dispersion stay at full strength at any alpha.");

            public static readonly GUIContent StrokeBend = new GUIContent(
                "Stroke Bend",
                "How far the brush strokes drag the view through the surface, as if the glass " +
                "were laid on in paint. Needs the brush on.");

            public static readonly GUIContent StrokeAlpha = new GUIContent(
                "Stroke Alpha",
                "How far the brush strokes break up the transparency where the surface is partly " +
                "see-through, so a soft fade turns into strokes. Fully clear and fully solid areas " +
                "are left alone. On glass it breaks up the tint instead.");

            public static readonly GUIContent RefractionStrength = new GUIContent(
                "Strength",
                "How far the view through the surface is bent, as if through glass up to a quarter " +
                "of a metre thick. It shrinks with distance the way real refraction does.");

            public static readonly GUIContent RefractionBlur = new GUIContent(
                "Blur",
                "Frosts what is seen through the surface — frosted glass, murky water.");

            public static readonly GUIContent Dispersion = new GUIContent(
                "Dispersion",
                "Splits the bent view into its colours along the bend, so edges pick up a thin " +
                "rainbow fringe the way a prism does. It only acts where the surface bends light.");

            public static readonly GUIContent Specular = new GUIContent(
                "Specular",
                "Highlights and reflections. Reflections come from the Reflection Probes placed in " +
                "the scene and fall back to the live sky wherever no probe reaches, so they follow " +
                "the time of day; indoors, place a probe or they fade with the baked light. " +
                "Highlights are laid in two flat tones, a sheen and a core, broken up by the brush.");

            public static readonly GUIContent Metallic = new GUIContent(
                "Metallic",
                "At 1 the surface has no diffuse colour of its own: all it shows is reflection, " +
                "tinted by Base Color. Metals read by what they reflect, so give them a Reflection " +
                "Probe nearby and some Smoothness.");

            public static readonly GUIContent Smoothness = new GUIContent(
                "Smoothness",
                "How sharp the highlight and reflections are. With a Mask Map this scales its " +
                "alpha channel.");

            public static readonly GUIContent MaskMap = new GUIContent(
                "Mask Map",
                "The HDRP layout: red is Metallic, green Occlusion, alpha Smoothness; blue is " +
                "unused. Metallic then comes from the map alone and Smoothness scales it. Occlusion " +
                "darkens ambient light and reflections even with Specular off. Import it with sRGB " +
                "off.");

            public static readonly GUIContent Wetness = new GUIContent(
                "Wetness",
                "How dark and glossy this surface gets at its wettest. Where and when it gets wet does not " +
                "change.");

            public static readonly GUIContent SnowCover = new GUIContent(
                "Snow Cover",
                "How much of this surface the snow can hide at its deepest. 1 turns it fully white; lower " +
                "values let the surface show through.");

            public static readonly GUIContent ReceiveWeather = new GUIContent(
                "Receive Weather",
                "Lets the Style Weather volume wet this surface and settle snow on it wherever it has " +
                "open sky above — never under a roof or an overhang. Wet surfaces darken and take on " +
                "a gloss, with ripples on flat ground while it rains. Snow settles in patches, reaches " +
                "the top of a slope before its foot and the middle of a ledge before its edges, and " +
                "fills everything upward-facing once it lies deep.\n\n" +
                "Anything with something above it stays dry and bare, whether that shelter is static " +
                "or built at runtime. " +
                "On transparent surfaces and glass the snow is solid.\n\n" +
                "Static: for things that never move, such as ground, walls, rocks and buildings. " +
                "Everything above, with water running down slopes and ripples on flat ground. Its " +
                "patterns are laid in world space, so they would slide across a moving object. It " +
                "shelters what lies under it.\n" +
                "Moving: for characters, vehicles and props. Wets like Static, but instead of running " +
                "water and ripples the rain leaves beaded drops, and snow lies as small flakes instead " +
                "of a blanket. Each drop or flake lands at once, then shrinks away over its Life while " +
                "new ones land. They travel with the object; on an animated character set Brush Space " +
                "to Rest Pose so they stay fixed to the limbs as well. It never shelters the ground " +
                "under it, so passing or parked movers leave the snow and rain alone.\n" +
                "None: for water, fire, screens and anything else weather should not touch.\n\n" +
                "Costs nothing while the weather is dry.");

            public static readonly GUIContent DropDensity = new GUIContent(
                "Drop Density",
                "How many rain drops sit on the surface at once when it is fully wet.");

            public static readonly GUIContent DropSize = new GUIContent(
                "Drop Size",
                "Spacing of the rain drops in metres; each drop is about half of it across.");

            public static readonly GUIContent DropLife = new GUIContent(
                "Drop Life",
                "Seconds a rain drop stays, on average, before it has shrunk away. Each drop varies " +
                "around it so they do not all vanish together.");

            public static readonly GUIContent FlakeLife = new GUIContent(
                "Flake Life",
                "Seconds a snow flake stays, on average, before it has melted away. Each flake varies " +
                "around it so they do not all vanish together.");

            public static readonly GUIContent FlakeDensity = new GUIContent(
                "Flake Density",
                "How many snow flakes sit on the surface at once when the snow has settled.");

            public static readonly GUIContent FlakeSize = new GUIContent(
                "Flake Size",
                "Spacing of the snow flakes in metres; each flake is about half of it across.");

            public static readonly GUIContent Subsurface = new GUIContent("Subsurface");

            public static readonly string[] SubsurfaceModeNames = { "Skin", "Cloth" };

            public static readonly GUIContent SubsurfaceMode = new GUIContent(
                "Material",
                "Skin lets light sink in and come back out coloured: the edge between light and " +
                "shadow warms and softens, and thin parts glow when lit from behind. Cloth catches a " +
                "soft sheen along its edges, the fuzz of fibres catching the light.");

            public static readonly GUIContent SubsurfaceColor = new GUIContent(
                "Subsurface Color",
                "Colour the light takes on inside the skin — the warm red of blood under it.");

            public static readonly GUIContent SubsurfaceStrength = new GUIContent(
                "Strength", "How strongly that colour shows along the edge between light and shadow.");

            public static readonly GUIContent SubsurfaceWidth = new GUIContent(
                "Width", "How far the coloured edge reaches into the shadow side.");

            public static readonly GUIContent Transmission = new GUIContent(
                "Transmission",
                "Glow of thin parts — ears, nostrils, fingers — when the sun is behind them. How thin a " +
                "part is comes from the sun's shadow map, so it needs shadows on and the object casting " +
                "them. Other lights glow through every part alike.");

            public static readonly GUIContent TransmissionBend = new GUIContent(
                "Distortion",
                "How far the surface's shape bends the light passing through. 0 glows only when looking " +
                "straight at the light; higher lets the glow wrap round the form.");

            public static readonly GUIContent TransmissionFocus = new GUIContent(
                "Concentration",
                "How tight the glow is. Low spreads it over the whole backlit side, high keeps it to a " +
                "small spot around the light.");

            public static readonly GUIContent ClothTransmissionColor = new GUIContent(
                "Transmission Color",
                "Colour of the light that passes through the cloth, multiplied by the light's own.");

            public static readonly GUIContent ClothTransmission = new GUIContent(
                "Transmission",
                "How much light passes through the cloth when a light is behind it, like a lamp inside a " +
                "tent. Anything between the light and the cloth casts its shadow on it. 0 turns it off.");

            public static readonly GUIContent ThicknessMapToggle = new GUIContent(
                "Use Translucency Map",
                "Takes the thinness from a painted map instead of measuring it, for every light.");

            public static readonly GUIContent ThicknessMap = new GUIContent(
                "Translucency Map",
                "Red channel: white where the skin is thin and lets light through, black where it is " +
                "thick.");

            public static readonly GUIContent SheenColor = new GUIContent(
                "Sheen Color", "Colour of the soft highlight along the cloth's edges.");

            public static readonly GUIContent SheenStrength = new GUIContent("Strength");

            public static readonly GUIContent SheenPower = new GUIContent(
                "Width", "Higher keeps the sheen to a thin line at the very edge; lower spreads it.");

            public static readonly GUIContent Weave = new GUIContent(
                "Weave",
                "A fine woven pattern in the light, laid out along the mesh's UVs, or in the triplanar " +
                "space when Triplanar Mapping is on. Fades out where the threads would be smaller than a pixel.");

            public static readonly GUIContent WeaveScale = new GUIContent(
                "Threads", "How many threads fit across one UV unit, or across one tile with Triplanar Mapping.");

            public static readonly string[] SurfaceTypeNames = { "Opaque", "Transparent" };
            public static readonly string[] BlendModeNames = { "Alpha", "Premultiply", "Additive" };

            public const string AlbedoOnlyHint =
                "This material samples no textures. Gradation comes from the ambient gradient, " +
                "so a flat colour still reads in three tones.";
        }

        const string k_AlphaTestKeyword = "_ALPHATEST_ON";
        const string k_ReceiveShadowsOffKeyword = "_RECEIVE_SHADOWS_OFF";
        const string k_BrushAnchorKeyword = "_HB_BRUSH_ANCHOR";
        const string k_PremultiplyKeyword = "_HB_PREMULTIPLY";
        const string k_RefractionKeyword = "_HB_REFRACTION";
        const string k_AdditiveKeyword = "_HB_ADDITIVE";

        static readonly (string property, string keyword)[] s_ToggleKeywords =
        {
            ("_BrushEnabled", "_HB_BRUSH"),
            ("_BrushMaskEnabled", "_HB_BRUSH_MASK"),
            ("_RimEnabled", "_HB_RIM"),
            ("_SpecularEnabled", "_HB_SPECULAR"),
            ("_BaseMapEnabled", "_HB_BASE_MAP"),
            ("_NormalMapEnabled", "_NORMALMAP"),
            ("_MaskMapEnabled", "_HB_MASK_MAP"),
            ("_TriplanarEnabled", "_HB_TRIPLANAR"),
            ("_EmissionEnabled", "_HB_EMISSION"),
            ("_SubsurfaceEnabled", "_HB_SUBSURFACE")
        };

        MaterialProperty m_SurfaceType;
        MaterialProperty m_BlendMode;
        MaterialProperty m_CastColoredShadows;
        MaterialProperty m_RefractionEnabled;
        MaterialProperty m_Refraction;
        MaterialProperty m_RefractionBlur;
        MaterialProperty m_Dispersion;
        MaterialProperty m_StrokeBend;
        MaterialProperty m_StrokeAlpha;
        MaterialProperty m_BaseColor;
        MaterialProperty m_DiffuseWrap;
        MaterialProperty m_DiffuseSoftness;
        MaterialProperty m_HalfTone;
        MaterialProperty m_BakedTones;
        MaterialProperty m_BakedSoftness;

        MaterialProperty m_RimEnabled;
        MaterialProperty m_RimColor;
        MaterialProperty m_RimPower;
        MaterialProperty m_RimIntensity;

        MaterialProperty m_SpecularEnabled;
        MaterialProperty m_Metallic;
        MaterialProperty m_Smoothness;


        MaterialProperty m_BrushEnabled;
        MaterialProperty m_BrushObjectSpace;
        MaterialProperty m_BrushScale;
        MaterialProperty m_BrushPaint;
        MaterialProperty m_BrushEdgeKeep;
        MaterialProperty m_ToneStrokes;
        MaterialProperty m_BakedStrokes;
        MaterialProperty m_BrushRelief;
        MaterialProperty m_BrushShading;
        MaterialProperty m_BrushAmbient;
        MaterialProperty m_BrushMaskEnabled;
        MaterialProperty m_BrushMask;
        MaterialProperty m_BrushAlbedo;

        MaterialProperty m_TriplanarEnabled;
        MaterialProperty m_TriplanarSpace;
        MaterialProperty m_BaseMapEnabled;
        MaterialProperty m_BaseMap;
        MaterialProperty m_NormalMapEnabled;
        MaterialProperty m_BumpMap;
        MaterialProperty m_BumpScale;
        MaterialProperty m_MaskMapEnabled;
        MaterialProperty m_MaskMap;
        MaterialProperty m_EmissionEnabled;
        MaterialProperty m_EmissionColor;
        MaterialProperty m_EmissionMap;

        MaterialProperty m_AlphaClipEnabled;
        MaterialProperty m_Cutoff;
        MaterialProperty m_ReceiveShadows;
        MaterialProperty m_ReceiveWeather;
        MaterialProperty m_Wetness;
        MaterialProperty m_SnowCover;
        MaterialProperty m_DropDensity;
        MaterialProperty m_DropSize;
        MaterialProperty m_FlakeDensity;
        MaterialProperty m_FlakeSize;
        MaterialProperty m_DropLife;
        MaterialProperty m_FlakeLife;
        MaterialProperty m_Cull;
        MaterialProperty m_QueueOffset;

        MaterialProperty m_SubsurfaceEnabled;
        MaterialProperty m_SkinMode;
        MaterialProperty m_SubsurfaceColor;
        MaterialProperty m_SubsurfaceStrength;
        MaterialProperty m_SubsurfaceWidth;
        MaterialProperty m_Transmission;
        MaterialProperty m_TransmissionBend;
        MaterialProperty m_TransmissionFocus;
        MaterialProperty m_TransmissionColor;
        MaterialProperty m_ThicknessMapEnabled;
        MaterialProperty m_ThicknessMap;
        MaterialProperty m_SheenColor;
        MaterialProperty m_SheenStrength;
        MaterialProperty m_SheenPower;
        MaterialProperty m_Weave;
        MaterialProperty m_WeaveScale;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            EditorGUI.BeginChangeCheck();

            DrawSurface(materialEditor);
            DrawMaps(materialEditor);
            DrawShading(materialEditor);
            DrawBrush(materialEditor);
            DrawSpecular(materialEditor);
            DrawRim(materialEditor);
            DrawEmission(materialEditor);
            DrawSubsurface(materialEditor);
            DrawExtraSections(materialEditor);
            DrawWeather(materialEditor);
            DrawAdvanced(materialEditor);

            CoreEditorUtils.DrawSplitter();

            if (EditorGUI.EndChangeCheck())
                ValidateTargets(materialEditor);
        }

        void DrawTransmissionShape(MaterialEditor materialEditor)
        {
            if (m_Transmission.floatValue <= 0f)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                materialEditor.ShaderProperty(m_TransmissionBend, Styles.TransmissionBend);
                materialEditor.ShaderProperty(m_TransmissionFocus, Styles.TransmissionFocus);
            }
        }

        void DrawSubsurface(MaterialEditor materialEditor)
        {
            if (m_SubsurfaceEnabled == null)
                return;

            var reset = ResetAction(materialEditor, m_SkinMode, m_SubsurfaceColor, m_SubsurfaceStrength,
                                    m_SubsurfaceWidth, m_Transmission, m_TransmissionBend, m_TransmissionFocus, m_TransmissionColor,
                                    m_SheenColor, m_SheenStrength, m_SheenPower,
                                    m_Weave, m_WeaveScale);

            var expanded = StyleMaterialHeader.Draw(Styles.Subsurface, "Subsurface", materialEditor,
                                                    m_SubsurfaceEnabled, reset);

            if (expanded)
            {
                DrawPopup(materialEditor, m_SkinMode, Styles.SubsurfaceMode, Styles.SubsurfaceModeNames);

                if (IsEnabled(m_SkinMode))
                {
                    materialEditor.ShaderProperty(m_SheenColor, Styles.SheenColor);
                    materialEditor.ShaderProperty(m_SheenStrength, Styles.SheenStrength);
                    materialEditor.ShaderProperty(m_SheenPower, Styles.SheenPower);
                    materialEditor.ShaderProperty(m_Weave, Styles.Weave);

                    if (m_Weave.floatValue > 0f)
                    {
                        using (new EditorGUI.IndentLevelScope())
                            materialEditor.ShaderProperty(m_WeaveScale, Styles.WeaveScale);
                    }

                    materialEditor.ShaderProperty(m_TransmissionColor, Styles.ClothTransmissionColor);
                    materialEditor.ShaderProperty(m_Transmission, Styles.ClothTransmission);
                    DrawTransmissionShape(materialEditor);
                }
                else
                {
                    materialEditor.ShaderProperty(m_SubsurfaceColor, Styles.SubsurfaceColor);
                    materialEditor.ShaderProperty(m_SubsurfaceStrength, Styles.SubsurfaceStrength);
                    materialEditor.ShaderProperty(m_SubsurfaceWidth, Styles.SubsurfaceWidth);
                    materialEditor.ShaderProperty(m_Transmission, Styles.Transmission);
                    DrawTransmissionShape(materialEditor);
                    materialEditor.ShaderProperty(m_ThicknessMapEnabled, Styles.ThicknessMapToggle);

                    if (IsEnabled(m_ThicknessMapEnabled))
                    {
                        using (new EditorGUI.IndentLevelScope())
                            materialEditor.TexturePropertySingleLine(Styles.ThicknessMap, m_ThicknessMap);
                    }
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        protected virtual void DrawExtraSections(MaterialEditor materialEditor)
        {
        }

        protected virtual void FindExtraProperties(MaterialProperty[] properties)
        {
        }

        protected virtual void ValidateExtra(Material material)
        {
        }

        public override void ValidateMaterial(Material material)
        {
            Validate(material);
            ValidateExtra(material);
        }

        static void MigrateTriplanarSpace(Material material)
        {
            if (!material.HasProperty("_TriplanarSpace") || !material.HasProperty("_BrushObjectSpace"))
                return;

            var floats = new SerializedObject(material).FindProperty("m_SavedProperties.m_Floats");

            for (var index = 0; floats != null && index < floats.arraySize; index++)
            {
                var saved = floats.GetArrayElementAtIndex(index).FindPropertyRelative("first");

                if (saved != null && saved.stringValue == "_TriplanarSpace")
                    return;
            }

            material.SetFloat("_TriplanarSpace", material.GetFloat("_BrushObjectSpace"));
        }

        public static void Validate(Material material)
        {
            var alphaClip = material.HasProperty("_AlphaClipEnabled") &&
                            material.GetFloat("_AlphaClipEnabled") >= 0.5f;
            CoreUtils.SetKeyword(material, k_AlphaTestKeyword, alphaClip);

            var receiveShadows = !material.HasProperty("_ReceiveShadows") ||
                                 material.GetFloat("_ReceiveShadows") >= 0.5f;
            CoreUtils.SetKeyword(material, k_ReceiveShadowsOffKeyword, !receiveShadows);

            MigrateTriplanarSpace(material);

            var triplanarRest = material.HasProperty("_TriplanarSpace") && material.HasProperty("_TriplanarEnabled") &&
                                material.GetFloat("_TriplanarEnabled") >= 0.5f &&
                                material.GetFloat("_TriplanarSpace") >= 1.5f;
            var brushRest = material.HasProperty("_BrushObjectSpace") &&
                            material.GetFloat("_BrushObjectSpace") >= 1.5f;
            var restPose = triplanarRest || brushRest;
            CoreUtils.SetKeyword(material, k_BrushAnchorKeyword, restPose);

            foreach (var (property, keyword) in s_ToggleKeywords)
            {
                if (material.HasProperty(property))
                    CoreUtils.SetKeyword(material, keyword, material.GetFloat(property) >= 0.5f);
            }

            var subsurface = GetToggle(material, "_SubsurfaceEnabled");
            var cloth = subsurface && GetToggle(material, "_SkinMode");

            CoreUtils.SetKeyword(material, "_HB_CLOTH", cloth);
            CoreUtils.SetKeyword(material, "_HB_THICKNESS_MAP",
                                 subsurface && !cloth && GetToggle(material, "_ThicknessMapEnabled"));

            if (material.HasProperty("_EmissionEnabled"))
            {
                var flags = material.globalIlluminationFlags;

                flags = material.GetFloat("_EmissionEnabled") >= 0.5f && material.HasProperty("_EmissionColor")
                    ? MaterialEditor.FixupEmissiveFlag(material.GetColor("_EmissionColor"), flags)
                    : flags | MaterialGlobalIlluminationFlags.EmissiveIsBlack;

                if (material.globalIlluminationFlags != flags)
                    material.globalIlluminationFlags = flags;
            }

            var transparent = material.HasProperty("_Surface") && material.GetFloat("_Surface") >= 0.5f;
            var blend = transparent && material.HasProperty("_Blend")
                ? Mathf.RoundToInt(material.GetFloat("_Blend"))
                : 0;
            var additive = blend == 2;
            CoreUtils.SetKeyword(material, k_PremultiplyKeyword, blend == 1);
            CoreUtils.SetKeyword(material, k_AdditiveKeyword, additive);

            var refraction = transparent && !additive && material.HasProperty("_RefractionEnabled") &&
                             material.GetFloat("_RefractionEnabled") >= 0.5f;
            CoreUtils.SetKeyword(material, k_RefractionKeyword, refraction);

            material.SetShaderPassEnabled("UniversalForward", !transparent);
            material.SetShaderPassEnabled("DepthOnly", !transparent);
            material.SetShaderPassEnabled("DepthNormals", !transparent);
            var coloredShadows = transparent && !additive && material.HasProperty("_CastColoredShadows") &&
                                 material.GetFloat("_CastColoredShadows") >= 0.5f;
            material.SetShaderPassEnabled("ShadowCaster", !coloredShadows && !additive);
            material.SetShaderPassEnabled("HiddenBullShadowTint", coloredShadows);
            material.SetShaderPassEnabled("HiddenBullOITMoments", transparent && !additive);
            material.SetShaderPassEnabled("HiddenBullOITColor", transparent);
            material.SetShaderPassEnabled("HiddenBullOITRefraction", refraction);

            material.SetOverrideTag("RenderType",
                transparent ? "Transparent" : alphaClip ? "TransparentCutout" : "Opaque");

            var queueOffset = material.HasProperty("_QueueOffset") ? (int)material.GetFloat("_QueueOffset") : 0;
            var baseQueue = transparent ? (int)RenderQueue.Transparent
                          : alphaClip ? (int)RenderQueue.AlphaTest
                          : (int)RenderQueue.Geometry;
            material.renderQueue = baseQueue + queueOffset;
        }

        void FindProperties(MaterialProperty[] properties)
        {
            m_SurfaceType = Optional("_Surface", properties);
            m_BlendMode = Optional("_Blend", properties);
            m_CastColoredShadows = Optional("_CastColoredShadows", properties);
            m_RefractionEnabled = Optional("_RefractionEnabled", properties);
            m_Refraction = Optional("_Refraction", properties);
            m_RefractionBlur = Optional("_RefractionBlur", properties);
            m_Dispersion = Optional("_Dispersion", properties);
            m_StrokeBend = Optional("_StrokeBend", properties);
            m_StrokeAlpha = FindProperty("_StrokeAlpha", properties);
            m_BaseColor = FindProperty("_BaseColor", properties);
            m_DiffuseWrap = FindProperty("_DiffuseWrap", properties);
            m_DiffuseSoftness = FindProperty("_DiffuseSoftness", properties);
            m_HalfTone = FindProperty("_HalfTone", properties);
            m_BakedTones = FindProperty("_BakedTones", properties);
            m_BakedSoftness = FindProperty("_BakedSoftness", properties);

            m_RimEnabled = Optional("_RimEnabled", properties);
            m_RimColor = Optional("_RimColor", properties);
            m_RimPower = Optional("_RimPower", properties);
            m_RimIntensity = Optional("_RimIntensity", properties);

            m_SpecularEnabled = Optional("_SpecularEnabled", properties);
            m_Metallic = Optional("_Metallic", properties);
            m_Smoothness = Optional("_Smoothness", properties);


            m_BrushEnabled = FindProperty("_BrushEnabled", properties);
            m_BrushObjectSpace = FindProperty("_BrushObjectSpace", properties);
            m_BrushScale = FindProperty("_BrushScale", properties);
            m_BrushPaint = FindProperty("_BrushPaint", properties);
            m_BrushEdgeKeep = FindProperty("_BrushEdgeKeep", properties);
            m_ToneStrokes = FindProperty("_ToneStrokes", properties);
            m_BakedStrokes = FindProperty("_BakedStrokes", properties);
            m_BrushRelief = FindProperty("_BrushRelief", properties);
            m_BrushShading = FindProperty("_BrushShading", properties);
            m_BrushAmbient = FindProperty("_BrushAmbient", properties);
            m_BrushMaskEnabled = FindProperty("_BrushMaskEnabled", properties);
            m_BrushMask = FindProperty("_BrushMask", properties);
            m_BrushAlbedo = FindProperty("_BrushAlbedo", properties);

            m_TriplanarEnabled = FindProperty("_TriplanarEnabled", properties);
            m_TriplanarSpace = FindProperty("_TriplanarSpace", properties, false);
            m_BaseMapEnabled = FindProperty("_BaseMapEnabled", properties);
            m_BaseMap = FindProperty("_BaseMap", properties);
            m_NormalMapEnabled = FindProperty("_NormalMapEnabled", properties);
            m_BumpMap = FindProperty("_BumpMap", properties);
            m_BumpScale = FindProperty("_BumpScale", properties);
            m_MaskMapEnabled = FindProperty("_MaskMapEnabled", properties);
            m_MaskMap = FindProperty("_MaskMap", properties);
            m_EmissionEnabled = Optional("_EmissionEnabled", properties);
            m_EmissionColor = Optional("_EmissionColor", properties);
            m_EmissionMap = Optional("_EmissionMap", properties);

            m_AlphaClipEnabled = FindProperty("_AlphaClipEnabled", properties);
            m_Cutoff = FindProperty("_Cutoff", properties);
            m_ReceiveShadows = FindProperty("_ReceiveShadows", properties);
            m_ReceiveWeather = FindProperty("_ReceiveWeather", properties);
            m_Wetness = FindProperty("_Wetness", properties);
            m_SnowCover = FindProperty("_SnowCover", properties);
            m_DropDensity = FindProperty("_DropDensity", properties);
            m_DropSize = FindProperty("_DropSize", properties);
            m_FlakeDensity = FindProperty("_FlakeDensity", properties);
            m_FlakeSize = FindProperty("_FlakeSize", properties);
            m_DropLife = FindProperty("_DropLife", properties);
            m_FlakeLife = FindProperty("_FlakeLife", properties);
            m_Cull = FindProperty("_Cull", properties);
            m_QueueOffset = FindProperty("_QueueOffset", properties);

            m_SubsurfaceEnabled = Optional("_SubsurfaceEnabled", properties);
            m_SkinMode = Optional("_SkinMode", properties);
            m_SubsurfaceColor = Optional("_SubsurfaceColor", properties);
            m_SubsurfaceStrength = Optional("_SubsurfaceStrength", properties);
            m_SubsurfaceWidth = Optional("_SubsurfaceWidth", properties);
            m_Transmission = Optional("_Transmission", properties);
            m_TransmissionBend = Optional("_TransmissionBend", properties);
            m_TransmissionFocus = Optional("_TransmissionFocus", properties);
            m_TransmissionColor = Optional("_TransmissionColor", properties);
            m_ThicknessMapEnabled = Optional("_ThicknessMapEnabled", properties);
            m_ThicknessMap = Optional("_ThicknessMap", properties);
            m_SheenColor = Optional("_SheenColor", properties);
            m_SheenStrength = Optional("_SheenStrength", properties);
            m_SheenPower = Optional("_SheenPower", properties);
            m_Weave = Optional("_Weave", properties);
            m_WeaveScale = Optional("_WeaveScale", properties);

            FindExtraProperties(properties);
        }

        void DrawSurface(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_SurfaceType, m_BlendMode, m_CastColoredShadows,
                                    m_RefractionEnabled, m_Refraction, m_RefractionBlur, m_Dispersion,
                                    m_StrokeBend, m_BaseColor);

            var expanded = StyleMaterialHeader.Draw(Styles.Surface, "Surface", materialEditor, reset: reset);

            if (expanded)
            {
                if (m_SurfaceType != null)
                    DrawPopup(materialEditor, m_SurfaceType, Styles.SurfaceType, Styles.SurfaceTypeNames);

                if (IsEnabled(m_SurfaceType))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        DrawPopup(materialEditor, m_BlendMode, Styles.BlendMode, Styles.BlendModeNames);

                        var additive = m_BlendMode != null && !m_BlendMode.hasMixedValue &&
                                       Mathf.RoundToInt(m_BlendMode.floatValue) == 2;

                        if (!additive)
                        {
                            materialEditor.ShaderProperty(m_CastColoredShadows, Styles.CastColoredShadows);
                            materialEditor.ShaderProperty(m_RefractionEnabled, Styles.Refraction);
                        }

                        if (!additive && IsEnabled(m_RefractionEnabled))
                        {
                            using (new EditorGUI.IndentLevelScope())
                            {
                                materialEditor.ShaderProperty(m_Refraction, Styles.RefractionStrength);
                                materialEditor.ShaderProperty(m_RefractionBlur, Styles.RefractionBlur);
                                materialEditor.ShaderProperty(m_Dispersion, Styles.Dispersion);

                                if (IsEnabled(m_BrushEnabled))
                                    materialEditor.ShaderProperty(m_StrokeBend, Styles.StrokeBend);
                            }
                        }
                    }
                }

                materialEditor.ShaderProperty(m_BaseColor, m_BaseColor.displayName);

                if (!IsEnabled(m_BaseMapEnabled) && !IsEnabled(m_NormalMapEnabled) && !IsEnabled(m_MaskMapEnabled))
                    EditorGUILayout.HelpBox(Styles.AlbedoOnlyHint, MessageType.None);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawMaps(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_TriplanarEnabled, m_BaseMapEnabled, m_NormalMapEnabled,
                                    m_BumpScale, m_MaskMapEnabled);

            var expanded = StyleMaterialHeader.Draw(Styles.Textures, "Maps", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_TriplanarEnabled, Styles.Triplanar);

                if (IsEnabled(m_TriplanarEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (m_TriplanarSpace != null)
                            materialEditor.ShaderProperty(m_TriplanarSpace, Styles.TriplanarSpace);

                        if (!IsEnabled(m_BaseMapEnabled))
                            materialEditor.TextureScaleOffsetProperty(m_BaseMap);

                        DrawRestPoseWarning(materialEditor);
                    }
                }

                materialEditor.ShaderProperty(m_BaseMapEnabled, m_BaseMapEnabled.displayName);

                if (IsEnabled(m_BaseMapEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.TexturePropertySingleLine(new GUIContent(m_BaseMap.displayName), m_BaseMap);
                        materialEditor.TextureScaleOffsetProperty(m_BaseMap);
                    }
                }

                materialEditor.ShaderProperty(m_NormalMapEnabled, m_NormalMapEnabled.displayName);

                if (IsEnabled(m_NormalMapEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        materialEditor.TexturePropertySingleLine(
                            new GUIContent(m_BumpMap.displayName), m_BumpMap, m_BumpScale);
                    }
                }

                materialEditor.ShaderProperty(m_MaskMapEnabled, m_MaskMapEnabled.displayName);

                if (IsEnabled(m_MaskMapEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.TexturePropertySingleLine(Styles.MaskMap, m_MaskMap);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawShading(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_DiffuseWrap, m_DiffuseSoftness, m_HalfTone, m_BakedTones,
                                    m_BakedSoftness);

            var expanded = StyleMaterialHeader.Draw(Styles.Shading, "Shading", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_DiffuseWrap, Styles.DiffuseWrap);
                materialEditor.ShaderProperty(m_DiffuseSoftness, Styles.DiffuseSoftness);
                materialEditor.ShaderProperty(m_HalfTone, Styles.HalfTone);
                materialEditor.ShaderProperty(m_BakedTones, Styles.BakedTones);

                if (m_BakedTones.floatValue > 0f)
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_BakedSoftness, Styles.BakedSoftness);
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawBrush(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_BrushObjectSpace, m_BrushScale, m_BrushPaint,
                                    m_BrushEdgeKeep, m_ToneStrokes, m_BakedStrokes, m_BrushRelief,
                                    m_BrushShading, m_BrushAmbient, m_BrushAlbedo, m_StrokeAlpha,
                                    m_BrushMaskEnabled);

            var expanded = StyleMaterialHeader.Draw(Styles.Brush, "Brush", materialEditor, m_BrushEnabled, reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_BrushObjectSpace, Styles.BrushSpace);
                DrawRestPoseWarning(materialEditor);

                materialEditor.ShaderProperty(m_BrushScale, Styles.BrushSize);
                materialEditor.ShaderProperty(m_BrushPaint, Styles.BrushPaint);

                if (m_BrushPaint.floatValue > 0f)
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_BrushEdgeKeep, Styles.BrushEdgeKeep);
                }

                materialEditor.ShaderProperty(m_ToneStrokes, Styles.ToneStrokes);

                if (m_ToneStrokes.floatValue > 0f)
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_BakedStrokes, Styles.BakedStrokes);
                }

                materialEditor.ShaderProperty(m_BrushRelief, Styles.BrushRelief);
                materialEditor.ShaderProperty(m_BrushShading, Styles.BrushShading);
                materialEditor.ShaderProperty(m_BrushAmbient, Styles.BrushAmbient);
                materialEditor.ShaderProperty(m_BrushAlbedo, Styles.BrushAlbedo);

                if (IsEnabled(m_SurfaceType))
                    materialEditor.ShaderProperty(m_StrokeAlpha, Styles.StrokeAlpha);

                materialEditor.ShaderProperty(m_BrushMaskEnabled, m_BrushMaskEnabled.displayName);

                if (IsEnabled(m_BrushMaskEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.TexturePropertySingleLine(Styles.BrushMask, m_BrushMask);
                }

                EditorGUILayout.HelpBox(Styles.BrushAtlasHint, MessageType.None);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawSpecular(MaterialEditor materialEditor)
        {
            if (m_SpecularEnabled == null)
                return;

            var reset = ResetAction(materialEditor, m_Metallic, m_Smoothness);

            var expanded = StyleMaterialHeader.Draw(Styles.Specular, "Specular", materialEditor, m_SpecularEnabled,
                                                    reset);

            if (expanded)
            {
                if (!IsEnabled(m_MaskMapEnabled))
                    materialEditor.ShaderProperty(m_Metallic, Styles.Metallic);

                materialEditor.ShaderProperty(m_Smoothness, Styles.Smoothness);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawRim(MaterialEditor materialEditor)
        {
            if (m_RimEnabled == null)
                return;

            var reset = ResetAction(materialEditor, m_RimColor, m_RimPower, m_RimIntensity);

            var expanded = StyleMaterialHeader.Draw(Styles.Rim, "Rim", materialEditor, m_RimEnabled, reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_RimColor, m_RimColor.displayName);
                materialEditor.ShaderProperty(m_RimPower, m_RimPower.displayName);
                materialEditor.ShaderProperty(m_RimIntensity, m_RimIntensity.displayName);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawEmission(MaterialEditor materialEditor)
        {
            if (m_EmissionEnabled == null)
                return;

            var reset = ResetAction(materialEditor, m_EmissionColor);

            var expanded = StyleMaterialHeader.Draw(Styles.Emission, "Emission", materialEditor, m_EmissionEnabled,
                                                    reset);

            if (expanded)
            {
                materialEditor.TexturePropertyWithHDRColor(Styles.EmissionMap, m_EmissionMap, m_EmissionColor, false);
                materialEditor.LightmapEmissionFlagsProperty(0, true, true);
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawWeather(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_ReceiveWeather, m_Wetness, m_SnowCover, m_DropDensity, m_DropSize,
                                    m_DropLife, m_FlakeDensity, m_FlakeSize, m_FlakeLife);

            var expanded = StyleMaterialHeader.Draw(Styles.Weather, "Weather", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_ReceiveWeather, Styles.ReceiveWeather);

                var off = !m_ReceiveWeather.hasMixedValue && m_ReceiveWeather.floatValue < 0.5f;

                using (new EditorGUI.DisabledScope(off))
                {
                    materialEditor.ShaderProperty(m_Wetness, Styles.Wetness);
                    materialEditor.ShaderProperty(m_SnowCover, Styles.SnowCover);
                }

                if (!m_ReceiveWeather.hasMixedValue && m_ReceiveWeather.floatValue > 1.5f)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField(Styles.Drops, EditorStyles.miniBoldLabel);
                        materialEditor.ShaderProperty(m_DropDensity, Styles.DropDensity);
                        materialEditor.ShaderProperty(m_DropSize, Styles.DropSize);
                        materialEditor.ShaderProperty(m_DropLife, Styles.DropLife);

                        EditorGUILayout.LabelField(Styles.Flakes, EditorStyles.miniBoldLabel);
                        materialEditor.ShaderProperty(m_FlakeDensity, Styles.FlakeDensity);
                        materialEditor.ShaderProperty(m_FlakeSize, Styles.FlakeSize);
                        materialEditor.ShaderProperty(m_FlakeLife, Styles.FlakeLife);
                    }
                }
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawAdvanced(MaterialEditor materialEditor)
        {
            var reset = ResetAction(materialEditor, m_AlphaClipEnabled, m_Cutoff, m_ReceiveShadows, m_Cull,
                                    m_QueueOffset);

            var expanded = StyleMaterialHeader.Draw(Styles.Advanced, "Advanced", materialEditor, reset: reset);

            if (expanded)
            {
                materialEditor.ShaderProperty(m_AlphaClipEnabled, m_AlphaClipEnabled.displayName);

                if (IsEnabled(m_AlphaClipEnabled))
                {
                    using (new EditorGUI.IndentLevelScope())
                        materialEditor.ShaderProperty(m_Cutoff, m_Cutoff.displayName);
                }

                materialEditor.ShaderProperty(m_ReceiveShadows, m_ReceiveShadows.displayName);
                materialEditor.ShaderProperty(m_Cull, m_Cull.displayName);
                materialEditor.ShaderProperty(m_QueueOffset, m_QueueOffset.displayName);

                materialEditor.EnableInstancingField();
                materialEditor.DoubleSidedGIField();
            }

            StyleMaterialHeader.End(expanded);
        }

        void DrawRestPoseWarning(MaterialEditor materialEditor)
        {
            if (m_BrushObjectSpace.hasMixedValue || m_BrushObjectSpace.floatValue < 1.5f ||
                materialEditor.target is not Material material)
                return;

            if (SelectionLacksRestPose(material))
                EditorGUILayout.HelpBox(Styles.RestPoseMissing, MessageType.Warning);
        }

        static bool SelectionLacksRestPose(Material material)
        {
            foreach (var selected in Selection.gameObjects)
            {
                foreach (var renderer in selected.GetComponentsInChildren<Renderer>(true))
                {
                    if (Array.IndexOf(renderer.sharedMaterials, material) < 0)
                        continue;

                    if (renderer is not SkinnedMeshRenderer skinned || skinned.sharedMesh == null ||
                        !skinned.sharedMesh.HasVertexAttribute(VertexAttribute.TexCoord3))
                        return true;
                }
            }

            return false;
        }
    }
}
