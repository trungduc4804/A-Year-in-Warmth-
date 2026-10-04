from PIL import Image, ImageDraw
from pathlib import Path
import json, struct, zlib

OUT=Path(__file__).resolve().parents[2]/'Assets/_Art/HanoiStreetBase'
OUT.mkdir(parents=True,exist_ok=True)
P={
'ink':'#403e45','road':'#87979a','roadDark':'#728488','roadLight':'#9ba9aa',
'pave':'#d7d1b7','paveDark':'#b2ad98','paveLight':'#ece3c7',
'brick':'#bb6950','brickDark':'#94513f','brickLight':'#dc8a62',
'court':'#d48964','courtDark':'#b16c51','courtLight':'#e5a47b',
'cement':'#c6c4af','cementDark':'#a8aa9c','cementLight':'#dedbc1',
'yellow':'#ebc576','cream':'#f2d898','old':'#d5af69','wallDark':'#b38c53',
'roof':'#bc5c45','roofDark':'#8c443d','roofLight':'#dc7d55',
'wood':'#906447','woodDark':'#634b3e','woodLight':'#ba8956',
'green':'#53876c','greenDark':'#366553','greenLight':'#79a77a',
'red':'#d9584d','redDark':'#a13d42','redLight':'#f18164',
'pink':'#e58a9d','pinkLight':'#ffc0b4','pinkDark':'#c96786',
'skin':'#e4ae80','blue':'#5e839b','gold':'#f3d477'}
def canvas(w=16,h=16):return Image.new('RGBA',(w,h))
def draw(im):return ImageDraw.Draw(im)
def rect(d,b,c):d.rectangle(b,fill=P.get(c,c))
def line(d,pts,c,width=1):d.line(pts,fill=P.get(c,c),width=width)
def poly(d,pts,c):d.polygon(pts,fill=P.get(c,c))
assets={}; groups={}
def add(name,im,group):assets[name]=im;groups[name]=group;return im

