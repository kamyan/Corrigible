"""F6: dokuz FX HUD durumunun maskeli farkı; tek başına kabul testi değildir."""
from pathlib import Path
import csv,json
from PIL import Image,ImageDraw,ImageChops,ImageStat,ImageFilter
root=Path(__file__).resolve().parents[2];out=root/'Docs/UI_Fx/captures/F6';ref=root/'Docs/UI_Fx/SonSalter_Fx_Paketi/reference';reports=[]
keys={'turn','objective','top-buttons','team','end','unit-card','hover-card','event-slip','end-confirm','log-drawer','rules-card','retreat-confirm','result','bigstamp'}
contact=Image.new('RGB',(960*3,270*3))
for n in range(1,10):
 a=Image.open(ref/f'screens/F-Baskin{n}.jpg').convert('RGB');b=Image.open(out/f'S{n}_Tr_1920x1080.png').convert('RGB');mask=Image.new('L',a.size);d=ImageDraw.Draw(mask)
 with open(ref/f'measure/F-Baskin{n}.tsv') as f:
  for row in csv.DictReader(f,delimiter='\t'):
   if row['id'] in keys:
    x,y,w,h=map(float,(row['x'],row['y'],row['w'],row['h']));d.rectangle((x-6,y-6,x+w+6,y+h+6),fill=255)
 # Çeviriye göre büyüyen gerçek paneller de fark alanına dahildir.
 if n==6:d.rectangle((1468,68,1910,686),fill=255)
 if n==8:d.rectangle((1325,8,1910,300),fill=255)
 if n==9:d.rectangle((950,440,1415,540),fill=255);d.rectangle((1234,534,1706,984),fill=255)
 for r in json.loads((ref/f'measure/F-Baskin{n}_mask.json').read_text())['rects']:d.rectangle((r['x'],r['y'],r['x']+r['w'],r['y']+r['h']),fill=0)
 diff=ImageChops.difference(a,b);mean=sum(ImageStat.Stat(diff,mask).mean)/3
 def edges(im):return ImageChops.multiply(im.convert('L').filter(ImageFilter.FIND_EDGES).point(lambda p:255 if p>34 else 0),mask)
 ea,eb=edges(a),edges(b);coverage=ImageStat.Stat(ImageChops.multiply(ea,eb.filter(ImageFilter.MaxFilter(5)))).sum[0]/max(1,ImageStat.Stat(ea).sum[0])*100
 Image.composite(diff,Image.new('RGB',a.size),mask).save(out/f'S{n}_hud-diff.png');mask.save(out/f'S{n}_hud-mask.png');overlay=Image.blend(a,b,.5);overlay.save(out/f'S{n}_overlay.png')
 row=Image.new('RGB',(960,270));row.paste(a.resize((480,270)),(0,0));row.paste(b.resize((480,270)),(480,0));contact.paste(row,(((n-1)%3)*960,((n-1)//3)*270))
 reports.append(dict(screen=n,meanRgbDifference255=mean,referenceEdgeCoverageWithin2pxPercent=coverage))
contact.save(out/'comparison-contact.jpg',quality=92)
result=dict(scope='HUD/panel alanları ve büyüyen yerelleştirilmiş kutuların birleşimi; zamana bağlı küreler dışarıda. Kaynak JPG ile Unity TMP/cam/ışıma ve bazı kutu yükseklikleri farklıdır. Piksel eşliği kabulü değildir.',screens=reports)
(out/'comparison-summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(reports)
