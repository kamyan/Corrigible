using System.Collections;
using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 11, parça P4b: savunmanın PlayMode sürücüsü (aşama 11 tasarımı §8.10). <see cref="Tune"/> =
    /// <see cref="FacilityRewardPlayKit.Tune"/> + alarm her vardiya eşik kadar (ilk kapanış kuşatma kurar; bellek dengesi, asset değişmez).
    /// <see cref="Breach"/> ilk kanadın giriş odasında gerçek bir duvar yıkımı yapar (lokum: Core <c>DoAction</c>; iz raporla
    /// <c>Facility.Marks</c>'a girer). <see cref="FallBack"/> gerçek EMRİ İLET (Enter) ile içeri çekilir: ilk basış onay, ikinci basış
    /// GEÇ. <see cref="HoldToEnd"/> süre dolana kadar tutar, <see cref="BreakRelay"/> hedef odasında saha rölesini Teknisyen'in sabotajıyla
    /// kırar ("her kaynak kırar"). Işınlama ve makinelerin kaldırılması test kurulumudur; komutlar ve raporlar gerçek yoldan gider.
    /// </summary>
    public static class FacilityDefensePlayKit
    {
        /// <summary>Tesis açık, olaylar kapalı, alarm her vardiya eşik kadar.</summary>
        public static void Tune(Balance b)
        {
            FacilityRewardPlayKit.Tune(b);
            b.liberationAlarmPerShift = b.liberationAlarmThreshold;
        }

        /// <summary>Son <see cref="Breach"/>'in yıktığı duvarın odası ve karesi (yoksa null).</summary>
        public static string BreachRoom { get; private set; }
        public static Baskin.Pt? BreachAt { get; private set; }

        /// <summary>
        /// <see cref="FacilityRewardPlayKit.WinRaid"/>'in oda kancası: kanadın giriş odasına gidişte, makineler kalkmadan önce, ayaktaki ilk
        /// uzmana bir hak lokum verilir (test kurulumu), bitişiğinde duvar olan boş zemine konur ve duvar Core'un hamlesiyle yıkılır.
        /// </summary>
        public static void Breach(Baskin.BState B)
        {
            if (B.Room == null || B.Room.Index != 1 || B.Escape || BreachAt != null && BreachRoom == B.Room.Id) return;
            foreach (var e in B.Units.Where(u => u.Side == 'e' && !u.Dead)) { e.Dead = true; e.Intent = null; }
            var u = B.Units.Where(v => Baskin.IsSpecialist(v) && Baskin.OnBoard(v)).OrderBy(v => v.Slot).First();
            u.Gear = FacilityGear.Charge; u.GearLeft = 1;
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    if (c.T != "floor" || c.O != null || c.Door != 0 || c.Vent != 0 || Baskin.IsGateTile(B, x, y)) continue;
                    var other = Baskin.UnitAt(B, x, y);
                    if (other != null && other != u) continue;
                    u.X = x; u.Y = y;
                    var targets = Baskin.ActionTargets(B, u, Baskin.ActCharge).Where(t =>
                    {
                        var w = Baskin.CellAt(B, t.X, t.Y);
                        return w.T == "wall" && !w.Shutter && !w.Crate;
                    }).ToList();
                    if (targets.Count == 0) continue;
                    var at = targets[0];
                    Assert.IsTrue(Baskin.DoAction(B, u.Id, Baskin.ActCharge, at.X, at.Y), "lokum: " + B.Room.Id);
                    Assert.AreEqual("floor", Baskin.CellAt(B, at.X, at.Y).T, "duvar yıkıldı");
                    BreachRoom = B.Room.Id;
                    BreachAt = at;
                    return;
                }
            Assert.Fail("giriş odasında lokumla yıkılacak duvar yok: " + B.Room.Id);
        }

        public static void ResetBreach() { BreachRoom = null; BreachAt = null; }

        static T Get<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

        /// <summary>Baskın HUD'unun EMRİ İLET yazısı (İÇERİ ÇEKİL / EMİN MİSİN?).</summary>
        public static string EndLabel => Get<TMPro.TMP_Text>(Object.FindFirstObjectByType<RaidHudView>(), "endLabel").text;

        /// <summary>Baskın anteti (KANAT nn · PAFTA n/3 · SAVUNMA).</summary>
        public static string Heading => Get<TMPro.TMP_Text>(Object.FindFirstObjectByType<RaidBlueprintView>(), "heading").text;

        /// <summary>Tur şeridinde açık hücre sayısı.</summary>
        public static int StripCells => Get<UnityEngine.UI.Image[]>(Object.FindFirstObjectByType<RaidHudView>(), "turnFills").Count(f => f.transform.parent.gameObject.activeSelf);

        /// <summary>
        /// İçeri çekilme: makineler kalkar, ayaktakiler sırayla kapıya girer (gerçek EnterGate), EMRİ İLET (Enter) "İÇERİ ÇEKİL": ilk basış
        /// onayı açar (rapor yok), ikinci basış odayı terk eder (raid_room) ve sıradaki pafta çevrilir.
        /// </summary>
        public static IEnumerator FallBack(LiveRaidDataSource src, int roomTrust)
        {
            FacilityPlayKit.Calm(src);
            foreach (var u in FacilityPlayKit.Standing(src.State)) yield return FacilityPlayKit.ToGate(src, u);
            Assert.IsTrue(src.Current.CanPass, "herkes kapıda: İÇERİ ÇEKİL");
            Assert.AreEqual(FacilityDefenseRaidText.PassLabel, EndLabel, "EMRİ İLET İÇERİ ÇEKİL olur");
            int filed = BoardPlayKit.Sim.S.Raid.Reports.Count, room = src.State.Room.Index;
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(src.Current.EndConfirm, "ilk basış onay sorar");
            Assert.AreEqual(FacilityDefenseRaidText.PassTitle, src.Current.ConfirmTitle);
            StringAssert.StartsWith(Loc.F("Bu oda terk edilir: güven −{0}, izler silinir.", roomTrust), src.Current.FatalWarning);
            Assert.AreEqual(Loc.T("EMİN MİSİN?"), EndLabel);
            Assert.AreEqual(filed, BoardPlayKit.Sim.S.Raid.Reports.Count, "ilk basış odayı terk etmez");
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => BoardPlayKit.Sim.S.Raid.Reports.Count == filed + 1, 4, "ikinci basış odayı terk etmedi");
            yield return FacilityPlayKit.Settle(src, "pafta çevirme");
            Assert.AreEqual(room + 1, src.State.Room.Index, "sıradaki pafta");
        }

        /// <summary>Süre dolana kadar: her turda makineler kalkar ve EMRİ İLET (son turda onaylı). Sonuç kartı gelir.</summary>
        public static IEnumerator HoldToEnd(LiveRaidDataSource src)
        {
            for (int guard = 0; guard < 14 && !src.Current.Finished; guard++)
            {
                FacilityPlayKit.Calm(src);
                yield return FacilityPlayKit.EndTurnConfirmed(src);
            }
            yield return BoardPlayKit.WaitFor(() => src.Current.Finished, 6, "sonuç kartı gelmedi");
        }

        /// <summary>Hedef odasında saha rölesi Teknisyen'in sabotajıyla kırılana kadar (her tur bir vuruş; makineler kalkık).</summary>
        public static IEnumerator BreakRelay(LiveRaidDataSource src)
        {
            for (int guard = 0; guard < 6 && !src.Current.Finished; guard++)
            {
                FacilityPlayKit.Calm(src);
                yield return FacilityPlayKit.Settle(src, "röle öncesi");
                var B = src.State;
                var relay = FacilityDefenseRaidText.RelayAt(B);
                Assert.IsTrue(relay.HasValue, "hedef odasında röle");
                var tek = FacilityPlayKit.Team(src, "tek");
                var spot = FacilityPlayKit.FreeNeighbor(B, relay.Value, true);
                FacilityPlayKit.Put(tek, spot.X, spot.Y);
                src.ClearHover();
                src.Select('T');
                src.Arm();
                Assert.IsTrue(src.Armed, "sabotaj kurulamadı");
                src.Use(new Vector2Int(relay.Value.X, relay.Value.Y));
                Assert.IsTrue(tek.Acted, "sabotaj röleye");
                yield return FacilityPlayKit.Settle(src, "sabotaj");
                if (B.Phase != "player") break;
                yield return FacilityPlayKit.EndTurnConfirmed(src);
            }
            yield return BoardPlayKit.WaitFor(() => src.Current.Finished, 6, "sonuç kartı gelmedi");
        }

        /// <summary>Sonuç kartından masaya (kart kilidi geçince Continue): raidres, sahne kapanır, operasyon raporu iner.</summary>
        public static IEnumerator Continue(LiveRaidDataSource src)
        {
            yield return BoardPlayKit.WaitSeconds(1f);
            src.Continue();
            yield return BoardPlayKit.WaitFor(() => BoardPlayKit.Sim.S.Raid == null, 4, "raidres yazılmadı");
            yield return BoardPlayKit.WaitFor(() => !Corrigible.Game.Raid.RaidLauncher.IsRaidLoaded, 6, "baskın sahnesi kapanmadı");
        }
    }
}
