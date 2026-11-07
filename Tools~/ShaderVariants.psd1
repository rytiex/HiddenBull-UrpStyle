@{
    Urp = @{
        Minimal = @()
        Full = @(
            '_MAIN_LIGHT_SHADOWS_CASCADE', '_SHADOWS_SOFT', '_CLUSTER_LIGHT_LOOP', '_ADDITIONAL_LIGHT_SHADOWS',
            'LIGHTMAP_ON', 'SHADOWS_SHADOWMASK', 'LIGHTMAP_SHADOW_MIXING', '_LIGHT_LAYERS', '_LIGHT_COOKIES',
            '_SCREEN_SPACE_OCCLUSION', '_DBUFFER_MRT3'
        )
        Transparent = @(
            '_MAIN_LIGHT_SHADOWS_CASCADE', '_SHADOWS_SOFT_MEDIUM', '_CLUSTER_LIGHT_LOOP', 'LIGHTMAP_ON', 'SHADOWS_SHADOWMASK',
            'LIGHTMAP_SHADOW_MIXING', '_LIGHT_LAYERS', '_LIGHT_COOKIES'
        )
    }

    Variants = @(
        @{
            Name = 'URP Lit Forward (reference)'
            IsReference = $true
            Include = 'Tools~/Shaders/UrpLitReference.hlsl'
            Entry = 'LitPassFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Core Probe'
            Include = 'Tools~/Shaders/CoreProbe.hlsl'
            Entry = 'CoreProbeFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit Forward (albedo only)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @()
            Slots = 500
            Reads = 9
        }
        @{
            Name = 'Lit Forward (brush)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BRUSH')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit Forward (all, UV)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BRUSH', '_HB_BRUSH_MASK', '_HB_SPECULAR',
                        '_HB_RIM', '_HB_SUBSURFACE', '_HB_EMISSION', '_HB_WEATHER', '_ALPHATEST_ON')
            Slots = 2550
            Reads = 60
        }
        @{
            Name = 'Lit Forward (all, triplanar)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_NORMALMAP', '_HB_MASK_MAP', '_HB_TRIPLANAR', '_HB_BRUSH', '_HB_BRUSH_ANCHOR',
                        '_HB_BRUSH_MASK', '_HB_SPECULAR', '_HB_RIM', '_HB_SUBSURFACE', '_HB_CLOTH', '_HB_EMISSION',
                        '_HB_WEATHER', '_ALPHATEST_ON')
            Slots = 2550
            Reads = 60
        }
        @{
            Name = 'Lit Forward (all, UV, moving)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BRUSH', '_HB_BRUSH_MASK', '_HB_SPECULAR',
                        '_HB_RIM', '_HB_SUBSURFACE', '_HB_EMISSION', '_HB_WEATHER_MOVING', '_ALPHATEST_ON')
            Slots = 2550
            Reads = 60
        }
        @{
            Name = 'Lit Forward (all, triplanar, moving)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_NORMALMAP', '_HB_MASK_MAP', '_HB_TRIPLANAR', '_HB_BRUSH', '_HB_BRUSH_ANCHOR',
                        '_HB_BRUSH_MASK', '_HB_SPECULAR', '_HB_RIM', '_HB_SUBSURFACE', '_HB_CLOTH', '_HB_EMISSION',
                        '_HB_WEATHER_MOVING', '_ALPHATEST_ON')
            Slots = 2550
            Reads = 60
        }
        @{
            Name = 'Lit Forward (all, debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BRUSH', '_HB_SPECULAR', 'HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit Forward (weather only)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_WEATHER')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit Forward (weather moving only)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_WEATHER_MOVING')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit Forward (weather, debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_HB_WEATHER', 'HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit Forward vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitForwardPass.hlsl'
            Entry = 'HB_LitVertex'
            Stage = 'vs'
            Urp = 'Full'
            Defines = @('_NORMALMAP', '_HB_BRUSH_ANCHOR')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit ShadowCaster (clip, triplanar)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitDepthPass.hlsl'
            Entry = 'HB_ShadowFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER', '_ALPHATEST_ON', '_HB_BASE_MAP', '_HB_TRIPLANAR', '_HB_BRUSH_ANCHOR')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit DepthNormals (normal map)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitDepthPass.hlsl'
            Entry = 'HB_DepthNormalsFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS', '_NORMALMAP', '_ALPHATEST_ON', '_HB_BASE_MAP')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Lit DepthOnly vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitDepthPass.hlsl'
            Entry = 'HB_DepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Forward (all)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON', '_HB_BRUSH', '_HB_BRUSH_MASK')
            Slots = 2500
            Reads = 60
        }
        @{
            Name = 'Foliage Forward (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantVertex'
            Stage = 'vs'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage ShadowCaster fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantShadowFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage ShadowCaster vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage DepthOnly fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage DepthOnly vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage DepthNormals fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthNormalsFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage DepthNormals vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Scene Selection fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantSceneFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_PLANT_SCENE', 'SCENESELECTIONPASS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Scene Picking fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantSceneFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_PLANT_SCENE', 'SCENEPICKINGPASS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Scene vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_PLANT_SCENE', 'SCENESELECTIONPASS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Meta'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageMetaPass.hlsl'
            Entry = 'HB_PlantMetaFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body Forward (all)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', '_HB_BRUSH', '_HB_BRUSH_MASK')
            Slots = 2500
            Reads = 60
        }
        @{
            Name = 'Foliage Body Forward (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantVertex'
            Stage = 'vs'
            Urp = 'Full'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body ShadowCaster fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantShadowFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER', 'HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body ShadowCaster vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER', 'HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body DepthOnly fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body DepthOnly vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body DepthNormals fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthNormalsFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS', 'HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body DepthNormals vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS', 'HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body Scene Selection fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantSceneFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_PLANT_SCENE', 'SCENESELECTIONPASS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body Scene Picking fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantSceneFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_PLANT_SCENE', 'SCENEPICKINGPASS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body Scene vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', 'HB_PLANT_SCENE', 'SCENESELECTIONPASS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body Meta'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageMetaPass.hlsl'
            Entry = 'HB_PlantMetaFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Sky'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Sky/SkyPass.hlsl'
            Entry = 'HB_SkyFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 1500
            Reads = 20
        }
        @{
            Name = 'Atmosphere fullscreen'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Atmosphere/AtmospherePass.hlsl'
            Entry = 'HB_AtmosphereFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 800
            Reads = 24
        }
        @{
            Name = 'Atmosphere fullscreen (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Atmosphere/AtmospherePass.hlsl'
            Entry = 'HB_AtmosphereFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Anti Aliasing fullscreen'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/AntiAliasing/AntiAliasingPass.hlsl'
            Entry = 'HB_AntiAliasingFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 800
            Reads = 40
        }
        @{
            Name = 'Transparent Tint (brush)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl'
            Entry = 'HB_TransparentTintFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_TRANSPARENT', '_HB_BASE_MAP', '_HB_BRUSH', '_HB_TINTED')
            Slots = 800
            Reads = 12
        }
        @{
            Name = 'Transparent Nearest'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl'
            Entry = 'HB_TransparentNearestFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_TRANSPARENT')
            Slots = 100
            Reads = 2
        }
        @{
            Name = 'Refraction Blur Downsample'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/RefractionBlurPass.hlsl'
            Entry = 'HB_RefractionDownsampleFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 120
            Reads = 13
        }
        @{
            Name = 'Transparent Shadow Tint (triplanar)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentShadowPass.hlsl'
            Entry = 'HB_ShadowTintFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_HB_TRIPLANAR', '_HB_BRUSH_ANCHOR')
            Slots = 200
            Reads = 4
        }
        @{
            Name = 'Transparent Shadow Tint vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentShadowPass.hlsl'
            Entry = 'HB_DepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Transparent Forward (alpha, all)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl'
            Entry = 'HB_TransparentFragment'
            Stage = 'ps'
            Urp = 'Transparent'
            Defines = @('HB_TRANSPARENT', '_HB_BASE_MAP', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BRUSH',
                        '_HB_SPECULAR', '_HB_RIM', '_HB_EMISSION')
            Slots = 2000
            Reads = 45
        }
        @{
            Name = 'Transparent Forward (tinted or refractive, brush)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Transparent/TransparentPass.hlsl'
            Entry = 'HB_TransparentFragment'
            Stage = 'ps'
            Urp = 'Transparent'
            Defines = @('HB_TRANSPARENT', '_HB_BRUSH', '_HB_SPECULAR', '_HB_TINTED')
            Slots = 2200
            Reads = 70
        }
        @{
            Name = 'Particle Forward (lit, all)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Particle/ParticlePass.hlsl'
            Entry = 'HB_ParticleFragment'
            Stage = 'ps'
            Urp = 'Transparent'
            Defines = @('_FLIPBOOKBLENDING_ON', '_HB_PARTICLE_LIT', '_HB_SOFT_PARTICLES', '_HB_CAMERA_FADE',
                        '_HB_BRUSH', '_HB_EMISSION')
            Slots = 1400
            Reads = 36
        }
        @{
            Name = 'Particle Forward (additive, flipbook)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Particle/ParticlePass.hlsl'
            Entry = 'HB_ParticleFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_FLIPBOOKBLENDING_ON', '_HB_ADDITIVE', '_HB_SOFT_PARTICLES', '_HB_CAMERA_FADE')
            Slots = 260
            Reads = 8
        }
        @{
            Name = 'Lit Meta'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Lit/LitMetaPass.hlsl'
            Entry = 'HB_MetaFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_HB_EMISSION')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Preview'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute'
            Entry = 'Preview'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Heights'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute'
            Entry = 'Heights'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Margin X'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute'
            Entry = 'DilateX'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Margin Z'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute'
            Entry = 'DilateZ'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Masks'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute'
            Entry = 'Masks'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Uncover'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/StyleShelter.compute'
            Entry = 'Uncover'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Forward (all) (GPU)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON', '_HB_BRUSH', '_HB_BRUSH_MASK', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 2500
            Reads = 60
        }
        @{
            Name = 'Foliage vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantVertex'
            Stage = 'vs'
            Urp = 'Full'
            Defines = @('_HB_BASE_MAP', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage ShadowCaster vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER', '_HB_BASE_MAP', '_ALPHATEST_ON', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage DepthOnly vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('_HB_BASE_MAP', '_ALPHATEST_ON', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage DepthNormals vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS', '_HB_BASE_MAP', '_ALPHATEST_ON', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body Forward (all) (GPU)'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', '_HB_BRUSH', '_HB_BRUSH_MASK', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 2500
            Reads = 60
        }
        @{
            Name = 'Foliage Body vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageForwardPass.hlsl'
            Entry = 'HB_PlantVertex'
            Stage = 'vs'
            Urp = 'Full'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body ShadowCaster vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER', 'HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body DepthOnly vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Foliage Body DepthNormals vertex (GPU)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/FoliageDepthPass.hlsl'
            Entry = 'HB_PlantDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS', 'HB_PLANT_BODY', '_NORMALMAP', '_HB_MASK_MAP', '_HB_BASE_MAP', '_ALPHATEST_ON', 'PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard Forward'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardForwardPass.hlsl'
            Entry = 'HB_BillboardFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 1500
            Reads = 30
        }
        @{
            Name = 'Billboard Forward (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardForwardPass.hlsl'
            Entry = 'HB_BillboardFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup', 'HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardForwardPass.hlsl'
            Entry = 'HB_BillboardVertex'
            Stage = 'vs'
            Urp = 'Full'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard ShadowCaster fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardShadowPass.hlsl'
            Entry = 'HB_BillboardShadowFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup', 'HB_SHADOW_CASTER')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard ShadowCaster vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardShadowPass.hlsl'
            Entry = 'HB_BillboardShadowVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup', 'HB_SHADOW_CASTER')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard DepthOnly fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardDepthPass.hlsl'
            Entry = 'HB_BillboardDepthFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard DepthOnly vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardDepthPass.hlsl'
            Entry = 'HB_BillboardDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard DepthNormals fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardDepthPass.hlsl'
            Entry = 'HB_BillboardDepthNormalsFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup', 'HB_DEPTH_NORMALS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Billboard DepthNormals vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/BillboardDepthPass.hlsl'
            Entry = 'HB_BillboardDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('PROCEDURAL_INSTANCING_ON', 'UNITY_INSTANCING_PROCEDURAL_FUNC=HB_PlantInstancingSetup', 'HB_DEPTH_NORMALS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Plant Capture vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl'
            Entry = 'HB_CaptureVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Plant Capture top vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl'
            Entry = 'HB_CaptureTopVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Plant Capture albedo'
            Include = 'Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl'
            Entry = 'HB_CaptureAlbedoFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Plant Capture normal'
            Include = 'Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl'
            Entry = 'HB_CaptureNormalFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Plant Capture top'
            Include = 'Packages/com.hiddenbull.urpstyle/Editor/Foliage/StylePlantCapture.hlsl'
            Entry = 'HB_CaptureTopFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Plant Cull'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/StylePlant.compute'
            Entry = 'Cull'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Plant Sky'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Foliage/StylePlant.compute'
            Entry = 'Sky'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Depth Pyramid Base'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleDepthPyramid.compute'
            Entry = 'Base'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Depth Pyramid Down'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Library/Core/StyleDepthPyramid.compute'
            Entry = 'Down'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Underside plant vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/ShelterUndersidePass.hlsl'
            Entry = 'HB_UndersidePlantVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_UNDERSIDE_PLANT')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Shelter Underside fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/ShelterUndersidePass.hlsl'
            Entry = 'HB_UndersideFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Rain vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl'
            Entry = 'HB_RainVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Rain vertex (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl'
            Entry = 'HB_RainVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Rain fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl'
            Entry = 'HB_RainFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Snow vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl'
            Entry = 'HB_SnowVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Snow fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationPass.hlsl'
            Entry = 'HB_SnowFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Far vertex (rain)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationFarPass.hlsl'
            Entry = 'HB_FarVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Far vertex (snow)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationFarPass.hlsl'
            Entry = 'HB_FarVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_FAR_SNOW')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Far rain fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationFarPass.hlsl'
            Entry = 'HB_FarRainFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Far rain fragment (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationFarPass.hlsl'
            Entry = 'HB_FarRainFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Far snow fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationFarPass.hlsl'
            Entry = 'HB_FarSnowFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_FAR_SNOW')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Wind vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationWindPass.hlsl'
            Entry = 'HB_WindVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Wind vertex (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationWindPass.hlsl'
            Entry = 'HB_WindVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Precipitation Wind fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Weather/PrecipitationWindPass.hlsl'
            Entry = 'HB_WindFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Terrain Forward'
            Reference = 'URP Lit Forward (reference)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainForwardPass.hlsl'
            Entry = 'HB_TerrainForwardFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @()
            Slots = 4500
            Reads = 175
        }
        @{
            Name = 'Terrain Forward (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainForwardPass.hlsl'
            Entry = 'HB_TerrainForwardFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Terrain vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainForwardPass.hlsl'
            Entry = 'HB_TerrainForwardVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Terrain ShadowCaster vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainDepthPass.hlsl'
            Entry = 'HB_TerrainDepthVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('HB_SHADOW_CASTER')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Terrain DepthOnly fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainDepthPass.hlsl'
            Entry = 'HB_TerrainDepthFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Terrain DepthNormals fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Terrain/TerrainDepthPass.hlsl'
            Entry = 'HB_TerrainDepthNormalsFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Forward'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('HB_GRASS_FORWARD')
            Slots = 2000
            Reads = 30
        }
        @{
            Name = 'Grass Forward (debug)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_GRASS_FORWARD', 'HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass DepthOnly fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassDepthFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass DepthNormals fragment'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassDepthNormalsFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_DEPTH_NORMALS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Place'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrass.compute'
            Entry = 'Place'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Occlude'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrass.compute'
            Entry = 'Occlude'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Cull'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrass.compute'
            Entry = 'Cull'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Trail'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/StyleGrass.compute'
            Entry = 'Trail'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Flowers Forward'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('HB_GRASS_FORWARD', '_HB_GRASS_FLOWERS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Flowers vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('_HB_GRASS_FLOWERS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Cards Forward'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassFragment'
            Stage = 'ps'
            Urp = 'Full'
            Defines = @('HB_GRASS_FORWARD', '_HB_GRASS_CARDS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Cards vertex'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassVertex'
            Stage = 'vs'
            Urp = 'Minimal'
            Defines = @('_HB_GRASS_CARDS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Grass Forward (light list)'
            Include = 'Packages/com.hiddenbull.urpstyle/Runtime/Shaders/Grass/GrassPass.hlsl'
            Entry = 'HB_GrassFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_GRASS_FORWARD', '_ADDITIONAL_LIGHTS')
            Slots = 0
            Reads = 0
        }
        @{
            Name = 'Terrain Layer Rules'
            Include = 'Packages/com.hiddenbull.urpstyle/Editor/Terrain/StyleTerrainRules.compute'
            Entry = 'Rules'
            Stage = 'cs'
            Urp = 'Minimal'
            Defines = @()
            Slots = 0
            Reads = 0
        }
    )
}
