using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis aşama 9, parça 3 (aşama 9 tasarımı §7.6, §11.6): sunucu ödülünün masadaki akışı gerçek DeskGame'de, Play'de. Bölge
    /// komutlarla tamamlanır (<see cref="FacilityRewardPlayKit.CompleteRegion"/>), operasyon raporu iner ("ÖDÜLÜ SEÇ"), Enter raporu
    /// dosyalar ve ödül kâğıdı kapıdan gelir (Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A.0): kapının yanındaki belge çekmecesine
    /// iner, düşüş sesi ve kapının tek vuruşu (<see cref="SiginakAdmission.KnockDelivery"/>), kapı lambası yanar, EVRAK sessiz kalır; K8'den
    /// çekmece açılıp kâğıda tıklanınca ele gelir; 2 + Enter kazanımı seçer (<c>server_reward</c> kaydı), kâğıt çekmeceye konur ve şalterle
    /// gider. İkinci bölgede Esc kâğıdı çekmeceye geri koyar: görev fişi "ÖDÜL SEÇİLMEDİ", baskın emri reddedilir, çekmeceden yeniden
    /// alınır; kaydet ve kayıttan devam: kâğıt animasyonsuz ve vuruşsuz çekmecede (çekmece kapalı), rapor yeniden açılmaz.
    /// </summary>
    public sealed class FacilityServerRewardPlayTests
    {
        readonly List<(string status, string message)> telex = new List<(string, string)>();
        readonly List<ShelterEventSound> sounds = new List<ShelterEventSound>();
        System.Action<string, string> onTelex;
        System.Action<ShelterEventSound> onSound;
        SiginakServerReward listened;
        AdmissionSoundLog knocks;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Unlisten();
            telex.Clear();
            sounds.Clear();
            knocks?.Dispose();
            knocks = null;
            yield return ShiftPlayKit.End();
        }

        static Simulation Sim => BoardPlayKit.Sim;
        static SiginakServerReward Reward => BoardPlayKit.Find<SiginakServerReward>();
        static SiginakOperation Operation => BoardPlayKit.Find<SiginakOperation>();

        static void RequireWired()
        {
            var reward = Reward;
            Assert.IsNotNull(reward, "SiginakServerReward sahnede");
            Assert.AreSame(reward, ShiftPlayKit.Get<SiginakServerReward>(BoardPlayKit.Surface, "serverReward"), "LiberationDeskView.serverReward");
            Assert.AreSame(reward, ShiftPlayKit.Get<SiginakServerReward>(BoardPlayKit.Desk, "serverReward"), "SiginakDesk.serverReward");
            Assert.AreSame(reward, ShiftPlayKit.Get<SiginakServerReward>(Operation, "serverReward"), "SiginakOperation.serverReward");
            Assert.IsNotNull(reward.View, "ödül kâğıdı");
            Assert.AreNotSame(reward.View, BoardPlayKit.Find<SiginakShelterEvent>().View, "tutanaktan ayrı kâğıt");
            Assert.IsTrue(reward.View.RestAnchor.IsChildOf(ShiftPlayKit.Tray.transform), "kâğıdın çapası EVRAK tepsisinde");
            Assert.IsNotNull(reward.View.DoorAnchor, "kâğıdın kapı çapası bağlı");
            Assert.AreEqual("Sunucu ödülü kapıda", reward.View.DoorAnchor.name, "kâğıdın kapı çapası çekmecede");
            Assert.AreEqual("Tabla", reward.View.DoorAnchor.parent.name, "çapa çekmecenin tablasında");
            Assert.AreSame(AdmissionPlayKit.Admission, ShiftPlayKit.Get<SiginakAdmission>(reward, "admission"), "SiginakServerReward.admission");
        }

        /// <summary>Teleks satırları ve kâğıdın ses olayları dinlenir (kayıttan devamdan sonra yeniden).</summary>
        void Listen()
        {
            Unlisten();
            listened = Reward;
            onTelex = (s, m) => telex.Add((s, m));
            onSound = s => sounds.Add(s);
            listened.TelexLine += onTelex;
            listened.Sounded += onSound;
        }

        void Unlisten()
        {
            if (listened != null)
            {
                if (onTelex != null) listened.TelexLine -= onTelex;
                if (onSound != null) listened.Sounded -= onSound;
            }
            listened = null;
        }

        /// <summary>Kapının ses olayları (vuruş) dinlenir; kayıttan devamdan sonra yeni kapı bileşeni için yeniden.</summary>
        void ListenDoor()
        {
            knocks?.Dispose();
            knocks = new AdmissionSoundLog(AdmissionPlayKit.Admission);
        }

        /// <summary>
        /// Bölge tamamlanır; rapor iner ve "ÖDÜLÜ SEÇ" der; Enter dosyalar, kâğıt kapı panosuna iner (düşüş sesi, kapı vurur, kapı lambası,
        /// kamera masaya döner, EVRAK etiketi ödülü söylemez, masa açık); K8'den panodaki kâğıda tık onu ele getirir.
        /// </summary>
        IEnumerator RegionAndReport(string context)
        {
            string region = FacilityRewardPlayKit.CompleteRegion(Sim);
            Assert.AreEqual(region, Sim.Facility.PendingReward, context + ": ödül bekliyor");
            Assert.AreEqual("ÖDÜLÜ SEÇ", OperationReportBuilder.Build(Sim).Action, context + ": raporun düğmesi");
            yield return BoardPlayKit.WaitFor(() => Operation.ReportOpen, 8, context + ": operasyon raporu inmedi");
            yield return BoardPlayKit.WaitSeconds(1.2f);
            Assert.IsFalse(Reward.InHand || Reward.AtDoor || Reward.View.Visible, context + ": rapor dosyalanmadan kâğıt gelmez (" + Reward.View.State + ")");
            Assert.IsTrue(BoardPlayKit.Surface.InputBlocked, context + ": rapor açıkken masa kilitli");
            sounds.Clear();
            knocks.Clear();
            yield return BoardPlayKit.PressKey(Key.Enter);
            // Kâğıt ele değil kapı panosuna iner, haritanın BÖLGE ÖZGÜR dizisinden sonra; kamera kapıya dönmez (rapor masaya, dizi haritaya)
            bool revealFirst = BoardPlayKit.Surface.MapRevealPending;
            yield return BoardPlayKit.WaitFor(() => !Operation.ReportOpen, 3, context + ": rapor dosyalanmadı");
            Assert.IsTrue(revealFirst || BoardPlayKit.Surface.MapRevealPending || Reward.AtDoor, context + ": dizi sırada ya da kâğıt geldi");
            if (BoardPlayKit.Surface.MapRevealPending)
                Assert.IsFalse(Reward.AtDoor || Reward.View.Visible, context + ": dizi oynamadan kâğıt gelmez (" + Reward.View.State + ")");
            yield return ShiftPlayKit.WaitDoor(Reward.View, context + ": ödül kâğıdı", 6);
            Assert.IsFalse(BoardPlayKit.Surface.MapRevealPending, context + ": kâğıt dizi oynadıktan sonra geldi");
            Assert.IsFalse(Operation.ReportOpen, context + ": rapor dosyalandı");
            Assert.IsTrue(Reward.AtDoor, context + ": kâğıt kapıda");
            Assert.IsFalse(Reward.InHand, context + ": kâğıt ele gelmez");
            Assert.IsFalse(Reward.InTray, context + ": kâğıt tepsiye düşmez");
            Assert.Less(Vector3.Distance(Reward.View.transform.position, Reward.View.DoorAnchor.position), 0.005f,
                context + ": kâğıt kapı panosunun çapasında");
            CollectionAssert.Contains(sounds, ShelterEventSound.Drop, context + ": kâğıdın düşüş sesi");
            Assert.GreaterOrEqual(knocks.Count(AdmissionSound.Knock), 1, context + ": kapı vurur (" + knocks + ")");
            yield return BoardPlayKit.WaitFor(() => !ShiftPlayKit.Rig.InTransition, 3, context + ": kamera durulmadı");
            Assert.AreNotEqual(SiginakPose.Door, ShiftPlayKit.Rig.Current, context + ": kamera kapıya kendiliğinden dönmez");
            yield return BoardPlayKit.WaitFor(() => !BoardPlayKit.Surface.InputBlocked, 3, context + ": kâğıt kapıdayken masa açık");
            yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(AdmissionPlayKit.Admission.DeliveryWaiting, context + ": kapıda belge bekliyor");
            Assert.IsTrue(AdmissionPlayKit.Door.Knocking, context + ": kapı lambası belgeyi gösterir");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, context + ": kâğıt kapıdayken EVRAK etiketi ödülü söylemez");
            Assert.AreEqual(Loc.T(FacilityRewardText.DoorTitle), Reward.View.DoorTitle, context + ": panodaki kâğıdın etiketi");
            Assert.AreEqual(Reward.Last.HoverLine, Reward.View.DoorLine, context + ": panodaki kâğıdın satırı");
            yield return ShiftPlayKit.TakeFromDoor(Reward.View, context + ": ödül kâğıdı");
            Assert.IsTrue(Reward.InHand, context + ": panodaki kâğıt ele geldi (" + Reward.View.State + ")");
            Assert.IsFalse(Reward.AtDoor, context + ": kâğıt kapıdan alındı");
            Assert.IsTrue(BoardPlayKit.Surface.InputBlocked, context + ": kâğıt eldeyken masa kilitli");
            var d = Reward.Last;
            Assert.IsNotNull(d, context + ": kâğıt verisi");
            Assert.AreEqual(2, d.Rows.Count);
            StringAssert.StartsWith("TARİF · ", d.Rows[0].Name);
            StringAssert.StartsWith("KAZANIM · ", d.Rows[1].Name);
        }

        [UnityTest, Timeout(360000)]
        public IEnumerator Report_ThenPaper_KeysChoose_EscToDrawer_SaveLoad()
        {
            yield return BoardPlayKit.LoadDesk(tune: FacilityRewardPlayKit.Tune);
            RequireWired();
            Listen();
            ListenDoor();
            Assert.IsFalse(Reward.View.Visible, "başta kâğıt yok");

            // ---- 1. bölge: rapor → kâğıt kapıda → panodan ele → 2 + Enter (kazanım)
            yield return RegionAndReport("1. bölge");
            string region = Sim.Facility.PendingReward;
            var mark = ShiftPlayKit.Mark();
            yield return BoardPlayKit.PressKey(Key.Digit2);
            Assert.IsTrue(Reward.Last.Rows[1].Selected, "2 kazanımı işaretler");
            Assert.IsTrue(Reward.Last.ConfirmReady);
            ShiftPlayKit.AssertUnchanged(mark, "işaret komut yazmaz");
            yield return BoardPlayKit.PressKey(Key.Enter);
            var cmds = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, cmds.Count, "tek komut: " + ShiftPlayKit.Describe(cmds));
            Assert.AreEqual("server_reward", cmds[0].kind);
            Assert.AreEqual(region, cmds[0].a);
            Assert.AreEqual(FacilityGear.ChoicePerk, cmds[0].b);
            Assert.AreEqual("", Sim.Facility.PendingReward ?? "");
            CollectionAssert.Contains(Sim.Facility.Perks, FacilityGear.Reward(region).Perk);
            Assert.IsTrue(telex.Any(t => t.status == "EMİR UYGULANDI" && t.message.StartsWith("Kazanım: ")), "teleks Core sonucunu basar");
            yield return ShiftPlayKit.SendFromDrawer(Reward.View, "seçilen ödül");
            yield return BoardPlayKit.WaitFor(() => !Reward.View.Visible && !Reward.Busy, 4, "kâğıt gönderilip gizlenmedi");
            Assert.AreEqual(1, ShiftPlayKit.Since(mark.commands).Count, "şalter komut yazmaz");
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "seçimden sonra masa açık");

            // ---- 2. bölge: rapor → kâğıt çekmecede → çekmeceden ele → Esc çekmeceye; kilit; çekmeceden yeniden alınır
            yield return RegionAndReport("2. bölge");
            region = Sim.Facility.PendingReward;
            var pose = ShiftPlayKit.Rig.Current;
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return BoardPlayKit.WaitFor(() => Reward.View.State == ShelterEventPaperView.Where.Door && !Reward.View.Moving, 3,
                "Esc kâğıdı çekmeceye koymadı (" + Reward.View.State + ")");
            Assert.IsTrue(Reward.AtDoor, "Esc kâğıdı çekmeceye geri koyar");
            Assert.IsFalse(Reward.InTray, "EVRAK tepsisine inmez");
            Assert.AreEqual(pose, ShiftPlayKit.Rig.Current, "Esc'te kamera yerinde kalır");
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "kâğıt çekmecedeyken masa açık");
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, "EVRAK ışığı sönük");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, "EVRAK etiketi ödülü söylemez");
            Assert.IsTrue(AdmissionPlayKit.Admission.DeliveryWaiting, "kapıda belge bekliyor");
            Assert.AreEqual("ÖDÜL SEÇİLMEDİ", RaidPaperFacts.DisabledReason(Sim), "görev fişinin kısa notu");
            // Ekip sonraki vardiyada hazır: yeni cephe ve kapanış; baskın emri Core'da reddedilir
            FacilityRewardPlayKit.Close(Sim);
            Assert.IsTrue(Sim.SelectLiberationRegion(Sim.LiberationFrontier.First()).Ok);
            ShiftPlayKit.RaiseChanged();
            yield return BoardPlayKit.Frames(2);
            var next = Sim.NextLiberationSector(Sim.S.Liberation.CurrentRegion);
            mark = ShiftPlayKit.Mark();
            var launched = BoardPlayKit.Surface.LaunchSector(next.Id);
            Assert.IsNotNull(launched, "masa açık: emir denenir");
            Assert.IsFalse(launched.Ok, "bekleyen ödül baskın emrini tutar");
            Assert.AreEqual("Önce sunucu ödülünü seç: tarif ya da kazanım.", launched.Msg);
            ShiftPlayKit.AssertUnchanged(mark, "reddedilen emir");
            Assert.IsTrue(Reward.AtDoor, "kâğıt hâlâ çekmecede");
            // EVRAK tıkı kâğıdı getirmez; çekmeceden yeniden alınır ve Esc yine çekmeceye koyar
            Assert.IsFalse(Reward.TakeFromTray(), "EVRAK tıkı çekmecedeki kâğıdı getirmez");
            yield return ShiftPlayKit.TakeFromDoor(Reward.View, "çekmeceden yeniden al");
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return BoardPlayKit.WaitFor(() => Reward.View.State == ShelterEventPaperView.Where.Door && !Reward.View.Moving, 3, "ikinci Esc");

            // ---- kaydet ve kayıttan devam: kâğıt animasyonsuz ve vuruşsuz çekmecede (çekmece kapalı), rapor açılmaz
            Sim.Record.day = Sim.S.Day;
            SaveStore.Write(Sim.Record);
            int commands = Sim.Record.commands.Count;
            yield return ShiftPlayKit.Continue();
            RequireWired();
            Assert.AreEqual(commands, Sim.Record.commands.Count, "yükleme komut yazmaz");
            Assert.AreEqual(region, Sim.Facility.PendingReward, "kayıttan: ödül bekliyor");
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, Reward.View.State, "kayıttan devamda kâğıt çekmecede");
            Assert.IsFalse(ShiftPlayKit.Drawer.IsOpen, "kayıttan devamda çekmece kapalı");
            Assert.IsTrue(Reward.AtDoor, "kayıttan devam: kâğıt kapıda");
            Assert.IsFalse(Reward.InTray, "kayıttan devamda kâğıt tepside değil (konum sunum durumu, kayda girmez)");
            Assert.IsFalse(Reward.View.Moving, "kayıttan devamda animasyon yok");
            Assert.IsFalse(AdmissionPlayKit.Door.KnockPlaying, "yüklemede vuruş yok");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, "kayıttan devam: EVRAK etiketi ödülü söylemez");
            Assert.IsFalse(Operation.ReportOpen, "rapor yeniden açılmaz");
            // Tarif seçimi sunumun karar yolundan (kâğıt kapıdayken de): defterde yeni sayfa
            Listen();
            int pages = Sim.BoardRecipes.Count;
            Assert.IsTrue(Reward.Decide(0), "karar: tarif");
            Assert.AreEqual(pages + 1, Sim.BoardRecipes.Count, "defterde yeni sayfa");
            Assert.IsTrue(telex.Any(t => t.message.StartsWith("Defterde yeni sayfa: TARİF ")), "teleksin ikinci satırı");
            yield return ShiftPlayKit.SendFromDrawer(Reward.View, "tarif");
            yield return BoardPlayKit.WaitFor(() => !Reward.View.Visible, 4, "kâğıt gizlenmedi");
        }

        /// <summary>
        /// Son ekran (K29, inceleme bulgusu 10): bekleyen ödülün kâğıdı çekmecedeyken ve eldeyken oyun biterse (<c>S.Ending</c>) bileşen
        /// kâğıdı gizler, çekmecenin lambası söner, EVRAK sessiz kalır; komut yazılmaz. Raporsuz gelen kâğıt çekmeceye iner (kapı bir kez
        /// vurur, EVRAK sessiz); çekmeceden alınıp Esc ile geri konur. Kurulum: bekleyen ödül ve son elle yazılır (kayda girmez).
        /// </summary>
        [UnityTest, Timeout(120000)]
        public IEnumerator Ending_HidesPaper_TrayLightAndLabelOff()
        {
            yield return BoardPlayKit.LoadDesk(tune: FacilityRewardPlayKit.Tune);
            RequireWired();
            var tray = ShiftPlayKit.Tray;
            ListenDoor();
            // Raporsuz gelen kâğıt çekmeceye iner: kapı bir kez vurur, EVRAK sessiz
            Sim.Facility.PendingReward = "sa";
            ShiftPlayKit.RaiseChanged();
            yield return ShiftPlayKit.WaitDoor(Reward.View, "ödül kâğıdı", 4);
            Assert.IsTrue(Reward.AtDoor, "raporsuz gelen kâğıt kapıda");
            Assert.AreEqual(1, knocks.Count(AdmissionSound.Knock), "kapı bir kez vurur (" + knocks + ")");
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(tray.AttentionLit, "kâğıt kapıdayken EVRAK ışığı sönük");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, "kâğıt kapıdayken EVRAK etiketi ödülü söylemez");
            Assert.IsTrue(AdmissionPlayKit.Door.Knocking, "kapı lambası belgeyi gösterir");
            // Çekmeceden ele, Esc çekmeceye; EVRAK sessiz, çekmecenin lambası amber
            yield return ShiftPlayKit.TakeFromDoor(Reward.View, "ödül kâğıdı");
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return BoardPlayKit.WaitFor(() => Reward.View.State == ShelterEventPaperView.Where.Door && !Reward.View.Moving, 3,
                "Esc kâğıdı çekmeceye koymadı (" + Reward.View.State + ")");
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(tray.AttentionLit, "EVRAK ışığı sönük");
            Assert.AreEqual(PassDrawer.Lamp.Amber, ShiftPlayKit.Drawer.LampState, "çekmecenin lambası belgeyi gösterir");
            var mark = ShiftPlayKit.Mark();
            Sim.S.Ending = "silme";
            yield return BoardPlayKit.Frames(3);
            Assert.IsFalse(FacilityRewardText.Pending(Sim), "son ekranda bekleyen ödül yok");
            Assert.IsFalse(Reward.View.Visible, "son ekranda kâğıt gizli (" + Reward.View.State + ")");
            Assert.IsNull(Reward.Last, "kâğıt verisi yok");
            Assert.IsFalse(Reward.InTray);
            Assert.IsFalse(tray.AttentionLit, "EVRAK ışığı sönük");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, "EVRAK etiketi tepsinin kendisi");
            Assert.AreEqual(PassDrawer.Lamp.Off, ShiftPlayKit.Drawer.LampState, "çekmecenin lambası söndü");
            ShiftPlayKit.AssertUnchanged(mark, "son ekran komut yazmaz");

            // Eldeyken: oyun geri açılır (kurulum), kâğıt ele gelir, son yazılınca gizlenir ve masa kilidi kalkar
            Sim.S.Ending = null;
            Sim.Facility.PendingReward = "me";
            ShiftPlayKit.RaiseChanged();
            yield return ShiftPlayKit.WaitDoor(Reward.View, "ikinci ödül kâğıdı", 4);
            yield return ShiftPlayKit.TakeFromDoor(Reward.View, "ikinci ödül kâğıdı");
            Assert.IsTrue(Reward.InHand, "çekmecedeki kâğıt ele geldi");
            Sim.S.Ending = "silme";
            yield return BoardPlayKit.Frames(3);
            Assert.IsFalse(Reward.View.Visible, "eldeki kâğıt son ekranda gizlenir (" + Reward.View.State + ")");
            Assert.IsFalse(Reward.Busy, "masa kilidi kalktı");
            Assert.IsFalse(tray.AttentionLit);
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride);
            Sim.S.Ending = null;
        }
    }
}
