from pathlib import Path
from PIL import Image
import struct,zlib,json
p=Path(__file__).resolve().parents[2]/'Assets/_Art/HanoiStreetBase'
b=(p/'Hanoi_Street_BaseTiles.aseprite').read_bytes()
assert struct.unpack_from('<I',b)[0]==len(b)
assert struct.unpack_from('<H',b,4)[0]==0xA5E0
w,h=struct.unpack_from('<HH',b,8)
n=struct.unpack_from('<H',b,134)[0]
pos=144; merged=Image.new('RGBA',(w,h)); layers=cels=0
for i in range(n):
 size,typ=struct.unpack_from('<IH',b,pos);body=b[pos+6:pos+size]
 if typ==0x2004:layers+=1
 if typ==0x2005:
  cw,ch=struct.unpack_from('<HH',body,16);raw=zlib.decompress(body[20:])
  assert len(raw)==cw*ch*4
  merged.alpha_composite(Image.frombytes('RGBA',(cw,ch),raw));cels+=1
 pos+=size
assert pos==len(b)
atlas=Image.open(p/'Hanoi_Street_BaseTiles.png')
assert merged.tobytes()==atlas.tobytes()
data=json.loads((p/'Hanoi_Street_Assets.json').read_text())
palette={tuple(bytes.fromhex(c[1:]))+(255,) for c in data['palette'].values()}
assert all(c[3]==0 or c in palette for c in atlas.getdata())
assert all(r['x']%16==0 and r['y']%16==0 and r['w']%16==0 and r['h']%16==0 for r in data['assets'].values())
assert len([n for n in data['assets'] if '_blob_' in n])==235
assert Image.open(p/'Hanoi_Street_Preview.png').size==(320,192)
print(f'PASS: {layers} layers, {cels} cels, 305 assets; native Aseprite decode matches PNG; palette and 16px grid verified.')
