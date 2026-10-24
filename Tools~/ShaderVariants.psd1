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
            Name = 'Core Probe (debug)'
            Include = 'Tools~/Shaders/CoreProbe.hlsl'
            Entry = 'CoreProbeFragment'
            Stage = 'ps'
            Urp = 'Minimal'
            Defines = @('HB_STYLE_DEBUG')
            Slots = 0
            Reads = 0
        }
    )
}