# Palette first; all drawing coordinates are native integer pixels.
pal=canvas(16*len(P),16)
for i,c in enumerate(P.values()):draw(pal).rectangle((i*16,0,i*16+15,15),fill=c)
pal.save(OUT/'Hanoi_Street_Palette.png')
(OUT/'Hanoi_Street_Palette.gpl').write_text('GIMP Palette\nName: Hanoi Street Base\nColumns: 8\n#\n'+'\n'.join(f'{int(c[1:3],16)} {int(c[3:5],16)} {int(c[5:7],16)} {n}' for n,c in P.items()))
def ground(kind,variation=0):
 im=canvas();d=draw(im);rect(d,(0,0,15,15),kind)
 if kind in ['pave','brick','court']:
  step=8 if kind=='pave' else 4
  for y in range(0,16,step):
   line(d,[(0,y),(15,y)],kind+'Dark')
   for x in range((0 if (y//step)%2==0 else 4),16,8):line(d,[(x,y),(x,y+step-1)],kind+'Dark')
  if kind!='pave':line(d,[(1,1),(6,1)],kind+'Light')
 if variation==1:line(d,[(3,6),(6,6)],kind+'Dark');line(d,[(10,12),(12,12)],kind+'Light')
 if variation==2:line(d,[(5,4),(6,6),(5,8),(8,10)],kind+'Dark')
 if variation==3:
  if kind=='road':rect(d,(0,0,15,15),'roadLight')
  else:line(d,[(2,10),(5,10)],kind+'Light')
 return im
for k in ['road','pave','brick','court','cement']:
 for v,n in enumerate(['clean','worn','crack','light']):add(f'{k}_{n}',ground(k,v),'ground')
style=canvas(96,48)
for y in range(3):
 for x in range(6):style.paste(assets[['pave_clean','road_clean','brick_clean'][y]],(x*16,y*16))
style.save(OUT/'Hanoi_Street_StyleCheck.png')

# 47 valid blob masks: N,E,S,W,NE,SE,SW,NW. Missing diagonals cut inner corners.
masks=[]
for m in range(256):
 if all(not(m&(1<<di)) or (m&(1<<a) and m&(1<<b)) for di,a,b in [(4,0,1),(5,1,2),(6,2,3),(7,3,0)]):masks.append(m)
for k in ['road','pave','brick','court','cement']:
 for m in masks:
  im=ground(k);d=draw(im)
  for bit,box in [(0,(0,0,15,1)),(1,(14,0,15,15)),(2,(0,14,15,15)),(3,(0,0,1,15))]:
   if not m&(1<<bit):rect(d,box,k+'Dark')
  for di,a,b,box in [(4,0,1,(13,0,15,2)),(5,1,2,(13,13,15,15)),(6,2,3,(0,13,2,15)),(7,3,0,(0,0,2,2))]:
   if not m&(1<<di):rect(d,box,k+'Dark')
  add(f'{k}_blob_{m:03}',im,'ground')

for name in ['drain_square','drain_round','gutter_h','gutter_v','curb_outlet']:
 im=canvas();d=draw(im)
 if name=='drain_square':rect(d,(3,3,12,12),'ink');rect(d,(4,4,11,11),'roadDark');[line(d,[(5,y),(10,y)],'roadLight') for y in [5,8,11]]
 elif name=='drain_round':d.ellipse((3,3,12,12),fill=P['ink']);d.ellipse((4,4,11,11),fill=P['roadDark']);[line(d,[(5,y),(10,y)],'roadLight') for y in [6,9]]
 elif name=='gutter_h':rect(d,(0,6,15,9),'roadDark');line(d,[(0,6),(15,6)],'ink');line(d,[(0,10),(15,10)],'paveLight')
 elif name=='gutter_v':rect(d,(6,0,9,15),'roadDark');line(d,[(6,0),(6,15)],'ink');line(d,[(10,0),(10,15)],'paveLight')
 else:rect(d,(2,5,13,10),'paveDark');rect(d,(3,7,12,9),'ink');[line(d,[(x,7),(x,9)],'roadDark') for x in [5,8,11]]
 add(name,im,'drains')
for c in ['yellow','cream','old']:
 im=canvas();rect(draw(im),(0,0,15,15),c);add('wall_'+c,im,'buildings')
 base=im.copy();rect(draw(base),(0,12,15,15),'wallDark');line(draw(base),[(0,12),(15,12)],'woodLight');add('wall_'+c+'_base',base,'buildings')
for n in ['center','left','right','top','bottom']:
 im=canvas();d=draw(im);rect(d,(0,0,15,15),'roof')
 for y in range(0,16,4):
  line(d,[(0,y+3),(15,y+3)],'roofDark')
  for x in range((y//4%2)*4,16,8):line(d,[(x,y),(x,y+2)],'roofDark');line(d,[(x+1,y),(min(x+6,15),y)],'roofLight')
 if n=='bottom':rect(d,(0,13,15,15),'roofDark')
 if n=='top':rect(d,(0,0,15,2),'roofLight')
 if n=='left':line(d,[(0,0),(0,15)],'roofDark',2)
 if n=='right':line(d,[(15,0),(15,15)],'roofDark',2)
 add('roof_'+n,im,'buildings')
for n in ['single','double','closed','open']:
 w=32 if n=='double' else 16;im=canvas(w,32);d=draw(im)
 rect(d,(1,2,w-2,31),'woodDark');rect(d,(3,4,w-4,29),'wood')
 if n=='open':rect(d,(4,4,12,29),'ink');rect(d,(2,4,4,29),'woodLight')
 else:
  for x in range(5,w-3,5):line(d,[(x,5),(x,28)],'woodDark')
  line(d,[(3,16),(w-4,16)],'woodLight');rect(d,(w//2+2,18,w//2+2,19),'gold')
 rect(d,(0,30,w-1,31),'paveDark');add('door_'+n,im,'buildings')
for n in ['shutter','open','closed']:
 im=canvas();d=draw(im);rect(d,(1,1,14,14),'woodDark');rect(d,(2,2,13,13),'greenDark')
 if n=='open':rect(d,(5,2,10,13),'ink');rect(d,(1,2,3,13),'greenLight');rect(d,(12,2,14,13),'green')
 else:
  rect(d,(2,2,6,12),'green');rect(d,(9,2,13,12),'green')
  for y in [3,6,9]:line(d,[(2,y),(6,y)],'greenLight');line(d,[(9,y),(13,y)],'greenLight')
 rect(d,(0,14,15,15),'paveLight');add('window_'+n,im,'buildings')
for n,w,h in [('step',16,16),('column',16,32),('porch',32,16),('awning',32,16),('balcony',32,16),('shop_counter',32,16)]:
 im=canvas(w,h);d=draw(im)
 if n=='step':rect(d,(0,8,15,14),'paveDark');rect(d,(0,8,15,10),'paveLight')
 elif n=='column':rect(d,(6,0,9,31),'wallDark');rect(d,(6,0,7,31),'cream');rect(d,(4,28,11,31),'wallDark')
 elif n=='porch':rect(d,(0,3,31,8),'wood');rect(d,(0,2,31,3),'woodLight');rect(d,(2,8,4,15),'woodDark');rect(d,(27,8,29,15),'woodDark')
 elif n=='awning':
  poly(d,[(2,0),(29,0),(31,10),(0,10)],'cream')
  for x in range(0,32,8):rect(d,(x,2,x+3,11),'red')
  rect(d,(0,12,31,13),'redDark')
 elif n=='balcony':
  rect(d,(0,13,31,15),'wallDark');line(d,[(0,5),(31,5)],'greenDark',2)
  for x in range(2,32,5):line(d,[(x,5),(x,12)],'greenDark')
 else:rect(d,(1,5,30,14),'woodDark');rect(d,(0,3,31,5),'woodLight');rect(d,(3,6,28,12),'wood');[rect(d,(x,1,x+3,2),'gold') for x in [4,10,16,22]]
 add(n,im,'buildings')

def plant(d,x,y,flowers=False):
 poly(d,[(x-5,y),(x+5,y),(x+3,y+7),(x-3,y+7)],'brick');line(d,[(x-5,y),(x+5,y)],'brickLight')
 rect(d,(x-1,y-7,x,y-1),'wood')
 for dx,dy in [(-4,-5),(2,-7),(-2,-10)]:
  rect(d,(x+dx-2,y+dy-2,x+dx+2,y+dy+1),'greenDark');rect(d,(x+dx-1,y+dy-2,x+dx+2,y+dy),'green')
  if flowers:rect(d,(x+dx,y+dy-2,x+dx+1,y+dy-1),'pinkLight')
for n,w,h in [('motorbike',32,32),('bicycle',32,32),('stool',16,16),('table',16,16),('pot_plant',16,32),('pot_flower',16,32),('utility_pole',16,48),('wire_h',16,16),('wire_drop',16,16),('lantern',16,16),('flag',16,16),('red_sign',16,16),('crate',16,16),('basket',16,16),('peach_branch',16,32),('peach_pot',32,48),('bush',32,16),('street_tree',32,48),('door_flowers',32,16),('npc_blue',16,32),('npc_red',16,32)]:
 im=canvas(w,h);d=draw(im)
 if n in ['motorbike','bicycle']:
  for x in [3,22]:d.ellipse((x,20,x+7,27),outline=P['ink'],width=2)
  line(d,[(7,23),(14,14),(24,23),(7,23),(19,13),(24,23)],'greenDark' if n=='bicycle' else 'ink')
  line(d,[(19,13),(22,9),(26,9)],'ink');line(d,[(11,13),(16,13)],'woodDark',2)
  if n=='motorbike':poly(d,[(8,16),(17,15),(20,20),(12,23),(8,21)],'red');rect(d,(9,13,17,15),'ink');rect(d,(22,12,25,17),'redLight');rect(d,(25,12,27,14),'cream')
 elif n=='stool':rect(d,(3,5,12,8),'red');rect(d,(2,7,13,9),'redLight');rect(d,(3,10,5,14),'redDark');rect(d,(10,10,12,14),'redDark')
 elif n=='table':rect(d,(2,4,13,8),'wood');rect(d,(2,4,13,5),'woodLight');rect(d,(4,9,5,14),'woodDark');rect(d,(10,9,11,14),'woodDark')
 elif n in ['pot_plant','pot_flower']:plant(d,8,23,n=='pot_flower')
 elif n=='utility_pole':rect(d,(7,3,9,45),'wood');rect(d,(6,40,10,47),'cementDark');line(d,[(2,8),(14,8)],'woodDark',2);rect(d,(3,5,4,7),'cream');rect(d,(12,5,13,7),'cream')
 elif n=='wire_h':line(d,[(0,4),(4,5),(11,5),(15,4)],'ink')
 elif n=='wire_drop':line(d,[(0,3),(6,5),(15,10)],'ink')
 elif n=='lantern':line(d,[(8,0),(8,2)],'woodDark');rect(d,(5,2,10,3),'gold');poly(d,[(4,4),(11,4),(13,7),(13,10),(10,12),(5,12),(2,10),(2,7)],'redDark');rect(d,(4,5,11,10),'red');rect(d,(5,5,6,9),'redLight');rect(d,(6,12,9,13),'gold');line(d,[(8,14),(8,15)],'red')
 elif n=='flag':line(d,[(2,1),(2,15)],'woodDark');poly(d,[(3,2),(14,3),(13,11),(3,10)],'red');poly(d,[(8,4),(9,6),(11,6),(9,7),(10,9),(8,8),(6,9),(7,7),(5,6),(7,6)],'gold')
 elif n=='red_sign':rect(d,(3,1,12,14),'redDark');rect(d,(4,2,11,13),'red');rect(d,(6,4,9,5),'gold');rect(d,(7,7,8,10),'gold')
 elif n=='crate':rect(d,(2,4,13,14),'woodDark');rect(d,(3,5,12,13),'wood');line(d,[(3,8),(12,8)],'woodLight');line(d,[(3,11),(12,11)],'woodLight');line(d,[(3,5),(12,13)],'woodDark')
 elif n=='basket':d.arc((3,1,12,11),180,360,fill=P['woodDark']);poly(d,[(2,6),(13,6),(11,14),(4,14)],'wood');[line(d,[(4,y),(11,y)],'woodLight') for y in [7,10,12]]
 elif n in ['peach_branch','peach_pot']:
  cx=8 if w==16 else 16;by=28 if w==16 else 38
  line(d,[(cx,by),(cx-1,by-12),(cx-6,by-23)],'woodDark',2);line(d,[(cx-1,by-10),(cx+7,by-24)],'wood',2);line(d,[(cx-2,by-16),(cx+1,by-28)],'wood')
  for dx,dy in [(-6,-23),(-3,-17),(5,-21),(7,-25),(1,-28),(0,-12)]:
   rect(d,(cx+dx-2,by+dy-1,cx+dx+1,by+dy+1),'pinkDark');rect(d,(cx+dx-1,by+dy-2,cx+dx+2,by+dy),'pink');rect(d,(cx+dx,by+dy-1,cx+dx+1,by+dy),'pinkLight')
  if n=='peach_pot':poly(d,[(cx-7,by),(cx+7,by),(cx+5,by+8),(cx-5,by+8)],'brick');rect(d,(cx-7,by,cx+7,by+1),'brickLight')
 elif n=='bush':
  poly(d,[(1,13),(1,7),(5,7),(5,3),(13,3),(13,1),(23,1),(23,4),(29,4),(30,13)],'greenDark');rect(d,(5,5,26,10),'green');rect(d,(9,3,21,5),'greenLight')
 elif n=='street_tree':
  rect(d,(14,23,17,45),'woodDark');rect(d,(14,25,15,44),'wood');poly(d,[(4,30),(1,25),(1,14),(6,14),(6,7),(12,7),(12,3),(22,3),(22,7),(28,7),(28,14),(31,14),(31,25),(26,30)],'greenDark');poly(d,[(4,22),(4,14),(9,14),(9,9),(23,9),(23,14),(28,14),(28,22),(22,25),(10,25)],'green');rect(d,(10,9,22,12),'greenLight')
 elif n=='door_flowers':
  for x in [7,23]:plant(d,x,8,True)
 elif n.startswith('npc'):
  rect(d,(5,24,7,29),'ink');rect(d,(9,24,11,29),'ink');rect(d,(4,14,12,23),'blue' if n=='npc_blue' else 'red');rect(d,(3,16,4,21),'skin');rect(d,(12,16,13,21),'skin');rect(d,(5,6,11,12),'skin');rect(d,(4,5,12,7),'woodDark');rect(d,(4,7,5,10),'woodDark');rect(d,(10,9,10,9),'ink')
 add(n,im,'props' if not n.startswith('npc') else 'placeholders')

# Shelf pack on a 16 px grid, with one transparent cell between categories.
W=512; placements={};x=y=rowh=0;prev=None
for n,im in assets.items():
 if groups[n]!=prev:
  if x:y+=rowh;x=0;rowh=0
  if prev is not None:y+=16
  prev=groups[n]
 if x+im.width>W:y+=rowh;x=0;rowh=0
 placements[n]={'x':x,'y':y,'w':im.width,'h':im.height,'group':groups[n],'pivot':[im.width//2,im.height-1]}
 x+=im.width;rowh=max(rowh,im.height)
H=y+rowh;layers={g:canvas(W,H) for g in dict.fromkeys(groups.values())}
for n,im in assets.items():r=placements[n];layers[groups[n]].paste(im,(r['x'],r['y']))
atlas=canvas(W,H)
for im in layers.values():atlas.alpha_composite(im)
atlas.save(OUT/'Hanoi_Street_BaseTiles.png')

preview=canvas(320,192)
def put(n,x,y):preview.alpha_composite(assets[n],(x,y))
for gy in range(12):
 for gx in range(20):put('road_clean' if 5<=gy<=7 else 'pave_clean',gx*16,gy*16)
# Two house rows: same modular facade vocabulary, second row continues off-screen.
for row in [0,144]:
 for j in range(5):
  bx=j*64;c=['yellow','cream','old','yellow','cream'][j]
  for yy in range(row,row+64,16):
   for xx in range(bx,bx+64,16):put('wall_'+c,xx,yy)
  for xx in range(bx,bx+64,16):put('roof_top',xx,row);put('roof_bottom',xx,row+16);put('wall_'+c+'_base',xx,row+48)
  put('window_shutter',bx+8,row+24);put('window_open',bx+40,row+24)
  put('door_open' if j in [1,3] else 'door_closed',bx+24,row+32)
  if j in [1,3]:put('awning',bx+16,row+32);put('shop_counter',bx+40,row+48)
  else:put('step',bx+24,row+56)
  put('lantern' if j%2 else 'flag',bx+4,row+36)
for x in range(0,320,16):put('gutter_h',x,76);put('gutter_h',x,120)
put('drain_square',256,96);put('curb_outlet',192,76)
put('motorbike',74,64);put('bicycle',220,64);put('peach_pot',144,32)
for x in [10,116,278]:put('pot_plant',x,48)
put('stool',204,68);put('table',188,64);put('pot_flower',300,48)
put('npc_blue',136,80);put('npc_red',244,96);put('basket',54,64)
preview.save(OUT/'Hanoi_Street_Preview.png')
preview.resize((1280,768),Image.Resampling.NEAREST).save(OUT/'Hanoi_Street_Preview_4x.png')
atlas.resize((W*2,H*2),Image.Resampling.NEAREST).save(OUT/'Hanoi_Street_BaseTiles_2x.png')

# Native Aseprite RGBA file: one frame, editable named layers, compressed cels, palette.
def u16(v):return struct.pack('<H',v)
def u32(v):return struct.pack('<I',v)
def string(s):b=s.encode();return u16(len(b))+b
def chunk(t,b):return u32(6+len(b))+u16(t)+b
chunks=[]
for idx,(name,im) in enumerate(layers.items()):
 chunks.append(chunk(0x2004,u16(3)+u16(0)+u16(0)+u16(W)+u16(H)+u16(0)+bytes([255])+bytes(3)+string(name)))
 chunks.append(chunk(0x2005,u16(idx)+struct.pack('<hh',0,0)+bytes([255])+u16(2)+struct.pack('<h',0)+bytes(5)+u16(W)+u16(H)+zlib.compress(im.tobytes())))
pb=u32(len(P))+u32(0)+u32(len(P)-1)+bytes(8)
for n,c in P.items():pb+=u16(1)+bytes.fromhex(c[1:])+bytes([255])+string(n)
chunks.append(chunk(0x2019,pb))
framebody=b''.join(chunks);frame=u32(16+len(framebody))+u16(0xF1FA)+u16(len(chunks))+u16(100)+bytes(2)+u32(0)+framebody
header=u32(128+len(frame))+u16(0xA5E0)+u16(1)+u16(W)+u16(H)+u16(32)+u32(1)+u16(100)+bytes(8)+bytes([0])+bytes(3)+u16(len(P))+bytes([1,1])+struct.pack('<hhHH',0,0,16,16)+bytes(84)
assert len(header)==128
(OUT/'Hanoi_Street_BaseTiles.aseprite').write_bytes(header+frame)
(OUT/'Hanoi_Street_Assets.json').write_text(json.dumps({'tileSize':16,'imageSize':[W,H],'autotile':'47 blob masks. bits 0..7: N E S W NE SE SW NW; bit=1 means same terrain neighbor. All valid masks included for all five materials.','palette':P,'assets':placements},indent=2))
(OUT/'README.md').write_text('''# Phố cổ Hà Nội cơ bản

Native grid: 16×16. Atlas: transparent RGBA; coordinates in Hanoi_Street_Assets.json.
Each terrain includes 47 blob masks plus clean/worn/crack/light center variants.
Blob tile suffix is decimal neighbor mask: bits N,E,S,W,NE,SE,SW,NW. A diagonal is present only when both adjacent cardinal neighbors are present. Use mask 255 for center. These are image tiles, not preconfigured Unity RuleTile assets.
Building facades use 16×16 walls/roof/window modules; doors are 16×32 or 32×32. Props have whole-grid bounding boxes and bottom-center pivots. Draw ground, buildings, then sort props by foot Y.
The Aseprite file has editable ground, drains, buildings, props, and placeholders layers, one RGBA frame, a named palette, and a 16×16 grid. It uses regular image layers rather than Aseprite tilemap layers.
Preview is 320×192; enlarged previews use nearest-neighbor only. Native assets contain no antialias, blur, gradients or random noise.
Unity import: Sprite (2D and UI), Multiple, Pixels Per Unit 16, Filter Mode Point, Compression None, Generate Mip Maps off. Slice using JSON rectangles (Unity Y = atlas height - y - h). Avoid slicing large objects into 16×16 sprites.
Rebuild: run Tools/HanoiTiles/build_hanoi.py with Python and Pillow. All artwork is drawn at its native pixel resolution; enlarged PNGs are viewing copies only.
''',encoding='utf8')
print(json.dumps({'output':str(OUT),'assets':len(assets),'autotilesPerTerrain':len(masks),'atlas':[W,H],'preview':[320,192]}))
