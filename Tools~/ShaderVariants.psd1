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
                        '_HB_RIM', '_HB_SUBSURFACE', '_HB_EMISSION', '_ALPHATEST_ON')
            Slots = 2500
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
                        '_ALPHATEST_ON')
            Slots = 2500
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
    )
}
