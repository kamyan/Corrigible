using System;
using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using TelexEntry = Corrigible.Desk.Bridge.ShelterEventSnapshotBuilder.TelexEntry;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak olaylarının masadaki sunumu (KOS aşama 4, tasarım §7; soru 8: tutanak EVRAK tepsisinde). Kapanışta açılan olayın
    /// tutanağı dosyalama karanlığı bittikten sonra tepsiye düşer ve teleks OLAY satırını basar; tepsiye (ya da kâğıda) tık kâğıdı
    /// ele getirir (yeni kamera pozu yok). Elde satıra tık ya da 1–3 satırı işaretler (sunum durumu, <see cref="ShelterEventUi"/>;
    /// kayda girmez), KARAR kutusuna tık ya da Enter kararı verir: önce Core'un denetimi (<c>ShelterEventDecisionCheck</c>), sonra
    /// tek komut <c>GameController.ExecuteBoardCommand(s =&gt; s.DecideShelterEvent(id))</c>. Kapalı satır ve seçimsiz karar komutsuz
    /// bir ret sesidir, gerekçe kâğıdın not satırına yazılır. Başarıda kâğıda "KARARA BAĞLANDI" izi basılır, teleks KARAR satırı
    /// düşer, kâğıt kısa bir beklemeden sonra tepsiye inip gizlenir.
    /// <para>
    /// Olay günlüğünün yeni girdileri (bulgu, bulgu yok, söküm, açlıktan ayrılma, kapanan tutanak, vadeli sonuçlar) teleksten
    /// basılır: kapanışta "VARDİYA DEVREDİLDİ"den sonra, KAPI satırlarından önce (<c>DefaultExecutionOrder(-10)</c>). Kayıttan devamda
    /// açık olayın kâğıdı animasyonsuz tepsidedir, satırlar yeniden basılmaz (yan dosyada zaten vardır).
    /// </para>
    /// <para>
    /// Kâğıt eldeyken masa kilitlidir (<see cref="Busy"/> → <c>LiberationDeskView.InputBlocked</c>); Esc, sürüklemesiz sağ tık, 1–3 ve
    /// Enter'ı bu bileşen kendisi okur (<c>SiginakShiftClose.ReadHandInput</c> kalıbı), kamera kısayolları askıdadır. Esc'in tek girişi
    /// <c>LiberationDeskView.Escape</c> da önce <see cref="HandleEscape"/>'e sorar.
    /// </para>
    /// <para>
    /// Tutanak tepside beklerken EVRAK'ın hedef ışığı soluk nabızla yanar (<c>EvrakTray.SetAttention</c>). Oyuncu tutanağı tepsiye
    /// koyduktan sonraki ilk EVRAK tıkı eski yolu açar (arşivdeki son devir fişi, yoksa emir belgesi); ondan sonraki tık tutanağı
    /// yeniden ele getirir. Kâğıdın kendisine tık her zaman tutanağı ele getirir.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class SiginakShelterEvent : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] TelexFeed telex;
        [Tooltip("Dosyalama sürerken yeni kâğıt ve teleks satırları bekler (ShiftFiling).")]
        [SerializeField] SiginakDesk desk;
        [Tooltip("Tutanak tepside karar beklerken EVRAK'ın hedef ışığı soluk nabızla yanar (EvrakTray.SetAttention).")]
        [SerializeField] EvrakTray tray;
        [Tooltip("EVRAK tepsisinin hedefi: tutanak tepsideyken üzerine gelme etiketi onu söyler.")]
        [SerializeField] DeskTarget trayTarget;
        [SerializeField] ShelterEventPaperView view;
        [SerializeField] HoverTag hoverTag;

        /// <summary>Tepsideki tutanağın etiket başlığı (kaynak metin; <c>DeskTarget.HoverTitle</c> çevirir).</summary>
        static readonly string TrayTitle = Loc.N("EVRAK · OLAY TUTANAĞI");

        readonly ShelterEventUi ui = new ShelterEventUi();
        readonly List<TelexEntry> pendingTelex = new List<TelexEntry>();
        GameRecord uiOf;
        ShelterEventPaperData last;
        /// <summary>Sunulmuş (ya da yüklemede var sayılan) açık olayın anahtarı; null = olay yok.</summary>
        string announcedKey;
        int seenLog;
        bool commanding, arrivalPending, suspendedByMe, trayOverridden;
        /// <summary>Oyuncu tutanağı tepsiye koydu: sonraki EVRAK tıkı eski yolu (arşiv fişi, emir belgesi) açar.</summary>
        bool archiveNext;
        float stampTimer = -1f;
        int inputFrame = -10;
        RightButtonGesture right;

        Simulation Sim => game != null ? game.Sim : null;
        /// <summary>Rehber son şalteri mühürlüyor: "konum çözüldü" teleksi onu seçenek olarak önermez.</summary>
        bool TutorialSeal => desk != null && desk.TutorialSealsSwitch;

        /// <summary>Kâğıt elde ya da hareket hâlinde, ya da karar izi basılıyor: masa kilitli.</summary>
        public bool Busy => view != null && view.Busy || stampTimer >= 0f;
        /// <summary>Son çizilen tutanak (gösterilecek olay yoksa null).</summary>
        public ShelterEventPaperData Last => last;
        public ShelterEventUi Ui => ui;
        public ShelterEventPaperView View => view;
        /// <summary>Sonraki EVRAK tıkı arşivi açar (tutanak az önce tepsiye kondu; testler).</summary>
        public bool ArchiveNext => archiveNext;
        /// <summary>Teleks satırı basıldı (durum, ileti; testler).</summary>
        public event Action<string, string> TelexLine;
        /// <summary>Çalınan her ses olayı (testler; ses <see cref="Sfx"/> ile ayrıca çalar).</summary>
        public event Action<ShelterEventSound> Sounded;

        static float StampHoldS => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf("shelterEvent.motion.stampHoldS", 0.6f) : 0.6f;

        // ------------------------------------------------------------------ yaşam döngüsü

        void OnEnable()
        {
            if (game != null) { game.Changed += Refresh; game.GameLoaded += Restore; game.NewGameStarted += Restore; }
            DeskLoc.Instance.Changed += Relabel;
            if (view != null)
            {
                view.Clicked += ClickPaper;
                view.RowClicked += RowClicked;
                view.ConfirmClicked += ConfirmClicked;
                view.PutBackClicked += PutBack;
                view.Sound += Play;
                if (view.Tag == null && hoverTag != null) view.Tag = hoverTag;
                if (view.View == null && rig != null) view.View = rig.View;
            }
            Restore();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.GameLoaded -= Restore; game.NewGameStarted -= Restore; }
            DeskLoc.Instance.Changed -= Relabel;
            if (view != null)
            {
                view.Clicked -= ClickPaper;
                view.RowClicked -= RowClicked;
                view.ConfirmClicked -= ConfirmClicked;
                view.PutBackClicked -= PutBack;
                view.Sound -= Play;
            }
            right.Reset();
            if (suspendedByMe && rig != null) rig.InputSuspended = false;
            suspendedByMe = false;
            ClearTrayOverride();
            if (tray != null) tray.SetAttention(false);
        }

        /// <summary>Kâğıdın kendisine tık: arşiv sırası beklese de tutanak ele gelir.</summary>
        void ClickPaper() => Take();
        void RowClicked(int i) => Select(i);
        void ConfirmClicked() => Confirm();

        // ------------------------------------------------------------------ durum

        /// <summary>
        /// Kayıttan devam, yeni oyun ve açılış: sunum durumu sıfırlanır, günlük okunmuş sayılır (teleks yeniden basılmaz), açık olayın
        /// kâğıdı animasyonsuz tepside.
        /// </summary>
        public void Restore()
        {
            var sim = Sim;
            stampTimer = -1f;
            arrivalPending = false;
            archiveNext = false;
            pendingTelex.Clear();
            ui.Reset();
            right.Reset();
            uiOf = sim?.Record;
            seenLog = sim?.ShelterEvents?.Log.Count ?? 0;
            announcedKey = ShelterEventSnapshotBuilder.KeyOf(ShelterEventSnapshotBuilder.Visible(sim));
            last = Build();
            if (view != null) view.Restore(last);
            SyncSuspend();
        }

        /// <summary>Testler: tazelemeyi şimdi uygular (hata ayıklama kurulumu Changed yaymaz).</summary>
        public void RefreshNow() => Refresh();

        void Relabel() => Render();

        /// <summary>
        /// Oyun değişti: günlüğün yeni girdileri teleks sırasına, yeni olay duyuru ve düşüş sırasına (dosyalama bitince); giden olayın
        /// kâğıdı (kendi kararımız değilse) gizlenir; aynı kâğıt yeniden çizilir. Kendi komutumuz sürerken atlanır.
        /// </summary>
        void Refresh()
        {
            if (commanding) return;
            var sim = Sim;
            if (sim == null) return;
            if (!ReferenceEquals(sim.Record, uiOf)) { Restore(); return; }
            pendingTelex.AddRange(ShelterEventSnapshotBuilder.TelexLines(sim, seenLog, out seenLog, TutorialSeal));
            string key = ShelterEventSnapshotBuilder.KeyOf(ShelterEventSnapshotBuilder.Visible(sim));
            if (key != announcedKey)
            {
                announcedKey = key;
                ui.Reset();
                archiveNext = false;
                if (stampTimer < 0f && view != null && view.State != ShelterEventPaperView.Where.Filing) view.HideNow();
                // Yeni kâğıt dosyalama bitince düşer (Flush); o zamana dek tepside kâğıt yok
                last = null;
                arrivalPending = key != null;
                Flush();
                return;
            }
            Render();
        }

        ShelterEventPaperData Build() => ShelterEventSnapshotBuilder.Build(Sim, ui, game != null && game.StoryHold);

        /// <summary>Aynı kâğıdı yeniden çizer (seçim, ret, kilit, dil); iz basılırken ya da dosyalanırken dokunulmaz.</summary>
        void Render()
        {
            if (arrivalPending) return;
            var d = Build();
            if (d != null && d.Key != announcedKey) return;
            last = d;
            if (view == null) return;
            var w = view.State;
            if (w == ShelterEventPaperView.Where.Stamping || w == ShelterEventPaperView.Where.Filing) return;
            if (d == null) { if (w != ShelterEventPaperView.Where.Hidden) view.HideNow(); return; }
            view.Bind(d, false);
        }

        /// <summary>
        /// Bekleyen teleks satırları ve yeni kâğıdın gelişi (dosyalama karanlığı sürerken bekler): önce günlük satırları, sonra OLAY
        /// duyurusu ve tepsiye düşüş.
        /// </summary>
        void Flush()
        {
            if (pendingTelex.Count == 0 && !arrivalPending) return;
            if (desk != null && desk.ShiftFiling) return;
            foreach (var t in pendingTelex) Post(t);
            pendingTelex.Clear();
            if (!arrivalPending) return;
            arrivalPending = false;
            var e = ShelterEventSnapshotBuilder.Visible(Sim);
            if (e == null || ShelterEventSnapshotBuilder.KeyOf(e) != announcedKey) { last = null; return; }
            Post(ShelterEventSnapshotBuilder.Announcement(e));
            last = Build();
            if (view != null)
            {
                view.HideNow();
                view.Bind(last, true);
            }
            Play(ShelterEventSound.Drop);
        }

        void Post(TelexEntry t)
        {
            int shift = surface != null ? surface.Shift : (Sim?.S.Day ?? 0) + 1;
            if (telex != null) telex.Post(shift, t.Status, t.Message, t.Kind);
            TelexLine?.Invoke(t.Status, t.Message);
        }

        // ------------------------------------------------------------------ girdiler

        /// <summary>Kâğıt tıklanabilir mi: masanın öteki kilitleri yok (kâğıdın kendisi masa kilidini oluşturur).</summary>
        bool Open => surface == null || !surface.BlockedExceptEvent;

        /// <summary>
        /// EVRAK tepsisine tık (<c>SiginakDesk.OpenTray</c> önce buraya sorar): tutanak tepsideyse ele gelir. Oyuncu onu az önce tepsiye
        /// koyduysa bu tık bir kez eski yola bırakılır (arşiv fişi, emir belgesi; sonraki tık yine tutanak). Ele geldiyse true; değilse
        /// çağıran eski EVRAK yolunu açar.
        /// </summary>
        public bool TakeFromTray()
        {
            if (archiveNext && last != null && view != null && view.State == ShelterEventPaperView.Where.Tray && Open && stampTimer < 0f)
            {
                archiveNext = false;
                return false;
            }
            return Take();
        }

        bool Take()
        {
            if (view == null || last == null || !Open || stampTimer >= 0f) return false;
            if (view.View == null && rig != null) view.View = rig.View;
            if (!view.ToHand()) return false;
            archiveNext = false;
            // Önceki elde kalmanın reddi (eski dilde ya da bayat) yeni bakışa taşınmaz
            if (ui.LastDenied != null) { ui.LastDenied = null; Render(); }
            ClearTrayOverride();
            SyncSuspend();
            return true;
        }

        /// <summary>Eldeki kâğıt tepsiye döner (Esc, sağ tık, kâğıdın boş yerine tık); sonraki EVRAK tıkı arşivi açar.</summary>
        public void PutBack()
        {
            if (view == null || stampTimer >= 0f || !InHand) return;
            inputFrame = Time.frameCount;
            right.Reset();
            ui.LastDenied = null;
            archiveNext = true;
            view.PutBack();
        }

        /// <summary>
        /// Esc'in tek girişinden (<c>LiberationDeskView.Escape</c>): kâğıt eldeyse tepsiye koyar. Kâğıt meşgulse ya da Esc bu karede
        /// işlendiyse de true (masaya ve ayarlara ulaşmaz).
        /// </summary>
        public bool HandleEscape()
        {
            if (inputFrame == Time.frameCount) return true;
            if (InHand) { PutBack(); return true; }
            return Busy;
        }

        bool InHand => view != null && view.State == ShelterEventPaperView.Where.Hand && stampTimer < 0f;

        /// <summary>
        /// Satırı işaretler (1–3 ya da satıra tık): kapalı satır komutsuz ret sesi ve not satırında gerekçe; açık satır işaretlenir
        /// (aynı satıra ikinci tık işareti kaldırır). Yalnız sunum durumu: komut yazmaz. İşlediyse true.
        /// </summary>
        public bool Select(int index)
        {
            if (!InHand || last == null || index < 0 || index >= last.Rows.Count) return false;
            var row = last.Rows[index];
            ui.Align(last.Key);
            if (!row.Enabled)
            {
                ui.LastDenied = row.Closed ?? last.Note;
                Play(ShelterEventSound.Deny);
                Render();
                return false;
            }
            ui.SelectedId = ui.SelectedId == row.Id ? null : row.Id;
            ui.LastDenied = null;
            Play(ShelterEventSound.Mark);
            Render();
            return true;
        }

        /// <summary>
        /// KARAR kutusu (ya da Enter): seçim yoksa ya da Core denetimi reddederse komutsuz ret; değilse tek komut
        /// (<c>event_decide</c>), karar izi, teleks KARAR satırı ve kâğıdın dosyalanışı. Karar verildiyse true.
        /// </summary>
        public bool Confirm()
        {
            if (!InHand || last == null || game == null) return false;
            var sim = Sim;
            ui.Align(last.Key);
            string id = last.Selected?.Id;
            if (id == null) return Deny(Loc.T("Önce bir satır seç."));
            if (game.StoryHold) return Deny(Loc.T("Önce açılış belgesini kapat."));
            string why = sim.ShelterEventDecisionCheck(id);
            if (why != null) return Deny(why);
            string kind = sim.PendingShelterEvent?.Kind;
            ActResult result;
            commanding = true;
            try { result = game.ExecuteBoardCommand(s => s.DecideShelterEvent(id)); }
            finally { commanding = false; }
            if (result == null || !result.Ok) return Deny(result?.Msg);
            // Kendi kararımız: günlükteki KARAR satırı hemen; olay kalktı, Refresh bunu "giden olay" saymasın
            foreach (var t in ShelterEventSnapshotBuilder.TelexLines(sim, seenLog, out seenLog, TutorialSeal)) Post(t);
            announcedKey = ShelterEventSnapshotBuilder.KeyOf(ShelterEventSnapshotBuilder.Visible(sim));
            ui.Reset();
            stampTimer = StampHoldS;
            if (view != null) view.Stamp(ShelterEventSnapshotBuilder.StampLines(kind, id));
            Play(ShelterEventSound.Stamp);
            last = null;
            SyncSuspend();
            return true;
        }

        bool Deny(string why)
        {
            ui.LastDenied = why;
            Play(ShelterEventSound.Deny);
            Render();
            return false;
        }

        // ------------------------------------------------------------------ kare

        void Update()
        {
            if (view != null && view.View == null && rig != null) view.View = rig.View;
            Flush();
            if (stampTimer >= 0f)
            {
                stampTimer -= Time.unscaledDeltaTime;
                if (stampTimer < 0f)
                {
                    stampTimer = -1f;
                    if (view != null) view.FileAndHide();
                }
            }
            ReadHandInput();
            SyncSuspend();
        }

        void LateUpdate()
        {
            if (view != null) view.Interactable = Open;
            bool waiting = last != null && view != null && view.State == ShelterEventPaperView.Where.Tray;
            if (tray != null) tray.SetAttention(waiting);
            UpdateTrayTarget(waiting);
        }

        /// <summary>
        /// Tutanak tepsideyken EVRAK'ın etiketi onu söyler; değilse (ya da sonraki tık arşivi açacaksa) tepsinin kendi etiketi.
        /// </summary>
        void UpdateTrayTarget(bool waiting)
        {
            if (trayTarget == null) return;
            bool on = waiting && !archiveNext;
            if (!on) { ClearTrayOverride(); return; }
            trayTarget.TitleOverride = TrayTitle;
            trayTarget.LineOverride = last.HoverLine;
            trayOverridden = true;
        }

        void ClearTrayOverride()
        {
            if (!trayOverridden || trayTarget == null) return;
            trayTarget.TitleOverride = null;
            trayTarget.LineOverride = null;
            trayOverridden = false;
        }

        /// <summary>Elde Esc ve sürüklemesiz sağ tık kâğıdı tepsiye koyar; 1–3 satırı işaretler; Enter karar verir.</summary>
        void ReadHandInput()
        {
            if (!InHand) { right.Reset(); return; }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { PutBack(); return; }
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Select(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Select(1);
                else if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Select(2);
                else if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) { Confirm(); return; }
            }
            var mouse = Mouse.current;
            if (mouse == null) { right.Reset(); return; }
            var button = mouse.rightButton;
            var position = mouse.position.ReadValue();
            if (button.wasPressedThisFrame) right.Press(position);
            if (!right.Held) return;
            if (button.isPressed) right.Move(position, mouse.delta.ReadValue(), SiginakCameraRig.RightClickSlop);
            else if (button.wasReleasedThisFrame) { if (right.Release()) PutBack(); }
            else right.Reset();
        }

        /// <summary>Kamera kısayolları, bakış ve sağ tık kâğıt meşgulken kapalı (yalnız kendi açtığı askıyı kaldırır).</summary>
        void SyncSuspend()
        {
            if (rig == null) return;
            bool want = Busy;
            if (want && !suspendedByMe) { rig.InputSuspended = true; suspendedByMe = true; }
            else if (!want && suspendedByMe) { rig.InputSuspended = false; suspendedByMe = false; }
        }

        // ------------------------------------------------------------------ ses

        void Play(ShelterEventSound sound)
        {
            Sounded?.Invoke(sound);
            Sfx.Play(CueOf(sound));
        }

        /// <summary>Tutanağın ses olayı → var olan cue (yeni klip yok).</summary>
        public static Cue CueOf(ShelterEventSound sound) => sound switch
        {
            ShelterEventSound.Drop => Cue.PaperSlide,
            ShelterEventSound.ToHand => Cue.BoardLift,
            ShelterEventSound.Mark => Cue.AdmissionMarker,
            ShelterEventSound.Stamp => Cue.StampHeavy,
            ShelterEventSound.Deny => Cue.BoardDeny,
            ShelterEventSound.ToTray => Cue.BoardReturn,
            _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, null),
        };
    }
}
