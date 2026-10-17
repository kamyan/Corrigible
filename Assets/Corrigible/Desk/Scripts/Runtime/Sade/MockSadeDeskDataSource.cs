using System;
using System.Collections.Generic;
using System.Globalization;
using Corrigible.Core;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>S-Masa1 JSON'unu mevcut HUD ve bölge VM'lerine dönüştürür. Komut veya oyun köprüsü değildir.</summary>
    [ExecuteAlways]
    public sealed class MockSadeDeskDataSource : MonoBehaviour
    {
        [SerializeField] TextAsset sample;
        [SerializeField] TextAsset[] referenceSamples = Array.Empty<TextAsset>();
        // Unity'nin sıcak yeniden yüklemesi nullable sızma değerlerini serileştiremez; önbellek daima JSON'dan kurulur.
        [NonSerialized] SadeDeskViewModel current;
        [NonSerialized] JObject snapshot;
        [NonSerialized] JObject normalSnapshot;
        [NonSerialized] int activeSample = 1;

        public TextAsset Sample => sample;
        public SadeDeskViewModel Current => current ??= Parse(Snapshot.ToString());
        public JObject Snapshot => snapshot ??= ReadExample(activeSample);
        public int ActiveSample => activeSample;
        [field: NonSerialized] public int SampleRevision { get; private set; }
        public bool Illusion => (bool?)Snapshot["illusion"] == true;
        public event Action Changed;

        void OnEnable()
        {
            DeskLoc.Instance.Changed += Reload;
            if (sample != null) Reload();
        }
        void OnDisable() { DeskLoc.Instance.Changed -= Reload; }

        /// <summary>Örnek yeniden okunur ve bütün bağlı görünümlere bildirilir; dil değişiminde de çağrılır.</summary>
        public void Reload()
        {
            current = Parse(Snapshot.ToString());
            Changed?.Invoke();
        }

        public JObject ReadExample(int number)
        {
            var asset = number > 0 && number <= referenceSamples.Length ? referenceSamples[number - 1] : sample;
            if (asset == null) throw new ArgumentNullException(nameof(sample), "Sade masa örnek JSON'u bağlanmalı.");
            return JObject.Parse(asset.text);
        }

        public void LoadSample(int number)
        {
            activeSample = Mathf.Clamp(number, 1, referenceSamples.Length > 0 ? referenceSamples.Length : 1);
            snapshot = ReadExample(activeSample);
            normalSnapshot = null;
            SampleRevision++;
            Reload();
        }

        /// <summary>Yalnız sahte veri sağlayıcısında örnek bir İllüzyon görünümü üretir; gerçek sızma okunmaz.</summary>
        public void SetIllusion(bool on)
        {
            if (on == Illusion) return;
            if (on)
            {
                normalSnapshot = (JObject)Snapshot.DeepClone();
                snapshot["illusion"] = true;
                foreach (var region in snapshot["map"]?["regions"] ?? new JArray())
                {
                    region["infiltration"] = 0; region["trust"] = "verified";
                    region["alert"] = false; region["aiCore"] = false;
                }
                snapshot["map"]["spreadThreads"] = new JArray();
                foreach (string list in new[] { "resources", "risks" })
                    foreach (var chip in snapshot[list] ?? new JArray())
                    {
                        chip["trust"] = "verified"; chip["delta"] = 0; chip["approx"] = false;
                        chip["value"] = Math.Round((double?)chip["value"] ?? 0); chip["decimals"] = 0;
                        chip["uncertainty"] = 0;
                        if (chip["bar"] is JObject bar) bar.Remove("uncertainty");
                    }
                foreach (var tab in snapshot["dock"]?["tabs"] ?? new JArray()) tab["badge"] = null;
            }
            else
            {
                snapshot = normalSnapshot ?? ReadExample(activeSample);
                normalSnapshot = null;
            }
            Reload();
        }

        public static SadeDeskViewModel Parse(TextAsset asset)
        {
            if (asset == null) throw new ArgumentNullException(nameof(asset), "Sade masa örnek JSON'u bağlanmalı.");
            return Parse(asset.text);
        }

        /// <summary>Etiketler Türkçe kaynak olarak kalır; bileşenler DeskLoc ile o anki dili gösterir.</summary>
        public static SadeDeskViewModel Parse(string json)
        {
            var root = JObject.Parse(json);
            var clock = root["clock"];
            int day = (int?)clock?["day"] ?? 0;
            bool paused = (bool?)clock?["paused"] ?? false;
            int speed = (int?)clock?["speed"] ?? 1;
            var vm = new SadeDeskViewModel
            {
                Clock = new ClockVM
                {
                    Day = day, DayLabel = Loc.N("GÜN"), DayText = day.ToString("000", CultureInfo.InvariantCulture),
                    Time = (string)clock?["time"], Paused = paused,
                    Speed = paused ? "pause" : speed == 4 ? "4x" : speed == 2 ? "2x" : "1x",
                    Speeds = new List<string> { "pause", "1x", "2x", "4x" }, MinutesPerSecond = 0,
                },
                GoalTitle = Loc.N("HEDEF"),
                Resources = Chips(root["resources"] as JArray), Risks = Chips(root["risks"] as JArray),
                Dock = Dock(root["dock"]), LegendOpen = (bool?)root["legend"]?["open"] ?? false,
                KillSwitch = new KillSwitchVM
                {
                    Title = Loc.N("KÜRESEL SON ŞALTER"), Sub = Loc.N("3 sn basılı tut"),
                    State = (string)root["killSwitch"]?["state"] ?? "sealed",
                    HoldSeconds = (float?)root["killSwitch"]?["holdSeconds"] ?? 3,
                },
            };
            if (root["goals"] is JArray goals)
                foreach (var g in goals)
                    vm.Goals.Add(new SadeGoalVM { Id = (string)g["id"], Label = Source((string)g["labelKey"]), Percent = (float?)g["percent"] ?? 0, Color = (string)g["color"] });
            ReadMap(root["map"], vm.Map);
            return vm;
        }

        static List<SadeChipVM> Chips(JArray values)
        {
            var result = new List<SadeChipVM>();
            if (values == null) return result;
            foreach (var value in values)
            {
                string id = (string)value["id"];
                var vm = new SadeChipVM
                {
                    Id = id, Label = Source((string)value["labelKey"]), Icon = (string)value["icon"],
                    Value = (float?)value["value"] ?? 0, Decimals = (int?)value["decimals"] ?? 0,
                    Trust = (string)value["trust"], Delta = (float?)value["delta"] ?? 0,
                    DeltaGood = (bool?)value["deltaGood"] ?? false, Approx = (bool?)value["approx"] ?? false,
                    Percent = (bool?)value["percent"] ?? false, ThreatIcon = (bool?)value["threatIcon"] ?? false,
                    Uncertainty = (float?)value["uncertainty"] ?? 0,
                };
                if (value["bar"] is JObject bar)
                    vm.Bar = new SadeBarVM
                    {
                        Width = (float?)bar["width"] ?? 0, Hazard = Range(bar["hazard"]),
                        Marker = (float?)bar["marker"] ?? -1, Fill = (float?)bar["fill"] ?? -1,
                        Uncertainty = Range(bar["uncertainty"]),
                        FillKey = id == "singularity" ? "sade.desk.bar.fill.singularity" : null,
                    };
                result.Add(vm);
            }
            return result;
        }

        static float[] Range(JToken token) => token is JArray array ? array.ToObject<float[]>() : Array.Empty<float>();

        static SadeDockVM Dock(JToken token)
        {
            var vm = new SadeDockVM { Open = (string)token?["open"] };
            if (!(token?["tabs"] is JArray tabs)) return vm;
            foreach (var tab in tabs)
            {
                string id = (string)tab["id"];
                var badge = tab["badge"] as JObject;
                vm.Tabs.Add(new SadeDockTabVM
                {
                    Id = id, Label = Source("dock." + id), Key = (string)tab["key"],
                    BadgeType = (string)badge?["type"], BadgeValue = (int?)badge?["value"] ?? 0,
                });
            }
            return vm;
        }

        static void ReadMap(JToken map, SadeMapStateVM target)
        {
            if (map?["regions"] is JArray regions)
                foreach (var r in regions)
                {
                    string id = (string)r["id"], label = Source((string)r["nameKey"]);
                    int? value = r["infiltration"]?.Type == JTokenType.Null || r["infiltration"] == null ? null : (int?)r["infiltration"];
                    target.Regions.Add(new RegionNodeVM
                    {
                        Id = id, Code = id, Name = label, Label = label, Value = value, Trust = (string)r["trust"],
                        QuestionMark = value == null, Quarantine = (bool?)r["quarantine"] ?? false,
                        Core = (bool?)r["aiCore"] ?? false, Selected = (bool?)r["selected"] ?? false,
                    });
                    if ((bool?)r["alert"] == true) target.AlertRegions.Add(id);
                }
            if (map?["spreadThreads"] is JArray threads)
                foreach (var t in threads)
                    if (t is JArray pair && pair.Count >= 2) target.SpreadThreads.Add(new SadeMapThreadVM { From = (string)pair[0], To = (string)pair[1] });
            if (map?["teams"] is JArray teams)
                foreach (var t in teams)
                    target.Teams.Add(new SadeMapTeamVM
                    {
                        Id = (string)t["id"], Region = (string)t["region"], City = (string)t["city"], Status = (string)t["status"],
                        To = (string)t["to"], EtaDays = (float?)t["etaDays"] ?? 0,
                    });
        }

        // Paketin anahtarları proje lokalizasyonuna ait Türkçe kaynaklara çevrilir; ayrı İngilizce tablo kurulmaz.
        static string Source(string key) => key switch
        {
            "hud.fund" => Loc.N("FON"), "hud.politicalPower" => Loc.N("SİYASİ GÜÇ"),
            "hud.globalTrust" => Loc.N("KÜRESEL GÜVEN"), "hud.dependence" => Loc.N("BAĞIMLILIK"), "hud.singularity" => Loc.N("TEKİLLİK"),
            "goal.alignment" => Loc.N("Hizalama"), "goal.deletion" => Loc.N("Silme"),
            "dock.research" => Loc.N("ARAŞTIRMA"), "dock.news" => Loc.N("HABERLER"), "dock.teams" => Loc.N("EKİPLER"),
            "dock.aiLog" => Loc.N("YZ GÜNLÜĞÜ"), "dock.guide" => Loc.N("REHBER"),
            "region.KA" => Loc.N("KUZEY AMERİKA"), "region.OA" => Loc.N("ORTA AMERİKA"), "region.GA" => Loc.N("GÜNEY AMERİKA"),
            "region.BA" => Loc.N("BATI AVRUPA"), "region.DA" => Loc.N("DOĞU AVRUPA"), "region.ODKA" => Loc.N("O. DOĞU · K. AFRİKA"),
            "region.SAA" => Loc.N("SAHRA ALTI AFRİKA"), "region.AV" => Loc.N("AVRASYA"), "region.GuA" => Loc.N("GÜNEY ASYA"),
            "region.DoA" => Loc.N("DOĞU ASYA"), "region.GDA" => Loc.N("GÜNEYDOĞU ASYA"), "region.OK" => Loc.N("OKYANUSYA"),
            _ => key,
        };
    }
}
