using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak kapısının oyun bağlantısı (Revizyon 2 · S4). Biri kapıyı çalınca (vardiya raporu dosyalanıp masa boşalınca)
    /// üç vuruş gelir, vuruş lambası amber yanar ve her vuruşta parlar, tavan ampulü titrer, teleks “KAPI ÇALIYOR” basar ve
    /// kamera kapıya döner; kâğıt açılır. Kapalı kapıda gözetleme deliğinden bakmak (kapak kayar) ve interkomdan parolayı
    /// sormak (amber nokta, cevap daktiloyla) sunumdur; kapıyı açmak, açmamak ve açık kapıdaki seçimler
    /// <see cref="LiberationDeskView.AnswerDoor"/> komutudur (yetersiz kaynakta komut denenir, teleks gerekçeyi basar, kâğıt
    /// titrer). Kapı açılınca merdivende kartın silueti görünür. Esc kâğıdı kapatır, ziyaretçi vardiya bitene kadar bekler
    /// (4 · KAPI, K ya da kapıya tıklamak geri getirir; vardiya biterse açılmadı sayılır ve teleksten basılır). Kapıyı açmak
    /// harekâtı bitirdiyse (VURULDU, SIZMA) atış anında oda ve lamba kırmızıya döner, kâğıda damga iner; yalnız Enter / SON
    /// RAPORA GEÇ etkindir ve son rapor ona basılana kadar bekletilir. Kapının yanındaki tebeşir çentiği ziyaretçileri sayar.
    /// </summary>
    public sealed class SiginakDoor : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SimDeskDataSource source;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] TelexFeed telex;
        [SerializeField] DoorProp prop;
        [SerializeField] DoorCardView card;
        [SerializeField] DeskTarget target;
        [Tooltip("Vuruşun duyulduğu nokta (sesin sağ-sol dengesi).")]
        [SerializeField] Transform soundPoint;
        [SerializeField] Camera view;
        [Tooltip("Vuruşta tavan ampulü titrer.")]
        [SerializeField] BulbFlicker bulb;
        [Tooltip("Vuruştan sonra kameranın kapıya dönmesine kadar geçen süre.")]
        [SerializeField, Min(0)] float knockSeconds = 1.1f;
        [Tooltip("Kapının yanındaki tebeşir ziyaretçi çentiği (her ziyaretçi bir çizgi).")]
        [SerializeField] ChalkTally tally;
        [Tooltip("Ölümcül kapıda kırmızıya dönen oda ışıkları (tavan ampulü, banker lambası; Soru 10'un ara cevabı).")]
        [SerializeField] Light[] roomLights = System.Array.Empty<Light>();
        [Tooltip("Kapı açıldıktan sonra atışın (ölümcül anın) gecikmesi.")]
        [SerializeField, Min(0)] float fatalSeconds = 0.9f;

        /// <summary>Kapının üzerine gelme etiketi (sahnedeki başlık bu kaynağı taşır; satır dile göre buradan yazılır).</summary>
        static readonly string TargetTitle = Loc.N("SIĞINAK KAPISI"), TargetLine = Loc.N("Biri çalınca: gözetleme deliği, interkom, kapıyı aç ya da açma.");

        DoorPhase phase;
        DoorVisit shown, announced;
        bool peeked, asked, red;
        int logSeen, quietUntil = -1;
        float turnAt = -1, fatalAt = -1;
        Color[] roomColors = System.Array.Empty<Color>();

        Simulation Sim => game != null ? game.Sim : null;
        /// <summary>Kâğıt açık, kapı kımıldıyor ya da vuruş kameranın dönmesini bekliyor: masa kilitli.</summary>
        public bool Busy => phase != DoorPhase.None || turnAt >= 0 || prop != null && prop.Moving && prop.isActiveAndEnabled ||
            Time.frameCount <= quietUntil;

        public void Configure(GameController controller, LiberationDeskView deskView, SimDeskDataSource data, SiginakCameraRig cameraRig, TelexFeed feed,
            DoorProp door, DoorCardView paper, DeskTarget doorTarget, Transform knock, Camera camera)
        {
            game = controller; surface = deskView; source = data; rig = cameraRig; telex = feed; prop = door; card = paper; target = doorTarget;
            soundPoint = knock; view = camera;
        }

        void OnEnable()
        {
            if (game != null) { game.NewGameStarted += ResetDoor; game.GameLoaded += ResetDoor; }
            card.Clicked += Choose;
            card.Dismissed += Dismiss;
            target.Clicked += Reopen;
            DeskLoc.Instance.Changed += Relabel;
            // Sahnedeki başlık çeviri tablosundaki kaynakla aynı olmalı (etiket onu çevirir)
            if (target.Title != TargetTitle) Debug.LogWarning($"Kapının etiketi '{target.Title}', beklenen '{TargetTitle}'.");
            ResetDoor();
            Relabel();
        }

        void OnDisable()
        {
            if (game != null) { game.NewGameStarted -= ResetDoor; game.GameLoaded -= ResetDoor; }
            card.Clicked -= Choose;
            card.Dismissed -= Dismiss;
            target.Clicked -= Reopen;
            DeskLoc.Instance.Changed -= Relabel;
        }

        /// <summary>Dil değişti: etiket satırı ve açık kâğıt yeniden yazılır.</summary>
        void Relabel()
        {
            target.LineOverride = Loc.T(TargetLine);
            if (card.IsOpen && shown != null) card.Bind(Build());
        }

        /// <summary>Yeni oyun ya da kayıttan devam: kâğıt kapanır, kapı kapalı; bekleyen ziyaretçi yeniden çalar.</summary>
        void ResetDoor()
        {
            card.Hide();
            prop.ResetClosed();
            RoomRed(false);
            if (phase != DoorPhase.None && rig != null) rig.Locked = false;
            phase = DoorPhase.None;
            shown = announced = null;
            turnAt = fatalAt = -1;
            logSeen = Sim?.S.Door?.Log.Count ?? 0;
            if (source != null) source.State.HoldEnding = false;
        }

        void Update()
        {
            var sim = Sim;
            if (sim?.S.Door == null) return;
            ReportUnanswered(sim);
            if (tally != null) tally.Show(sim.S.Door.Log.Count);
            prop.SetIntercom(card.IsOpen && card.IntercomTyping);
            if (fatalAt >= 0 && Time.unscaledTime >= fatalAt) Fatal();
            if (turnAt >= 0)
            {
                if (Time.unscaledTime < turnAt) return;
                turnAt = -1;
                if (sim.DoorVisitor != null && !surface.BlockedExceptDoor) Open(sim.DoorVisitor);
                return;
            }
            if (phase == DoorPhase.None)
            {
                var v = sim.DoorVisitor;
                prop.SetKnocking(v != null && !v.Open);
                if (surface.BlockedExceptDoor || rig.InTransition) return;
                if (v != null && v != announced) { Knock(v); return; }
                var kb = Keyboard.current;
                if (KeyPressed(kb?.digit4Key) || KeyPressed(kb?.numpad4Key) || KeyPressed(kb?.kKey)) Visit();
                return;
            }
            ReadKeys();
        }

        void ReadKeys()
        {
            var kb = Keyboard.current;
            if (kb == null || card.Animating) return;
            bool fatal = shown?.Ending != null;
            // Sonuçta Enter geniş düğmeye basar; ölümcülde öteki tuşlar devre dışı (Esc de)
            if (phase == DoorPhase.Result && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                card.PressAction();
                return;
            }
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (phase == DoorPhase.Result) { if (!fatal) Choose(DoorViewBuilder.Close); }
                else Close();
                return;
            }
            if (phase == DoorPhase.Result) return;
            for (int i = 0; i < card.Slots && i < 9; i++)
                if (KeyPressed(kb[Key.Digit1 + i]) || KeyPressed(kb[Key.Numpad1 + i])) { card.PressKey(i); return; }
        }

        static bool KeyPressed(UnityEngine.InputSystem.Controls.KeyControl key) => key != null && key.wasPressedThisFrame;

        /// <summary>Kâğıdın alt satırı (“masaya dön”): Esc gibi, sonuçta değil (orada geniş düğme kapatır).</summary>
        void Dismiss()
        {
            if (phase == DoorPhase.Closed || phase == DoorPhase.Open) Close();
        }

        /// <summary>Biri kapıyı çaldı: ses, lamba, teleks; biraz sonra kamera kapıya döner.</summary>
        void Knock(DoorVisit v)
        {
            announced = v;
            peeked = asked = false;
            prop.Knock();
            if (bulb != null) bulb.Burst();
            Sfx.Play(Cue.DoorKnock, 0.9f, Pan());
            telex.Post(surface.Shift, Loc.T("KAPI ÇALIYOR"), Loc.T("Sığınağın kapısında biri var · KAPI'dan bak"), TelexKind.Alarm);
            turnAt = Time.unscaledTime + knockSeconds;
        }

        /// <summary>
        /// KAPI (anahtar rayı, kenar satırı ya da 4/K tuşu): bekleyen ziyaretçinin kâğıdı açılır; kapıda kimse yoksa kamera
        /// kapıya bakar (MASA masaya döndürür).
        /// </summary>
        public void Visit()
        {
            var v = Sim?.DoorVisitor;
            if (phase != DoorPhase.None || surface.BlockedExceptDoor || rig.InTransition || turnAt >= 0) return;
            if (v == null) rig.Focus(SiginakPose.Door);
            else if (v == announced) Open(v);
        }

        /// <summary>Kapıya tıklamak: bekleyen ziyaretçinin kâğıdı yeniden açılır.</summary>
        void Reopen()
        {
            var v = Sim?.DoorVisitor;
            if (v == null || phase != DoorPhase.None || surface.BlockedExceptDoor) return;
            Open(v);
        }

        void Open(DoorVisit v)
        {
            if (v != announced) { announced = v; peeked = asked = false; }
            shown = v;
            phase = v.Open ? DoorPhase.Open : DoorPhase.Closed;
            rig.Focus(SiginakPose.Door);
            rig.Locked = true;
            if (v.Open) OpenDoor(v);
            card.Show(Build(), true);
        }

        /// <summary>Kapı açılır; merdivende kartın silueti (kimse durmuyorsa boş), ziyarete bağlı boy ve taraf.</summary>
        void OpenDoor(DoorVisit v)
        {
            var (height, mirror) = DoorViewBuilder.Variation(v);
            prop.SetOpen(true, DoorCards.Get(v.Card).Nobody ? null : DoorViewBuilder.Silhouette(v.Card), height, mirror);
        }

        DoorCardData Build() => DoorViewBuilder.Build(Sim, phase, shown, peeked, asked);

        void Choose(string id)
        {
            var sim = Sim;
            if (sim == null || shown == null) return;
            switch (id)
            {
                case DoorViewBuilder.Peek:
                    peeked = true;
                    prop.SetPeek(true);
                    Sfx.Play(Cue.DoorPeep, 0.7f, Pan());
                    card.Bind(Build());
                    return;
                case DoorViewBuilder.Ask:
                    // Cızırtı 0,2 s, amber nokta yanar; cevap kâğıda daktiloyla yazılır
                    asked = true;
                    Sfx.Play(Cue.TeamRadio, 0.6f, Pan());
                    card.Bind(Build());
                    return;
                case DoorViewBuilder.Close:
                    Close();
                    return;
                case DoorViewBuilder.Ending:
                    // Son rapor ancak şimdi açılır; oda ışığı geri döner
                    if (source != null) { source.State.HoldEnding = false; source.MarkDirty(); }
                    card.Hide();
                    RoomRed(false);
                    prop.SetFatal(false);
                    phase = DoorPhase.None;
                    rig.Locked = false;
                    Quiet();
                    return;
            }
            Answer(sim, id);
        }

        /// <summary>Komut: kapıyı aç, açma ya da açık kapıda bir seçim. Harekâtı bitirirse son rapor kâğıt kapanana kadar bekler.</summary>
        void Answer(Simulation sim, string id)
        {
            var visit = shown;
            if (source != null) source.State.HoldEnding = true;
            var result = surface.AnswerDoor(id);
            if (source != null && sim.S.Ending == null) source.State.HoldEnding = false;
            if (result == null || !result.Ok)
            {
                // Yapılamadı (ör. yetersiz erzak): teleks gerekçeyi bastı, kâğıt titrer
                card.Bind(Build());
                if (result != null) card.Shake();
                return;
            }
            prop.SetKnocking(false);
            if (id == Simulation.DoorOpen)
            {
                prop.SetPeek(false);
                OpenDoor(visit);
                Sfx.Play(Cue.DoorOpen, 0.9f, Pan());
            }
            // Harekâtı bitiren kapı: kapı aralanınca atış (ya da sızma) anı
            if (visit.Ending != null) fatalAt = Time.unscaledTime + (id == Simulation.DoorOpen ? fatalSeconds : 0.1f);
            // Ziyaret sürüyorsa açık kapı, bittiyse sonuç
            phase = sim.DoorVisitor == visit ? DoorPhase.Open : DoorPhase.Result;
            if (phase == DoorPhase.Result)
            {
                logSeen = sim.S.Door.Log.Count;
                telex.Post(surface.Shift, visit.Ending != null ? Loc.T("KAPI") : visit.Open ? Loc.T("KAPI AÇILDI") : Loc.T("KAPI AÇILMADI"),
                    DoorViewBuilder.Telex(visit), visit.Ending != null ? TelexKind.Alarm : TelexKind.Done);
            }
            card.Bind(Build());
        }

        /// <summary>Kâğıt kapanır, kamera masaya döner; ziyaret bittiyse kapı da kapanır.</summary>
        void Close()
        {
            card.Hide(true);
            prop.SetPeek(false);
            var v = Sim?.DoorVisitor;
            if (v == null || !v.Open)
            {
                if (prop.IsOpen) Sfx.Play(Cue.DoorClose, 0.8f, Pan());
                prop.SetOpen(false);
            }
            phase = DoorPhase.None;
            shown = null;
            rig.Locked = false;
            rig.ResetView();
            Quiet();
        }

        /// <summary>
        /// Ölümcül an (S4 › hareket): atış sesi (VURULDU), oda ışığı 0,05 s'de kırmızı, lamba kırmızı, kâğıt sarsılır ve
        /// damga iner (ağır “tak”); düğme 0,6 s sonra.
        /// </summary>
        void Fatal()
        {
            fatalAt = -1;
            if (shown?.Ending == null) return;
            if (shown.Ending == "vuruldu") Sfx.Play(Cue.DoorShot, 1f, Pan());
            Sfx.Play(Cue.StampHeavy, 0.9f);
            prop.SetFatal(true);
            RoomRed(true);
            card.PlayFatal();
        }

        /// <summary>Oda ışıklarının rengi: ölümcülde door.fatalRoomLight, değilse sahnedeki renk.</summary>
        void RoomRed(bool on)
        {
            if (red == on) return;
            red = on;
            if (roomColors.Length != roomLights.Length)
            {
                roomColors = new Color[roomLights.Length];
                for (int i = 0; i < roomLights.Length; i++) roomColors[i] = roomLights[i] != null ? roomLights[i].color : Color.white;
            }
            var fatalColor = SiginakTheme.Active.ColorOf("door.fatalRoomLight");
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) roomLights[i].color = on ? fatalColor : roomColors[i];
        }

        /// <summary>Kâğıdı kapatan Esc ya da Enter aynı karede masaya (ayarlar, şalter) geçmesin.</summary>
        void Quiet() => quietUntil = Time.frameCount + 1;

        /// <summary>Vardiya biterken kapıda kalan ziyaretçi açılmadı sayıldı: teleksten basılır.</summary>
        void ReportUnanswered(Simulation sim)
        {
            var log = sim.S.Door.Log;
            if (log.Count < logSeen) logSeen = 0;
            for (; logSeen < log.Count; logSeen++)
            {
                var v = log[logSeen];
                if (!v.Auto) continue;
                telex.Post(surface.Shift, Loc.T("KAPI AÇILMADI"), DoorViewBuilder.Telex(v), TelexKind.Info);
                if (v == announced) { announced = null; prop.SetKnocking(false); }
                if (v.Open) { prop.SetOpen(false); Sfx.Play(Cue.DoorClose, 0.8f, Pan()); }
            }
        }

        float Pan() => soundPoint != null && view != null ? Sfx.PanOf(view, soundPoint.position) : 0.6f;
    }
}
