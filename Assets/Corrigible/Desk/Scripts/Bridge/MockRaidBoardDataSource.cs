using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.Raid;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    public enum RaidBoardPreset { B1, B2, B3, B4, B5, B6, B7a, B7b, B8 }

    /// <summary>F4 tahta örnekleri. Yalnız paket verisini sunar; GameController, kayıt ve oyuncu komutu içermez.</summary>
    public sealed class MockRaidBoardDataSource : MonoBehaviour
    {
        [SerializeField] TextAsset boardStates;
        [SerializeField] RaidBoard3D board;
        [SerializeField] SadeRaidCameraFraming framing;
        [SerializeField] RaidBoardPreset preset;
        [SerializeField] Color previewInk;
        RaidBoardPreset? shown;
        public Baskin.BState Current { get; private set; }
        public string Preset => preset.ToString();
        public IReadOnlyList<string> Presets => Enum.GetNames(typeof(RaidBoardPreset));

        void Start() { if (Application.isPlaying) LoadPreset(Preset); }
        void Update() { if (Application.isPlaying && shown != preset) LoadPreset(Preset); }

        public void LoadPreset(string id)
        {
            if (!Enum.TryParse(id, out RaidBoardPreset next)) throw new ArgumentException("Bilinmeyen tahta ön ayarı: " + id);
            preset = next;
            var data = JObject.Parse(boardStates.text);
            var snapshot = Parse(data, id);
            Current = snapshot.State; shown = preset;
            framing.Refresh();
            board.Build(Current); board.Sync(Current, snapshot.Marks);
            foreach (var item in snapshot.IntentStates) board.SetIntentPresentation(item.Key, item.Value);
            var raw = data["states"][id];
            var pushes = (raw["pushes"] as JArray ?? new JArray()).Select(p => ((int)p["from"][0], (int)p["from"][1], (int)p["to"][0], (int)p["to"][1])).ToList();
            var text = new List<(float, float, string, Color)>();
            foreach (var p in raw["pushes"] as JArray ?? new JArray())
                if ((bool?)p["blocked"] == true) text.Add(((float)p["to"][0], (float)p["to"][1], Loc.T("ÇARPIŞMA"), previewInk));
            if (id == "B3") text.Add((6, 3, Loc.T("ZIRH"), previewInk));
            if (id == "B6") text.Add((6, 3, Loc.T("HEDEF KIRILIR"), previewInk));
            if (pushes.Count > 0 || text.Count > 0) board.SetPreview(text, pushes);
            board.InputEnabled = false;
        }

        public sealed class Snapshot
        {
            public Baskin.BState State;
            public RaidBoard3D.Marks Marks;
            public Dictionary<int, string> IntentStates = new Dictionary<int, string>();
        }

        /// <summary>Mock durumunu kurar; RNG çağırmaz ve gerçek niyet yönü uydurmaz.</summary>
        public static Snapshot Parse(JObject data, string id)
        {
            var raw = data["states"]?[id] as JObject ?? throw new ArgumentException("Ön ayar yok: " + id);
            var state = new Baskin.BState
            {
                Turn = (int)raw["turn"], MaxTurns = (int)raw["turns"], HasBackup = (bool?)raw["yedekleme"] == true,
                Deceptive = (raw["aiAbilities"] as JArray)?.Any(x => (string)x == "Aldatma") == true,
                Phase = (string)raw["phase"] == "end" ? ((string)raw["outcome"] == "win" ? "won" : "lost") : (string)raw["phase"] == "ai" ? "enemy" : "player",
                Params = new RaidParams(), Layout = Baskin.Layouts[0].Name,
                OutcomeSuccess = (string)raw["outcome"] == "win",
            };
            var marks = new RaidBoard3D.Marks();
            for (int y = 0; y < 8; y++)
            {
                var row = ((string)data["grid"][y]).Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < 8; x++)
                {
                    string token = row[x];
                    string kind = token switch { "C" => "node", "Y" => state.HasBackup ? "backup" : "civ", "S" => "civ", "K" => "cool", "U" => "spawner", _ => null };
                    int max = kind == "node" ? 4 : kind == "backup" || kind == "spawner" ? 2 : 1;
                    var change = raw["objects_override"]?[x + "," + y];
                    bool broken = (bool?)change?["broken"] == true;
                    if (broken) marks.Rubble.Add(y * 8 + x);
                    state.Cells[y, x] = new Baskin.Cell
                    {
                        T = token == "#" ? "wall" : token == "~" ? "cable" : "floor",
                        O = kind == null || broken ? null : new Baskin.Obj { K = kind, Max = max, Hp = (int?)change?["hp"] ?? max },
                    };
                }
            }
            var result = new Snapshot { State = state, Marks = marks };
            var noMark = new HashSet<int>();
            foreach (var item in raw["units"].Concat(raw["robots"]))
            {
                bool player = item["letter"] != null;
                string type = player ? (string)item["letter"] switch { "T" => "tek", "E" => "emp", _ => "ana" } : (string)item["type"] == "maint" ? "bakim" : "dron";
                var u = new Baskin.Unit
                {
                    Id = state.NextId++, Type = type, Side = player ? 'p' : 'e', X = (int)item["pos"][0], Y = (int)item["pos"][1],
                    Hp = (int)item["hp"], Max = (int)item["max"], Dead = (int)item["hp"] <= 0,
                    Moved = (string)item["state"] == "done", Acted = (string)item["state"] == "done",
                };
                if (u.Moved) marks.Spent.Add(u.Id);
                var intent = item["intent"];
                if (intent is JObject)
                {
                    int dir = Direction((string)intent["dir"], intent["dst"], u.X, u.Y);
                    u.Order = (int)intent["order"];
                    u.Intent = new Baskin.Intent { Kind = (string)intent["kind"] switch { "fire" => "shoot", "melee" => "melee", _ => "repair" }, Dir = dir, Shown = dir };
                    if ((bool?)intent["nomark"] == true) noMark.Add(u.Id);
                    if (intent["state"] != null) result.IntentStates[u.Id] = (string)intent["state"];
                }
                state.Units.Add(u);
            }
            foreach (var p in raw["wakes"]) { state.Spawns.Add(new Baskin.Pt((int)p[0], (int)p[1])); marks.Spawns.Add((int)p[1] * 8 + (int)p[0]); }
            void Mark(string name, HashSet<int> set) { foreach (var p in raw["marks"]?[name] as JArray ?? new JArray()) set.Add((int)p[1] * 8 + (int)p[0]); }
            Mark("move", marks.Moves); Mark("target", marks.Targets); Mark("target_strong", marks.Targets);
            var selected = raw["marks"]?["sel"]?[0];
            if (selected != null) marks.Selected = state.Units.FirstOrDefault(u => u.X == (int)selected[0] && u.Y == (int)selected[1])?.Id ?? -1;
            foreach (var intent in Baskin.Intents(state))
            {
                if (noMark.Contains(intent.Unit)) continue;
                if (!intent.HasTarget || intent.TargetNone) continue;
                int k = intent.TargetY * 8 + intent.TargetX;
                if (intent.Kind == "repair") marks.Heal.Add(k);
                else marks.Danger[k] = !intent.Uncertain;
                marks.Arrows.Add((intent.FromX, intent.FromY, intent.TargetX, intent.TargetY, Color.white, intent.Uncertain));
            }
            return result;
        }

        static int Direction(string direction, JToken to, int x, int y)
        {
            if (direction != null) return direction switch { "N" => 0, "E" => 1, "S" => 2, _ => 3 };
            if (to == null) return 0;
            int dx = (int)to[0] - x, dy = (int)to[1] - y;
            return Mathf.Abs(dx) > Mathf.Abs(dy) ? dx > 0 ? 1 : 3 : dy > 0 ? 2 : 0;
        }
    }
}
