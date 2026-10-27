using System.Collections.Generic;
using Corrigible.Core;
using Corrigible.Game;
using UnityEngine;
using UnityEngine.Rendering;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak (K4) oyun bağlantısı (Docs/UI_Siginak Aşama 6): kesit maketinin odaları, BOŞTA tepsisi ve sığınak özeti
    /// panosu. Simülasyonu yalnız okur; ONAR / GELİŞTİR ve ± PERS. <see cref="LiberationDeskView"/>'in komutlarından geçer
    /// (sonuç ve gerekçe teleksten). Raf pozunda (K4) maket bütün olarak değil oda oda tıklanır; oda ampulleri ve ortam
    /// ışığı poza göre ölçeklenir (tokens › lights.roomBulb.k4Scale, lights.ambient.k4Scale).
    /// </summary>
    public sealed class SiginakShelter : MonoBehaviour
    {
        [SerializeField] GameController game;
        [SerializeField] LiberationDeskView surface;
        [SerializeField] SiginakCameraRig rig;
        [SerializeField] RoomView[] rooms = System.Array.Empty<RoomView>();
        [SerializeField] DeskTarget[] roomTargets = System.Array.Empty<DeskTarget>();
        [SerializeField] IdleTray tray;
        [SerializeField] ShelterClipboard board;
        [Tooltip("K1'de maketin bütünü (2 SIĞINAK); raf pozunda kapanır, odalar tek tek tıklanır.")]
        [SerializeField] DeskTarget shelfTarget;

        string selected = ShelterLogic.Living;
        readonly Dictionary<string, int> workers = new Dictionary<string, int>();
        float k4;
        bool ambientSaved;
        Color sky, equator, ground;
        System.Action[] roomClicks = System.Array.Empty<System.Action>();

        Simulation Sim => game != null ? game.Sim : null;
        public string Selected => selected;

        public void Configure(GameController controller, LiberationDeskView desk, SiginakCameraRig cameraRig, RoomView[] roomViews, DeskTarget[] targets,
            IdleTray idleTray, ShelterClipboard clipboard, DeskTarget shelf)
        {
            game = controller; surface = desk; rig = cameraRig; rooms = roomViews; roomTargets = targets; tray = idleTray; board = clipboard; shelfTarget = shelf;
        }

        void OnEnable()
        {
            if (game != null) { game.Changed += Changed; game.GameLoaded += ResetState; game.NewGameStarted += ResetState; }
            DeskLoc.Instance.Changed += Changed;
            board.ActionClicked += Upgrade;
            board.MinusClicked += Minus;
            board.PlusClicked += Plus;
            roomClicks = new System.Action[roomTargets.Length];
            for (int i = 0; i < roomTargets.Length; i++)
            {
                int index = i;
                roomClicks[i] = () => Select(rooms[index].RoomId);
                roomTargets[i].Clicked += roomClicks[i];
            }
            workers.Clear();
            Refresh(false);
        }

        void OnDisable()
        {
            if (game != null) { game.Changed -= Changed; game.GameLoaded -= ResetState; game.NewGameStarted -= ResetState; }
            DeskLoc.Instance.Changed -= Changed;
            board.ActionClicked -= Upgrade;
            board.MinusClicked -= Minus;
            board.PlusClicked -= Plus;
            for (int i = 0; i < roomTargets.Length && i < roomClicks.Length; i++) roomTargets[i].Clicked -= roomClicks[i];
            RestoreAmbient();
        }

        void ResetState() { workers.Clear(); Refresh(false); }
        void Changed() => Refresh(true);

        public void Select(string roomId)
        {
            if (surface != null && surface.InputBlocked) return;
            selected = roomId;
            Refresh(false);
        }

        RoomView View(string id)
        {
            foreach (var r in rooms) if (r.RoomId == id) return r;
            return null;
        }

        void Refresh(bool animate)
        {
            var sim = Sim;
            if (sim == null) return;
            var data = ShelterViewBuilder.Rooms(sim, selected);
            // ± PERS. hareketi: piyon tepsiden yuvaya ya da yuvadan tepsiye zıplar (bağlamadan önceki konumlar)
            Vector3 fromTray = tray.LastPawnWorld();
            string arrived = null, left = null;
            int arrivedIndex = -1;
            Vector3 leftFrom = default;
            foreach (var d in data)
            {
                if (!animate || !workers.TryGetValue(d.Id, out int before) || before == d.Workers) continue;
                if (d.Workers == before + 1) { arrived = d.Id; arrivedIndex = before; }
                else if (d.Workers == before - 1) { left = d.Id; leftFrom = View(d.Id)?.PawnWorld(d.Workers) ?? default; }
            }
            foreach (var d in data)
            {
                var view = View(d.Id);
                if (view != null) view.Bind(d);
                workers[d.Id] = d.Workers;
            }
            tray.Bind(ShelterLogic.AvailableWorkers(sim.Shelter));
            if (arrived != null) View(arrived)?.HopIn(arrivedIndex, fromTray);
            if (left != null) tray.HopIn(leftFrom);
            board.Bind(ShelterViewBuilder.Board(sim, selected));
        }

        RoomView SelectedView => View(selected);

        void Upgrade()
        {
            var result = surface.UpgradeRoom(selected);
            if (result == null) return;
            if (result.Ok)
            {
                board.StampDone();
                SelectedView?.LightUp();
            }
            else board.Shake(0);
        }

        void Minus() => Assign(-1, 1);
        void Plus() => Assign(1, 2);

        void Assign(int delta, int button)
        {
            var room = Sim?.Shelter?.Room(selected);
            if (room == null) return;
            var result = surface.AssignRoom(selected, room.Workers + delta);
            if (result != null && !result.Ok) board.Shake(button);
        }

        void Update()
        {
            if (rig == null) return;
            bool atShelf = rig.Current == SiginakPose.Shelf;
            // K1'de maket bütün olarak “2 SIĞINAK” hedefi; raf pozunda odalar tek tek seçilir
            var shelfCollider = shelfTarget != null ? shelfTarget.GetComponent<Collider>() : null;
            if (shelfCollider != null && shelfCollider.enabled == atShelf) shelfCollider.enabled = !atShelf;
            foreach (var t in roomTargets)
            {
                var c = t.GetComponent<Collider>();
                if (c != null && c.enabled != atShelf) c.enabled = atShelf;
            }
            foreach (var r in rooms) if (r != null) r.SelectionShown = atShelf;

            // K4 ışığı: oda ampulleri ve ortam ışığı poza göre (lights.*.k4Scale; three.js'te ×2,4 / 0,22, ACES'siz URP'de
            // referans render'la ölçülerek); geçiş poz süresince
            var theme = SiginakTheme.Active;
            float seconds = Mathf.Max(0.05f, theme.NumberOf("motion.poseS", 0.45f));
            k4 = Mathf.MoveTowards(k4, atShelf ? 1 : 0, Time.unscaledDeltaTime / seconds);
            float bulbs = theme.NumberOf("lights.roomBulb.k4Scale", 1);
            foreach (var r in rooms) r.LightScale = Mathf.Lerp(1, bulbs, k4);
            if (!ambientSaved && RenderSettings.ambientMode == AmbientMode.Trilight)
            {
                sky = RenderSettings.ambientSkyColor; equator = RenderSettings.ambientEquatorColor; ground = RenderSettings.ambientGroundColor;
                ambientSaved = true;
            }
            if (ambientSaved)
            {
                // Vardiya dizisinde oda kararır (K6a, ShiftSequence.AmbientScale)
                float a = Mathf.Lerp(1, theme.NumberOf("lights.ambient.k4Scale", 1), k4) * ShiftSequence.AmbientScale;
                RenderSettings.ambientSkyColor = sky * a;
                RenderSettings.ambientEquatorColor = equator * a;
                RenderSettings.ambientGroundColor = ground * a;
            }
        }

        void RestoreAmbient()
        {
            if (!ambientSaved) return;
            RenderSettings.ambientSkyColor = sky; RenderSettings.ambientEquatorColor = equator; RenderSettings.ambientGroundColor = ground;
            ambientSaved = false;
            k4 = 0;
        }
    }
}
