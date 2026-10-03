# Hanoi Tet complete tilemap

This palette covers the playable Tet Hanoi vertical slice: Old Quarter street, mossy walls, sidewalk, house gate, Hang Be market, Bac An courtyard, floors, props and collision-ready blockers.

## Tile groups

| File prefix | Scene use | Collider when generated |
| --- | --- | --- |
| `CourtyardBrick`, `MossBrick` | Bac An courtyard floor | No |
| `SidewalkStone`, `OldQuarterStreet` | Sidewalk and Old Quarter road | No |
| `GrassGarden`, `Water` | Garden and pond ground | No |
| `MossWall`, `Roof`, `Facade` | Old Quarter buildings | Wall only |
| `Gate`, `Fence` | House entrance and garden boundary | Yes |
| `MarketStall`, `MarketGoods` | Hang Be market | Stall only |
| `PeachGarden`, `CourtyardProps` | Peach tree, table and courtyard details | Props only |
| `Blocker` | Trees and rocks | Yes |

All 256 PNG tiles are 16 x 16 px and fully opaque. There are no empty black cells or transparent holes in the palette.

## Unity import and automatic colliders

1. Select `Tiles`, set Texture Type to `Sprite (2D and UI)`, Filter Mode to `Point (no filter)`, Compression to `None`, and Pixels Per Unit to `16`.
2. Let Unity compile the editor script, then choose `Tools > A Year in Warmth > Build Hanoi Tet Tile Palette`.
3. Drag the generated assets from `GeneratedTiles` to Tile Palette. Add `Tilemap Collider 2D` and `Composite Collider 2D` to the Building and Props Tilemaps.

The builder assigns Grid colliders to walls, gates, fences, market stalls, courtyard props, trees and rocks. Ground, street, sidewalk, grass and water do not receive collision.

## Preview

`HanoiTet_Complete_MapPreview.png` shows Hang Be market, an Old Quarter facade, street and sidewalk, then the gate and courtyard of Bac An's house. It is a composition reference; build it in separate Ground, Buildings and Props Tilemaps for correct draw order.
