# Hanoi environment and architecture

Environment assets for the Hanoi Tet chapter, built on a 16 x 16 px tile grid with separate 32 x 32 px architecture sprites.

## Environment tile sheet

`HanoiEnvironment_16px_Tileset.png` is a 16-column by 6-row sheet.

| Row | Tiles | Use |
| --- | --- | --- |
| 0 | RedBrick 00 to 15 | Fired-red courtyard brick variations |
| 1 | GraySidewalk 00 to 15 | Gray stone sidewalk and Old Quarter alley paving |
| 2 | GrassTransition 00 to 15 | Grass-to-brick transition masks |
| 3 | PuddleTransition 00 to 15 | Spring-rain puddle-to-stone transition masks |
| 4 | YellowMossWall 00 to 15 | Cracked yellow plaster with moss variations |
| 5 | RedRoofEdge 00 to 15 | Red roof tiles, edges and side-border variations |

The same 96 cells are available as standalone PNGs in `Tiles`.

## Architecture sprites

`HanoiArchitecture_32px_Spritesheet.png` is a 4-column by 2-row sheet.

| Row | Column 0 | Column 1 | Column 2 | Column 3 |
| --- | --- | --- | --- | --- |
| 0 | Brown wooden door | Blue wooden door | Brown barred window | Blue barred window |
| 1 | Open brick arch gate | Blue double arch gate | Projecting roof eave | Cracked moss wall panel |

The eight frames are also available individually in `ArchitectureFrames`.

## Unity import

- Environment sheet: Sprite Mode `Multiple`, slice by cell size `16 x 16`, Pixels Per Unit `16`.
- Architecture sheet: Sprite Mode `Multiple`, slice by cell size `32 x 32`, pivot `Bottom Center`, Pixels Per Unit `16`.
- For all assets use Filter Mode `Point (no filter)` and Compression `None`.
- Add Tilemap Collider 2D to the wall layer. Doors, windows and roof artwork belong on a decoration layer; add colliders to closed doors and gates only.

`HanoiCourtyard_Background_320x180.png` is a native-resolution composition made from this asset set and can be used as a visual reference or static background.
