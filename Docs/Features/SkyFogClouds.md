# Sky, Fog and Clouds

## Time of day

**HB Style Sun** on the directional light owns the time of day. **Time Of Day** and **Heading** place the
sun; day, dusk and night settings in the volumes below blend from the sun's elevation.

## Style Sky

Day, dusk and night sky gradients and ambient colors, sky and ambient intensity, a painterly brush over
the sky, sunset focus, sky light and moon light, sun and moon disc colors, and the elevations where
dusk and night begin. Ambient light comes from these gradients, blended with baked light and probes.

## Style Celestial

Sun intensity, size and glow (with separate dusk values), sun brush and rays, moon intensity, size and
glow, stars and star twinkle.

## Style Fog

A volumetric height fog (froxel grid with temporal reprojection) with start and end distance, height
falloff, base height, maximum opacity, sun scattering, a painted look, the silhouette option that keeps
shapes readable through fog, and day / dusk / night fog colors. Fog is lit by the realtime sun shadow
only. Opaque surfaces are fogged by a composite pass; transparent ones fog themselves.

## Style Clouds

Coverage, tint and shade tint, scale, softness, stepped painterly shading (steps, spacing, shading,
taper) and translucency. Make cloud atlases in Texture Studio → Cloud.
