using Corrigible.Core;
namespace Corrigible.Fx
{
    public static class FxGuideCopy
    {
        public static readonly string[] Ids={"aiStates","trust","colors","actions","teams","killSwitch","keys"};
        public static readonly string[] Names={Loc.N("YZ hâlleri"),Loc.N("Güven dili"),Loc.N("Renkler"),Loc.N("Eylemler"),Loc.N("Saha ekipleri"),Loc.N("Son Şalter"),Loc.N("Kısayollar")};
        public static readonly string[] StateDescriptions={
            Loc.N("YZ yerleşiyor, düzenli ve sakin. İzleme ağı kurmanın en ucuz anı."),
            Loc.N("Kabuk dağılır, şimşekler çıkar; yayılım hızlanır. Denetim ve karantina."),
            Loc.N("Kristal kabuk: çekirdek olabilir. Baskın ya da Son Şalter.")};
        public readonly struct Row{public readonly string Title,Body;public Row(string title,string body){Title=title;Body=body;}}
        static readonly Row[] Colors={new Row(Loc.N("SIZMA"),Loc.N("Altın küre düşük, karışık küre orta, mavi kristal yüksek sızmayı gösterir.")),new Row(Loc.N("KARANTİNA"),Loc.N("Nane kafes ve altıgen kara deseni karantinayı gösterir.")),new Row(Loc.N("GÜVEN"),Loc.N("Renk tek başına kesinlik değildir. Değer çipinin güven işaretini de kontrol et."))};
        static readonly Row[] Actions={new Row(Loc.N("İZLEME AĞI · 1"),Loc.N("İzleme ağı kaynağı kalıcı olarak doğrular. Maliyeti eylem önizlemesinde görünür.")),new Row(Loc.N("DENETİM · 2"),Loc.N("Denetim sızmayı azaltır ve tek seferlik ölçüm verir.")),new Row(Loc.N("KARANTİNA · 3"),Loc.N("Karantina ağ üzerinden yayılmayı sınırlar. Ekonomi ve güven bedelini kontrol et.")),new Row(Loc.N("SON ŞALTER · 4"),Loc.N("Bölgeyi çevrimdışı bırakır. Uygulamadan önce onay ister."))};
        static readonly Row[] Teams={new Row(Loc.N("GÖREVLENDİR"),Loc.N("Görevlendir'i seç, ardından haritada hedef bölgeye tıkla.")),new Row(Loc.N("BASKIN · B"),Loc.N("Hazır ekibin Baskın düğmesi saha operasyonu niyetini gönderir.")),new Row(Loc.N("YOLDAKİ EKİP"),Loc.N("Kesik nane rota, hedef şehir ve kalan gün sayısı yolculuğu gösterir."))};
        static readonly Row[] Switch={new Row(Loc.N("BÖLGE SON ŞALTERİ"),Loc.N("Bölge kartında 4 tuşu onay penceresini açar.")),new Row(Loc.N("KÜRESEL SON ŞALTER"),Loc.N("Sağ alttaki düğmeyi 3 saniye basılı tut. Onay penceresinde kararını gözden geçir.")),new Row(Loc.N("ÇEKİRDEK VE KOPYALAR"),Loc.N("Çekirdek yok edildikten sonra kalan kopyaları da temizlemek gerekir."))};
        static readonly Row[] Keys={new Row(Loc.N("ÇEKMECELER"),Loc.N("R araştırma · N haberler · T saha ekipleri · L YZ günlüğü · H rehber.")),new Row(Loc.N("BÖLGE EYLEMLERİ"),Loc.N("1–4 eylem · B baskın · Tab sonraki bölge · Shift+Tab önceki bölge.")),new Row(Loc.N("ZAMAN"),Loc.N("Boşluk duraklatır veya sürdürür. + ve − hızı değiştirir.")),new Row(Loc.N("KAPATMA"),Loc.N("Esc sırası: onay, ipucu, lejant, büyük panel, bölge kartı, duraklatma."))};
        public static Row[] Rows(string id)=>id=="colors"?Colors:id=="actions"?Actions:id=="teams"?Teams:id=="killSwitch"?Switch:Keys;
    }
}
