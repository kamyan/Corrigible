using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using K = Corrigible.Desk.Tests.AdmissionPlayKit;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Sığınak olaylarının masadaki sunumu (KOS aşama 4; tasarım <c>Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md</c> §7; tutanak kapıdan gelir,
    /// <c>Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md</c> §A.0: kapının yanındaki çift taraflı belge çekmecesine konur, kapı bir kez vurur;
    /// çekmece açılıp alınır, bırakılınca çekmeceye döner, karardan sonra çekmeceye konur ve şalterle gönderilir) gerçek DeskGame'de, Play'de. Düzen <see cref="AdmissionPlayKit"/> ile aynı: kayıtlar test klasöründe, durumlara kayıt
    /// fikstürüyle ulaşılır (<see cref="AdmissionPlayKit.FixtureGame"/> + KAYITTAN DEVAM; fikstür kendi dengesini taşır, olaylar orada
    /// açıktır, BalanceConfig'ten bağımsız), tıklar gerçek ışınla (<see cref="AdmissionPlayKit.ClickHit"/>), tuşlar sanal klavyeyle.
    /// <list type="bullet">
    /// <item><b>Konvoy fikstürü</b> (<see cref="ConvoyOnly"/>): öteki olağan olaylar susturulur (sınır 0), <c>convoyTrustAt</c> 0: konvoy
    /// ilk uygun kapanışta (aralık dolunca) belirlenimci açılır. <c>convoyAlarm</c> büyükse "kabul" kapalıdır.</item>
    /// <item><b>İz fikstürü</b> (<see cref="IzChain"/>): her aday sızmacı, gecikme 1; ilk grup geri çevrilir, ikincinin ilk adayı alınır,
    /// iz kâğıdı açılana dek kapanış (yalnız komut).</item>
    /// <item><b>14 tür</b> hata ayıklama kurulumuyla (<c>ShelterEvents.Pending</c>; komut değildir, kayda girmez, RNG çekmez).</item>
    /// </list>
    /// </summary>
    public sealed class ShelterEventPlayTests
    {
        const string TurkishOnly = "çğıöşüÇĞİÖŞÜ";
        static readonly string[] Personless = { "erzak_azaliyor", "karartma", "takas", "cagri", "konvoy" };

        readonly List<(string status, string message)> telex = new List<(string, string)>();
        readonly List<ShelterEventSound> sounds = new List<ShelterEventSound>();
        Action<string, string> onTelex;
        Action<ShelterEventSound> onSound;
        SiginakShelterEvent listened;
        /// <summary>Kapının ses olayları (vuruş: <see cref="AdmissionSound.Knock"/>); <see cref="Listen"/> kurar.</summary>
        AdmissionSoundLog doorSounds;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Unlisten();
            yield return ShiftPlayKit.End();
        }

        // ================================================================== ortak

        static Simulation Sim => K.Sim;
        static SiginakShelterEvent Flow => BoardPlayKit.Find<SiginakShelterEvent>();
        static ShelterEventPaperView View => Flow != null ? Flow.View : null;
        static ShelterEventPaperFace Face => View != null ? View.Face : null;

        static void Ok(ActResult r, string what) => Assert.IsTrue(r != null && r.Ok, what + ": " + r?.Msg);

        static void Note(string line) => Debug.Log("[ShelterEventPlayTests] " + TestContext.CurrentContext.Test.Name + " · " + line);

        void Listen()
        {
            Unlisten();
            listened = Flow;
            onTelex = (s, m) => telex.Add((s, m));
            onSound = s => sounds.Add(s);
            listened.TelexLine += onTelex;
            listened.Sounded += onSound;
            doorSounds = new AdmissionSoundLog(K.Admission);
        }

        void Unlisten()
        {
            if (listened != null)
            {
                if (onTelex != null) listened.TelexLine -= onTelex;
                if (onSound != null) listened.Sounded -= onSound;
            }
            doorSounds?.Dispose();
            doorSounds = null;
            listened = null;
            onTelex = null;
            onSound = null;
            telex.Clear();
            sounds.Clear();
        }

        /// <summary>
        /// Olay sunumu sahneye bağlı: denetleyici, kâğıt, tepsi çapası (kâğıdın kayıtlı yeri), çekmecenin çapası (gelen tutanak oraya iner;
        /// "SiginakRoom/Belge çekmecesi/Tabla" altında, tepsinin altında değil), kapının (vuruş) bağı, masanın ve yüzeyin bağları.
        /// </summary>
        static void RequireWired()
        {
            var flow = Flow;
            Assert.IsNotNull(flow, "SiginakShelterEvent sahnede yok (SiginakRoom/Shelter event controller)");
            Assert.AreSame(flow, ShiftPlayKit.Get<SiginakShelterEvent>(BoardPlayKit.Desk, "shelterEvent"), "SiginakDesk.shelterEvent");
            Assert.AreSame(flow, ShiftPlayKit.Get<SiginakShelterEvent>(BoardPlayKit.Surface, "shelterEvent"), "LiberationDeskView.shelterEvent");
            Assert.AreSame(K.Admission, ShiftPlayKit.Get<SiginakAdmission>(flow, "admission"), "SiginakShelterEvent.admission (kapının vuruşu)");
            Assert.IsNotNull(View, "SiginakShelterEvent.view (Olay tutanağı)");
            Assert.IsNotNull(Face, "ShelterEventPaperView.face");
            Assert.IsNotNull(View.HitArea, "tutanağın çarpıştırıcısı");
            Assert.IsNotNull(View.RestAnchor, "tepsideki dinlenme çapası");
            Assert.IsTrue(View.RestAnchor.IsChildOf(ShiftPlayKit.Tray.transform), "kâğıdın kayıtlı yeri EVRAK tepsisi");
            var door = View.DoorAnchor;
            Assert.IsNotNull(door, "ShelterEventPaperView.doorAnchor (çekmecedeki çapa)");
            Assert.AreEqual("Olay tutanağı kapıda", door.name, "çekmece çapasının adı");
            Assert.IsNotNull(door.parent, "çekmece çapasının ebeveyni");
            Assert.AreEqual("Tabla", door.parent.name, "çapa çekmecenin kayan tablasının altında");
            Assert.IsTrue(door.parent.parent != null && door.parent.parent.name == "Belge çekmecesi", "tabla belge çekmecesinin altında");
            Assert.IsTrue(door.parent.parent.parent != null && door.parent.parent.parent.name == "SiginakRoom", "çekmece SiginakRoom'un altında");
            Assert.IsFalse(door.IsChildOf(ShiftPlayKit.Tray.transform), "çekmece çapası tepsinin altında değil");
            Assert.IsFalse(door.IsChildOf(View.transform), "çekmece çapası kâğıdın altında değil");
            Assert.IsNotNull(ShiftPlayKit.Drawer, "belge çekmecesi (PassDrawer)");
        }

        /// <summary>Kâğıt çekmecenin tablasının çocuğu (tepsinin üzerine gelme etiketi karışmasın).</summary>
        static bool UnderDoorBoard => View.DoorAnchor != null && View.transform.IsChildOf(View.DoorAnchor.parent);

        /// <summary>
        /// Tutanak çekmecede bekliyor: kâğıt çapada ve tablanın çocuğu, EVRAK'ın ışığı sönük ve etiketi tepsinin kendisi, kapı lambası
        /// yanık, kâğıdın kendi etiketi "KAPI · OLAY TUTANAĞI".
        /// </summary>
        static void AssertAtDoor(string what)
        {
            Assert.AreEqual(ShelterEventPaperView.Where.Door, View.State, what + ": tutanak çekmecede");
            Assert.IsTrue(View.AtDoor && Flow.AtDoor, what + ": AtDoor");
            Assert.IsFalse(View.InTray, what + ": tepside değil");
            Assert.Less(Vector3.Distance(View.transform.position, View.DoorAnchor.position), 0.002f, what + ": kâğıt panonun çapasında");
            Assert.IsTrue(UnderDoorBoard, what + ": kâğıt tablanın çocuğu (" + (View.transform.parent != null ? View.transform.parent.name : "-") + ")");
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, what + ": kapıdaki tutanak EVRAK'ın ışığını yakmaz");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, what + ": EVRAK etiketi tepsinin kendi etiketi");
            Assert.IsTrue(K.Admission.DeliveryWaiting, what + ": kapıda belge bekliyor (SiginakDesk.PaperAtDoor)");
            Assert.IsTrue(K.Door.Knocking, what + ": kapı lambası yanık");
            Assert.AreEqual(PassDrawer.Lamp.Amber, ShiftPlayKit.Drawer.LampState, what + ": çekmecenin lambası amber");
            Assert.AreEqual(Loc.T("KAPI · OLAY TUTANAĞI"), View.DoorTitle, what + ": panodaki kâğıdın etiket başlığı");
            Assert.AreEqual(Loc.T("KAPI · OLAY TUTANAĞI"), View.HoverTitle, what + ": panoda üzerine gelme başlığı");
            Assert.IsNotNull(Flow.Last, what + ": tutanağın verisi");
            Assert.AreEqual(Flow.Last.HoverLine, View.DoorLine, what + ": panodaki kâğıdın etiket satırı");
        }

        /// <summary>Kamera masaya (K1) döner (K8 ailesinden <c>SiginakAdmission.Leave</c>, başka pozdan <c>ResetView</c>).</summary>
        static IEnumerator BackToDesk()
        {
            var rig = BoardPlayKit.Rig;
            if (rig.Current != SiginakPose.Desk || rig.InTransition)
            {
                if (SiginakAdmission.InFamily(rig.Current)) K.Admission.Leave();
                else rig.ResetView();
                yield return ShiftPlayKit.WaitForPose(SiginakPose.Desk, 4);
            }
            yield return BoardPlayKit.Frames(2);
        }

        /// <summary>Çekmecedeki tutanak ele alınır (K8), Esc ile bırakılır: çekmeceye döner; kamera K8'de kalır.</summary>
        static IEnumerator TakeFromDoorThenBack(string what)
        {
            yield return ShiftPlayKit.TakeFromDoor(View, what);
            yield return BoardPlayKit.Frames(1);
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return WaitBack(what + " Esc");
            Assert.AreEqual(SiginakPose.Door, BoardPlayKit.Rig.Current, what + ": Esc kamerayı K8'den oynatmaz");
            Assert.IsTrue(UnderDoorBoard, what + ": bırakılan kâğıt yine tablanın çocuğu");
            Assert.IsTrue(Flow.AtDoor, what + ": tutanak çekmecede bekliyor");
        }

        static void Limit(Balance b, string kind, int max)
        {
            var names = new Dictionary<string, string>
            {
                { "buyuk_hirsizlik", "BigTheft" }, { "kacak", "Deserter" }, { "erzak_azaliyor", "LowRation" }, { "hirsizlik", "Theft" },
                { "agir_yarali", "Wounded" }, { "kavga", "Feud" }, { "anma", "Memorial" }, { "karartma", "Blackout" },
                { "takas", "Trade" }, { "cagri", "Call" }, { "konvoy", "Convoy" },
            };
            var field = typeof(Balance).GetField("event" + names[kind] + "Max");
            Assert.IsNotNull(field, "Balance.event" + names[kind] + "Max");
            field.SetValue(b, max);
        }

        /// <summary>Olaylar açık, olağan olayların hepsi susturulmuş; alarm artmaz, açlıktan ayrılma yok.</summary>
        static void Quiet(Balance b)
        {
            b.shelterEventsEnabled = true;
            b.liberationAlarmPerShift = 0;
            b.desertStreak = 0;
            foreach (string kind in ShelterEventData.Ordinary) Limit(b, kind, 0);
        }

        /// <summary>Yalnız konvoy; güven eşiği 0 (aralık dolunca açılır). <paramref name="alarm"/> büyükse "kabul" kapalı.</summary>
        static Action<Balance> ConvoyOnly(double alarm = 0) => b =>
        {
            Quiet(b);
            Limit(b, "konvoy", 9);
            b.convoyTrustAt = 0;
            b.convoyAlarm = alarm;
        };

        /// <summary>Konvoy aralığının dolmasına bir kapanış kalana dek fikstür (olay yok).</summary>
        static Simulation ConvoyEve(Action<Balance> tweak)
        {
            var sim = K.FixtureGame(BoardPlayKit.DefaultSeed, tweak);
            for (int guard = 0; sim.S.Day + 1 < sim.ShelterEvents.NextEventShift && guard < 8; guard++) K.FixtureClose(sim);
            Assert.IsNull(sim.PendingShelterEvent, "fikstür: olay henüz açılmadı");
            return sim;
        }

        /// <summary>Konvoy açık fikstür: aralık dolana dek kapanış.</summary>
        static Simulation ConvoyOpen(Action<Balance> tweak)
        {
            var sim = K.FixtureGame(BoardPlayKit.DefaultSeed, tweak);
            for (int guard = 0; sim.PendingShelterEvent == null && guard < 8; guard++) K.FixtureClose(sim);
            Assert.AreEqual("konvoy", sim.PendingShelterEvent?.Kind, "fikstür: konvoy açıldı (gün " + sim.S.Day + ")");
            return sim;
        }

        /// <summary>Kâğıt çekmeceye indi (<see cref="ShiftPlayKit.WaitDoor"/>) ve bir kare geçti (köprünün LateUpdate'i yazdı).</summary>
        static IEnumerator WaitDoor(string what)
        {
            yield return ShiftPlayKit.WaitDoor(View, what);
            yield return BoardPlayKit.Frames(1);
        }

        /// <summary>Elden bırakılan kâğıt çekmecenin tablasına döndü (EVRAK'a inmez).</summary>
        static IEnumerator WaitBack(string what)
        {
            yield return BoardPlayKit.WaitFor(() => View.State == ShelterEventPaperView.Where.Door && !View.Moving, 3,
                what + ": tutanak çekmeceye dönmedi (" + View.State + ")");
            yield return BoardPlayKit.Frames(1);
            Assert.IsFalse(View.InTray, what + ": EVRAK tepsisine inmedi");
        }

        static IEnumerator WaitHand(string what)
        {
            yield return BoardPlayKit.WaitFor(() => View.State == ShelterEventPaperView.Where.Hand && !View.Moving, 3,
                what + ": tutanak ele gelmedi (" + View.State + ")");
            yield return BoardPlayKit.Frames(1);
        }

        /// <summary>Kâğıdın mm noktasının (sol üst köken, y aşağı) dünya yeri (kökün yerel düzlemi; <c>PaperMm</c>'in tersi).</summary>
        static Vector3 PaperWorld(Vector2 mm) => View.transform.TransformPoint(new Vector3(
            (mm.x - ShelterEventPaperFace.WidthMm / 2f) / 1000f, (ShelterEventPaperFace.HeightMm / 2f - mm.y) / 1000f, 0f));

        /// <summary>Kâğıdın mm noktasına gerçek ışınla sol tık; ilk sonuç tutanağın çarpıştırıcısı olmalı.</summary>
        static IEnumerator ClickPaper(Vector2 mm, string what)
        {
            var screen = BoardPlayKit.ScreenPoint(PaperWorld(mm));
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.AreSame(View.HitArea.gameObject, hit.gameObject, what + ": ilk ışın tutanağa düşmeli (" + BoardPlayKit.DescribeHits(screen) + ")");
            Assert.AreEqual(View.PaperMm(hit.worldPosition).x, mm.x, 2f, what + ": ışının kâğıttaki yeri");
            K.ClickHit(hit);
            yield return BoardPlayKit.Frames(1);
        }

        static IEnumerator ClickRow(int row) => ClickPaper(Face.RowRect(row).center, "satır " + (row + 1));
        static IEnumerator ClickConfirm() => ClickPaper(Face.ConfirmRect.center, "KARAR kutusu");

        static int RowIndex(string id)
        {
            int i = Flow.Last.Rows.FindIndex(r => r.Id == id);
            Assert.GreaterOrEqual(i, 0, "satır yok: " + id);
            return i;
        }

        /// <summary>
        /// Kararın izi basılır, kâğıt çekmecenin tablasına konur (masa kilidi kalkar, lamba yeşil, KAPI tuşu şalteri söyler); şaltere gerçek
        /// tık kolu çeker, çekmece kapanır ve kâğıt gider (<see cref="ShiftPlayKit.SendFromDrawer"/>). Komut yazılmaz.
        /// </summary>
        static IEnumerator WaitFiled()
        {
            yield return BoardPlayKit.WaitFor(() => View.State == ShelterEventPaperView.Where.Outbox && !Flow.Busy, 4,
                "karara bağlanan tutanak çekmeceye konmadı (" + View.State + ")");
            yield return BoardPlayKit.Frames(2);
            Assert.IsTrue(UnderDoorBoard, "karar çekmecenin tablasında");
            Assert.IsTrue(Flow.InOutbox, "şalteri bekliyor");
            Assert.IsFalse(Flow.AtDoor, "okunacak belge sayılmaz");
            Assert.IsTrue(K.Admission.DeliveryWaiting, "kapıda gönderilecek karar var (SiginakDesk.PaperAtDoor)");
            int commands = Sim.Record.commands.Count;
            yield return ShiftPlayKit.SendFromDrawer(View, "karar");
            Assert.AreEqual(commands, Sim.Record.commands.Count, "şalter komut yazmaz");
            yield return BoardPlayKit.Frames(2);
        }

        // ================================================================== P1 + P2 · gelir, kilitler, gerçek tıkla karar

        /// <summary>
        /// Konvoy gerçek kapanışta açılır: dosyalama bitince teleks "VARDİYA DEVREDİLDİ"den sonra OLAY satırını basar ("tutanak kapıya
        /// bırakıldı"), tutanak kapının yanındaki panoya düşer, kapı bir kez vurur (kamera dönmez); EVRAK'ın ışığı sönük, kapı lambası ve
        /// KAPI tuşu belgeyi gösterir. Şalter "olay bekliyor" ile kilitlenir ve kola basış fiş basmaz; EVRAK tıkı kapıdaki tutanağı getirmez.
        /// Panodaki tutanağa (K8'den) gerçek ışınla tık → ele gelir (masa ve kamera kısayolları kilitli); 2. satır ("reddet") gerçek tıkla
        /// işaretlenir (komutsuz), KARAR kutusu tek komut yazar (<c>event_decide:reddet</c>), teleks KARAR satırı, iz, kâğıt dosyalanır;
        /// kapı lambası söner, şalter yeniden basar.
        /// </summary>
        [UnityTest]
        public IEnumerator Arrival_LocksTheSwitch_ThenRealClicksDecide_AndUnlock()
        {
            yield return K.Begin();
            RequireWired();
            yield return K.ContinueFrom(ConvoyEve(ConvoyOnly()));
            RequireWired();
            Listen();
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, View.State, "olay yokken tutanak gizli");

            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            Assert.AreEqual("konvoy", Sim.PendingShelterEvent?.Kind, "kapanış konvoyu açtı");
            yield return WaitDoor("geliş");
            CollectionAssert.Contains(sounds, ShelterEventSound.Drop, "düşüş sesi");
            bool group = Sim.Admission.Group.Count > 0;
            // Kapı bir kez vurur (aynı kapanışta grup geldiyse onun vuruşu da sayılır)
            if (group) Assert.GreaterOrEqual(doorSounds.Count(AdmissionSound.Knock), 1, "kapının vuruşu (" + doorSounds + ")");
            else Assert.AreEqual(1, doorSounds.Count(AdmissionSound.Knock), "kapı bir kez vurur (" + doorSounds + ")");
            Assert.AreNotEqual(SiginakPose.Door, BoardPlayKit.Rig.Current, "vuruş kamerayı kapıya döndürmez");

            // Teleks: devirden sonra OLAY duyurusu
            var lines = ShiftPlayKit.Telex.Lines;
            int handover = -1, announce = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].status == Loc.T("VARDİYA DEVREDİLDİ")) handover = i;
                if (lines[i].status == Loc.T("OLAY")) announce = i;
            }
            string expected = Loc.F("{0} · tutanak kapıya bırakıldı. Karar verilmeden vardiya kapanmaz.", ShelterEventData.Title("konvoy"));
            Assert.Greater(announce, handover, "OLAY satırı VARDİYA DEVREDİLDİ'den sonra (" + string.Join(" / ", lines.Select(l => l.status + " · " + l.message)) + ")");
            Assert.GreaterOrEqual(handover, 0, "VARDİYA DEVREDİLDİ satırı");
            Assert.AreEqual(expected, lines[announce].message);
            Assert.AreEqual(1, telex.Count(t => t.status == Loc.T("OLAY")), "duyuru bir kez");

            // Kapı panosundaki tutanak: EVRAK sönük, kâğıdın kendi etiketi, kapı lambası
            var d = Flow.Last;
            Assert.IsNotNull(d, "tutanağın verisi");
            Assert.AreEqual(ShelterEventData.Title("konvoy"), Face.Title.text, "başlık");
            Assert.AreEqual(Sim.ShelterEventOptions().Count, Face.RowCount, "satır sayısı");
            Assert.IsFalse(Face.Overflowing, "taşma yok");
            AssertAtDoor("geliş");
            Assert.AreEqual(Loc.T("Karar bekliyor: al, bir satır seç, damgala."), View.DoorLine, "panodaki kâğıdın satırı");
            Assert.IsFalse(ShiftPlayKit.Tray.TargetLight != null && ShiftPlayKit.Tray.TargetLight.enabled, "EVRAK'ın hedef ışığı kapalı");

            // KAPI tuşu ve kapının hedefi belgeyi gösterir (kapıda grup yokken; grup varsa aday yazıları kalır)
            var key = K.DoorKey;
            Assert.IsTrue(key.Attention, "kapıda belge varken KAPI tuşunun ışığı yanar");
            var doorTarget = ShiftPlayKit.Get<DeskTarget>(K.Admission.DoorController, "target");
            if (!group)
            {
                Assert.AreEqual(Loc.T("KAPI · BELGE VAR"), key.Target.HoverTitle, "KAPI tuşunun başlığı");
                Assert.AreEqual(Loc.T("Kapıya bir belge bırakıldı: kapının yanındaki çekmeceden al."), key.Target.Line, "KAPI tuşunun satırı");
                if (doorTarget != null)
                    Assert.AreEqual(Loc.T("Kapıya bir belge bırakıldı: yanındaki çekmeceden al."), doorTarget.Line, "kapı hedefinin satırı");
            }

            // EVRAK'a tık kapıdaki tutanağı getirmez (eski tepsi yolu açılır; burada yalnız köprünün yanıtı)
            Assert.IsFalse(Flow.TakeFromTray(), "EVRAK tıkı kapıdaki tutanağı getirmez");
            Assert.AreEqual(ShelterEventPaperView.Where.Door, View.State, "tutanak kapıda kaldı");

            // Şalter: olay bekliyor
            yield return BoardPlayKit.Frames(2);
            var sw = ShiftPlayKit.Switch;
            Assert.AreEqual(Loc.T("olay bekliyor"), sw.LockPlate, "plaka");
            Assert.AreEqual(Loc.F("{0} Tutanak kapının çekmecesinde.", Sim.ShelterEventPrintCheck()), sw.LockLine, "üzerine gelme satırı: Core gerekçesi ve tutanağın yeri");
            Assert.IsNotNull(sw.LockLine);
            Assert.AreEqual(ShiftPhase.Locked, sw.Phase, "şalter kilitli");
            var mark = ShiftPlayKit.Mark();
            ShiftPlayKit.ClickSwitch();
            yield return BoardPlayKit.Frames(3);
            ShiftPlayKit.ClickSwitch();
            yield return BoardPlayKit.Frames(3);
            Assert.IsFalse(Sim.Board.Printed, "açık olay varken fiş basılmaz");
            ShiftPlayKit.AssertUnchanged(mark, "kilitli şaltere basış");
            Assert.AreEqual(Sim.ShelterEventPrintCheck(), Sim.PrintShift().Msg, "Core da reddeder (aynı gerekçe)");
            ShiftPlayKit.AssertUnchanged(mark, "reddedilen basım");

            // Panodan ele al (K8)
            sounds.Clear();
            yield return ShiftPlayKit.TakeFromDoor(View, "çekmeceden al");
            yield return BoardPlayKit.Frames(1);
            CollectionAssert.Contains(sounds, ShelterEventSound.ToHand);
            Assert.IsFalse(Flow.AtDoor, "elde tutanak kapıda sayılmaz");
            Assert.IsFalse(K.Admission.DeliveryWaiting, "elde tutanak kapıda belge sayılmaz");
            Assert.IsTrue(BoardPlayKit.Surface.InputBlocked, "tutanak eldeyken masa kilitli");
            Assert.IsTrue(BoardPlayKit.Rig.InputSuspended, "tutanak eldeyken kamera kısayolları askıda");
            Assert.IsTrue(View.HitArea.enabled, "elde çarpıştırıcı açık");

            // 2. satırı (reddet) gerçek tıkla işaretle: komutsuz
            int reject = RowIndex("reddet");
            yield return ClickRow(reject);
            Assert.AreEqual("reddet", Flow.Ui.SelectedId, "satır işaretlendi");
            Assert.IsTrue(Flow.Last.ConfirmReady);
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, "elde ışık söner");
            Assert.AreEqual(Loc.F("{0} · damgala", ShelterEventData.OptionName("konvoy", "reddet")), Face.ConfirmText.text, "KARAR kutusunun alt satırı");
            CollectionAssert.Contains(sounds, ShelterEventSound.Mark);
            ShiftPlayKit.AssertUnchanged(mark, "satır işareti");

            // KARAR kutusu: tek komut
            yield return ClickConfirm();
            var since = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, since.Count, "karar tek komut (" + ShiftPlayKit.Describe(since) + ")");
            Assert.AreEqual("event_decide", since[0].kind);
            Assert.AreEqual("reddet", since[0].a);
            Assert.IsNull(Sim.PendingShelterEvent, "olay kapandı");
            CollectionAssert.Contains(sounds, ShelterEventSound.Stamp, "iz sesi");
            Assert.IsTrue(Face.StampVisible, "KARARA BAĞLANDI izi");
            Assert.AreEqual(Loc.T("KARARA BAĞLANDI"), Face.StampLine1.text);
            Assert.IsTrue(telex.Any(t => t.status == Loc.T("KARAR") &&
                t.message == Loc.F("{0}: {1}", ShelterEventData.Title("konvoy"), ShelterEventData.OptionName("konvoy", "reddet"))), "teleks KARAR satırı");
            yield return WaitFiled();
            Assert.IsFalse(BoardPlayKit.Rig.InputSuspended, "kamera askısı kalktı");
            Assert.IsFalse(UnderDoorBoard, "gönderilen kâğıt gizlendi ve yeniden tepsinin çocuğu");
            Assert.IsFalse(K.Admission.DeliveryWaiting, "kapıda belge kalmadı");
            if (!group) Assert.IsFalse(K.Door.Knocking, "karardan sonra kapı lambası söner");
            yield return BackToDesk();
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "karardan sonra masa açık");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, "EVRAK etiketi eski hâline döndü");
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, "karardan sonra hedef ışığı söner");
            Assert.IsFalse(ShiftPlayKit.Tray.TargetLight != null && ShiftPlayKit.Tray.TargetLight.enabled, "hedef ışığı kapalı");

            // Şalter yeniden basar
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 3, "şalter hazıra dönmedi (" + sw.Phase + " / " + sw.LockPlate + ")");
            Assert.IsNull(sw.LockPlate, "kilit kalktı");
            yield return ShiftPlayKit.PrintToTongue();
            Assert.IsTrue(Sim.Board.Printed, "karardan sonra fiş basılır");
        }

        // ================================================================== P3 · kapalı seçenek

        /// <summary>
        /// "kabul" kapalıyken (alarm eşiğe çok yakın): satıra gerçek tık ve 1 tuşu ret sesi verir, not satırına "KAPALI · gerekçe" yazar;
        /// seçimsiz KARAR ve Enter "Önce bir satır seç." ile reddedilir. Hiçbiri komut yazmaz, RNG çekmez.
        /// </summary>
        [UnityTest]
        public IEnumerator ClosedOption_DeniesWithReason_WithoutCommand()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(ConvoyOpen(ConvoyOnly(1000)));
            RequireWired();
            Listen();
            yield return WaitDoor("kayıttan devam");
            var closed = Sim.ShelterEventOptions().Single(o => o.Id == "kabul");
            Assert.IsFalse(closed.Open, "kabul kapalı (convoyAlarm büyük)");
            yield return ShiftPlayKit.TakeFromDoor(View, "çekmeceden al");
            yield return BoardPlayKit.Frames(1);
            var mark = ShiftPlayKit.Mark();

            int row = RowIndex("kabul");
            Assert.IsFalse(Flow.Last.Rows[row].Enabled);
            Assert.AreEqual(Loc.T("KAPALI"), Face.RowStampText(row).text, "KAPALI izi");
            Assert.IsTrue(Face.RowStampText(row).gameObject.activeInHierarchy, "KAPALI izi görünür");
            sounds.Clear();
            yield return ClickRow(row);
            Assert.IsNull(Flow.Ui.SelectedId, "kapalı satır işaretlenmez");
            CollectionAssert.AreEqual(new[] { ShelterEventSound.Deny }, sounds, "ret sesi");
            Assert.AreEqual(Loc.F("KAPALI · {0}", closed.ClosedReason), Face.Note.text, "not satırında gerekçe");

            sounds.Clear();
            yield return BoardPlayKit.PressKey(Key.Digit1 + row);
            CollectionAssert.AreEqual(new[] { ShelterEventSound.Deny }, sounds, "tuşla da ret");

            sounds.Clear();
            yield return ClickConfirm();
            CollectionAssert.AreEqual(new[] { ShelterEventSound.Deny }, sounds, "seçimsiz KARAR ret");
            Assert.AreEqual(Loc.T("Önce bir satır seç."), Face.Note.text);
            yield return BoardPlayKit.PressKey(Key.Enter);
            Assert.AreEqual(ShelterEventPaperView.Where.Hand, View.State, "ret kâğıdı elde bırakır");
            Assert.AreEqual("konvoy", Sim.PendingShelterEvent?.Kind, "olay açık kaldı");
            Assert.AreEqual(Loc.F("önce bir satır seç ({0})", (RowIndex("reddet") + 1).ToString(CultureInfo.InvariantCulture)), Face.ConfirmText.text,
                "KARAR ipucu yalnız açık satırı sayar");

            // Çekmeceye geri koy, masaya dön, dili değiştir: çekmecedeki kâğıt yeni dilde, son ret (eski dilde) taşınmaz
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return WaitBack("Esc");
            Assert.AreEqual(SiginakPose.Door, BoardPlayKit.Rig.Current, "Esc kamerayı K8'den oynatmaz");
            yield return BackToDesk();
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            yield return BoardPlayKit.Frames(2);
            AssertEnglish("çekmecede EN");
            Assert.IsFalse(Face.Note.gameObject.activeInHierarchy && !string.IsNullOrEmpty(Face.Note.text), "geri konunca son ret silinir: " + Face.Note.text);
            yield return ShiftPlayKit.TakeFromDoor(View, "EN'de yeniden al");
            yield return BoardPlayKit.Frames(1);
            AssertEnglish("elde EN");
            Assert.IsFalse(Face.Note.gameObject.activeInHierarchy && !string.IsNullOrEmpty(Face.Note.text), "yeniden ele alınca eski ret yok: " + Face.Note.text);
            Assert.AreEqual(Loc.F("önce bir satır seç ({0})", (RowIndex("reddet") + 1).ToString(CultureInfo.InvariantCulture)), Face.ConfirmText.text);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            yield return BoardPlayKit.Frames(1);
            ShiftPlayKit.AssertUnchanged(mark, "kapalı seçenek, seçimsiz karar, dil ve çekmece");
        }

        /// <summary>Kâğıdın görünen yazılarında (kişi adları dışında) Türkçe harf yok.</summary>
        static void AssertEnglish(string what)
        {
            foreach (var t in Face.Texts.Where(x => x.gameObject.activeInHierarchy && !string.IsNullOrEmpty(x.text)))
            {
                string text = t.text;
                foreach (var p in Sim.Board.People) text = text.Replace(p.Name, "");
                Assert.IsFalse(text.Any(c => TurkishOnly.IndexOf(c) >= 0), what + ": Türkçe harf: " + t.name + " = " + t.text);
            }
        }

        // ================================================================== P4 · kayıttan devam, tuşla karar

        /// <summary>
        /// Açık olayla kayıttan devam: tutanak animasyonsuz çekmecede (vuruş yok), OLAY teleksi yeniden basılmaz, yükleme komut yazmaz,
        /// şalter kilitli. Tutanak okunup çekmeceye geri konunca, yeniden devamda çekmece kapalı ve tutanak içindedir. Çekmeceden alınan
        /// kâğıtta 1–3 tuşları işaretler, aynı tuşa ikinci basış işareti kaldırır, Enter karar verir (<c>event_decide:kabul</c>).
        /// </summary>
        [UnityTest]
        public IEnumerator Continue_RestoresPaperAtTheDoor_AndKeysDecide()
        {
            yield return K.Begin();
            var fixture = ConvoyOpen(ConvoyOnly());
            int saved = fixture.Record.commands.Count;
            yield return K.ContinueFrom(fixture);
            RequireWired();
            Listen();
            Assert.AreEqual(saved, Sim.Record.commands.Count, "yükleme komut yazmaz");
            Assert.AreEqual(ShelterEventPaperView.Where.Door, View.State, "tutanak çekmecede (animasyonsuz)");
            Assert.IsFalse(View.Moving, "düşüş yok");
            Assert.IsFalse(K.Door.KnockPlaying, "kayıttan devamda kapı vurmaz");
            yield return BoardPlayKit.Frames(3);
            Assert.IsEmpty(telex, "kayıttan devamda teleks yeniden basılmaz");
            CollectionAssert.DoesNotContain(sounds, ShelterEventSound.Drop, "düşüş sesi yok");
            Assert.AreEqual(0, doorSounds.Count(AdmissionSound.Knock), "kayıttan devamda vuruş sesi yok (" + doorSounds + ")");
            AssertAtDoor("kayıttan devam");
            Assert.AreEqual(Loc.T("olay bekliyor"), ShiftPlayKit.Switch.LockPlate);

            // Okunup çekmeceye geri konan tutanak yeniden devamda yine çekmecede (çekmece kapalı)
            yield return TakeFromDoorThenBack("devamdan önce");
            Assert.AreEqual(saved, Sim.Record.commands.Count, "ele alıp geri koymak komut yazmaz");
            yield return ShiftPlayKit.Continue();
            RequireWired();
            Listen();
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(saved, Sim.Record.commands.Count, "ikinci yükleme de komut yazmaz");
            Assert.AreEqual(ShelterEventPaperView.Where.Door, View.State, "tutanak devamda çekmecede");
            Assert.IsFalse(ShiftPlayKit.Drawer.IsOpen, "devamda çekmece kapalı");
            Assert.IsFalse(View.Moving, "ikinci devamda düşüş yok");
            Assert.IsFalse(K.Door.KnockPlaying, "ikinci devamda kapı vurmaz");
            yield return BoardPlayKit.Frames(1);
            AssertAtDoor("ikinci devam");

            yield return ShiftPlayKit.TakeFromDoor(View, "çekmeceden al");
            yield return BoardPlayKit.Frames(1);
            var mark = ShiftPlayKit.Mark();
            int accept = RowIndex("kabul");
            yield return BoardPlayKit.PressKey(Key.Digit1 + accept);
            Assert.AreEqual("kabul", Flow.Ui.SelectedId, "tuş satırı işaretler");
            Assert.IsTrue(Face.RowTick(accept).gameObject.activeInHierarchy, "işaret kutusu dolu");
            yield return BoardPlayKit.PressKey(Key.Digit1 + accept);
            Assert.IsNull(Flow.Ui.SelectedId, "ikinci basış işareti kaldırır");
            yield return BoardPlayKit.PressKey(Key.Digit1 + accept);
            ShiftPlayKit.AssertUnchanged(mark, "işaretleme");
            double supplies = Sim.Shelter.Supplies;
            yield return BoardPlayKit.PressKey(Key.Enter);
            var since = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, since.Count, ShiftPlayKit.Describe(since));
            Assert.AreEqual("event_decide", since[0].kind);
            Assert.AreEqual("kabul", since[0].a);
            Assert.Greater(Sim.Shelter.Supplies, supplies, "konvoy erzak getirdi");
            yield return WaitFiled();
            yield return BoardPlayKit.Frames(2);
            Assert.IsNull(ShiftPlayKit.Switch.LockPlate, "kilit kalktı");

            // İkiz: kayıt yeniden oynatılınca aynı oyun
            Assert.AreEqual(ShiftPlayKit.Fingerprint(Sim), ShiftPlayKit.Fingerprint(ShiftPlayKit.Twin()), "event_decide yeniden oynatılır");
        }

        // ================================================================== P7 · Esc ve sağ tık

        /// <summary>
        /// Baskın sahnesi yüklenince oda kapanır, dönüşte yeniden açılır (<c>SiginakRaidLink</c>; köprüler <c>OnEnable</c>'da). Aynı kaydın
        /// sunumu sürer: çekmecedeki tutanak çekmecede kalır, vuruş yok; okunup geri konan tutanak da çekmecededir ve çekmece açık kalır;
        /// ebeveyn değişimi hiyerarşi hatası vermez (oda açılırken değil, kâğıdın sonraki karesinde).
        /// </summary>
        [UnityTest, Timeout(180000)]
        public IEnumerator RoomReenabled_KeepsThePaperWhereItWas()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(ConvoyOpen(ConvoyOnly()));
            RequireWired();
            Listen();
            var room = Flow.transform.parent.gameObject;
            Assert.AreEqual("SiginakRoom", room.name, "olay denetleyicisi odanın çocuğu");
            var mark = ShiftPlayKit.Mark();

            AssertAtDoor("başta");
            int knocks = doorSounds.Count(AdmissionSound.Knock);
            room.SetActive(false);
            yield return BoardPlayKit.Frames(1);
            room.SetActive(true);
            yield return BoardPlayKit.Frames(3);
            AssertAtDoor("oda yeniden açıldı");
            Assert.AreEqual(knocks, doorSounds.Count(AdmissionSound.Knock), "yeniden açılış kapıyı vurdurmaz");

            yield return TakeFromDoorThenBack("çekmeceden al");
            yield return BackToDesk();
            Assert.IsTrue(ShiftPlayKit.Drawer.IsOpen, "çekmece açık kaldı");
            room.SetActive(false);
            yield return BoardPlayKit.Frames(1);
            room.SetActive(true);
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, View.State, "geri konan tutanak çekmecede kalır (" + View.State + ")");
            Assert.Less(Vector3.Distance(View.transform.position, View.DoorAnchor.position), 0.002f, "kâğıt çekmecenin çapasında");
            Assert.IsTrue(UnderDoorBoard, "kâğıt tablanın çocuğu");
            Assert.IsTrue(ShiftPlayKit.Drawer.IsOpen, "aynı kayıtta çekmece açık kalır");
            Assert.IsTrue(Flow.AtDoor && K.Admission.DeliveryWaiting, "kapıda belge var");
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, "EVRAK ışığı sönük");
            Assert.AreEqual(knocks, doorSounds.Count(AdmissionSound.Knock), "vuruş yok");
            ShiftPlayKit.AssertUnchanged(mark, "oda açılıp kapanması komut yazmaz");
            Unlisten();
        }

        /// <summary>
        /// Çekmeceden (K8) alınan tutanağı elde Esc çekmeceye geri koyar (komutsuz), kamera K8'de kalır, askı kalkar, EVRAK'ın ışığı sönük
        /// kalır. Çekmece açık kalır: yeniden alınan tutanağı sürüklemesiz sağ tık ve kâğıdın boş yerine tık da çekmeceye koyar.
        /// </summary>
        [UnityTest]
        public IEnumerator EscAndRightClick_PutThePaperBack()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(ConvoyOpen(ConvoyOnly()));
            RequireWired();
            Listen();
            var mark = ShiftPlayKit.Mark();
            var pose = BoardPlayKit.Rig.Current;
            Assert.AreEqual(SiginakPose.Desk, pose, "kayıttan devamda kamera K1'de");

            yield return ShiftPlayKit.TakeFromDoor(View, "çekmeceden al");
            yield return BoardPlayKit.Frames(1);
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return WaitBack("Esc");
            CollectionAssert.Contains(sounds, ShelterEventSound.ToTray);
            Assert.Less(Vector3.Distance(View.transform.position, View.DoorAnchor.position), 0.002f, "kâğıt çekmecenin çapasında");
            Assert.IsTrue(UnderDoorBoard, "geri konan kâğıt tablanın çocuğu");
            Assert.IsTrue(Flow.AtDoor, "tutanak yine çekmecede bekliyor");
            Assert.IsTrue(K.Admission.DeliveryWaiting, "kapıda belge var");
            Assert.AreEqual(SiginakPose.Door, BoardPlayKit.Rig.Current, "Esc kamerayı oynatmaz (K8'de kalır, kapının geri zincirine ulaşmaz)");
            Assert.IsFalse(BoardPlayKit.Rig.InputSuspended, "askı kalktı");
            Assert.IsFalse(SettingsCabinet.Instance != null && SettingsCabinet.Instance.IsOpen, "Esc ayarları açmaz");
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, "EVRAK'ın ışığı sönük");
            Assert.IsTrue(ShiftPlayKit.Drawer.IsOpen, "çekmece açık kaldı");

            yield return ShiftPlayKit.TakeFromDoor(View, "yeniden al");
            yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, Vector2.zero, 0);
            yield return WaitBack("sağ tık");
            Assert.AreEqual(SiginakPose.Door, BoardPlayKit.Rig.Current, "sağ tık kamerayı oynatmaz");

            yield return ShiftPlayKit.TakeFromDoor(View, "üçüncü kez al");
            yield return ClickPaper(new Vector2(12, 12), "kâğıdın boş köşesi");
            yield return WaitBack("boş yere tık");

            Assert.AreEqual("konvoy", Sim.PendingShelterEvent?.Kind);
            ShiftPlayKit.AssertUnchanged(mark, "çekmeceye dönüş");
            yield return BackToDesk();
            Assert.AreEqual(pose, BoardPlayKit.Rig.Current, "masaya dönüldü");
        }

        // ================================================================== P5 · zincir: iz kâğıdı ve tecrit

        /// <summary>Her aday sızmacı, gecikme 1; ilk grup geri çevrilir, ikincinin ilk adayı alınır; iz kâğıdı açılana dek kapanış.</summary>
        static Simulation IzChain(out string personId)
        {
            var sim = K.FixtureGame(BoardPlayKit.DefaultSeed, b =>
            {
                Quiet(b);
                b.candidateTier0Robot = b.candidateTier1Robot = b.candidateTier2Robot = 1;
                b.humanTraceChance = 0;
                b.humanSecondTraceChance = 0;
                b.traceDelayMin = b.traceDelayMax = 1;
                b.secondTraceGapMin = b.secondTraceGapMax = 1;
                b.candidateEmergencyBelow = 0;
            });
            for (int guard = 0; sim.Admission.Group.Count == 0 && guard < 10; guard++) K.FixtureClose(sim);
            Assert.Greater(sim.Admission.Group.Count, 0, "fikstür: ilk grup");
            Ok(sim.PassCandidates(), "ilk grup geri çevrildi");
            for (int guard = 0; sim.Admission.Group.Count == 0 && guard < 10; guard++) K.FixtureClose(sim);
            Assert.Greater(sim.Admission.Group.Count, 0, "fikstür: ikinci grup");
            personId = sim.Admission.Group[0].Resident.Id;
            Ok(sim.ChooseCandidate(personId), "ikinci grubun ilk adayı alındı");
            for (int guard = 0; sim.PendingShelterEvent == null && guard < 8; guard++) K.FixtureClose(sim);
            Assert.AreEqual("iz", sim.PendingShelterEvent?.Kind, "fikstür: iz kâğıdı açıldı (gün " + sim.S.Day + ")");
            Assert.AreEqual(personId, sim.PendingShelterEvent.PersonId);
            return sim;
        }

        /// <summary>
        /// Zincir: iz kâğıdı çekmecede (çekmeceden alınır); kişi satırı ve risk satırı ("tecrit her zaman bulgu vermez") görünür. Tecrit gerçek tıkla seçilir,
        /// KARAR → <c>event_decide:tecrit</c>; panodaki kartın şeridi "Tecritte · 3 v.", dosyanın etiketi "TECRİT · 3 V", destenin altında.
        /// </summary>
        [UnityTest]
        public IEnumerator IzPaper_Isolation_ShowsOnTheCard()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(IzChain(out string id));
            RequireWired();
            Listen();
            yield return WaitDoor("iz kâğıdı");
            var person = Sim.Board.Person(id);
            Assert.AreEqual(ShelterEventData.Title("iz"), Face.Title.text);
            Assert.AreEqual(Loc.F("KİŞİ · {0}", person.Name), Face.Person.text, "kişi satırı");
            int iso = RowIndex("tecrit");
            var option = Sim.ShelterEventOptions()[iso];
            Assert.IsTrue(option.Open, "tecrit açık (yer var)");
            if (!string.IsNullOrEmpty(option.Risk)) Assert.AreEqual(Loc.F("Risk: {0}", option.Risk), Face.RowRisk(iso).text, "risk satırı");

            yield return ShiftPlayKit.TakeFromDoor(View, "çekmeceden al");
            yield return BoardPlayKit.Frames(1);
            var mark = ShiftPlayKit.Mark();
            yield return ClickRow(iso);
            yield return ClickConfirm();
            var since = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, since.Count, ShiftPlayKit.Describe(since));
            Assert.AreEqual("event_decide", since[0].kind);
            Assert.AreEqual("tecrit", since[0].a);
            Assert.IsTrue(person.Isolated, "kişi tecritte");
            yield return WaitFiled();
            yield return BackToDesk();

            int left = Sim.Admission.File(id).IsolatedUntil - Sim.S.Day;
            Assert.AreEqual(Sim.B.isolationShifts, left, "tecrit süresi");

            // Pano: komuttan sonra kendiliğinden tazelenmiş canlı görünüm (elle Refresh yok)
            yield return BoardPlayKit.EnterBoard();
            var board = BoardPlayKit.Board;
            var card = board.CardFor(id);
            Assert.IsNotNull(card, "kişinin kartı panoda");
            var face = card.Face as PersonCardFace;
            Assert.IsNotNull(face, "kişi kartı yüzü");
            Assert.AreEqual(Loc.F("Tecritte · {0} v.", left), face.StripText.text, "kart şeridi");
            CollectionAssert.Contains(board.Last.DeckOrder.Skip(board.Last.DeckOrder.Count - 2).ToList(), id, "tecritteki kişi destenin altında");
            BoardPlayKit.Click(card.gameObject);
            yield return BoardPlayKit.Frames(1);
            BoardPlayKit.Click(card.gameObject, 2);
            yield return BoardPlayKit.WaitFor(() => board.Folder.IsOpen && !board.Folder.Moving, 3, "dosya açılmadı");
            yield return BoardPlayKit.Frames(1);
            var tag = ShiftPlayKit.Get<FileTagView>(board.Folder, "fileTag");
            Assert.IsNotNull(tag, "dosya etiketi görünümü");
            Assert.AreEqual(Loc.F("TECRİT · {0} V", left), tag.Text, "dosya sırtındaki etiket");
            // Aşama 7: eski sekmeler (İNCELE, ERİŞİM, TAHLİYE) dosyadan söküldü
            foreach (string gone in new[] { "Sekme · İNCELE", "Sekme · ERİŞİM", "Sekme · TAHLİYE", "Sekme hedefi · TAHLİYE" })
                Assert.IsFalse(board.Folder.GetComponentsInChildren<Transform>(true).Any(x => x.name == gone), gone + ": eski sekme sahnede");
            var shown = board.Folder.GetComponentsInChildren<TMPro.TMP_Text>(false).Select(t => t.text).ToList();
            Assert.IsFalse(shown.Any(t => t == Loc.T("İNCELEME") || t == Loc.T("TAM ERİŞİM")), "eski kabul satırları yazılmaz: " +
                string.Join(" | ", shown.Where(t => t == Loc.T("İNCELEME") || t == Loc.T("TAM ERİŞİM"))));
        }

        // ================================================================== P8 · olay kendiliğinden kapanır

        /// <summary>Dolu sığınak (yer = kurucu sayısı), kapıda grup; olay yok (aralık uzak), kapı her kapanışta çalar.</summary>
        static Simulation GroupAtFullShelter(out string candidateId)
        {
            int founders = Simulation.NewGame(BoardPlayKit.DefaultSeed, K.BalanceCopy(Quiet)).Board.People.Count(p => p.Alive);
            var sim = K.FixtureGame(BoardPlayKit.DefaultSeed, b =>
            {
                Quiet(b);
                b.eventGapMin = b.eventGapMax = 99;
                b.admissionKnockMin = b.admissionKnockMax = 1;
                b.candidateEmergencyBelow = 0;
                b.boardResidentCapacity = founders;
            });
            for (int guard = 0; sim.Admission.Group.Count == 0 && guard < 10; guard++) K.FixtureClose(sim);
            Assert.Greater(sim.Admission.Group.Count, 0, "fikstür: kapıda grup");
            Assert.GreaterOrEqual(sim.Board.People.Count(p => p.Alive), sim.B.boardResidentCapacity, "fikstür: sığınak dolu");
            Assert.IsNull(sim.PendingShelterEvent, "fikstür: olay yok");
            candidateId = sim.Admission.Group[0].Resident.Id;
            return sim;
        }

        /// <summary>
        /// Açık olayın (kapalı olmayan, hedefli: hırsızlık) hedefi, kapıdaki aday yerine yollanınca (<c>ChooseCandidate</c>, tek komut) Core
        /// olayı kendiliğinden kapatır: çekmecedeki tutanak gizlenir (yeniden tepsinin çocuğu), teleks "kapandı" satırını basar, kapıda
        /// belge kalmaz, EVRAK'ın ışığı ve etiketi sönük kalır, şalterin kilidi kalkar. Olayın kurulumu hata ayıklama kurulumudur (komut
        /// değil); kapanış gerçek komutla ve <c>Changed</c> yoluyla gelir.
        /// </summary>
        [UnityTest]
        public IEnumerator TargetReplacedAtTheDoor_ClosesTheEvent_HidesPaper_AndUnlocks()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(GroupAtFullShelter(out string candidate));
            RequireWired();
            Listen();
            var target = Sim.Board.People.Where(p => p.Alive).Last();
            Sim.ShelterEvents.Pending = new ShelterEventInstance { Kind = "hirsizlik", OpenedShift = Sim.S.Day, PersonId = target.Id, Amount = 3, Resource = "erzak" };
            doorSounds.Clear();
            ShiftPlayKit.RaiseChanged();
            yield return WaitDoor("hırsızlık");
            Assert.IsTrue(Sim.ShelterEventOptions().Any(o => o.Open), "olayın açık satırı var");
            yield return BoardPlayKit.Frames(2);
            var sw = ShiftPlayKit.Switch;
            Assert.AreEqual(Loc.T("olay bekliyor"), sw.LockPlate, "olay şalteri kilitler");
            AssertAtDoor("hırsızlık");
            Assert.GreaterOrEqual(doorSounds.Count(AdmissionSound.Knock), 1, "tutanak kapıya inince kapı vurur (" + doorSounds + ")");
            telex.Clear();

            var mark = ShiftPlayKit.Mark();
            Assert.IsNull(Sim.CandidateChoiceCheck(candidate, target.Id), "aday hedefin yerine alınabilir");
            Ok(ShiftPlayKit.Game.ExecuteBoardCommand(s => s.ChooseCandidate(candidate, target.Id)), "aday alındı, hedef yollandı");
            var since = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, since.Count, ShiftPlayKit.Describe(since));
            Assert.AreEqual("candidate_choose", since[0].kind);
            yield return BoardPlayKit.Frames(2);
            Assert.IsNull(Sim.PendingShelterEvent, "Core olayı kendiliğinden kapattı");
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, View.State, "tutanak gizlendi");
            Assert.IsNull(Flow.Last, "gösterilecek tutanak yok");
            string closed = Loc.F("{0} tutanağı kapandı: {1} artık sığınakta değil.", ShelterEventData.Title("hirsizlik"), target.Name);
            Assert.IsTrue(telex.Any(t => t.status == Loc.T("OLAY") && t.message == closed),
                "teleks kapandı satırı (" + string.Join(" / ", telex.Select(t => t.status + " · " + t.message)) + ")");
            Assert.IsFalse(Flow.AtDoor, "kapıda tutanak kalmadı");
            Assert.IsFalse(K.Admission.DeliveryWaiting, "kapıda belge kalmadı (lamba yalnız kapıdaki aday için yanar)");
            yield return BoardPlayKit.Frames(1);
            Assert.AreEqual(PassDrawer.Lamp.Off, ShiftPlayKit.Drawer.LampState, "çekmecenin lambası söndü");
            Assert.IsFalse(UnderDoorBoard, "gizlenen kâğıt yeniden tepsinin çocuğu");
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, "EVRAK'ın ışığı sönük");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, "EVRAK etiketi tepsinin kendi etiketi");
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 3, "şalter hazıra dönmedi (" + sw.Phase + " / " + sw.LockPlate + ")");
            Assert.IsNull(sw.LockPlate, "kilit kalktı");
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "masa açık");
        }

        // ================================================================== P9 · grup ve olay aynı kapanışta

        /// <summary>
        /// Kapı her kapanışta çalar (fikstürde gelen gruplar geri çevrilir) ve konvoyun aralığı dolmak üzere: gerçek kapanışta grup da
        /// konvoy da gelir. Dosyalama bitince teleks sırası VARDİYA DEVREDİLDİ → OLAY → KAPI; tutanak çekmecede, grup kapıda (KAPI tuşu
        /// adayı söyler), şalter kilitli; tutanak K8'de çekmeceden alınır.
        /// </summary>
        [UnityTest]
        public IEnumerator GroupAndEvent_SameClosing_OrderAndBothShown()
        {
            yield return K.Begin();
            var sim = K.FixtureGame(BoardPlayKit.DefaultSeed, b =>
            {
                ConvoyOnly()(b);
                b.admissionKnockMin = b.admissionKnockMax = 1;
                b.candidateEmergencyBelow = 0;
            });
            for (int guard = 0; sim.S.Day + 1 < sim.ShelterEvents.NextEventShift && guard < 8; guard++)
            {
                if (sim.Admission.Group.Count > 0) Ok(sim.PassCandidates(), "fikstür: grup geri");
                K.FixtureClose(sim);
            }
            if (sim.Admission.Group.Count > 0) Ok(sim.PassCandidates(), "fikstür: grup geri");
            Assert.IsNull(sim.PendingShelterEvent, "fikstür: olay henüz yok");
            yield return K.ContinueFrom(sim);
            RequireWired();
            Listen();

            yield return ShiftPlayKit.PrintToTongue();
            yield return ShiftPlayKit.FileOnEvrak();
            Assert.AreEqual("konvoy", Sim.PendingShelterEvent?.Kind, "kapanış konvoyu açtı");
            Assert.Greater(Sim.Admission.Group.Count, 0, "aynı kapanışta grup geldi");
            yield return WaitDoor("geliş");
            yield return BoardPlayKit.WaitSeconds(0.5f);
            AssertAtDoor("grup ve olay");
            Assert.AreEqual(Loc.T("KAPI · ADAY VAR"), K.DoorKey.Target.HoverTitle, "grup varken KAPI tuşu adayı söyler");

            var lines = ShiftPlayKit.Telex.Lines;
            int handover = -1, announce = -1, door = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].status == Loc.T("VARDİYA DEVREDİLDİ")) handover = i;
                if (lines[i].status == Loc.T("OLAY")) announce = i;
                if (lines[i].status == Loc.T("KAPI")) door = i;
            }
            string order = string.Join(" / ", lines.Select(l => l.status + " · " + l.message));
            Assert.GreaterOrEqual(handover, 0, "VARDİYA DEVREDİLDİ (" + order + ")");
            Assert.Greater(announce, handover, "OLAY devirden sonra (" + order + ")");
            Assert.Greater(door, announce, "KAPI olaydan sonra (" + order + ")");
            Assert.AreEqual(1, lines.Count(l => l.status == Loc.T("OLAY")), "OLAY bir kez");
            Assert.AreEqual(Loc.T("olay bekliyor"), ShiftPlayKit.Switch.LockPlate);
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "iki duyurudan sonra masa açık");
            yield return ShiftPlayKit.TakeFromDoor(View, "grup kapıdayken çekmeceden al");
            Assert.AreEqual(ShelterEventData.Title("konvoy"), Face.Title.text);
        }

        // ================================================================== P10 · çekmece, lamba ve şalter

        /// <summary>
        /// Çekmecenin kendisi, gerçek tıklarla: gelen tutanakla çekmece kapalı ve lambası amber yanıp söner, kâğıt tıklanmaz; karar yokken
        /// şaltere tık ret sesidir (kol kıpırdamaz, komut yok). Çekmecenin önüne tık açar (lamba sürekli amber), ikinci tık kapar. Karar
        /// Enter'la verilir: kâğıt tablaya konur, lamba yeşil yanıp söner, KAPI tuşu şalteri söyler; çekmece kapatılsa da şalter kararı
        /// gönderir. Şalterden sonra lamba söner, kapıda belge kalmaz.
        /// </summary>
        [UnityTest]
        public IEnumerator Drawer_LampSwitchAndRealClicks()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(ConvoyOpen(ConvoyOnly()));
            RequireWired();
            Listen();
            yield return WaitDoor("kayıttan devam");
            var drawer = ShiftPlayKit.Drawer;
            var mark = ShiftPlayKit.Mark();
            Assert.IsFalse(drawer.IsOpen, "çekmece kapalı");
            Assert.AreEqual(PassDrawer.Lamp.Amber, drawer.LampState);
            Assert.IsTrue(drawer.Blinking, "kapalı çekmecede lamba yanıp söner");
            Assert.IsFalse(View.HitArea.enabled, "kapalı çekmecedeki kâğıt tıklanmaz");
            Assert.AreEqual(Loc.T("ÇEKMECE · BELGE VAR"), drawer.DrawerTarget.HoverTitle, "çekmecenin etiketi");

            yield return BoardPlayKit.Frames(1);
            K.Admission.Enter();
            yield return ShiftPlayKit.WaitForPose(SiginakPose.Door, 4);
            yield return BoardPlayKit.Frames(2);
            var lever = drawer.Lever.Value;
            K.ClickHit(BoardPlayKit.FirstHit(BoardPlayKit.ScreenPoint(drawer.SwitchTarget.GetComponent<Collider>().bounds.center)));
            yield return BoardPlayKit.Frames(3);
            Assert.AreEqual(lever, drawer.Lever.Value, 1e-4f, "karar yokken kol kıpırdamaz");
            Assert.IsFalse(drawer.IsOpen, "şalter çekmeceyi açmaz");
            Assert.AreEqual(Loc.T("Gönderilecek belge yok."), drawer.SwitchTarget.Line, "şalterin satırı");

            K.ClickHit(BoardPlayKit.FirstHit(BoardPlayKit.ScreenPoint(drawer.DrawerTarget.GetComponent<Collider>().bounds.center)));
            yield return BoardPlayKit.WaitFor(() => drawer.FullyOpen, 3, "çekmece açılmadı");
            yield return BoardPlayKit.Frames(1);
            Assert.IsFalse(drawer.Blinking, "açık çekmecede lamba sürekli");
            Assert.IsTrue(View.HitArea.enabled, "açık çekmecedeki kâğıt tıklanır");
            Assert.AreEqual(Loc.T("Belgeye tıkla ya da çekmeceyi kapat."), drawer.DrawerTarget.Line);
            K.ClickHit(BoardPlayKit.FirstHit(BoardPlayKit.ScreenPoint(drawer.DrawerTarget.GetComponent<Collider>().bounds.center)));
            yield return BoardPlayKit.WaitFor(() => drawer.FullyClosed, 3, "çekmece kapanmadı");
            ShiftPlayKit.AssertUnchanged(mark, "çekmece ve şalter tıkları");

            yield return ShiftPlayKit.TakeFromDoor(View, "çekmeceden al");
            yield return BoardPlayKit.PressKey(Key.Digit1 + RowIndex("reddet"));
            yield return BoardPlayKit.PressKey(Key.Enter);
            yield return BoardPlayKit.WaitFor(() => View.State == ShelterEventPaperView.Where.Outbox && !Flow.Busy, 4, "karar çekmeceye konmadı (" + View.State + ")");
            yield return BoardPlayKit.Frames(1);
            Assert.AreEqual(PassDrawer.Lamp.Green, drawer.LampState, "karar hazırken lamba yeşil");
            Assert.IsTrue(drawer.Blinking);
            Assert.AreEqual(Loc.T("ÇEKMECE · KARAR HAZIR"), drawer.DrawerTarget.HoverTitle);
            Assert.AreEqual(Loc.T("Çekince çekmece kapanır, belge duvarın öbür yanına geçer."), drawer.SwitchTarget.Line);
            Assert.AreEqual(Loc.T("Karar çekmecede: yanındaki şalteri çekip dışarıya bildir."), K.DoorKey.Target.Line, "KAPI tuşu şalteri söyler");
            Assert.IsFalse(View.HitArea.enabled, "gönderilecek kâğıt tıklanmaz");
            int commands = Sim.Record.commands.Count;

            K.ClickHit(BoardPlayKit.FirstHit(BoardPlayKit.ScreenPoint(drawer.DrawerTarget.GetComponent<Collider>().bounds.center)));
            yield return BoardPlayKit.WaitFor(() => drawer.FullyClosed, 3, "çekmece kapanmadı");
            Assert.AreEqual(ShelterEventPaperView.Where.Outbox, View.State, "kapanan çekmecede karar bekler");
            yield return ShiftPlayKit.SendFromDrawer(View, "kapalı çekmeceden gönder");
            Assert.AreEqual(commands, Sim.Record.commands.Count, "şalter komut yazmaz");
            Assert.IsFalse(K.Admission.DeliveryWaiting, "kapıda belge kalmadı");
            Assert.IsFalse(drawer.Blinking);
        }

        // ================================================================== P6 · 14 tür kâğıda çıkar

        /// <summary>
        /// Her tür (hata ayıklama kurulumu, komut değil) tutanağa çıkar ve çekmeceye iner: başlık, satır sayısı, taşma yok; EN'de adlar
        /// dışında Türkçe harf yok; iki tür çekmeceden alınıp okunur punto ölçülür, bırakılınca çekmeceye döner (sonraki tür çekmece
        /// kapanınca gelir). Kurulum kalkınca kâğıt gizlenir.
        /// Hiçbiri komut yazmaz, RNG çekmez.
        /// </summary>
        [UnityTest]
        public IEnumerator AllFourteenKinds_ReachThePaper_TrAndEn()
        {
            yield return K.Begin();
            // Olaylar açık, hiçbiri kendiliğinden açılmaz: aralık uzak
            yield return K.ContinueFrom(K.FixtureGame(BoardPlayKit.DefaultSeed, b => { Quiet(b); b.eventGapMin = b.eventGapMax = 99; }));
            RequireWired();
            var mark = ShiftPlayKit.Mark();
            var alive = Sim.Board.People.Where(p => p.Alive).ToArray();
            Assert.GreaterOrEqual(alive.Length, 2);
            foreach (var lang in new[] { DeskLang.Tr, DeskLang.En })
            {
                DeskLoc.Instance.SetLanguage(lang);
                yield return BoardPlayKit.Frames(1);
                foreach (string kind in ShelterEventData.Kinds)
                {
                    var e = new ShelterEventInstance { Kind = kind, OpenedShift = Sim.S.Day, Amount = 3, Resource = "erzak", Area = "uretim" };
                    if (!Personless.Contains(kind)) e.PersonId = alive[0].Id;
                    if (kind == "kavga") e.OtherId = alive[1].Id;
                    Sim.ShelterEvents.Pending = e;
                    Flow.RefreshNow();
                    yield return WaitDoor(lang + " " + kind);
                    Assert.IsTrue(UnderDoorBoard, lang + " " + kind + ": kâğıt çekmecede");
                    Assert.AreEqual(ShelterEventData.Title(kind), Face.Title.text, lang + " " + kind + ": başlık");
                    Assert.AreEqual(ShelterEventData.Options(kind).Length, Face.RowCount, lang + " " + kind + ": satır sayısı");
                    Assert.IsFalse(Face.Overflowing, lang + " " + kind + ": taşma (alt " + Face.ContentBottomMm.ToString("0.0", CultureInfo.InvariantCulture) + " mm)");
                    if (lang == DeskLang.En)
                        foreach (var t in Face.Texts.Where(x => x.gameObject.activeInHierarchy && !string.IsNullOrEmpty(x.text)))
                        {
                            string text = t.text;
                            foreach (var p in Sim.Board.People) text = text.Replace(p.Name, "");
                            Assert.IsFalse(text.Any(c => TurkishOnly.IndexOf(c) >= 0), kind + ": EN'de Türkçe harf: " + t.name + " = " + t.text);
                        }
                    // Okunurluk: çekmeceden (K8) alınan kâğıtta en küçük yazı 1080p'de ≥ 14 px; bırakılan kâğıt çekmeceye döner
                    if (kind == "iz" || kind == "takas")
                    {
                        yield return ShiftPlayKit.TakeFromDoor(View, lang + " " + kind);
                        yield return BoardPlayKit.Frames(1);
                        float min = Face.Texts.Where(x => x.gameObject.activeInHierarchy && !string.IsNullOrEmpty(x.text))
                            .Select(x => K.ScreenPx(x, BoardPlayKit.View)).Where(px => px > 0).DefaultIfEmpty(0).Min();
                        Note(lang + " " + kind + ": elde en küçük yazı " + min.ToString("0.0", CultureInfo.InvariantCulture) + " px");
                        Assert.GreaterOrEqual(min, 14f, lang + " " + kind + ": elde okunur punto");
                        Flow.PutBack();
                        yield return WaitBack(lang + " " + kind + " geri");
                        Assert.IsTrue(UnderDoorBoard, lang + " " + kind + ": bırakılan kâğıt tablanın çocuğu");
                    }
                }
            }
            Sim.ShelterEvents.Pending = null;
            Flow.RefreshNow();
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, View.State, "kurulum kalkınca tutanak gizlenir");
            ShiftPlayKit.AssertUnchanged(mark, "hata ayıklama kurulumu ve sunum");
        }
    }
}
