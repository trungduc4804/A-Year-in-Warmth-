# Arthur character sprite set

`Arthur_4Direction_48px.png` is a 4 x 4 sprite sheet. Each cell is 48 x 48 pixels, with a transparent background.

| Row | Direction | Frames |
| --- | --- | --- |
| 0 | Down | 0, 1, 2, 3 |
| 1 | Right | 0, 1, 2, 3 |
| 2 | Up | 0, 1, 2, 3 |
| 3 | Left | 0, 1, 2, 3 |

The `Frames` folder contains all 16 cells as individual PNGs.

## Unity import

- Sprite Mode: `Multiple`
- Slice: `Grid by Cell Size`, 48 x 48 pixels, pivot `Bottom Center`
- Filter Mode: `Point (no filter)`
- Compression: `None`
- Pixels Per Unit: `16`

## Animation mapping

| Animation | Frames | Samples | Loop |
| --- | --- | --- | --- |
| Idle per direction | `01` | 2 | Yes |
| Walk per direction | `00, 01, 02, 03` | 8 | Yes |
| Jog per direction | `00, 01, 02, 03` | 12 | Yes |
