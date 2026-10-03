# Hanoi Old Quarter house sprites

Eight pixel-art house sprites for the Hanoi Tet chapter. Every frame is 64 x 64 px with a transparent background so it can be placed above the street and ground Tilemaps.

| Frame | Use |
| --- | --- |
| TubeHouse | Narrow traditional tube house |
| YellowShop | Yellow-plaster storefront with teal awning |
| CourtyardGate | Brick gate for Bac An's house entrance |
| CornerTeaHouse | Corner tea shop facade |
| NarrowAlleyHouse | Thin house for a tight Old Quarter alley |
| MarketFront | Hang Be market storefront |
| MossyElderHouse | Older house with moss and potted plants |
| TempleFacade | Decorative community or temple facade |

## Unity import

Use `HanoiOldQuarter_HouseSprites_64px.png` as a 4 x 2 sheet and slice it by grid cell size `64 x 64`, with pivot `Bottom Center`. Or use the standalone PNG files in `Frames` directly.

Set Texture Type to `Sprite (2D and UI)`, Filter Mode to `Point (no filter)`, Compression to `None`, and Pixels Per Unit to `16`. Put the sprites on a Buildings or Foreground sorting layer above ground tiles. Add a Box Collider 2D only to each building's lower facade or entrance object, not its roof artwork.
