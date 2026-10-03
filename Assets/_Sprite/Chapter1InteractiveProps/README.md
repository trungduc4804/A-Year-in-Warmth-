# Chapter 1 interactive props

Interactive pixel props for the Hanoi Tet chapter. All sprites use transparent backgrounds and match a 16 pixels-per-unit world grid.

## Assets

| Asset | Size and frames | Suggested use |
| --- | --- | --- |
| `PeachBlossomPot_32x48_3frames.png` | 3 horizontal frames, each 32 x 48 | PhotoTarget for the peach-blossom quest |
| `BanhChungLowTable_64x48.png` | One 64 x 48 sprite | BanhChungTable minigame trigger |
| `WoodenStool_00/01.png` | Two standalone 20 x 20 sprites | Decorative stools around the wrapping table |
| `WoodStoveFire_32px_3frames.png` | 3 horizontal frames, each 32 x 32 | Looping fire animation |
| `BoilingPotSteam_48px_3frames.png` | 3 horizontal frames, each 48 x 48 | Looping steam animation for the cooking pot |
| `FirewoodPile_32x24.png` | One 32 x 24 sprite | Blocking prop beside the stove |

The `Frames` folder contains every animation frame and static object as a separate PNG.

## Unity import

- Set Texture Type to `Sprite (2D and UI)`, Filter Mode to `Point (no filter)`, Compression to `None`, and Pixels Per Unit to `16`.
- Slice the peach sheet using cell size `32 x 48`.
- Slice the fire sheet using cell size `32 x 32`.
- Slice the pot sheet using cell size `48 x 48`.
- Use pivot `Bottom Center` for every world prop.

## Animation settings

| Clip | Sample rate | Loop |
| --- | --- | --- |
| Peach sway | 3 fps | Yes |
| Stove fire | 7 fps | Yes |
| Pot steam | 4 fps | Yes |

Use a Box Collider 2D around the lower ceramic pot of the peach tree, the table footprint, the stove, the lower half of the cooking pot and the firewood pile. Leave branches, smoke and flames outside the collider.

Attach `PhotoTarget` to the peach-blossom pot and set its target id to `PeachBlossom`. Attach `BanhChungTable` to the low wrapping table object.
