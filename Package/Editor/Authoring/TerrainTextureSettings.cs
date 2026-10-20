using System;
using UnityEngine;

namespace HiddenBull.UrpStyle.Editor
{
    [Serializable]
    public class TerrainTextureSettings
    {
        public TerrainTextureKind kind = TerrainTextureKind.Rock;

        [Range(2, 32)]
        [Tooltip("How many rock plates fit across one tile.")]
        public int plateCount = 6;

        [Range(0.01f, 0.3f)]
        [Tooltip("Width of the cracks between plates.")]
        public float crackWidth = 0.07f;

        [Range(0f, 1f)]
        [Tooltip("How deep the cracks cut into the relief.")]
        public float crackDepth = 0.55f;

        [Range(0f, 1f)]
        [Tooltip("Share of plate borders that are cracked open. Low values leave whole slabs with only a few " +
                 "cracks running through them.")]
        public float crackCoverage = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How far neighbouring plates sit at different heights.")]
        public float plateStep = 0.2f;

        [Range(0f, 1f)]
        [Tooltip("Large, slow swells running under the plates.")]
        public float strata = 0.5f;

        [Range(1, 16)]
        [Tooltip("How many soil clods fit across one tile.")]
        public int clodCount = 6;

        [Range(4, 128)]
        [Tooltip("How many pebble spots fit across one tile.")]
        public int pebbleCount = 24;

        [Range(0f, 1f)]
        [Tooltip("Share of pebble spots that actually hold a pebble.")]
        public float pebbleAmount = 0.3f;

        [Range(0.1f, 1f)]
        [Tooltip("Pebble size, relative to the spacing between pebble spots.")]
        public float pebbleSize = 0.45f;

        [Range(0f, 1f)]
        [Tooltip("Small crumbs over the soil.")]
        public float crumbs = 0.15f;

        [Range(2, 48)]
        [Tooltip("How many ripples fit across one tile.")]
        public int rippleCount = 12;

        [Range(0f, 1f)]
        [Tooltip("How sharp the ripple crests are. 0 gives soft, even waves, 1 a long gentle side and a steep " +
                 "drop like wind-blown sand.")]
        public float rippleSharpness = 0.45f;

        [Range(0f, 1f)]
        [Tooltip("How much the ripples wander and bend instead of running straight.")]
        public float rippleBend = 0.5f;

        [Range(1, 8)]
        [Tooltip("How many dunes fit across one tile. They also steer the ripples' bending.")]
        public int duneCount = 4;

        [Range(0f, 1f)]
        [Tooltip("Height of the dunes under the ripples.")]
        public float duneAmount = 0.35f;

        [Range(0f, 1f)]
        [Tooltip("Dark mineral specks in the sand, coloured by the Speck Color.")]
        public float speckle = 0.15f;

        [Range(1, 16)]
        [Tooltip("How many grass and bare-soil patches fit across one tile.")]
        public int patchCount = 4;

        [Range(0f, 1f)]
        [Tooltip("How much of the ground is bare soil.")]
        public float soilAmount = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How softly grass fades into the soil patches.")]
        public float soilSoftness = 0.5f;

        [Range(8, 160)]
        [Tooltip("How many grass tufts fit across one tile.")]
        public int tuftCount = 48;

        [Range(0f, 1f)]
        [Tooltip("How much the tufts stand out of the ground.")]
        public float tuftHeight = 0.45f;

        [Range(2, 64)]
        [Tooltip("How many stones fit across one tile.")]
        public int stoneCount = 12;

        [Range(0f, 1f)]
        [Tooltip("0 gives sharp, faceted stones, 1 round, worn pebbles.")]
        public float stoneRoundness = 0.5f;

        [Range(0.01f, 0.3f)]
        [Tooltip("Width of the gaps between stones.")]
        public float gapWidth = 0.08f;

        [Range(0f, 1f)]
        [Tooltip("How much the stones differ in height.")]
        public float stoneHeightVariation = 0.4f;

        [Range(2, 16)]
        [Tooltip("How many mud mounds and hollows fit across one tile.")]
        public int mudCount = 5;

        [Range(0f, 1f)]
        [Tooltip("How much of the hollows hold standing water: flat, glossy puddles in the Puddle Color.")]
        public float puddles = 0.35f;

        [Range(0f, 1f)]
        [Tooltip("Dried cracks splitting the mud into curled plates.")]
        public float cracks;

        [Range(2, 32)]
        [Tooltip("How many dried plates fit across one tile.")]
        public int crackCells = 8;

        [Range(4, 160)]
        [Tooltip("How many leaves fit across one tile.")]
        public int leafCount = 40;

        [Range(0.2f, 1.5f)]
        [Tooltip("Leaf size, relative to the spacing between leaves.")]
        public float leafSize = 0.8f;

        [Range(0f, 1f)]
        [Tooltip("How slender the leaves are: 0 broad, 1 narrow.")]
        public float leafShape = 0.2f;

        [Tooltip("Leaf shapes lying on the ground. Each leaf takes one of the chosen shapes at random.")]
        public TerrainLeafTypes leafTypes = TerrainLeafTypes.Oval | TerrainLeafTypes.Oak | TerrainLeafTypes.Maple;

