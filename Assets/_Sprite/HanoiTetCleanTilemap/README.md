# Hanoi Tet clean tilemap

This is a new, self-contained pixel tile set for the Hanoi Tet chapter. Every tile is a fully opaque 16 x 16 pixel image, so it cannot expose black or transparent gaps in a Unity Tilemap.

## Contents

| Asset | Purpose |
| --- | --- |
| `HanoiTet_Clean_16px_Tileset.png` | 16 by 16 sheet with 256 tiles |
| `Tiles/` | The same 256 tiles as standalone PNGs; drag them directly into Unity Tile Palette |
| `HanoiTet_Clean_16px_Preview.png` | Enlarged contact sheet for selecting materials |

## Palette rows

| Rows | Material |
| --- | --- |
| 0 to 1 | Red brick walls and courtyard brick |
| 2 to 3 | Grass and moss ground |
| 4 to 5 | Packed earth paths |
| 6 to 7 | Stone paving |
| 8 to 9 | Water surface |
| 10 to 11 | Red tile roofs |
| 12 to 13 | Aged plaster walls |
| 14 | Wooden fence and timber details |
| 15 | Dense foliage ground cover |

## Unity import

Select all PNGs in `Tiles` and set `Texture Type` to `Sprite (2D and UI)`, `Filter Mode` to `Point (no filter)`, `Compression` to `None`, and `Pixels Per Unit` to `16`. Then drag the folder into a new Tile Palette.
