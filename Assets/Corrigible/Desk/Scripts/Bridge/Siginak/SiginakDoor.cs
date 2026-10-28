using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak kapısının oyun bağlantısı. Biri kapıyı çalınca (vardiya raporu dosyalanıp masa boşalınca) vuruş sesi gelir,
    /// kapının lambası yanıp söner, tavan ampulü titrer, teleks “KAPI ÇALIYOR” basar ve kamera kapıya döner; kâğıt açılır. Kapalı kapıda gözetleme
    /// deliğinden bakmak ve interkomdan parolayı sormak sunumdur; kapıyı açmak, açmamak ve açık kapıdaki seçimler
    /// <see cref="LiberationDeskView.AnswerDoor"/> komutudur. Esc kâğıdı kapatır, ziyaretçi vardiya bitene kadar bekler
    /// (K ya da kapıya tıklamak geri getirir; vardiya biterse açılmadı sayılır ve teleksten basılır). Kapıyı açmak harekâtı
    /// bitirdiyse son rapor, kâğıttaki “Son rapor” düğmesine kadar bekletilir.
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

        /// <summary>Kapının üzerine gelme etiketi (sahnedeki başlık bu kaynağı taşır; satır dile göre buradan yazılır).</summary>
        static readonly string TargetTitle = Loc.N("SIĞINAK KAPISI"), TargetLine = Loc.N("Biri çalınca: gözetleme deliği, interkom, kapıyı aç ya da açma.");

        DoorPhase phase;
        DoorVisit shown, announced;
        bool peeked, asked;
        int logSeen, quietUntil = -1;
        float turnAt = -1, shotAt = -1;

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
            if (phase != DoorPhase.None && rig != null) rig.Locked = false;
            phase = DoorPhase.None;
            shown = announced = null;
            turnAt = shotAt = -1;
            logSeen = Sim?.S.Door?.Log.Count ?? 0;
            if (source != null) source.State.HoldEnding = false;
        }

        void Update()
        {
            var sim = Sim;
            if (sim?.S.Door == null) return;
            ReportUnanswered(sim);
            if (shotAt >= 0 && Time.unscaledTime >= shotAt)
            {
                shotAt = -1;
                Sfx.Play(Cue.DoorShot, 1f, Pan());
            }
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
                if (v == null || surface.BlockedExceptDoor || rig.InTransition) return;
                if (v != announced) Knock(v);
                else if (KeyPressed(Keyboard.current?.kKey)) Open(v);
                return;
            }
            ReadKeys();
        }

        void ReadKeys()
        {
            var kb = Keyboard.current;
            if (kb == null || card.Animating) return;
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (phase == DoorPhase.Result) Choose(shown?.Ending != null ? DoorViewBuilder.Ending : DoorViewBuilder.Close);
                else Close();
                return;
            }
            if (phase == DoorPhase.Result && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            {
                Choose(shown?.Ending != null ? DoorViewBuilder.Ending : DoorViewBuilder.Close);
                return;
            }
            for (int i = 0; i < card.Slots && i < 9; i++)
                if (KeyPressed(kb[Key.Digit1 + i]) || KeyPressed(kb[Key.Numpad1 + i])) { card.PressKey(i); return; }
        }

        static bool KeyPressed(UnityEngine.InputSystem.Controls.KeyControl key) => key != null && key.wasPressedThisFrame;

        /// <summary>Biri kapıyı çaldı: ses, lamba, teleks; biraz sonra kamera kapıya döner.</summary>
        void Knock(DoorVisit v)
        {
            announced = v;
            peeked = asked = false;
            prop.SetKnocking(true);
            if (bulb != null) bulb.Burst();
            Sfx.Play(Cue.DoorKnock, 0.9f, Pan());
            telex.Post(surface.Shift, Loc.T("KAPI ÇALIYOR"), Loc.T("Sığınağın kapısında biri var · K ile kapıya bak"), TelexKind.Alarm);
            turnAt = Time.unscaledTime + knockSeconds;
        }

        /// <summary>K ya da kapıya tıklamak: bekleyen ziyaretçinin kâğıdı yeniden açılır.</summary>
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
            if (v.Open) prop.SetOpen(true, !DoorCards.Get(v.Card).Nobody);
            card.Show(Build(), true);
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
                    card.Bind(Build());
                    return;
                case DoorViewBuilder.Ask:
                    asked = true;
                    Sfx.Play(Cue.TeamRadio, 0.6f, Pan());
                    card.Bind(Build());
                    return;
                case DoorViewBuilder.Close:
                    Close();
                    return;
                case DoorViewBuilder.Ending:
                    // Son rapor ancak şimdi açılır
                    if (source != null) { source.State.HoldEnding = false; source.MarkDirty(); }
                    card.Hide();
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
            var cardDef = DoorCards.Get(visit.Card);
            if (source != null) source.State.HoldEnding = true;
            var result = surface.AnswerDoor(id);
            if (source != null && sim.S.Ending == null) source.State.HoldEnding = false;
            if (result == null || !result.Ok) { card.Bind(Build()); return; }
            prop.SetKnocking(false);
            if (id == Simulation.DoorOpen)
            {
                prop.SetPeek(false);
                prop.SetOpen(true, !cardDef.Nobody);
                Sfx.Play(Cue.DoorOpen, 0.9f, Pan());
                if (visit.Ending == "vuruldu") shotAt = Time.unscaledTime + 0.9f;
            }
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
            card.Hide();
            prop.SetPeek(false);
            var v = Sim?.DoorVisitor;
            if (v == null || !v.Open) prop.SetOpen(false);
            phase = DoorPhase.None;
            shown = null;
            rig.Locked = false;
            rig.ResetView();
            Quiet();
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
                if (v.Open) prop.SetOpen(false);
            }
        }

        float Pan() => soundPoint != null && view != null ? Sfx.PanOf(view, soundPoint.position) : 0.6f;
    }
}
