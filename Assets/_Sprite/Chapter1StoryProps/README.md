# Chapter 1 story props

Pixel-art props for the Hanoi Tet chapter. The sheet is a 4 x 4 grid; every cell is 32 x 32 px with a transparent background.

| Row | Column 0 | Column 1 | Column 2 | Column 3 |
| --- | --- | --- | --- | --- |
| 0 | Peach branch | Dong leaves | Sticky rice | Mung beans |
| 1 | Pork belly | Wooden mold | Bamboo strings | Cooking pot |
| 2 | Wood stove | Ancestor altar | Five-fruit tray | Red envelope |
| 3 | Incense burner | Wrapped banh chung | Firewood bundle | Hot-water kettle |

## Unity import

Use `Chapter1_StoryProps_32px.png` as a Multiple Sprite sheet. In Sprite Editor choose `Slice > Grid by Cell Size`, set the cells to `32 x 32`, and use a `Bottom Center` pivot.

Set Filter Mode to `Point (no filter)`, Compression to `None`, and Pixels Per Unit to `16`. The individual PNG files in `Frames` can also be imported directly without slicing.

Place the ingredients and keepsakes on a Props sorting layer. Give colliders only to large world objects such as the cooking pot, wood stove, ancestor altar and wooden mold; inventory icons and minigame ingredients should remain collider-free.
