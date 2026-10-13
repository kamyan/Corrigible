using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sahte veri kaynağı (PROMPT §4.2): <c>Data/Mock/*.json</c> ekran durumlarını ve <c>map.json</c> harita geometrisini
    /// birleştirip <see cref="DeskViewModel"/> üretir. Komutlar <see cref="MockDeskCommands"/>'ta günlüğe yazılır ve
    /// sahte durumu en basit biçimde günceller.
    /// </summary>
    public sealed partial class MockDeskDataSource : MonoBehaviour, IDeskDataSource
    {
        [Tooltip("Ön ayarlar: C1_normal, C2_illusion, C3_event, C4_gameover.")]
        [SerializeField] TextAsset[] presets = Array.Empty<TextAsset>();
        [SerializeField] TextAsset mapSpec;
        [SerializeField] int presetIndex;
        [Tooltip("Sahte metinlerin İngilizcesi (TR ⇥ EN; yalnızca geliştirme/QA).")]
        [SerializeField] TextAsset englishMock;

        MockEnglish english;

        DeskViewModel current;

        public DeskViewModel Current => current ??= Build(presetIndex);
        public event Action<DeskViewModel> Changed;
        public int PresetCount => presets.Length;
        public int PresetIndex => presetIndex;

        public void Load(int index)
        {
            int i = Mathf.Clamp(index, 0, Mathf.Max(0, presets.Length - 1));
            DeskViewModel vm;
            try { vm = Build(i); }
            catch (Exception e)
            {
                // Şeması henüz desteklenmeyen ön ayar masayı düşürmesin (C3/C4 sonraki fazlarda)
                Debug.LogError($"[Desk] Ön ayar {presets[i].name} kurulamadı: {e.Message}", this);
                return;
            }
            presetIndex = i;
            current = vm;
            Changed?.Invoke(current);
        }

        /// <summary>Sahte durum değişti (komutlar); görünümler yeniden bağlanır.</summary>
        public void Notify() => Changed?.Invoke(current);

        void OnEnable() => DeskLoc.Instance.Changed += Reload;
        void OnDisable() => DeskLoc.Instance.Changed -= Reload;
        void Reload() { if (current != null) Load(presetIndex); }

        /// <summary>Ön ayarı kurar; İngilizcede sahte metinler <see cref="MockEnglish"/> ile çevrilir (oyunda veri kaynağı dili verir).</summary>
        DeskViewModel Build(int index)
        {
            var vm = BuildRaw(index);
            if (DeskLoc.Instance.Lang == DeskLang.En && englishMock != null)
            {
                english ??= new MockEnglish(englishMock.text);
                english.Apply(vm);
            }
            return vm;
        }

        /// <summary>Geliştirici: İngilizce sözlükte bulunmayan sahte metinler.</summary>
        public string EnglishMisses() => english?.DumpMisses() ?? "";

        DeskViewModel BuildRaw(int index)
        {
            if (presets.Length == 0) return new DeskViewModel();
            var root = JObject.Parse(presets[index].text);
            // C3 olay ön ayarı: arka plan C1 masası (duraklatılmış), üstünde olay modalı
            if (root["memo"] != null && index != 0)
            {
                var desk = BuildRaw(0);
                foreach (var r in root.Descendants().OfType<JProperty>().Where(p => p.Name == "rect").ToList())
                    if (r.Value is JArray arr) for (int k = 0; k < arr.Count; k++) if (arr[k].Type == JTokenType.String) arr[k] = -1;
                desk.Event = root.ToObject<EventVM>();
                // Sahte: önizlemesi olmayan seçeneklere ilk kazanç ve ilk bedelden basit not (oyunda VM verir)
                foreach (var o in desk.Event.Options)
                {
                    if (o.Preview != null) continue;
                    o.Preview = new EventPreviewVM { Cap = "ÖNİZLEME", Right = "imzalanırsa · G-075" };
                    foreach (var l in new[] { o.Gains.FirstOrDefault(), o.Costs.FirstOrDefault() })
                        if (l != null) o.Preview.Rows.Add(new EventPreviewRowVM { Label = l.Label, Value = l.Value, Trust = l.Trust, Right = "tahmin" });
                }
                desk.Screen = (string)root["screen"];
                desk.Clock.Paused = true;
                return desk;
            }
            // C4 oyun sonu: tam ekran rapor (arka plan masası yine kurulur ama görünmez)
            if (root["report"] != null && index != 0)
            {
                var desk = BuildRaw(0);
                desk.GameOver = root.ToObject<GameOverVM>();
                desk.Screen = (string)root["screen"];
                // Etiket kutularının konumu JSON'da yok; referans ölçümünden (C4_gameover.tsv, sol-üst)
                float[][] tagPos = { new[] { 1295f, 344f }, new[] { 1438f, 510f }, new[] { 817f, 508f } };
                var tags = desk.GameOver.Chart?.Tags;
                if (tags != null)
                    for (int i = 0; i < tags.Count && i < tagPos.Length; i++) { tags[i].X = tagPos[i][0]; tags[i].Y = tagPos[i][1]; }
                return desk;
            }
            var mapState = root["map"] as JObject;
            root.Remove("map");
            dossierTemplate = root["dossier"] as JObject;
            var vm = root.ToObject<DeskViewModel>();
            if (mapSpec != null) vm.Map = BuildMap(JObject.Parse(mapSpec.text), mapState);
            if (mapSpec != null) Enrich(vm, JObject.Parse(mapSpec.text));
            return vm;
        }

        static float[] F(JToken t) => t == null || t.Type == JTokenType.Null ? null : t.Select(x => (float)x).ToArray();
        static List<float[]> Pts(JToken t) => t == null ? new List<float[]>() : t.Select(F).ToList();

        static MapVM BuildMap(JObject spec, JObject state)
        {
            bool en = DeskLoc.Instance.Lang == DeskLang.En;
            var map = new MapVM
            {
                Header = new MapHeaderVM
                {
                    Title = (string)spec["header"]["title"],
                    Summary = (string)state?["header"]?["summary"] ?? "",
                    Right = (string)state?["header"]?["right"] ?? "",
                },
                AnalystLayer = state?["analystLayer"]?.Value<bool>() ?? true,
                SelectedRegion = (string)state?["selectedRegion"],
            };

            var states = state?["regions"] as JObject;
            foreach (var r in spec["regions"])
            {
                var id = (string)r["id"];
                var s = states?[id];
                var node = new RegionNodeVM
                {
                    Id = id, Code = (string)r["code"],
                    Name = (string)(en ? r["nameEN"] : r["nameTR"]),
                    Label = (string)(en ? r["labelEN"] : r["labelTR"]),
                    Center = F(r["center"]), Pin = F(r["pin"]),
                    LabelX = (float)r["label"]["x"], LabelY = (float)r["label"]["y"], LabelW = (float)r["label"]["w"],
                    LabelAlign = (string)r["label"]["align"],
                };
                if (s != null)
                {
                    node.Value = s["value"] == null || s["value"].Type == JTokenType.Null ? (int?)null : (int)s["value"];
                    node.Trust = (string)s["trust"];
                    node.RingVariant = (string)s["ringVariant"];
                    node.QuestionMark = s["questionMark"]?.Value<bool>() ?? false;
                    node.Selected = s["selected"]?.Value<bool>() ?? false;
                    // C1: true/false · C2: "clean" (kusursuz halka)
                    var q = s["quarantine"];
                    node.Quarantine = q != null && (q.Type == JTokenType.Boolean ? (bool)q : q.Type == JTokenType.String);
                    node.QuarantineClean = q != null && q.Type == JTokenType.String && (string)q == "clean";
                    node.Core = s["core"]?.Value<bool>() ?? false;
                }
                map.Regions.Add(node);
            }

            var cut = new HashSet<string>(state?["routesCut"]?.Select(x => (string)x) ?? Enumerable.Empty<string>());
            foreach (var r in spec["routes"])
            {
                var route = new RouteVM { Id = (string)r["id"], From = (string)r["from"], To = (string)r["to"], Q = Pts(r["q"]) };
                route.State = cut.Contains(route.Id) ? "cut" : "open";
                if (route.State == "cut" && r["cutMark"] != null)
                {
                    route.CutCenter = F(r["cutMark"]["center"]);
                    route.CutTangentDeg = (float)r["cutMark"]["tangentDeg"];
                }
                map.Routes.Add(route);
            }

            foreach (var e in spec["edgeRoutes"])
                map.EdgeRoutes.Add(new EdgeRouteVM { Q = Pts(e["q"]), ArrowPos = F(e["arrow"]["pos"]), ArrowRotDeg = (float)e["arrow"]["rotDeg"] });
            foreach (var e in spec["edgeLabels"])
                map.EdgeLabels.Add(new MapTextVM { Text = (string)(en ? e["textEN"] : e["text"]), X = (float)e["x"], Y = (float)e["y"] });
            foreach (var g in spec["graticuleLabels"])
                map.GraticuleLabels.Add(new MapTextVM { Text = en ? Graticule((string)g["text"]) : (string)g["text"], X = (float)g["x"], Y = (float)g["y"] });

            var tag = spec["teamTags"]?.FirstOrDefault();
            foreach (var t in spec["teams"])
            {
                var id = (string)t["id"];
                var st = state?["teams"]?.FirstOrDefault(x => (string)x["id"] == id);
                var rect = F(t["rect"]);
                var team = new TeamTokenVM { Id = id, X = rect[0], Y = rect[1], State = (string)st?["state"], Tag = (string)st?["tag"] };
                if (team.Tag != null && tag != null) { team.TagX = (float)tag["x"]; team.TagY = (float)tag["y"]; }
                map.Teams.Add(team);
            }

            var a = spec["analystLayer"];
            var analyst = new AnalystLayerVM { Pins = Pts(a["pins"]) };
            foreach (var s in a["strings"])
                analyst.Strings.Add(new AnalystStringVM { A = F(s["a"]), B = F(s["b"]), FlagPos = F(s["flag"]["pos"]), FlagRotDeg = (float)s["flag"]["rotDeg"] });
            foreach (var p in a["pencilArrows"])
                analyst.PencilArrows.Add(new PencilArrowVM
                {
                    Q = Pts(p["q"]), Color = (string)p["color"], Width = (float)p["width"], Dash = F(p["dash"]),
                    HeadPos = F(p["head"]["pos"]), HeadRotDeg = (float)p["head"]["rotDeg"],
                });
            foreach (var n in a["notes"])
                analyst.Notes.Add(new AnalystNoteVM { Text = (string)n["text"], Color = (string)n["color"], X = (float)n["x"], Y = (float)n["y"], RotDeg = (float)n["rotDeg"], Weight = (int)n["weight"] });
            var st2 = a["sticky"];
            analyst.Sticky = new StickyVM
            {
                Text = (string)st2["text"], X = (float)st2["rect"][0], Y = (float)st2["rect"][1], W = (float)st2["rect"][2], RotDeg = (float)st2["rotDeg"],
                TapeX = (float)st2["tape"]["x"], TapeY = (float)st2["tape"]["y"], TapeRotDeg = (float)st2["tape"]["rotDeg"],
            };
            foreach (var q in a["questionMarks"]) analyst.QuestionMarks.Add(new[] { (float)q["x"], (float)q["y"] });
            map.Analyst = analyst;

            var tr = state?["illusionTraces"];
            if (tr != null)
            {
                map.IllusionTraces = new IllusionTracesVM();
                foreach (var g in tr["ghostNotes"] ?? new JArray())
                    map.IllusionTraces.GhostNotes.Add(new GhostNoteVM
                    {
                        Text = (string)g["text"], X = (float)g["x"], Y = (float)g["y"], RotDeg = (float?)g["rotDeg"] ?? 0,
                        Alpha = (float?)g["alpha"] ?? 0.09f, Weight = (int?)g["weight"] ?? 400,
                    });
                var e = tr["eraserSmudge"];
                if (e != null)
                    map.IllusionTraces.EraserSmudge = new EraserVM { Center = F(e["center"]), Radius = F(e["radius"]), Color = (string)e["color"], Alpha = (float?)e["alpha"] ?? 0.025f };
            }

            var sel = spec["selection"]?["thread"];
            if (sel != null && map.SelectedRegion != null) map.Selection = new SelectionVM { From = F(sel["from"]), To = F(sel["to"]) };
            return map;
        }

        static string Graticule(string tr) => tr.Replace("K", "N").Replace("G", "S").Replace("B", "W").Replace("D", "E");
    }
}
