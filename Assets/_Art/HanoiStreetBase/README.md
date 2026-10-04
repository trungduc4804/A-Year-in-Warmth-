# Phố cổ Hà Nội cơ bản

Native grid: 16×16. Atlas: transparent RGBA; coordinates in Hanoi_Street_Assets.json.
Each terrain includes 47 blob masks plus clean/worn/crack/light center variants.
Blob tile suffix is decimal neighbor mask: bits N,E,S,W,NE,SE,SW,NW. A diagonal is present only when both adjacent cardinal neighbors are present. Use mask 255 for center. These are image tiles, not preconfigured Unity RuleTile assets.
Building facades use 16×16 walls/roof/window modules; doors are 16×32 or 32×32. Props have whole-grid bounding boxes and bottom-center pivots. Draw ground, buildings, then sort props by foot Y.
The Aseprite file has editable ground, drains, buildings, props, and placeholders layers, one RGBA frame, a named palette, and a 16×16 grid. It uses regular image layers rather than Aseprite tilemap layers.
Preview is 320×192; enlarged previews use nearest-neighbor only. Native assets contain no antialias, blur, gradients or random noise.
Unity import: Sprite (2D and UI), Multiple, Pixels Per Unit 16, Filter Mode Point, Compression None, Generate Mip Maps off. Slice using JSON rectangles (Unity Y = atlas height - y - h). Avoid slicing large objects into 16×16 sprites.
Rebuild: run Tools/HanoiTiles/build_hanoi.py with Python and Pillow. All artwork is drawn at its native pixel resolution; enlarged PNGs are viewing copies only.
