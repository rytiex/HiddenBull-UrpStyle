@{
    Urp = @{
        Minimal = @()
        Full = @(
            '_MAIN_LIGHT_SHADOWS_CASCADE', '_SHADOWS_SOFT', '_CLUSTER_LIGHT_LOOP', '_ADDITIONAL_LIGHT_SHADOWS',
            'LIGHTMAP_ON', 'SHADOWS_SHADOWMASK', 'LIGHTMAP_SHADOW_MIXING', '_LIGHT_LAYERS', '_LIGHT_COOKIES',
            '_SCREEN_SPACE_OCCLUSION', '_DBUFFER_MRT3'
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
