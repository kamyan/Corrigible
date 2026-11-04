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
    }
    [Serializable] public sealed class RaidTeamModel
    {
        public char Letter; public string Name, Thumb; public int Hp, Max, MoveRange;
        /// <summary>Out: kaçışta çıkıştan tahliye edildi; Left: süre dolarken tahtada kaldı (Down da işaretli).</summary>
        public bool Selected, Move, Ability, Armed, Down, Warn, Out, Left;
        /// <summary>Zırh plakası (ilk hasarı karşılar); yedek uzman (acemi); baskına çıkmadı (sığınakta yaralı).</summary>
        public int Armor; public bool Rookie, Absent;
        /// <summary>Kilit niyetinin hedefindeyse “KİLİT TEHDİDİ · #n”; kartın hak satırı yerine kırmızı (Revizyon 2 · S6c).</summary>
        public string Threat;
    }
    [Serializable] public sealed class RaidTargetModel
    {
        public string Name, Thumb; public int Hp, Max; public bool Armor, Broken;
        /// <summary>Paftadaki kare adı (“F4”); görev çipinde adın yanında (Revizyon 2 · S6).</summary>
        public string Cell;
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
        public string[] Effects = Array.Empty<string>();
        /// <summary>Yetenek alt düğmelerinin anlamı (Effects ile aynı sıra): “good” görev (yeşil), “bad” robota vurur (kırmızı), “plain”.</summary>
        public string[] EffectTones = Array.Empty<string>();
        public Vector2Int[] CollisionTiles = Array.Empty<Vector2Int>();
        public Vector2Int[] Moves = Array.Empty<Vector2Int>(), AbilityTargets = Array.Empty<Vector2Int>();
    }
}