        [Range(0f, 1f)]
        [Tooltip("How strongly the midrib and side veins stand out.")]
        public float leafVeins = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("How much the leaf edges curl up as they dry.")]
        public float leafCurl = 0.3f;

        [Range(0f, 1f)]
        [Tooltip("Small twigs lying among the leaves.")]
        public float twigs = 0.3f;

        [Range(2, 24)]
        [Tooltip("How many rows of stones fit across one tile. Rounded down to an even number so the offset rows " +
                 "still tile.")]
        public int cobbleRows = 8;

        [Range(1f, 3f)]
        [Tooltip("How much longer than tall each stone is.")]
        public float cobbleAspect = 1f;

        [Range(0f, 1f)]
        [Tooltip("How uneven the stones are in size and placement. 0 gives neat paving.")]
        public float cobbleJitter = 0.5f;

        [Range(0.01f, 0.3f)]
        [Tooltip("Width of the mortar between stones, relative to a row.")]
        public float mortarWidth = 0.08f;

        [Range(0f, 1f)]
        [Tooltip("0 gives flat, square-cut stones, 1 round, worn ones.")]
        public float cobbleRoundness = 0.6f;

        [ColorUsage(false)]
        [Tooltip("Colour of the raised, lit parts.")]
        public Color topColor = new Color(0.5f, 0.48f, 0.45f, 1f);

        [ColorUsage(false)]
        [Tooltip("Colour of the low parts and hollows.")]
        public Color hollowColor = new Color(0.275f, 0.264f, 0.248f, 1f);

        [ColorUsage(false)]
        public Color secondColor = new Color(0.16f, 0.15f, 0.14f, 1f);

        [Range(0f, 1f)]
        [Tooltip("How much height picks between the Hollow and Top colours. At 0 each feature keeps its own " +
                 "random tone instead.")]
        public float heightTint = 0.4f;

        [Range(0f, 1f)]
        [Tooltip("Extra darkening inside cracks, gaps and small hollows.")]
        public float cavityDarkening = 0.3f;

        [Range(0f, 1f)]
        [Tooltip("Light and shade painted into the colour like a hand-painted texture: one side of every bump is " +
                 "lit, the other falls into shade.")]
        public float paintedLight = 0.6f;

        [Range(0f, 1f)]
        [Tooltip("Lit parts lean warm and shaded parts and hollows lean cool, the way painters shift hue rather " +
                 "than only brightness.")]
        public float warmCool = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("A light catching the top edges of stones, plates and ridges.")]
        public float edgeHighlight = 0.4f;

        [Range(0f, 1f)]
        [Tooltip("Large, soft colour patches across the tile so it does not read as one flat colour.")]
        public float toneVariation = 0.3f;

        [Range(1, 16)]
        [Tooltip("How many tone patches fit across one tile.")]
        public int tonePatches = 3;

        [Tooltip("Smoothness of the roughest and the smoothest parts. Stored in the albedo alpha and in the " +
                 "mask map.")]
        public Vector2 smoothness = new Vector2(0.12f, 0.24f);

        [Range(1, 8)]
        [Tooltip("How many noise octaves build the main shape. More levels add finer detail.")]
        public int detailLevels = 5;

        [Range(0f, 1.5f)]
        [Tooltip("Twists the whole pattern so straight cell borders and grid-like noise turn organic.")]
        public float warp = 0.35f;

        [Range(0f, 1f)]
        [Tooltip("Fine grain over the whole surface, in both relief and colour.")]
        public float grain = 0.3f;

        [Range(16, 256)]
        [Tooltip("How many grain cells fit across one tile.")]
        public int grainCount = 96;

        [Range(0f, 3f)]
        [Tooltip("How deep the normal map is. The layer's Normal Scale scales it further.")]
        public float normalStrength = 1f;

        [Range(0f, 1f)]
        [Tooltip("Turns the texture into painted tones: colours gather into a few flat bands and fine grain " +
                 "fades, so the brush strokes on the terrain stay in charge.")]
        public float painterly = 0.6f;

        [Range(2, 8)]
        [Tooltip("How many tone bands Painterly gathers the colours into.")]
        public int toneBands = 4;

        [Range(0f, 1f)]
        [Tooltip("How much Painterly softens the normal map. 0 keeps every crack and bump sharp, 1 leaves only " +
                 "the broad shapes.")]
        public float painterlyNormal = 0.25f;

        [Tooltip("Output texture size. All maps tile seamlessly at any size.")]
        public int resolution = 1024;

        [Tooltip("The same seed always produces the same textures.")]
        public int seed = 1;

        [Tooltip("Also saves a mask map: G ambient occlusion from the hollows, B height for the terrain's " +
                 "height blend, A smoothness. Assigned to the Terrain Layer.")]
        public bool maskMap = true;

        [Range(0f, 1f)]
        [Tooltip("How dark the hollows get in the mask map's ambient occlusion.")]
        public float occlusion = 0.6f;

        [Tooltip("Also saves a Terrain Layer that uses the maps, ready to paint or to pick as an automatic " +
                 "layer's texture.")]
        public bool createLayer = true;

        [Tooltip("Size in metres that one tile of the textures covers on the terrain.")]
        public float tileSize = 8f;
    }
}
