from pathlib import Path
from PIL import Image, ImageDraw
import csv, hashlib, random, shutil

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/_Sprite/HanoiTerrainGround"
TILES = OUT / "Tiles"
OVERLAYS = OUT / "Overlays"
CELL, COLS, ROWS = 16, 16, 16

P = {
    "stone": (91, 91, 83, 255), "stone2": (108, 106, 94, 255),
    "stone_dark": (59, 62, 58, 255), "moss": (63, 86, 48, 255),
    "gray": (118, 116, 106, 255), "gray2": (139, 134, 120, 255),
    "red": (137, 70, 52, 255), "red2": (164, 87, 62, 255),
    "brick_dark": (83, 47, 39, 255), "dirt": (119, 91, 62, 255),
    "dirt_dark": (82, 67, 50, 255), "grass": (68, 92, 48, 255),
    "grass2": (84, 111, 57, 255), "water": (75, 105, 112, 190),
    "highlight": (151, 169, 164, 180), "clear": (0, 0, 0, 0),
}

atlas = Image.new("RGBA", (COLS * CELL, ROWS * CELL), P["clear"])
records = []


def rng(name):
    return random.Random(int(hashlib.sha1(name.encode()).hexdigest()[:12], 16))


def px(draw, x, y, color):
    if 0 <= x < CELL and 0 <= y < CELL:
        draw.point((x, y), fill=color)


