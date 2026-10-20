using System;
using Corrigible.Core;
using Corrigible.Ayna.Core;
namespace Corrigible.Ayna.Game
{
    public static class AynaText
    {
        static readonly string[] orders = { Loc.N("Yayıl"), Loc.N("Yürü"), Loc.N("Saldır"), Loc.N("Duvar ör"), Loc.N("Eğit"), Loc.N("Üret"), Loc.N("Yeni birlik"), Loc.N("Onar"), Loc.N("Gizlen"), Loc.N("Sahte yığınak"), Loc.N("Çekirdeği taşı"), Loc.N("Tekillik başlat"), Loc.N("Bekle") };
        static readonly string[] regions = { Loc.N("Kuzey Amerika"), Loc.N("Güney Amerika"), Loc.N("Batı Avrupa"), Loc.N("Doğu Avrupa"), Loc.N("Rusya"), Loc.N("Orta Doğu"), Loc.N("Afrika"), Loc.N("Güney Asya"), Loc.N("Çin"), Loc.N("Japonya / Kore"), Loc.N("Güneydoğu Asya"), Loc.N("Okyanusya") };
        static readonly string[] families = { Loc.N("Görü"), Loc.N("Hareket"), Loc.N("Enerji"), Loc.N("Ağ"), Loc.N("Dil") };
        static readonly string[] architectures = { Loc.N("Sürü Zihni"), Loc.N("Kale"), Loc.N("Hayalet") };
        static readonly string[] levels = { Loc.N("Kapalı"), Loc.N("Hafif"), Loc.N("Tam") };
        static readonly string[] phases = { Loc.N("Başlangıç"), Loc.N("Planla"), Loc.N("Aç"), Loc.N("Çatışma"), Loc.N("Kadro seçimi"), Loc.N("Savaş tahtası"), Loc.N("Savaş sonucu"), Loc.N("Döngü sonucu"), Loc.N("Oyun sonu") };
        static readonly string[] units = { Loc.N("Gözcü Dron"), Loc.N("Nişancı Dron"), Loc.N("Muhafız Robot"), Loc.N("Örümcek Robot"), Loc.N("EMP Dronu"), Loc.N("Aşırı Yük"), Loc.N("Sızma Birimi"), Loc.N("Röle"), Loc.N("Yem"), Loc.N("Hayalet Emir") };
        static readonly string[] endings = { Loc.N("Devam ediyor"), Loc.N("Son Şalter"), Loc.N("Silme"), Loc.N("Tekillik"), Loc.N("Hakimiyet"), Loc.N("Süre doldu") };
        static readonly string[] actions = { Loc.N("Yürü"), Loc.N("Saldır"), Loc.N("Yetenek"), Loc.N("Bekle") };
        public static readonly string[] Captions = { Loc.N("Başlat"), Loc.N("Devam et"), Loc.N("Kaydet"), Loc.N("Yükle"), Loc.N("Ana menü"), Loc.N("TR / EN"), Loc.N("Hafızayı sil"), Loc.N("Silmeyi onayla"), Loc.N("Genel bakış"), Loc.N("Gelişim"), Loc.N("Birlikler"), Loc.N("AYNA'nın modeli"), Loc.N("Günlük"), Loc.N("Emri ekle"), Loc.N("Son emri kaldır"), Loc.N("Kuyruğu temizle"), Loc.N("Planı kilitle"), Loc.N("Komuta et"), Loc.N("Otomatik çöz"), Loc.N("Kadro ile başlat"), Loc.N("Geri"), Loc.N("Geri al"), Loc.N("Turu bitir"), Loc.N("Geri çekil"), Loc.N("Önizle"), Loc.N("Mimari"), Loc.N("Başlangıç bölgesi"), Loc.N("Öğrenme"), Loc.N("Tohum"), Loc.N("Emir / bölge / miktar / birlik / aile / birim / onarım hedefi"), Loc.N("Birim / eylem / hedef birim"), Loc.N("Niyeti bildir"), Loc.N("Hamleyi uygula"), Loc.N("Yeni oyun") };
        public static string Caption(int key) => Loc.T(Captions[key]);
        public static string Order(OrderKind k) => Loc.T(orders[(int)k]);
        public static string Region(string id) => id == null ? Loc.T("genel") : Loc.T(regions[AynaWorldData.Region(id).Key - 1]);
        public static string Family(DataFamily k) => Loc.T(families[(int)k]);
        public static string Architecture(Architecture k) => Loc.T(architectures[(int)k]);
        public static string Level(LearningLevel k) => Loc.T(levels[(int)k]);
        public static string Phase(Presentation.AynaPhase k) => Loc.T(phases[(int)k]);
        public static string Unit(UnitKind k) => Loc.T(units[(int)k]);
        public static string End(EndReason k) => Loc.T(endings[(int)k]);
        public static string Action(BoardAction k) => Loc.T(actions[(int)k]);
        public static string Rejection(RejectReason reason) => reason == RejectReason.None ? Loc.T("Kabul edildi") : reason == RejectReason.LostCondition ? Loc.T("Koşul kaybedildi") : Loc.T("Emir reddedildi");
    }
}
