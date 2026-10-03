# Arthur 8 direction animation set

This animation extension contains 80 transparent 48 x 48 px frames derived
directly from the original `Arthur/Arthur_4Direction_48px.png` sprite. Arthur's
hair, face, glasses, clothes, proportions, outline and palette are unchanged.

## Direction row order

| Row | Direction | Blend vector |
| --- | --- | --- |
| 0 | Down | 0, -1 |
| 1 | Down Right | 1, -1 |
| 2 | Right | 1, 0 |
| 3 | Up Right | 1, 1 |
| 4 | Up | 0, 1 |
| 5 | Up Left | -1, 1 |
| 6 | Left | -1, 0 |
| 7 | Down Left | -1, -1 |

## Sheets

- `Arthur8_Idle_4frames_48x64.png`: four columns by eight rows, 192 x 384 px.
- `Arthur8_Walk_6frames_48x64.png`: six columns by eight rows, 288 x 384 px.
- `Frames`: all 80 frames as standalone PNGs for reliable clip generation.

The filenames above are retained so Unity preserves their existing `.meta`
GUIDs. Their actual cells are now 48 x 48 px.

The four cardinal directions reuse the original pixels. Idle uses the planted
neutral frame `00`, keeps both feet fixed, and adds a one-pixel head breathing
motion. Walk expands the original four frames into a smoother six-step
ping-pong cycle. Diagonal directions use the matching original front/back frame
with a maximum one-pixel upper-body lean; no new character design or colors are
introduced.

Every generated clip repeats frame `00` at its exact endpoint and enables
`Loop Time`. This makes the final pose connect to the initial pose before Unity
wraps the playback time, preventing a visible one-frame loop seam.

## Unity setup

Let Unity compile `Arthur8DirectionAnimationBuilder.cs`, then select `Tools > A Year in Warmth > Build Arthur 8 Direction Animator`. The tool imports the standalone frames with Point filtering, 16 pixels per unit and no compression. It generates 16 looping AnimationClips and `Arthur8Direction.controller` inside `GeneratedAnimation`.

Assign the generated controller to Arthur's Animator. It uses `MoveX`, `MoveY`, `LastMoveX`, `LastMoveY` and `IsMoving`, matching the parameters already written by `PlayerController`.

Set `PlayerController > Facing Mode` to `None`. The new controller already contains dedicated Left and Right sprites, so horizontal flipping must be disabled.
