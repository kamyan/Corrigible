"""F5 tahta alanı: HUD ve zamanla değişen küreler dışarıda; renk ve kenar farkını raporlar."""
from pathlib import Path
import json
from PIL import Image, ImageDraw, ImageChops, ImageStat, ImageFilter
root=Path(__file__).resolve().parents[2]
out=root/'Docs/UI_Fx/captures/F5'
a=Image.open(root/'Docs/UI_Fx/SonSalter_Fx_Paketi/reference/screens/F-Baskin1.jpg').convert('RGB')
b=Image.open(out/'B1_Tr_1920x1080.png').convert('RGB')
mask=Image.new('L',a.size);d=ImageDraw.Draw(mask)
d.polygon([(960,64),(1770,528),(1770,559),(960,1038),(151,559),(151,528)],fill=255)
for r in [(0,0,420,197),(1465,0,1919,70),(0,983,1919,1079),(1092,147,1205,260)]:d.rectangle(r,fill=0)
mask.save(out/'comparison-mask.png')
diff=ImageChops.difference(a,b)
mean=sum(ImageStat.Stat(diff,mask).mean)/3
Image.composite(diff,Image.new('RGB',a.size),mask).save(out/'comparison-diff.png')
def edges(im):return ImageChops.multiply(im.convert('L').filter(ImageFilter.FIND_EDGES).point(lambda p:255 if p>34 else 0),mask)
ea,eb=edges(a),edges(b)
coverage=ImageStat.Stat(ImageChops.multiply(ea,eb.filter(ImageFilter.MaxFilter(5)))).sum[0]/max(1,ImageStat.Stat(ea).sum[0])*100
Image.merge('RGB',(ea,eb,Image.new('L',a.size))).save(out/'comparison-edges.png')
result=dict(reference='F-Baskin1',meanRgbDifference255=mean,referenceEdgeCoverageWithin2pxPercent=coverage,scope='Yalnız tahta çokgeni; F6 HUD alanları ve değişken düğüm küresi dışarıda. Kaynak mesh/font/ışıma farkları vardır; bu değerler piksel eşliği kabulü değildir.')
(out/'comparison-summary.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(result)
