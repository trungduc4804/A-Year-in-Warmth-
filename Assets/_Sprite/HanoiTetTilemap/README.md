# Hanoi Tet tile palette

This folder packages 564 Hanoi pixel tiles into a Unity-ready palette for the Tet chapter. The solid sheet background is removed where present, while the pixel-art outlines and material colors remain intact.

## Contents

| Folder or file | Use |
| --- | --- |
| `Tiles/HanoiTet_OldQuarter_*.png` | Mossy walls, red brick, tiled roofs, gates, paving, shrubs, ponds and path edges |
| `Tiles/HanoiTet_MarketAndGarden_*.png` | Market walls, garden edges, paving variations, grass tufts, water and decorative details |
| `Tiles/HanoiTet_CourtyardGround_*.png` | Courtyard brick ground, grass patches and walkable dirt variations |

Every image in `Tiles` is one 16 x 16 pixel tile with alpha retained. Unity imports them as standalone sprites, so no Sprite Editor slicing is required.

## Unity setup

1. Select the `HanoiTetTilemap/Tiles` folder in Project.
2. Select all imported PNGs and set `Texture Type` to `Sprite (2D and UI)`, `Filter Mode` to `Point (no filter)`, `Compression` to `None`, and `Pixels Per Unit` to `16`.
3. Open `Window > 2D > Tile Palette`, create a palette named `Hanoi Tet`, then drag the contents of the `Tiles` folder into it. Unity will create `Tile` assets in the location you choose.
4. Create separate Tilemaps for Ground, Buildings, Props and Foreground. Put collision only on the Buildings and Props Tilemaps.

## Suggested scene layers

| Tilemap | Palette families |
| --- | --- |
| Ground | CourtyardGround, grass, paving, water |
| Buildings | OldQuarter brick walls, roofs, gates |
| Market and garden | MarketAndGarden walls, garden borders and decorations |
| Foreground | Plants, wall tops and roof edges that can overlap Arthur |

The source sheets remain in `_Sprite/_Tilemap` so this palette is non-destructive.
