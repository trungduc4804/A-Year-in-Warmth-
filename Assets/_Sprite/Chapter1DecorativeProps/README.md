# Chapter 1 decorative props

Decorative pixel props for making Bac An's courtyard and the Old Quarter alleys feel warm and lived in. The standalone PNG files have transparent backgrounds.

## Master sheet layout

`Chapter1_DecorativeProps_64px_MasterSheet.png` uses a 4-column by 3-row grid with 64 x 64 px cells.

| Row | Column 0 | Column 1 | Column 2 | Column 3 |
| --- | --- | --- | --- | --- |
| 0 | Kumquat pot | Yellow chrysanthemum A | Yellow chrysanthemum B | Left red couplet |
| 1 | Right red couplet | Red lantern A | Red lantern B | Mossy well |
| 2 | Old bicycle | Clay jar | Straw broom | Green planter |

The `Frames` folder contains every prop at its natural cropped size.

## Unity import

- Master sheet: Sprite Mode `Multiple`, slice by cell size `64 x 64`, pivot `Bottom Center`.
- Standalone frames: Sprite Mode `Single`, pivot `Bottom Center`.
- Use Filter Mode `Point (no filter)`, Compression `None`, and Pixels Per Unit `16`.

Use Box Collider 2D or Polygon Collider 2D only on the lower body of the kumquat pot, well, bicycle and clay jar. Chrysanthemums, couplets, lanterns, broom and small planter can remain decoration-only. Put wall hangings on a Foreground sorting layer and courtyard objects on a Props sorting layer.
