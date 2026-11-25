using System;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis aşama 9: sunucu ödülünün masadaki seçimi (aşama 9 tasarımı §7.6, K25–K29; Docs/TESIS-BASKINLARI-TASARIMI.md §6.2). Bölgeyi
    /// tamamlayan baskının operasyon raporu dosyalanınca (<see cref="SiginakOperation"/> → <see cref="Present"/>) ödül kâğıdı kapıdan gelir
    /// (Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A): kapının yanındaki belge çekmecesinin tablasına konur, kapı bir kez vurur
    /// (<see cref="SiginakAdmission.KnockDelivery"/>), çekmecenin lambası, kapı lambası ve KAPI tuşu belgeyi gösterir; oyuncu çekmeceyi açar,
    /// tabladaki kâğıda tık onu ele getirir (<see cref="SiginakPassDrawer"/>).
    /// Kâğıt sığınak olayı tutanağının görünümünün ikinci örneğidir (<see cref="ShelterEventPaperView"/>): iki satır (TARİF, KAZANIM), 1/2
    /// ya da satır tıkı işaretler (sunum durumu; kayda girmez), Enter ya da KARAR kutusu tek komutu verir:
    /// <c>GameController.ExecuteBoardCommand(s =&gt; s.ChooseServerReward(bölge, seçim))</c> (<c>server_reward</c>; Rec ve Replay Core'da).
    /// Başarıda kâğıda "SEÇİLDİ" izi basılır, teleks Core'un sonucunu (ve tarifte defterin yeni sayfasını) yazar, kâğıt çekmecenin tablasına
    /// konur ve şalter çekilince duvarın öbür yanına geçer (yalnız sunum).
    /// <para>
    /// Esc, sürüklemesiz sağ tık ya da kâğıdın boş yerine tık: kâğıt çekmecenin tablasına döner. Çekmecesiz sahnede EVRAK tepsisine iner, tepsinin dikkat ışığı yanar ve etiketi
    /// "EVRAK · SUNUCU ÖDÜLÜ" olur; tepsiye ya da kâğıda tık geri getirir. Tepsi önceliği (K27): açık olay tutanağı varsa EVRAK tıkı önce onu
    /// getirir (<see cref="SiginakShelterEvent"/> değişmez; bu bileşen tepsiyi yalnız tutanak yokken üstlenir); kâğıdın kendi tıkı her zaman
    /// çalışır. Kayıttan devamda bekleyen ödülün kâğıdı animasyonsuz kapı panosundadır (rapor yeniden açılmaz, vuruş yok).
    /// </para>
    /// <para>
    /// Kâğıt eldeyken masa kilitlidir (<see cref="Busy"/> → <c>LiberationDeskView.InputBlocked</c>); kamera kısayolları askıdadır. Son ekranda
    /// (K29) <c>S.Ending</c> dolu olduğu için <see cref="FacilityRewardText.Pending"/> yanlıştır: kâğıt ve tepsi ışığı kapanır.
    /// </para>
    /// </summary>
    public sealed class SiginakServerReward : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] TelexFeed telex;
        [Tooltip("Kâğıt tepside beklerken EVRAK'ın hedef ışığı soluk nabızla yanar (EvrakTray.SetAttention).")]
        [SerializeField] EvrakTray tray;
        [Tooltip("EVRAK tepsisinin hedefi: kâğıt tepsideyken üzerine gelme etiketi onu söyler.")]
        [SerializeField] DeskTarget trayTarget;
        [Tooltip("Ödül kâğıdı: olay tutanağının görünümünün ikinci örneği.")]
        [SerializeField] ShelterEventPaperView view;
        [Tooltip("Olay tutanağı (tepsi önceliği: açık tutanak önce gelir).")]
        [SerializeField] SiginakShelterEvent shelterEvent;
        [SerializeField] HoverTag hoverTag;
        [Tooltip("Kapı (kartlı oyunda kabul): ödül kâğıdı çekmeceye konunca kapı bir kez vurur. Boşsa vuruş yok.")]
        [SerializeField] SiginakAdmission admission;
        [Tooltip("Belge çekmecesi: ödül kâğıdı yalnız çekmece kapalı ve boşken gelir. Boşsa beklemez.")]
        [SerializeField] SiginakPassDrawer passDrawer;

        GameRecord uiOf;
        ShelterEventPaperData last;
        /// <summary>Gösterilen (ya da yüklemede var sayılan) bekleyen ödülün bölgesi; null = ödül yok.</summary>
        string shownRegion;
        string selected, denied;
        bool commanding, arrivalPending, suspendedByMe, trayOverridden;
        float stampTimer = -1f;
        int inputFrame = -10;
        RightButtonGesture right;

        Simulation Sim => game != null ? game.Sim : null;

        /// <summary>Kâğıt elde ya da hareket hâlinde, ya da seçim izi basılıyor: masa kilitli.</summary>
        public bool Busy => view != null && view.Busy || stampTimer >= 0f;
        /// <summary>Son çizilen kâğıt (bekleyen ödül yoksa null).</summary>
        public ShelterEventPaperData Last => last;
        public ShelterEventPaperView View => view;
        /// <summary>Kâğıt elde (seçim yapılabilir).</summary>
        public bool InHand => view != null && view.State == ShelterEventPaperView.Where.Hand && stampTimer < 0f;
        /// <summary>Kâğıt tepside bekliyor.</summary>
        public bool InTray => last != null && view != null && view.State == ShelterEventPaperView.Where.Tray;
        /// <summary>Kâğıt çekmecenin tablasında okunmayı bekliyor (ya da oraya iniyor): KAPI tuşu ve kapı lambası bunu gösterir.</summary>
        public bool AtDoor => last != null && view != null && view.AtDoor;
        /// <summary>Seçilen ödülün kâğıdı tablada şalteri bekliyor.</summary>
        public bool InOutbox => view != null && view.InOutbox;
        /// <summary>Teleks satırı basıldı (durum, ileti; testler).</summary>
        public event Action<string, string> TelexLine;
        /// <summary>Çalınan her ses olayı (testler).</summary>
        public event Action<ShelterEventSound> Sounded;

        static float StampHoldS => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf("shelterEvent.motion.stampHoldS", 0.6f) : 0.6f;

        // ------------------------------------------------------------------ yaşam döngüsü

        void OnEnable()
        {
            if (game != null) { game.Changed += Refresh; game.GameLoaded += Restore; game.NewGameStarted += Restore; }
            DeskLoc.Instance.Changed += Render;
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
            // Oda baskından dönüp yeniden açılınca (SiginakRaidLink) aynı kaydın sunumu sürer: yeni ödül raporun dosyalanmasını bekler
            if (uiOf != null && Sim != null && ReferenceEquals(Sim.Record, uiOf)) Refresh();
            else Restore();
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Refresh; game.GameLoaded -= Restore; game.NewGameStarted -= Restore; }
            DeskLoc.Instance.Changed -= Render;
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
        }

        void ClickPaper() => Take();
        void RowClicked(int i) => Select(i);
        void ConfirmClicked() => Confirm();

        // ------------------------------------------------------------------ durum

        /// <summary>
        /// Kayıttan devam, yeni oyun ve açılış: sunum durumu sıfırlanır; bekleyen ödülün kâğıdı animasyonsuz kapı panosunda (rapor yeniden
        /// açılmaz, vuruş yok).
        /// </summary>
        public void Restore()
        {
            var sim = Sim;
            stampTimer = -1f;
            arrivalPending = false;
            selected = denied = null;
            right.Reset();
            uiOf = sim?.Record;
            shownRegion = FacilityRewardText.Region(sim);
            last = Build();
            if (view != null) view.Restore(last, true);
            SyncSuspend();
        }

        /// <summary>Testler: tazelemeyi şimdi uygular.</summary>
        public void RefreshNow() => Refresh();

        ShelterEventPaperData Build() => FacilityRewardText.Paper(Sim, selected, denied);

        /// <summary>
        /// Oyun değişti: yeni bekleyen ödül raporun dosyalanmasını bekler (<see cref="Present"/>; rapor yoksa hemen kapıya gelir); giden
        /// ödülün kâğıdı (seçildi, oyun bitti) gizlenir; aynı kâğıt yeniden çizilir. Kendi komutumuz sürerken atlanır.
        /// </summary>
        void Refresh()
        {
            if (commanding) return;
            var sim = Sim;
            if (sim == null) return;
            if (!ReferenceEquals(sim.Record, uiOf)) { Restore(); return; }
            string region = FacilityRewardText.Region(sim);
            if (region != shownRegion)
            {
                shownRegion = region;
                selected = denied = null;
                if (stampTimer < 0f && view != null && view.State != ShelterEventPaperView.Where.Filing && !view.InOutbox) view.HideNow();
                last = null;
                arrivalPending = region != null;
                Flush();
                return;
            }
            Render();
        }

        /// <summary>
        /// Operasyon raporu dosyalandı (<see cref="SiginakOperation"/>): bekleyen ödülün kâğıdı kapıdan gelir (kapı panosuna iner, kapı
        /// vurur); haritanın "BÖLGE ÖZGÜR" dizisi sıradaysa ondan sonra (<see cref="Flush"/>). Ödül yoksa hiçbir şey yapmaz. Kâğıt geldiyse
        /// ya da yoldaysa true.
        /// </summary>
        public bool Present()
        {
            var sim = Sim;
            if (sim == null || !FacilityRewardText.Pending(sim)) return false;
            shownRegion = FacilityRewardText.Region(sim);
            if (view == null) return false;
            // Önceki ödülün kâğıdı tablada şalteri bekliyorsa yeni kâğıt onun gidişini bekler (Flush)
            if (view.State != ShelterEventPaperView.Where.Hidden && !view.InOutbox) return true;
            arrivalPending = true;
            Flush();
            return true;
        }

        /// <summary>
        /// Yeni ödülün kâğıdı kapıya gelir: rapor bekliyorsa onun dosyalanmasını, haritanın özgür bölge dizisi sıradaysa onu bekler
        /// (önce bölgenin özgürlüğü, sonra kapıdaki vuruş).
        /// </summary>
        void Flush()
        {
            if (!arrivalPending) return;
            if (surface != null && (surface.RaidReportPending || surface.MapRevealPending)) return;
            // Çekmece: önceki kâğıt şalterle gitmeli, açık çekmece kapanmalı (dışarıdan yalnız kapalı çekmeceye konur)
            if (passDrawer != null && passDrawer.isActiveAndEnabled && !passDrawer.RequestDelivery()) return;
            arrivalPending = false;
            last = Build();
            if (view == null || last == null) return;
            Arrive();
        }

        /// <summary>Kâğıt kapı panosuna iner, kâğıt sesi ve kapının vuruşu.</summary>
        void Arrive()
        {
            view.HideNow();
            view.Bind(last, true, true);
            Play(ShelterEventSound.Drop);
            if (admission != null && admission.isActiveAndEnabled) admission.KnockDelivery();
        }

        /// <summary>Aynı kâğıdı yeniden çizer (seçim, ret, dil); iz basılırken ya da dosyalanırken dokunulmaz.</summary>
        void Render()
        {
            if (arrivalPending) return;
            var d = Build();
            last = d;
            if (view == null) return;
            var w = view.State;
            if (w == ShelterEventPaperView.Where.Stamping || w == ShelterEventPaperView.Where.Filing || w == ShelterEventPaperView.Where.Outbox) return;
            if (d == null) { if (w != ShelterEventPaperView.Where.Hidden) view.HideNow(); return; }
            if (w == ShelterEventPaperView.Where.Hidden) return;   // rapor dosyalanınca Present ya da Flush getirir
            view.Bind(d, false);
        }

        // ------------------------------------------------------------------ girdiler

        /// <summary>Kâğıt tıklanabilir mi: masanın öteki kilitleri yok (kâğıdın kendisi masa kilidini oluşturur).</summary>
        bool Open => surface == null || !surface.BlockedExceptReward;

        /// <summary>
        /// EVRAK tepsisine tık (<c>SiginakDesk.OpenTray</c> olay tutanağından sonra sorar): tutanak tepside değilse ve ödül kâğıdı tepsideyse ele
        /// gelir (§7.6: tepsiye tık geri getirir; tutanaktan farklı olarak arşiv sırası yok, ödül seçilene dek tepsi kâğıdı getirir).
        /// Ele geldiyse true.
        /// </summary>
        public bool TakeFromTray()
        {
            // Tepsi önceliği (K27) tepsideki tutanağındır; kapı panosundaki tutanak tepsiyi tutmaz
            if (shelterEvent != null && shelterEvent.isActiveAndEnabled && shelterEvent.InTray) return false;
            return InTray && Take();
        }

        bool Take()
        {
            if (view == null || last == null || stampTimer >= 0f || !Open) return false;
            if (view.View == null && rig != null) view.View = rig.View;
            if (!view.ToHand()) return false;
            if (denied != null) { denied = null; Render(); }
            ClearTrayOverride();
            SyncSuspend();
            return true;
        }

        /// <summary>Eldeki kâğıt EVRAK tepsisine iner (Esc, sağ tık, kâğıdın boş yerine tık; kapıdan alınmış olsa da); tepsiye ya da kâğıda tık
        /// geri getirir.</summary>
        public void PutBack()
        {
            if (view == null || stampTimer >= 0f || !InHand) return;
            inputFrame = Time.frameCount;
            right.Reset();
            denied = null;
            view.PutBack();
        }

        /// <summary>Esc'in tek girişinden (<c>LiberationDeskView.Escape</c>): kâğıt eldeyse tepsiye koyar; meşgulse de true.</summary>
        public bool HandleEscape()
        {
            if (inputFrame == Time.frameCount) return true;
            if (InHand) { PutBack(); return true; }
            return Busy;
        }

        /// <summary>Satırı işaretler (1–2 ya da satıra tık): kapalı satır komutsuz ret; açık satır işaretlenir (ikinci tık kaldırır).</summary>
        public bool Select(int index)
        {
            if (!InHand || last == null || index < 0 || index >= last.Rows.Count) return false;
            var row = last.Rows[index];
            if (!row.Enabled)
            {
                denied = row.Closed ?? last.Note;
                Play(ShelterEventSound.Deny);
                Render();
                return false;
            }
            selected = selected == row.Id ? null : row.Id;
            denied = null;
            Play(ShelterEventSound.Mark);
            Render();
            return true;
        }

        /// <summary>KARAR kutusu ya da Enter: seçim yoksa ya da Core reddederse komutsuz ret; değilse tek komut ve kâğıdın dosyalanışı.</summary>
        public bool Confirm() => InHand && Decide();

        /// <summary>
        /// Sunumun karar yolu (testler ve bitti ölçütü: <c>Decide(0)</c> tarif, <c>Decide(1)</c> kazanım): satırı işaretler ve kararı verir;
        /// kâğıt elde olmasa da (tepside) aynı komut ve iz. Karar verildiyse true.
        /// </summary>
        public bool Decide(int index)
        {
            if (last == null || index < 0 || index >= last.Rows.Count || stampTimer >= 0f) return false;
            if (!last.Rows[index].Enabled) { denied = last.Rows[index].Closed ?? last.Note; Render(); return false; }
            selected = last.Rows[index].Id;
            Render();
            return Decide();
        }

        bool Decide()
        {
            var sim = Sim;
            if (last == null || game == null || sim == null) return false;
            string region = FacilityRewardText.Region(sim);
            string choice = selected;
            if (choice == null) return Deny(Loc.T("Önce bir satır seç."));
            if (game.StoryHold) return Deny(Loc.T("Önce açılış belgesini kapat."));
            string why = sim.ChooseServerRewardCheck(region, choice);
            if (why != null) return Deny(why);
            var row = last.Selected;
            ActResult result;
            commanding = true;
            try { result = game.ExecuteBoardCommand(s => s.ChooseServerReward(region, choice)); }
            finally { commanding = false; }
            if (result == null || !result.Ok) return Deny(result?.Msg);
            Post(Loc.T("EMİR UYGULANDI"), result.Msg, TelexKind.Done);
            if (choice == FacilityGear.ChoiceRecipe)
            {
                string page = FacilityRewardText.NewPageLine(sim, region);
                if (!string.IsNullOrEmpty(page)) Post(Loc.T("EMİR UYGULANDI"), page, TelexKind.Done);
            }
            shownRegion = FacilityRewardText.Region(sim);
            selected = denied = null;
            stampTimer = StampHoldS;
            if (view != null) view.Stamp(FacilityRewardText.StampLines(row));
            Play(ShelterEventSound.Stamp);
            last = null;
            SyncSuspend();
            return true;
        }

        bool Deny(string why)
        {
            denied = why;
            Play(ShelterEventSound.Deny);
            Render();
            return false;
        }

        void Post(string status, string message, TelexKind kind)
        {
            int shift = surface != null ? surface.Shift : (Sim?.S.Day ?? 0) + 1;
            if (telex != null) telex.Post(shift, status, message, kind);
            TelexLine?.Invoke(status, message);
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
                    if (view != null) view.FileToDrawer();
                }
            }
            // Son ekran (K29): oyun bitince bekleyen ödül gösterilmez
            if (last != null && Sim != null && !FacilityRewardText.Pending(Sim) && stampTimer < 0f) Refresh();
            ReadHandInput();
            SyncSuspend();
        }

        void LateUpdate()
        {
            if (view != null)
            {
                view.Interactable = Open;
                view.DoorTitle = last != null ? Loc.T(FacilityRewardText.DoorTitle) : null;
                view.DoorLine = last?.HoverLine;
            }
            bool waiting = InTray;
            // Olay tutanağı kendi ışığını her karede yazar (DefaultExecutionOrder −10: önce); ödül kâğıdı bekliyorsa ışık açık kalır
            if (tray != null && waiting) tray.SetAttention(true);
            UpdateTrayTarget(waiting);
        }

        /// <summary>Kâğıt tepsideyken (ve tepside olay tutanağı yokken) EVRAK'ın etiketi onu söyler.</summary>
        void UpdateTrayTarget(bool waiting)
        {
            if (trayTarget == null) return;
            bool eventFirst = shelterEvent != null && shelterEvent.isActiveAndEnabled && shelterEvent.InTray;
            bool on = waiting && !eventFirst;
            if (!on) { ClearTrayOverride(); return; }
            trayTarget.TitleOverride = FacilityRewardText.TrayTitle;
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

        /// <summary>Elde Esc ve sürüklemesiz sağ tık kâğıdı tepsiye koyar; 1–2 satırı işaretler; Enter karar verir.</summary>
        void ReadHandInput()
        {
            if (!InHand) { right.Reset(); return; }
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) { PutBack(); return; }
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Select(0);
                else if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Select(1);
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

        void Play(ShelterEventSound sound)
        {
            Sounded?.Invoke(sound);
            Sfx.Play(SiginakShelterEvent.CueOf(sound));
        }
    }
}
