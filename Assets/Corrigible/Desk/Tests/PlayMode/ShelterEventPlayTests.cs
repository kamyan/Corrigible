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
    /// Sığınak olaylarının masadaki sunumu (KOS aşama 4; tasarım <c>Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md</c> §7, soru 8: tutanak EVRAK
    /// tepsisinde) gerçek DeskGame'de, Play'de. Düzen <see cref="AdmissionPlayKit"/> ile aynı: kayıtlar test klasöründe, durumlara kayıt
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
        }

        void Unlisten()
        {
            if (listened != null)
            {
                if (onTelex != null) listened.TelexLine -= onTelex;
                if (onSound != null) listened.Sounded -= onSound;
            }
            listened = null;
            onTelex = null;
            onSound = null;
            telex.Clear();
            sounds.Clear();
        }

        /// <summary>Olay sunumu sahneye bağlı: denetleyici, kâğıt, tepsi çapası, masanın ve yüzeyin bağları.</summary>
        static void RequireWired()
        {
            var flow = Flow;
            Assert.IsNotNull(flow, "SiginakShelterEvent sahnede yok (SiginakRoom/Shelter event controller)");
            Assert.AreSame(flow, ShiftPlayKit.Get<SiginakShelterEvent>(BoardPlayKit.Desk, "shelterEvent"), "SiginakDesk.shelterEvent");
            Assert.AreSame(flow, ShiftPlayKit.Get<SiginakShelterEvent>(BoardPlayKit.Surface, "shelterEvent"), "LiberationDeskView.shelterEvent");
            Assert.IsNotNull(View, "SiginakShelterEvent.view (Olay tutanağı)");
            Assert.IsNotNull(Face, "ShelterEventPaperView.face");
            Assert.IsNotNull(View.HitArea, "tutanağın çarpıştırıcısı");
            Assert.IsNotNull(View.RestAnchor, "tepsideki dinlenme çapası");
            Assert.IsTrue(View.RestAnchor.IsChildOf(ShiftPlayKit.Tray.transform), "tutanak EVRAK tepsisinde (soru 8)");
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

        static IEnumerator WaitTray(string what)
        {
            yield return BoardPlayKit.WaitFor(() => View.State == ShelterEventPaperView.Where.Tray && !View.Moving, 3,
                what + ": tutanak tepsiye oturmadı (" + View.State + ")");
            yield return BoardPlayKit.Frames(1);
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

        /// <summary>
        /// Tepsideki tutanağa gerçek ışınla tık → ele gelir. Tepside kâğıt EVRAK'ın çarpıştırıcısının içinde yatar: ilk ışın tepsiye
        /// (DeskTarget → <c>SiginakDesk.OpenTray</c> → <c>TakeFromTray</c>) ya da kâğıdın kendisine düşer; ikisi de kabul. Kâğıdın ortası
        /// önce denenir; örtülüyse (K6'da şalter önünde) kâğıdın görünen bir noktası (oyuncunun tıklayacağı yer).
        /// </summary>
        static IEnumerator TakeFromTray()
        {
            Assert.AreEqual(ShelterEventPaperView.Where.Tray, View.State, "tutanak tepside");
            var tray = ShiftPlayKit.Tray.transform;
            bool OnTray(RaycastResult h) => h.gameObject != null && (h.gameObject == View.HitArea.gameObject || h.gameObject.transform.IsChildOf(tray) ||
                ShiftPlayKit.TrayTarget != null && h.gameObject == ShiftPlayKit.TrayTarget.gameObject);
            var centre = BoardPlayKit.ScreenPoint(View.transform.position);
            var hit = BoardPlayKit.FirstHit(centre);
            if (!OnTray(hit))
                for (int i = 1; i <= 9 && !OnTray(hit); i++)
                    for (int j = 1; j <= 5 && !OnTray(hit); j++)
                        hit = BoardPlayKit.FirstHit(BoardPlayKit.ScreenPoint(PaperWorld(new Vector2(ShelterEventPaperFace.WidthMm * i / 10f, ShelterEventPaperFace.HeightMm * j / 6f))));
            Assert.IsTrue(OnTray(hit), "tepsideki tutanağın görünen bir noktasına tık EVRAK'a ya da kâğıda düşer (orta: " + BoardPlayKit.DescribeHits(centre) + ")");
            K.ClickHit(hit);
            yield return WaitHand("tepsiden tık (" + hit.gameObject.name + ")");
        }

        /// <summary>
        /// Tutanak tepsiye konduktan sonraki ilk EVRAK tıkı eski yolu açar (arşivdeki son devir fişi, yoksa emir belgesi); tutanak tepside
        /// kalır, EVRAK etiketi tepsinin kendi etiketidir. Esc açılanı kapatır (kâğıt hazır olana dek yeniden basılır; masa açılınca
        /// basılmaz), kamera geldiği poza döner. Sonraki tık yine tutanaktır.
        /// </summary>
        static IEnumerator OpenArchiveOnce(string what)
        {
            Assert.IsTrue(Flow.ArchiveNext, what + ": sonraki EVRAK tıkı arşivi açmalı");
            yield return BoardPlayKit.Frames(1);
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, what + ": EVRAK etiketi tepsinin kendi etiketi");
            var pose = BoardPlayKit.Rig.Current;
            var screen = BoardPlayKit.ScreenPoint(View.transform.position);
            var hit = BoardPlayKit.FirstHit(screen);
            Assert.IsTrue(hit.gameObject != null && hit.gameObject.transform.IsChildOf(ShiftPlayKit.Tray.transform) ||
                ShiftPlayKit.TrayTarget != null && hit.gameObject == ShiftPlayKit.TrayTarget.gameObject,
                what + ": tepside ışın EVRAK'a düşer (" + BoardPlayKit.DescribeHits(screen) + ")");
            K.ClickHit(hit);
            yield return BoardPlayKit.Frames(2);
            Assert.IsFalse(Flow.ArchiveNext, what + ": arşiv sırası tüketildi");
            Assert.AreEqual(ShelterEventPaperView.Where.Tray, View.State, what + ": tutanak tepside kalır");
            Assert.IsTrue(BoardPlayKit.Surface.InputBlocked, what + ": arşiv fişi ya da emir belgesi açıldı");
            for (float t = 0; BoardPlayKit.Surface.InputBlocked && t < 8f; t += 0.4f)
            {
                yield return BoardPlayKit.WaitSeconds(0.4f);
                if (BoardPlayKit.Surface.InputBlocked) yield return BoardPlayKit.PressKey(Key.Escape);
            }
            yield return BoardPlayKit.WaitFor(() => !BoardPlayKit.Surface.InputBlocked, 4, what + ": arşiv kapanmadı");
            yield return ShiftPlayKit.WaitForPose(pose, 4);
            yield return BoardPlayKit.Frames(2);
            Assert.AreEqual(ShelterEventPaperView.Where.Tray, View.State, what + ": tutanak hâlâ tepside");
            Assert.AreEqual(Loc.T("EVRAK · OLAY TUTANAĞI"), ShiftPlayKit.TrayTarget.HoverTitle, what + ": EVRAK etiketi yeniden tutanağı söyler");
        }

        static int RowIndex(string id)
        {
            int i = Flow.Last.Rows.FindIndex(r => r.Id == id);
            Assert.GreaterOrEqual(i, 0, "satır yok: " + id);
            return i;
        }

        /// <summary>Kararın izi basılır, kâğıt tepsiye inip gizlenir; masa kilidi kalkar.</summary>
        static IEnumerator WaitFiled()
        {
            yield return BoardPlayKit.WaitFor(() => View.State == ShelterEventPaperView.Where.Hidden && !Flow.Busy, 4,
                "karara bağlanan tutanak dosyalanmadı (" + View.State + ")");
            yield return BoardPlayKit.Frames(2);
        }

        // ================================================================== P1 + P2 · gelir, kilitler, gerçek tıkla karar

        /// <summary>
        /// Konvoy gerçek kapanışta açılır: dosyalama bitince teleks "VARDİYA DEVREDİLDİ"den sonra OLAY satırını basar, tutanak tepsiye
        /// düşer (EVRAK etiketi "EVRAK · OLAY TUTANAĞI"), şalter "olay bekliyor" ile kilitlenir ve kola basış fiş basmaz. Tutanağa gerçek
        /// ışınla tık → ele gelir (masa ve kamera kısayolları kilitli); 2. satır ("reddet") gerçek tıkla işaretlenir (komutsuz), KARAR
        /// kutusu tek komut yazar (<c>event_decide:reddet</c>), teleks KARAR satırı, iz, kâğıt dosyalanır; şalter yeniden basar.
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
            yield return WaitTray("geliş");
            CollectionAssert.Contains(sounds, ShelterEventSound.Drop, "düşüş sesi");

            // Teleks: devirden sonra OLAY duyurusu
            var lines = ShiftPlayKit.Telex.Lines;
            int handover = -1, announce = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].status == Loc.T("VARDİYA DEVREDİLDİ")) handover = i;
                if (lines[i].status == Loc.T("OLAY")) announce = i;
            }
            string expected = Loc.F("{0} · tutanak EVRAK tepsisinde. Karar verilmeden vardiya kapanmaz.", ShelterEventData.Title("konvoy"));
            Assert.Greater(announce, handover, "OLAY satırı VARDİYA DEVREDİLDİ'den sonra (" + string.Join(" / ", lines.Select(l => l.status + " · " + l.message)) + ")");
            Assert.GreaterOrEqual(handover, 0, "VARDİYA DEVREDİLDİ satırı");
            Assert.AreEqual(expected, lines[announce].message);
            Assert.AreEqual(1, telex.Count(t => t.status == Loc.T("OLAY")), "duyuru bir kez");

            // Tepsideki tutanak ve EVRAK etiketi
            var d = Flow.Last;
            Assert.IsNotNull(d, "tutanağın verisi");
            Assert.AreEqual(ShelterEventData.Title("konvoy"), Face.Title.text, "başlık");
            Assert.AreEqual(Sim.ShelterEventOptions().Count, Face.RowCount, "satır sayısı");
            Assert.IsFalse(Face.Overflowing, "taşma yok");
            Assert.AreEqual(Loc.T("EVRAK · OLAY TUTANAĞI"), ShiftPlayKit.TrayTarget.HoverTitle, "EVRAK'ın etiketi tutanağı söyler");
            Assert.AreEqual(Loc.T("Karar bekliyor: al, bir satır seç, damgala."), ShiftPlayKit.TrayTarget.Line);
            Assert.IsTrue(ShiftPlayKit.Tray.AttentionLit, "tutanak beklerken EVRAK'ın hedef ışığı yanar");
            Assert.IsTrue(ShiftPlayKit.Tray.TargetLight != null && ShiftPlayKit.Tray.TargetLight.enabled, "hedef ışığı açık");

            // Şalter: olay bekliyor
            yield return BoardPlayKit.Frames(2);
            var sw = ShiftPlayKit.Switch;
            Assert.AreEqual(Loc.T("olay bekliyor"), sw.LockPlate, "plaka");
            Assert.AreEqual(Loc.F("{0} Tutanak EVRAK tepsisinde.", Sim.ShelterEventPrintCheck()), sw.LockLine, "üzerine gelme satırı: Core gerekçesi ve tutanağın yeri");
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

            // Ele al
            sounds.Clear();
            yield return TakeFromTray();
            CollectionAssert.Contains(sounds, ShelterEventSound.ToHand);
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
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "karardan sonra masa açık");
            Assert.IsFalse(BoardPlayKit.Rig.InputSuspended, "kamera askısı kalktı");
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
            yield return WaitTray("kayıttan devam");
            var closed = Sim.ShelterEventOptions().Single(o => o.Id == "kabul");
            Assert.IsFalse(closed.Open, "kabul kapalı (convoyAlarm büyük)");
            yield return TakeFromTray();
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

            // Tepsiye koy, dili değiştir: tepsideki kâğıt yeni dilde, son ret (eski dilde) taşınmaz
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return WaitTray("Esc");
            DeskLoc.Instance.SetLanguage(DeskLang.En);
            yield return BoardPlayKit.Frames(2);
            AssertEnglish("tepside EN");
            Assert.IsFalse(Face.Note.gameObject.activeInHierarchy && !string.IsNullOrEmpty(Face.Note.text), "tepsiye konunca son ret silinir: " + Face.Note.text);
            yield return OpenArchiveOnce("EN");
            yield return TakeFromTray();
            AssertEnglish("elde EN");
            Assert.IsFalse(Face.Note.gameObject.activeInHierarchy && !string.IsNullOrEmpty(Face.Note.text), "yeniden ele alınca eski ret yok: " + Face.Note.text);
            Assert.AreEqual(Loc.F("önce bir satır seç ({0})", (RowIndex("reddet") + 1).ToString(CultureInfo.InvariantCulture)), Face.ConfirmText.text);
            DeskLoc.Instance.SetLanguage(DeskLang.Tr);
            yield return BoardPlayKit.Frames(1);
            ShiftPlayKit.AssertUnchanged(mark, "kapalı seçenek, seçimsiz karar, dil ve arşiv");
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
        /// Açık olayla kayıttan devam: tutanak animasyonsuz tepside, OLAY teleksi yeniden basılmaz, yükleme komut yazmaz, şalter kilitli.
        /// Elde 1–3 tuşları işaretler, aynı tuşa ikinci basış işareti kaldırır, Enter karar verir (<c>event_decide:kabul</c>).
        /// </summary>
        [UnityTest]
        public IEnumerator Continue_RestoresPaperOnTray_AndKeysDecide()
        {
            yield return K.Begin();
            var fixture = ConvoyOpen(ConvoyOnly());
            int saved = fixture.Record.commands.Count;
            yield return K.ContinueFrom(fixture);
            RequireWired();
            Listen();
            Assert.AreEqual(saved, Sim.Record.commands.Count, "yükleme komut yazmaz");
            Assert.AreEqual(ShelterEventPaperView.Where.Tray, View.State, "tutanak tepside (animasyonsuz)");
            Assert.IsFalse(View.Moving, "düşüş yok");
            yield return BoardPlayKit.Frames(3);
            Assert.IsEmpty(telex, "kayıttan devamda teleks yeniden basılmaz");
            CollectionAssert.DoesNotContain(sounds, ShelterEventSound.Drop, "düşüş sesi yok");
            Assert.AreEqual(Loc.T("olay bekliyor"), ShiftPlayKit.Switch.LockPlate);

            yield return TakeFromTray();
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

        /// <summary>Elde Esc ve sürüklemesiz sağ tık tutanağı tepsiye döndürür (komutsuz); kamera K1'de kalır, askı kalkar.</summary>
        [UnityTest]
        public IEnumerator EscAndRightClick_PutThePaperBack()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(ConvoyOpen(ConvoyOnly()));
            RequireWired();
            Listen();
            var mark = ShiftPlayKit.Mark();
            var pose = BoardPlayKit.Rig.Current;

            yield return TakeFromTray();
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return WaitTray("Esc");
            CollectionAssert.Contains(sounds, ShelterEventSound.ToTray);
            Assert.AreEqual(pose, BoardPlayKit.Rig.Current, "Esc kamerayı oynatmaz (masaya ulaşmaz)");
            Assert.IsFalse(BoardPlayKit.Rig.InputSuspended, "askı kalktı");
            Assert.IsFalse(SettingsCabinet.Instance != null && SettingsCabinet.Instance.IsOpen, "Esc ayarları açmaz");

            // Tepsiye konan tutanağın ardından EVRAK tıkı bir kez eski yolu açar (arşiv fişi / emir belgesi), sonra yine tutanak
            yield return OpenArchiveOnce("Esc'ten sonra");
            yield return TakeFromTray();
            yield return BoardPlayKit.RightDrag(BoardPlayKit.ScreenCentre, Vector2.zero, 0);
            yield return WaitTray("sağ tık");
            Assert.AreEqual(pose, BoardPlayKit.Rig.Current, "sağ tık kamerayı oynatmaz");

            // Kâğıdın boş yerine tık da tepsiye koyar
            yield return OpenArchiveOnce("sağ tıktan sonra");
            yield return TakeFromTray();
            yield return ClickPaper(new Vector2(12, 12), "kâğıdın boş köşesi");
            yield return WaitTray("boş yere tık");
            Assert.IsTrue(Flow.ArchiveNext, "boş yere tık da tepsiye koymaktır");

            Assert.AreEqual("konvoy", Sim.PendingShelterEvent?.Kind);
            ShiftPlayKit.AssertUnchanged(mark, "tepsiye dönüş");
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
        /// Zincir: iz kâğıdı tepside; kişi satırı ve risk satırı ("tecrit her zaman bulgu vermez") görünür. Tecrit gerçek tıkla seçilir,
        /// KARAR → <c>event_decide:tecrit</c>; panodaki kartın şeridi "Tecritte · 3 v.", dosyanın etiketi "TECRİT · 3 V", destenin altında.
        /// </summary>
        [UnityTest]
        public IEnumerator IzPaper_Isolation_ShowsOnTheCard()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(IzChain(out string id));
            RequireWired();
            Listen();
            yield return WaitTray("iz kâğıdı");
            var person = Sim.Board.Person(id);
            Assert.AreEqual(ShelterEventData.Title("iz"), Face.Title.text);
            Assert.AreEqual(Loc.F("KİŞİ · {0}", person.Name), Face.Person.text, "kişi satırı");
            int iso = RowIndex("tecrit");
            var option = Sim.ShelterEventOptions()[iso];
            Assert.IsTrue(option.Open, "tecrit açık (yer var)");
            if (!string.IsNullOrEmpty(option.Risk)) Assert.AreEqual(Loc.F("Risk: {0}", option.Risk), Face.RowRisk(iso).text, "risk satırı");

            yield return TakeFromTray();
            var mark = ShiftPlayKit.Mark();
            yield return ClickRow(iso);
            yield return ClickConfirm();
            var since = ShiftPlayKit.Since(mark.commands);
            Assert.AreEqual(1, since.Count, ShiftPlayKit.Describe(since));
            Assert.AreEqual("event_decide", since[0].kind);
            Assert.AreEqual("tecrit", since[0].a);
            Assert.IsTrue(person.Isolated, "kişi tecritte");
            yield return WaitFiled();

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
        /// olayı kendiliğinden kapatır: tutanak gizlenir, teleks "kapandı" satırını basar, EVRAK'ın ışığı ve etiketi söner, şalterin kilidi
        /// kalkar. Olayın kurulumu hata ayıklama kurulumudur (komut değil); kapanış gerçek komutla ve <c>Changed</c> yoluyla gelir.
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
            ShiftPlayKit.RaiseChanged();
            yield return WaitTray("hırsızlık");
            Assert.IsTrue(Sim.ShelterEventOptions().Any(o => o.Open), "olayın açık satırı var");
            yield return BoardPlayKit.Frames(2);
            var sw = ShiftPlayKit.Switch;
            Assert.AreEqual(Loc.T("olay bekliyor"), sw.LockPlate, "olay şalteri kilitler");
            Assert.IsTrue(ShiftPlayKit.Tray.AttentionLit);
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
            Assert.IsFalse(ShiftPlayKit.Tray.AttentionLit, "EVRAK'ın ışığı söndü");
            Assert.IsNull(ShiftPlayKit.TrayTarget.TitleOverride, "EVRAK etiketi eski hâline döndü");
            yield return BoardPlayKit.WaitFor(() => sw.Phase == ShiftPhase.Ready && !sw.Moving, 3, "şalter hazıra dönmedi (" + sw.Phase + " / " + sw.LockPlate + ")");
            Assert.IsNull(sw.LockPlate, "kilit kalktı");
            Assert.IsFalse(BoardPlayKit.Surface.InputBlocked, "masa açık");
        }

        // ================================================================== P9 · grup ve olay aynı kapanışta

        /// <summary>
        /// Kapı her kapanışta çalar (fikstürde gelen gruplar geri çevrilir) ve konvoyun aralığı dolmak üzere: gerçek kapanışta grup da
        /// konvoy da gelir. Dosyalama bitince teleks sırası VARDİYA DEVREDİLDİ → OLAY → KAPI; tutanak tepside, grup kapıda, şalter kilitli.
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
            yield return WaitTray("geliş");
            yield return BoardPlayKit.WaitSeconds(0.5f);

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
            yield return TakeFromTray();
            Assert.AreEqual(ShelterEventData.Title("konvoy"), Face.Title.text);
        }

        // ================================================================== P10 · K6'dan ele alma

        /// <summary>
        /// Vardiya kapanış pozundan (K6) EVRAK'a gerçek tık tutanağı ele getirir: kâğıt bütünüyle ekranda, ilk ışın kâğıdın kendisi, satır
        /// gerçek tıkla işaretlenir; Esc tepsiye koyar, kamera K6'da kalır.
        /// </summary>
        [UnityTest]
        public IEnumerator TakeFromK6_PaperOnScreen_RealClicks()
        {
            yield return K.Begin();
            yield return K.ContinueFrom(ConvoyOpen(ConvoyOnly()));
            RequireWired();
            yield return WaitTray("kayıttan devam");
            BoardPlayKit.Rig.Focus(SiginakPose.ShiftClose);
            yield return ShiftPlayKit.WaitForPose(SiginakPose.ShiftClose, 3);
            yield return BoardPlayKit.Frames(2);
            var mark = ShiftPlayKit.Mark();
            yield return TakeFromTray();
            var cam = BoardPlayKit.View;
            foreach (var mm in new[] { new Vector2(0, 0), new Vector2(ShelterEventPaperFace.WidthMm, 0), new Vector2(0, ShelterEventPaperFace.HeightMm),
                         new Vector2(ShelterEventPaperFace.WidthMm, ShelterEventPaperFace.HeightMm) })
            {
                var v = cam.WorldToViewportPoint(PaperWorld(mm));
                Assert.IsTrue(v.z > 0 && v.x >= -0.01f && v.x <= 1.01f && v.y >= -0.01f && v.y <= 1.01f, "K6: kâğıdın köşesi ekranda " + mm + " → " + v);
            }
            int reject = RowIndex("reddet");
            yield return ClickRow(reject);
            Assert.AreEqual("reddet", Flow.Ui.SelectedId, "K6'da satır gerçek tıkla işaretlendi");
            yield return BoardPlayKit.PressKey(Key.Escape);
            yield return WaitTray("K6 Esc");
            Assert.AreEqual(SiginakPose.ShiftClose, BoardPlayKit.Rig.Current, "Esc kamerayı K6'dan oynatmaz");
            ShiftPlayKit.AssertUnchanged(mark, "K6'da ele alma ve işaret");
        }

        // ================================================================== P6 · 14 tür kâğıda çıkar

        /// <summary>
        /// Her tür (hata ayıklama kurulumu, komut değil) tutanağa çıkar: başlık, satır sayısı, taşma yok; EN'de adlar dışında Türkçe harf
        /// yok. Kurulum kalkınca kâğıt gizlenir. Hiçbiri komut yazmaz, RNG çekmez.
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
                    yield return WaitTray(lang + " " + kind);
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
                    // Okunurluk: elde en küçük yazı 1080p'de ≥ 14 px
                    if (kind == "iz" || kind == "takas")
                    {
                        yield return TakeFromTray();
                        float min = Face.Texts.Where(x => x.gameObject.activeInHierarchy && !string.IsNullOrEmpty(x.text))
                            .Select(x => K.ScreenPx(x, BoardPlayKit.View)).Where(px => px > 0).DefaultIfEmpty(0).Min();
                        Note(lang + " " + kind + ": elde en küçük yazı " + min.ToString("0.0", CultureInfo.InvariantCulture) + " px");
                        Assert.GreaterOrEqual(min, 14f, lang + " " + kind + ": elde okunur punto");
                        Flow.PutBack();
                        yield return WaitTray(lang + " " + kind + " geri");
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