def seal_edges(im, kind, wet=False):
    """Give every variant of one material identical opposite edge pixels."""
    edge_palette={
        "stone":[P["stone"],P["stone2"],P["stone2"],P["stone_dark"]],
        "red":[P["red"],P["red2"],P["red"],P["brick_dark"]],
        "gray":[P["gray"],P["gray2"],P["gray"],(78,80,75,255)],
        "dirt":[P["dirt"],(137,103,68,255),P["dirt"],P["dirt_dark"]],
        "grass":[P["grass"],P["grass2"],P["grass"],(47,72,38,255)],
    }
    pal=edge_palette[kind]; pix=im.load()
    for x in range(CELL):
        c=pal[(x//3)%len(pal)]; pix[x,0]=c; pix[x,CELL-1]=c
    for y in range(CELL):
        c=pal[(y//3)%len(pal)]; pix[0,y]=c; pix[CELL-1,y]=c
    corner=pal[0]
    for x,y in ((0,0),(15,0),(0,15),(15,15)): pix[x,y]=corner
    return im


def stone(name, wet=False, moss=False, cracked=False):
    im = Image.new("RGBA", (CELL, CELL), P["stone_dark"])
    d, r = ImageDraw.Draw(im), rng(name)
    # A periodic irregular cobble pattern. Lines meet identically at tile edges.
    for row, y in enumerate(range(-4, 20, 5)):
        off = 0 if row % 2 == 0 else 3
        for x in range(-8 + off, 20, 7):
            col = P["stone2"] if r.random() > .42 else P["stone"]
            d.rectangle((x + 1, y + 1, x + 6, y + 4), fill=col)
            if r.random() > .55:
                d.line((x + 2, y + 1, x + 5, y + 1), fill=(128, 126, 110, 255))
    if cracked:
        x, y = r.randrange(4, 11), r.randrange(3, 8)
        d.line([(x, y), (x-1, y+3), (x+1, y+5), (x, y+8)], fill=(52, 53, 49, 255))
    if moss:
        for _ in range(12):
            x, y = r.randrange(CELL), r.randrange(CELL)
            if x % 7 in (0, 1) or y % 5 == 0: px(d, x, y, P["moss"])
    if wet:
        im=Image.alpha_composite(im,Image.new("RGBA",im.size,(35,54,58,42))); d=ImageDraw.Draw(im)
        d.line((2, 3, 6, 3), fill=(150, 165, 158, 255))
    return seal_edges(im,"stone",wet)


def bricks(name, red=True, wet=False, moss=False, cracked=False, uneven=False):
    mortar = P["brick_dark"] if red else (78, 80, 75, 255)
    a, b = (P["red"], P["red2"]) if red else (P["gray"], P["gray2"])
    im = Image.new("RGBA", (CELL, CELL), mortar)
    d, r = ImageDraw.Draw(im), rng(name)
    h = 4
    for row, y in enumerate(range(0, 16, h)):
        off = 0 if row % 2 == 0 else 4
        for x in range(-8 + off, 20, 8):
            wobble = r.choice([0, 0, 0, 1]) if uneven else 0
            d.rectangle((x+1, y+1, x+7, min(15, y+3+wobble)), fill=a if r.random()<.55 else b)
            d.line((x+2, y+1, x+6, y+1), fill=tuple(min(255,c+15) for c in (a if r.random()<.5 else b)[:3])+(255,))
    if cracked:
        x = r.randrange(5, 12); d.line([(x,2),(x-1,6),(x+1,9),(x,13)], fill=mortar)
    if moss:
        for _ in range(11):
            x,y=r.randrange(16),r.randrange(16)
            if y % 4 == 0 or x % 8 == 0: px(d,x,y,P["moss"])
    if wet:
        im=Image.alpha_composite(im,Image.new("RGBA",im.size,(31,55,62,48))); d=ImageDraw.Draw(im); d.line((9,2,13,2), fill=(166,174,161,255))
    return seal_edges(im,"red" if red else "gray",wet)


def dirt(name, wet=False, grass=False):
    base = P["dirt_dark"] if wet else P["dirt"]
    im=Image.new("RGBA",(CELL,CELL),base); d=ImageDraw.Draw(im); r=rng(name)
    for _ in range(22):
        x,y=r.randrange(16),r.randrange(16)
        c=(145,110,72,255) if r.random()<.5 else (91,72,52,255)
        px(d,x,y,c)
    if grass:
        for _ in range(9):
            x,y=r.randrange(1,15),r.randrange(2,15); px(d,x,y,P["grass2"]); px(d,x,y-1,P["grass"])
    if wet: im=Image.alpha_composite(im,Image.new("RGBA",im.size,(30,48,49,40)))
    return seal_edges(im,"dirt",wet)


def grass(name, sparse=False):
    im=Image.new("RGBA",(CELL,CELL),P["grass"]); d=ImageDraw.Draw(im); r=rng(name)
    for _ in range(30 if not sparse else 13):
        x,y=r.randrange(16),r.randrange(16); px(d,x,y,P["grass2"])
        if r.random()<.22: px(d,x,min(15,y+1),(47,72,38,255))
    if sparse:
        overlay=dirt(name+"_under"); im=Image.blend(overlay,im,.43)
    return seal_edges(im,"grass")


def puddle(base, name, size="small", edge=False):
    im=base.copy(); overlay=Image.new("RGBA",im.size,P["clear"]); d=ImageDraw.Draw(overlay); r=rng(name)
    boxes={"small":(5,7,10,11),"medium":(3,5,12,12),"large":(1,4,14,13)}
    box=boxes[size]
    if edge: box=(0,6,10,14)
    d.ellipse(box,fill=P["water"])
    x0,y0,x1,y1=box; d.line((x0+2,y0+2,x1-2,y0+2),fill=P["highlight"])
    if r.random()>.4: d.point(((x0+x1)//2,y1-2),fill=(39,74,82,180))
    return Image.alpha_composite(im,overlay)


def bordered(base, kind):
    im=base.copy(); d=ImageDraw.Draw(im)
    curb=(157,148,127,255); shadow=(64,61,54,255)
    if "top" in kind: d.rectangle((0,0,15,2),fill=curb); d.line((0,3,15,3),fill=shadow)
    if "bottom" in kind: d.rectangle((0,13,15,15),fill=curb); d.line((0,12,15,12),fill=shadow)
    if "left" in kind: d.rectangle((0,0,2,15),fill=curb); d.line((3,0,3,15),fill=shadow)
    if "right" in kind: d.rectangle((13,0,15,15),fill=curb); d.line((12,0,12,15),fill=shadow)
    return im


def blend_mask(primary, secondary, mask, name):
    # Bits: 1=N, 2=E, 4=S, 8=W. Secondary occupies selected borders.
    im=primary.copy(); band=4
    if mask & 1: im.paste(secondary.crop((0,0,16,band)),(0,0))
    if mask & 2: im.paste(secondary.crop((16-band,0,16,16)),(16-band,0))
    if mask & 4: im.paste(secondary.crop((0,16-band,16,16)),(0,16-band))
    if mask & 8: im.paste(secondary.crop((0,0,band,16)),(0,0))
    d=ImageDraw.Draw(im); seam=(58,63,49,255)
    if mask&1:d.line((0,band-1,15,band-1),fill=seam)
    if mask&2:d.line((16-band,0,16-band,15),fill=seam)
    if mask&4:d.line((0,16-band,15,16-band),fill=seam)
    if mask&8:d.line((band-1,0,band-1,15),fill=seam)
    return im


def inner_corner(primary, secondary, corner):
    im=primary.copy(); q={"TL":(0,0,5,5),"TR":(11,0,15,5),"BL":(0,11,5,15),"BR":(11,11,15,15)}[corner]
    im.paste(secondary.crop(q),q); d=ImageDraw.Draw(im)
    x0,y0,x1,y1=q; d.line((x0,y1,x1,y1),fill=(58,63,49,255)); d.line((x1,y0,x1,y1),fill=(58,63,49,255))
    return im


def add(row,col,name,category,image,notes=""):
    atlas.alpha_composite(image,(col*CELL,row*CELL))
    image.save(TILES/f"{name}.png")
    records.append((name,row,col,category,notes))


def main():
    OUT.mkdir(parents=True,exist_ok=True); TILES.mkdir(parents=True,exist_ok=True); OVERLAYS.mkdir(parents=True,exist_ok=True)
    ref=Path(r"C:/Users/Trung Duc/.codex/generated_images/01a0fc63-bc0f-7371-be40-78107b317aaf/exec-85e836f5-9d80-4c4b-998a-cfecc98d8d0c.png")
    if ref.exists(): shutil.copy2(ref,OUT/"HanoiTerrain_StyleReference.png")

    s=stone("road")
    topo=[("Center",s),("EdgeTop",bordered(s,"top")),("EdgeBottom",bordered(s,"bottom")),("EdgeLeft",bordered(s,"left")),("EdgeRight",bordered(s,"right")),
          ("OuterCornerTL",bordered(s,"top left")),("OuterCornerTR",bordered(s,"top right")),("OuterCornerBL",bordered(s,"bottom left")),("OuterCornerBR",bordered(s,"bottom right")),
          ("InnerCornerTL",inner_corner(s,bricks("walk",False),"TL")),("InnerCornerTR",inner_corner(s,bricks("walk",False),"TR")),("InnerCornerBL",inner_corner(s,bricks("walk",False),"BL")),("InnerCornerBR",inner_corner(s,bricks("walk",False),"BR"))]
    for c,(n,img) in enumerate(topo): add(0,c,"RoadStone_"+n,"road",img)
    add(0,13,"RoadStone_CrackedA","road",stone("cracka",cracked=True)); add(0,14,"RoadStone_CrackedB","road",stone("crackb",cracked=True)); add(0,15,"RoadStone_Moss","road",stone("moss",moss=True))

    roadvars=[stone("clean"),stone("wet",wet=True),puddle(stone("p1",wet=True),"p1"),puddle(stone("p2",wet=True),"p2","medium"),puddle(stone("pe",wet=True),"pe","medium",True)]
    for c,img in enumerate(roadvars): add(1,c,["RoadStone_Clean","RoadStone_Wet","Rain_PuddleSmallStone","Rain_PuddleMediumStone","Rain_PuddleRoadEdge"][c],"road-rain",img)
    for c in range(5,16):
        base=stone(f"roadextra{c}",wet=c%3==0,moss=c%4==0,cracked=c%2==0)
        if c%5==0: base=puddle(base,f"roadextrap{c}","small")
        add(1,c,f"RoadStone_Variant{c-4:02d}","road",base)

    for row,label,red in [(2,"SidewalkGray",False),(3,"SidewalkRed",True),(4,"AlleyBrick",True)]:
        variants=[("Clean",{}),("Cracked",{"cracked":True}),("Moss",{"moss":True}),("Wet",{"wet":True}),("Uneven",{"uneven":True}),("CrackedWet",{"cracked":True,"wet":True}),("MossWet",{"moss":True,"wet":True})]
        for c,(n,kw) in enumerate(variants): add(row,c,f"{label}_{n}",label,bricks(f"{label}{n}",red,**kw))
        for c in range(7,16): add(row,c,f"{label}_Variant{c-6:02d}",label,bricks(f"{label}v{c}",red,uneven=(c%2==0),moss=(c%3==0),wet=(c%5==0)))

    dirtvars=[("Dry",dirt("dry")),("Damp",dirt("damp",wet=True)),("GrassMix",dirt("mix",grass=True)),("GrassMixDamp",dirt("mixw",True,True))]
    for c,(n,img) in enumerate(dirtvars): add(5,c,"Dirt_"+n,"dirt",img)
    for c in range(4,16): add(5,c,f"Dirt_Variant{c-3:02d}","dirt",dirt(f"dv{c}",wet=c%4==0,grass=c%3==0))
    grassvars=[("Spring",grass("spring")),("Sparse",grass("sparse",True)),("BrickJoints",bricks("grassbrick",True,moss=True)),("Wet",puddle(grass("gw"),"gp","small"))]
    for c,(n,img) in enumerate(grassvars): add(6,c,"Grass_"+n,"grass",img)
    for c in range(4,16): add(6,c,f"Grass_Variant{c-3:02d}","grass",grass(f"gv{c}",c%3==0))

    transitions=[("RoadSidewalk",7,8,stone("trs"),bricks("trg",False)),("BrickDirt",9,10,bricks("tbd",True),dirt("tdd")),("DirtGrass",11,12,dirt("tdg"),grass("tgg")),("BrickGrass",13,14,bricks("tbg",True),grass("tgr"))]
    for label,row,row_inner,a,b in transitions:
        for mask in range(16): add(row,mask,f"Transition_{label}_Mask{mask:02X}","transition",blend_mask(a,b,mask,label+str(mask)),"bits N=1 E=2 S=4 W=8")
        for c,corner in enumerate(("TL","TR","BL","BR")): add(row_inner,c,f"Transition_{label}_Inner{corner}","transition-inner",inner_corner(a,b,corner))
        for c in range(4,16): add(row_inner,c,f"Transition_{label}_Variant{c-3:02d}","transition",blend_mask(a,b,[1,2,4,8][c%4],label+"v"+str(c)))

    # Transparent rain overlays for a separate effects Tilemap.
    for c,(name,box) in enumerate([("RippleSmall",(5,6,10,10)),("RippleWide",(3,5,12,11)),("RippleTwin",(2,4,13,12))]):
        overlay=Image.new("RGBA",(16,16),P["clear"]); d=ImageDraw.Draw(overlay); d.ellipse(box,outline=P["highlight"])
        if c==2:d.ellipse((7,8,14,13),outline=(126,151,150,130))
        overlay.save(OVERLAYS/f"Rain_{name}_Overlay.png")
        composed=stone("ripplebase"+str(c),wet=True); composed.alpha_composite(overlay)
        add(15,c,"Rain_"+name,"rain",composed)
    add(15,3,"Rain_WetBrick", "rain", bricks("rainbrick",True,wet=True))
    add(15,4,"Rain_WetGrayBrick", "rain", bricks("raingray",False,wet=True))
    add(15,5,"Rain_WetDirt", "rain", dirt("raindirt",wet=True))
    for c in range(6,16):
        choices=[stone(f"rain{c}",wet=True),bricks(f"rainb{c}",True,wet=True),bricks(f"raing{c}",False,wet=True),dirt(f"raind{c}",wet=True),puddle(stone(f"rainp{c}",wet=True),f"rainpv{c}","small")]
        add(15,c,f"Rain_GroundVariant{c-5:02d}","rain",choices[(c-6)%len(choices)])

    atlas.save(OUT/"Hanoi_TerrainGround_16px.png")
    atlas.resize((1024,1024),Image.Resampling.NEAREST).save(OUT/"Hanoi_TerrainGround_Preview4x.png")
    with open(OUT/"Hanoi_TerrainGround_Index.csv","w",newline="",encoding="utf-8-sig") as f:
        w=csv.writer(f); w.writerow(["name","row","column","category","notes"]); w.writerows(records)
    seamless_names=("RoadStone_Center","SidewalkGray_Clean","SidewalkRed_Clean","AlleyBrick_Clean","Dirt_Dry","Grass_Spring")
    for name in seamless_names:
        im=Image.open(TILES/f"{name}.png").convert("RGBA"); pix=im.load()
        assert all(pix[0,y]==pix[15,y] for y in range(16)), f"horizontal seam: {name}"
        assert all(pix[x,0]==pix[x,15] for x in range(16)), f"vertical seam: {name}"
    assert all(alpha==255 for *_,alpha in atlas.getdata()), "main atlas contains transparent gaps"
    print(f"Created {len(records)} tiles; atlas {atlas.size[0]}x{atlas.size[1]}; seamless checks passed")

if __name__ == "__main__": main()
