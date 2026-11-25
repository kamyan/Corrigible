using System;
using UnityEngine;

namespace Corrigible.Desk
{
    public interface IRaidDataSource { RaidViewModel Current { get; } event Action<RaidViewModel> Changed; }
    public interface IRaidCommands
    {
        void SelectUnit(char letter); void ClearSelection();
        void HoverTile(Vector2Int tile); void ClearHover();
        void MoveSelectedTo(Vector2Int tile); void ArmAbility(); void UseAbilityOn(Vector2Int tile);
        void Undo(); void EndTurn(); void CancelPending(); void SpeedUpMachines(bool on);
        void RequestRetreat(); void ToggleLog(); void ToggleRules(); void ContinueToReport();
        /// <summary>Tesis hamlesi (aşama 7): E sıradaki kullanılabilir hamleyi kurar, sonuncudan sonra kip kapanır; kart satırına tık
        /// o hamleyi kurar (kurulu satıra yeniden tık kapatır). Örnek tahtada etkisiz.</summary>
        void ArmAction(); void ChooseAction(int index);
        /// <summary>Tesis aşama 9: kartsız ya da dördüncü karttaki uzman olmayan birimi (köpek, tutsak) birim kimliğiyle seçer; aynı
        /// köpek seçiliyken çağrılırsa tahtadaki sıradaki köpeğe geçer. Örnek tahtada etkisiz. Varsayılan gövde: eski uygulayıcılar
        /// (test sahteleri) değişmeden derlenir.</summary>
        void SelectUnitId(int id) { }
        /// <summary>Tesis aşama 9: R seçili uzmanın ekipman eylemini kurar, kullanılabilir eylemler arasında döner (örme seti: tel →
        /// kalas → kapalı); kart satırına tık aynısını yapar. E ve altı hamle değişmez. Örnek tahtada etkisiz.</summary>
        void ArmGear() { }
        void ChooseGear(int index) { }
    }
    [Serializable] public sealed class RaidTeamModel
    {
        public char Letter; public string Name, Thumb; public int Hp, Max, MoveRange, Power; public string Equipment;
        /// <summary>Tesis aşama 9: uzman olmayan birimin kimliği (dördüncü kart: köpek; 0 ise harfle seçilen uzman) ve köpek kartı mı
        /// (istatistik satırında GÜÇ yok, hak satırı HAREKET + KOMUT). Örnek tahtada 0 / false.</summary>
        public int UnitId; public bool Dog;
        /// <summary>Out: kaçışta çıkıştan tahliye edildi; Left: süre dolarken tahtada kaldı (Down da işaretli).</summary>
        public bool Selected, Move, Ability, Armed, Down, Warn, Out, Left;
        /// <summary>Zırh plakası (ilk hasarı karşılar); yedek uzman (acemi); baskına çıkmadı (sığınakta yaralı).</summary>
        public int Armor; public bool Rookie, Absent;
        /// <summary>Kilit niyetinin hedefindeyse “KİLİT TEHDİDİ · #n”; kartın hak satırı yerine kırmızı (Revizyon 2 · S6c).</summary>
        public string Threat;
        /// <summary>Tesis odasında kapıda bekliyor (tahtadan kalktı; kart “✓ KAPIDA”, tıklanmaz). Örnek tahtada hep false.</summary>
        public bool AtGate;
        /// <summary>Tesis aşama 12 (Katar): kapıdaki Teknisyen AYIR verebilir (kart tıklanır, hak satırı “AYIR · Q”). Öbür durumlarda false.</summary>
        public bool Detach;
        /// <summary>Önceki bir tesis odasında düştüyse “DÜŞTÜ · PAFTA n” (Down ile); null ise kart bugünkü “DÜŞTÜ”yü yazar. Örnek
        /// tahtada hep null.</summary>
        public string DownText;
    }
    [Serializable] public sealed class RaidTargetModel
    {
        public string Name, Thumb; public int Hp, Max; public bool Armor, Broken;
        /// <summary>Paftadaki kare adı (“F4”); görev çipinde adın yanında (Revizyon 2 · S6).</summary>
        public string Cell;
        /// <summary>Kırık satırın damgası (tesis donanımı: KIRILDI, vana KAPANDI); null ise HUD bugünkü damgayı seçer. Max 0 olan satır
        /// (tesis odasının geçidi) can taşımaz. Örnek tahtada Stamp hep null, Max hep &gt; 0.</summary>
        public string Stamp;
    }
    [Serializable] public sealed class RaidCardRow
    { public string Icon, Text, Detail; public bool Good, Used, Armed; }
    [Serializable] public sealed class RaidInspectorModel
    {
        public string Thumb, Title; public int Hp, Max;
        /// <summary>Başlığın altındaki gri ikinci satır (mekânın adı: duvarın, kanalın, molozun bu binadaki hâli); boşsa yok ve
        /// başlık yerinde kalır.</summary>
        public string Subtitle;
        /// <summary>Win: hamle baskını kazandırır (son uzman çıkar); Escape: hamle kaçışı başlatır (kasa açılır, son hedef kırılır).</summary>
        public bool Win, Escape, IsLootMission; public RaidCardRow[] Rows = Array.Empty<RaidCardRow>();
    }
    [Serializable] public sealed class RaidQueueItem { public int Order; public string State; }
    [Serializable] public sealed class RaidLogModel { public string Text,Kind; }
    /// <summary>Sonuç kartının satırı (Revizyon 2 · S7): ad solda, değer sağda; iyi sonuç yeşil.</summary>
    [Serializable] public sealed class RaidResultRow { public string Label,Value; public bool Good; }
    [Serializable] public sealed class RaidUnusedModel { public char Letter; public string Name; public bool Move,Ability; public int ThreatOrder; }
    [Serializable] public sealed class RaidViewModel
    {
        public bool LastTurn,Finished,Success,EventVisible,IsLootMission;
        /// <summary>Baskının odası, büyük harfle (görev çipinin başında: “UPS ODASI · İKMAL”); harekât dışında boş.</summary>
        public string Place;
        /// <summary>R11 anteti: sunum kimliği, gerçek bina/oda/bölge ve kampanyanın sektör/vardiya numarası.</summary>
        public int BlueprintSeed, BlueprintSheet, BlueprintShift;
        /// <summary>Tesis baskınında kanadın numarası (antet “KANAT 01 · PAFTA 2/3”) ve dönüş odası mı (“· DÖNÜŞ”). Örnek tahtada
        /// 0 / false: antet bugünkü “OZALİT KOPYA · PAFTA n/3”.</summary>
        public int BlueprintWing; public bool BlueprintReturn;
        /// <summary>Tesis aşama 11: üç odalı savunma (antet “KANAT 01 · PAFTA 1/3 · SAVUNMA”, görev çipi “· SAVUNMA”). Saldırıda ve örnek
        /// tahtada false: bugünkü görünüm.</summary>
        public bool BlueprintDefense, Defense;
        /// <summary>Tesis aşama 11 (savunma): herkes kapıdayken EMRİ İLET'in yazısı ve alt satırı (“İÇERİ ÇEKİL” · “güven −1”; ilk basış onay
        /// sorar). Null ise bugünkü GEÇ · GERİ ALINMAZ (onaysız).</summary>
        public string PassLabel, PassNote;
        /// <summary>Tesis aşama 11 (savunma): geri çekilme onayının metni ve kurallar kartının kazanma satırının yerine savunma bölümü. Boşsa
        /// bugünkü metinler.</summary>
        public string RetreatBody, RulesDefense;
        public string BlueprintBuilding, BlueprintRoom, BlueprintRegion, BlueprintNote, CrateContents, FatalWarning;
        /// <summary>Kaçış sürüyor (kasa alındı ya da hedef kırıldı): bütün ekip girişten çıkmalı, geri çekilme yok. CargoLost:
        /// malzemeyi taşıyan Teknisyen düştü.</summary>
        public bool Escaping, CargoLost, CanRetreat;
        /// <summary>Sonuç kartı (S7): başlık yerindeki tek damga, “TUR 6/8”, yer satırı (“Doğu Avrupa · 02 Dosya katı”), üç satır.</summary>
        public string EventLine,ResultTitle,ResultTurn,ResultPlace;
        public RaidResultRow[] ResultRows=Array.Empty<RaidResultRow>();
        public Vector3 Spot;
        public RaidQueueItem[] Queue=Array.Empty<RaidQueueItem>();
        public RaidLogModel[] Log=Array.Empty<RaidLogModel>();
        public RaidUnusedModel[] Unused=Array.Empty<RaidUnusedModel>();
        public int Turn, Turns, Civilians, CivilianBroken;
        /// <summary>Bölge olayının tur şeridi: [tur] o turun sonunda işleyen olay (yoksa null; olaysız baskında null dizi).</summary>
        public string[] TurnEvents;
        /// <summary>Sivil güven: bu baskında kırılanlar düşülmüş hâli ve tavanı (tavan 0: harekâtsız önizleme, etiket SİVİL kalır).</summary>
        public int Trust, TrustMax;
        public bool Locked, AbilityArmed, CanUndo, EndConfirm, RetreatArmed, FastMachines, Paused, LogUnread;
        /// <summary>Tesis odasında ayaktaki herkes kapıda: EMRİ İLET “GEÇ” olur (altında “GERİ ALINMAZ”). Örnek tahtada hep false.</summary>
        public bool CanPass;
        /// <summary>“YAYINDA” lambası (tesis aşama 5, belge §7.3): emir telsizle gitti (EMRİ İLET ya da GEÇ), gözcünün ilk rapor satırı
        /// henüz gelmedi. Görünüm lambayı en az <c>radioDesk.lamp.holdS</c> yanık tutar; kaynak bekletmez.</summary>
        public bool OnAir;
        /// <summary>Makaralı teyp: bu baskında girilip raporu dosyalanan ve planı bilinen (Core <c>FacilityKnown</c>) paftalar, kayda
        /// geçiş sırasıyla (“KAYIT · PAFTA n” etiketi sonuncuyu yazar). Örnek tahtada boş.</summary>
        public int[] TapeSheets = Array.Empty<int>();
        /// <summary>Onay notunun başlığı (tesis odasında son tur: “SON TUR · GERİ ÇEKİLME”); boşsa not bugünkü başlığı seçer.</summary>
        public string ConfirmTitle;
        public string Phase, OpenPanel, AbilityTip, AbilityLabel, Status;
        /// <summary>Görev çipi (Revizyon 2 · S6): bölge özelliğinin işareti (sprite adı; yoksa null) ve amaç satırı.</summary>
        public string TraitGlyph, Goal;
        /// <summary>Kablo kanalının bu binadaki adı (bina kiti, BASKIN-MEKANLARI Katman 6; çevrilmiş): kurallar kartının lejantında
        /// "Elektrikli kanal · bara kanalı". Varsayılan kitte (eğitim, örnek baskın) null, lejant bugünkü gibi.</summary>
        public string CableName;
        /// <summary>Okunmamış telsiz satırı sayısı (TELSİZ düğmesindeki kırmızı yuvarlak).</summary>
        public int LogUnreadCount;
        /// <summary>Makine evresi: oynanan hamle ve toplam (kilitli kâğıtta “2/3 hamle”).</summary>
        public int MachineMove, MachineMoves;
        public RaidTeamModel[] Team = Array.Empty<RaidTeamModel>();
        public RaidTargetModel[] Targets = Array.Empty<RaidTargetModel>();
        public RaidTeamModel UnitCard; public RaidInspectorModel HoverCard;
        public RaidCardRow[] UnitRows = Array.Empty<RaidCardRow>();
        /// <summary>Tesis odası (aşama 7): seçili uzmanın bu odada anlamlı hamleleri (kartta Hareket / yetenek / plaka satırlarının
        /// altında; kullanılamayan soluk, ayrıntısı nedeni), E fişinin satırı (kip kapalıyken E'nin kuracağı, açıkken kurulu satır; −1
        /// yok) ve hamle kipi açık mı. Örnek tahtada boş, −1, false.</summary>
        public RaidCardRow[] ActionRows = Array.Empty<RaidCardRow>();
        public int ActionKey = -1;
        public bool ActionArmed;
        /// <summary>Tesis aşama 9: seçili uzmanın ekipman satırı (0 ya da 1 satır; kartta plakadan sonra, hamlelerden önce), R fişinin
        /// satırı (0 kullanılabilir ya da kip açıkken; −1 yok) ve ekipman kipi açık mı. Örnek tahtada ve ekipmansız uzmanda boş, −1, false.</summary>
        public RaidCardRow[] GearRows = Array.Empty<RaidCardRow>();
        public int GearKey = -1;
        public bool GearArmed;
        /// <summary>Tesis odası: yük yerde (taşıyıcı düştü; kareye giren ilk ekip üyesi alır). Görev çipi damgası YERDE. Örnek tahtada false.</summary>
        public bool CargoOnFloor;
        public string[] Effects = Array.Empty<string>();
        /// <summary>Yetenek alt düğmelerinin anlamı (Effects ile aynı sıra): “good” görev (yeşil), “bad” robota vurur (kırmızı), “plain”.</summary>
        public string[] EffectTones = Array.Empty<string>();
        public Vector2Int[] CollisionTiles = Array.Empty<Vector2Int>();
        public Vector2Int[] Moves = Array.Empty<Vector2Int>(), AbilityTargets = Array.Empty<Vector2Int>();
        /// <summary>Fare yürüyüş karesindeyken seçili birimin oraya yolu (başlangıç ve varış dahil; Core'un <c>Baskin.MovePath</c>'i);
        /// yoksa boş. Tahta bu yolu çizer, parça bu yoldan yürür.</summary>
        public Vector2Int[] MovePath = Array.Empty<Vector2Int>();
    }
}
