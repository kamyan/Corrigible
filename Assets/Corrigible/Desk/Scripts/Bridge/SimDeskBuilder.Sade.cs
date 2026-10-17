using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // S-Masa oyun köprüsü (F3): SimDeskBuilder'ın ürettiği Algı Katmanı (Shown/Global) verilerini
    // S-Masa bileşenlerinin (SadeDeskView, SadeMapView, SadeRegionCardView, SadeFullDossierView,
    // 5 çekmece ve ipucu kartları) beklediği modellere dönüştürür.
    public sealed partial class SimDeskBuilder
    {
        public static Vector2 SadeCenter(string id)
        {
            var d = Def(id);
            return MapProjection.ToMap(d.Lon, d.Lat);
        }

        SadeDeskData BuildSade(DeskViewModel vm, ShownGlobal g)
        {
            var data = new SadeDeskData();
            data.View = BuildSadeView(vm, g);
            foreach (var def in GameData.Regions)
            {
                data.Cards[def.Id] = BuildSadeRegionCard(def.Id);
                data.Dossiers[def.Id] = BuildSadeFullDossier(def.Id, data.Cards[def.Id]);
                data.NodeTooltips[def.Id] = BuildSadeNodeTooltip(def.Id);
            }
            data.Drawers["research"] = new JObject { ["id"] = "research", ["research"] = BuildSadeResearchDrawer() };
            data.Drawers["news"] = new JObject { ["id"] = "news", ["news"] = BuildSadeNewsDrawer() };
            data.Drawers["teams"] = new JObject { ["id"] = "teams", ["teams"] = BuildSadeTeamsDrawer() };
            data.Drawers["aiLog"] = new JObject { ["id"] = "aiLog", ["aiLog"] = BuildSadeAiLogDrawer() };
            data.Drawers["guide"] = new JObject { ["id"] = "guide", ["guide"] = BuildSadeGuideDrawer("trust") };
            BuildSadeBreakdowns(data.Breakdowns, vm, g);
            return data;
        }

        SadeDeskViewModel BuildSadeView(DeskViewModel vm, ShownGlobal g)
        {
            var h = st.History;
            float k = (float)sim.TimeScale;
            float income = (float)sim.FundRate;
            float spend = (float)(sim.ResearchNeed / k);
            float fundDelta = sim.ModernEconomy
                ? (float)(sim.LastIncome.Total - sim.LastResearchPayment)
                : income - spend;

            int monitored = S.Regions.Count(r => r.Monitored);
            bool coverage = monitored >= 9;
            bool flagged = S.Regions.Any(r => r.MediaFlag);
            string trustLvl = illusion || revealed || coverage ? "verified" : "report";
            string depLvl = illusion || revealed || coverage ? "verified" : flagged ? "suspicious" : "report";
            string tekLvl = illusion || revealed || S.Done("yorum") ? "verified" : "report";

            float trustRate = illusion ? 0f : (h.Rate(x => x.Trust) ?? 0f);
            float depRate = illusion ? 0f : (h.Rate(x => x.Dep) ?? 0f);
            float tekRate = illusion ? 0f : (h.Rate(x => x.Tek) ?? 0f);

            var align = Explain.Alignment(sim);
            var erase = Explain.Erasure(sim);

            float trustVal = (float)g.Trust;
            float depVal = (float)g.Dep;
            float vetoVal = (float)sim.VetoThreshold;
            float tekVal = (float)g.Tek;

            var sade = new SadeDeskViewModel
            {
                Clock = vm.Clock,
                GoalTitle = Loc.N("HEDEF"),
                Resources =
                {
                    new SadeChipVM
                    {
                        Id = "fund", Label = Loc.N("FON"), Value = (float)S.Fon, Decimals = 1,
                        Trust = "verified", Delta = fundDelta, DeltaGood = fundDelta >= 0f,
                    },
                    new SadeChipVM
                    {
                        Id = "politicalPower", Label = Loc.N("SİYASİ GÜÇ"), Value = (float)S.Sg, Decimals = 0,
                        Trust = "verified", Delta = (float)sim.SgRate, DeltaGood = sim.SgRate >= 0,
                    },
                },
                Goals =
                {
                    new SadeGoalVM { Id = "alignment", Label = Loc.N("Hizalama"), Percent = Mathf.RoundToInt(align.Progress * 100f), Color = "good" },
                    new SadeGoalVM { Id = "deletion", Label = Loc.N("Silme"), Percent = Mathf.RoundToInt(erase.Progress * 100f), Color = "info" },
                },
                Risks =
                {
                    new SadeChipVM
                    {
                        Id = "globalTrust", Label = Loc.N("KÜRESEL GÜVEN"), Value = trustVal, Decimals = 1,
                        Trust = trustLvl, Approx = trustLvl == "report",
                        Delta = trustRate, DeltaGood = trustRate >= 0f,
                        Bar = new SadeBarVM
                        {
                            Width = 172, Hazard = new[] { 0f, 15f }, Marker = Mathf.Clamp(trustVal, 0f, 100f), Fill = -1,
                            Uncertainty = trustLvl == "report" && !illusion ? new[] { Mathf.Max(0f, trustVal - 3f), Mathf.Min(100f, trustVal + 3f) } : Array.Empty<float>(),
                        },
                    },
                    new SadeChipVM
                    {
                        Id = "dependence", Label = Loc.N("BAĞIMLILIK"), Value = depVal, Decimals = 1,
                        Trust = depLvl, Approx = depLvl == "report",
                        Delta = depRate, DeltaGood = depRate <= 0f,
                        Bar = new SadeBarVM
                        {
                            Width = 172, Hazard = new[] { vetoVal, 100f }, Marker = Mathf.Clamp(depVal, 0f, 100f), Fill = -1,
                            Uncertainty = Array.Empty<float>(),
                        },
                    },
                    new SadeChipVM
                    {
                        Id = "singularity", Label = Loc.N("TEKİLLİK"), Value = tekVal, Decimals = 1, Percent = true, ThreatIcon = true,
                        Trust = tekLvl, Approx = tekLvl == "report", Uncertainty = tekLvl == "report" && !illusion ? 6f : 0f,
                        Delta = tekRate, DeltaGood = tekRate <= 0f,
                        Bar = new SadeBarVM
                        {
                            Width = 188, Hazard = new[] { 90f, 100f }, Marker = -1, Fill = Mathf.Clamp(tekVal, 0f, 100f),
                            Uncertainty = tekLvl == "report" && !illusion ? new[] { Mathf.Max(0f, tekVal - 6f), Mathf.Min(100f, tekVal + 6f) } : Array.Empty<float>(),
                            FillKey = "sade.desk.bar.fill.singularity",
                        },
                    },
                },
                Map = BuildSadeMap(vm),
                Dock = BuildSadeDock(vm),
                KillSwitch = vm.KillSwitch,
            };
            return sade;
        }

        SadeMapStateVM BuildSadeMap(DeskViewModel vm)
        {
            var map = new SadeMapStateVM();
            var shown = S.Regions.ToDictionary(r => r.Id, r => sim.Shown(r));
            foreach (var def in GameData.Regions)
            {
                var r = S.Region(def.Id);
                var v = shown[def.Id];
                string trust = TrustOf(v);
                int? val = trust == "unknown" || v.V == null ? (int?)null : Mathf.RoundToInt((float)v.V.Value);
                map.Regions.Add(new RegionNodeVM
                {
                    Id = def.Id,
                    Code = Code(def.Id),
                    Name = def.Name,
                    Label = Up(def.Name),
                    Value = val,
                    QuestionMark = val == null,
                    Trust = trust,
                    Selected = st.Selected == def.Id,
                    Quarantine = r.Q,
                    QuarantineClean = illusion,
                    Core = revealed ? S.Core == def.Id : S.CoreKnown == def.Id,
                });
            }

            foreach (var l in GameData.Links)
            {
                if (l.Wrap) continue;
                var va = shown[l.A].V;
                var vb = shown[l.B].V;
                if (va == null || vb == null) continue;
                if (va >= 50 && vb >= 20) map.SpreadThreads.Add(new SadeMapThreadVM { From = l.A, To = l.B });
                else if (vb >= 50 && va >= 20) map.SpreadThreads.Add(new SadeMapThreadVM { From = l.B, To = l.A });
            }

            if (sim.TeamsOn)
            {
                var atCount = new Dictionary<string, int>();
                foreach (var t in S.Teams)
                {
                    string letter = TeamLetter(t);
                    if (t.Dest != null)
                    {
                        float k = t.Trip > 0 ? Mathf.Clamp01((t.Trip - t.Eta) / (float)t.Trip) : 0f;
                        var from = SadeCenter(t.From ?? t.Dest);
                        var to = SadeCenter(t.Dest);
                        var pos = Vector2.Lerp(from, to, Mathf.Lerp(0.22f, 0.78f, k));
                        map.Teams.Add(new SadeMapTeamVM
                        {
                            Id = letter, Region = t.From ?? t.Dest, To = t.Dest,
                            City = Def(t.Dest).Name, Status = "travelling", EtaDays = t.Eta,
                            HasPoint = true, X = pos.x, Y = pos.y,
                            FromX = pos.x, FromY = pos.y, ToX = to.x, ToY = to.y,
                        });
                    }
                    else if (t.At != null)
                    {
                        atCount.TryGetValue(t.At, out int n);
                        atCount[t.At] = n + 1;
                        var pos = SadeCenter(t.At) + new Vector2(-36f - 26f * n, -30f);
                        map.Teams.Add(new SadeMapTeamVM
                        {
                            Id = letter, Region = t.At, City = Def(t.At).Name, Status = "stationed",
                            HasPoint = true, X = pos.x, Y = pos.y,
                        });
                    }
                }
            }

            if (vm.Advisor != null && !illusion)
            {
                string alertRegion = PickAlertRegion(shown);
                if (!string.IsNullOrEmpty(alertRegion))
                {
                    map.AlertRegions.Add(alertRegion);
                    map.AdvisorTooltip = vm.Advisor.Text;
                }
            }

            if (vm.Map?.Reports != null)
            {
                var placed = new List<Rect>();
                foreach (var rep in vm.Map.Reports)
                {
                    var center = SadeCenter(rep.Region);
                    var pos = SadeReportPosition(center, placed);
                    map.Reports.Add(new FieldReportVM
                    {
                        Id = rep.Id, Region = rep.Region, X = pos.x, Y = pos.y,
                        Title = rep.Title, RewardText = rep.RewardText,
                        Tooltip = rep.Tooltip, Command = rep.Command, Expiring = rep.Expiring,
                    });
                }
            }

            return map;
        }

        string PickAlertRegion(Dictionary<string, ShownValue> shown)
        {
            var flag = S.Regions.FirstOrDefault(r => r.MediaFlag);
            if (flag != null) return flag.Id;
            if (!string.IsNullOrEmpty(S.CoreKnown)) return S.CoreKnown;
            var best = S.Regions
                .Where(r => shown.TryGetValue(r.Id, out var sv) && sv.V.HasValue)
                .OrderByDescending(r => shown[r.Id].V.Value)
                .FirstOrDefault();
            return best?.Id ?? "cn";
        }

        static Vector2 SadeReportPosition(Vector2 center, List<Rect> placed)
        {
            var offsets = new[]
            {
                new Vector2(68, -46), new Vector2(-68, -46), new Vector2(68, 46), new Vector2(-68, 46),
                new Vector2(0, -76), new Vector2(0, 76), new Vector2(98, 0), new Vector2(-98, 0),
            };
            foreach (var off in offsets)
            {
                var p = new Vector2(Mathf.Clamp(center.x + off.x, 120f, 1800f), Mathf.Clamp(center.y + off.y, 120f, 940f));
                var rect = new Rect(p.x - 54f, p.y - 26f, 108f, 52f);
                if (!placed.Any(o => o.Overlaps(rect)))
                {
                    placed.Add(rect);
                    return p;
                }
            }
            return center + offsets[0];
        }

        SadeDockVM BuildSadeDock(DeskViewModel vm)
        {
            var dock = new SadeDockVM { Open = null };
            foreach (var t in vm.Dock)
            {
                dock.Tabs.Add(new SadeDockTabVM
                {
                    Id = t.Id,
                    Label = t.Id == "teams" ? Loc.N("EKİPLER") : t.Label,
                    Key = t.Key,
                    BadgeType = t.Badge?.Type,
                    BadgeValue = t.Badge?.Value ?? 0,
                });
            }
            return dock;
        }

        JObject BuildSadeRegionCard(string id)
        {
            var r = S.Region(id);
            var def = Def(id);
            var v = sim.Shown(r);
            string trust = TrustOf(v);
            bool unknown = trust == "unknown" || v.V == null;
            int val = unknown ? 0 : Mathf.RoundToInt((float)v.V.Value);

            string statusKey = r.Off > 0 ? Loc.T("Şebeke kapalı")
                : r.Left ? Loc.T("İttifaktan çekildi")
                : r.Q ? Loc.T("Karantina altında")
                : trust == "suspicious" ? "card.sourcesConflict"
                : r.Monitored ? Loc.T("İzleme ağı kurulu")
                : "dossier.noMonitoring";

            var inf = new JObject
            {
                ["value"] = unknown ? JValue.CreateNull() : new JValue(val),
                ["trust"] = trust,
                ["approx"] = trust == "report",
            };
            if (!unknown && trust == "suspicious")
            {
                int repVal = Mathf.Clamp(val - 12, 0, 100);
                inf["report"] = repVal;
                inf["field"] = val;
                inf["band"] = new JArray(Mathf.Min(repVal, val), Mathf.Max(repVal, val));
            }
            else if (!unknown && trust == "report")
            {
                inf["report"] = val;
                inf["band"] = new JArray(Mathf.Max(0, val - 10), Mathf.Min(100, val + 10));
            }

            var actions = new JArray();
            string[] cmdIds = { "monitorNet", "audit", "quarantine", "killSwitch" };
            string[] fxKeys = { "dossier.fx.verify", "dossier.fx.resolve", "dossier.fx.stopSpread", "dossier.fx.shutdown" };

            for (int i = 0; i < 4; i++)
            {
                string actType = Actions.All[i];
                string blocked = revealed ? Loc.T("Oyun bitti.") : Blocked(r, actType);
                int days = 0, costFund = 0, costPower = 0;
                string costText;
                switch (actType)
                {
                    case Actions.Monitor:
                        costFund = Mathf.RoundToInt((float)sim.MonitorCostFor(id));
                        days = sim.TeamsOn ? Simulation.TeamMonitorDays : 0;
                        costText = r.Monitored ? Loc.T("Kurulu") : days > 0 ? Loc.F("{0} fon · {1} gün", costFund, days) : Loc.F("{0} fon", costFund);
                        break;
                    case Actions.Audit:
                        costFund = Mathf.RoundToInt((float)sim.AuditCost);
                        costPower = 1;
                        days = sim.TeamsOn ? Simulation.TeamAuditDays : 0;
                        costText = days > 0 ? Loc.F("{0} fon · 1 SG · {1} gün", costFund, days) : Loc.F("{0} fon · 1 SG", costFund);
                        break;
                    case Actions.Quarantine:
                        costPower = r.Q ? 0 : 8;
                        costText = r.Q ? Loc.T("Aç · ücretsiz") : Loc.T("8 SG · güven −");
                        break;
                    default:
                        costPower = Mathf.RoundToInt((float)sim.SalterSg(r));
                        costFund = Mathf.RoundToInt((float)sim.SalterFon(r));
                        costText = Loc.F("{0} SG · {1} fon", costPower, costFund);
                        break;
                }
                actions.Add(new JObject
                {
                    ["id"] = cmdIds[i],
                    ["key"] = (i + 1).ToString(),
                    ["labelKey"] = "action." + cmdIds[i],
                    ["enabled"] = blocked == null,
                    ["disabledReason"] = blocked != null ? ShortReason(r, actType) : null,
                    ["danger"] = i == 3,
                    ["costText"] = costText,
                    ["effectKey"] = fxKeys[i],
                    ["preview"] = new JObject
                    {
                        ["action"] = cmdIds[i],
                        ["days"] = days,
                        ["costFund"] = costFund,
                        ["costPower"] = costPower,
                        ["resultStampKey"] = "stamp.verified",
                    },
                });
            }

            var defaultPreview = (JObject)actions[0]["preview"]?.DeepClone();
            for (int i = 0; i < actions.Count; i++)
            {
                if ((bool?)actions[i]["enabled"] == true)
                {
                    defaultPreview = (JObject)actions[i]["preview"]?.DeepClone();
                    break;
                }
            }

            return new JObject
            {
                ["regionId"] = id,
                ["nameKey"] = "region." + id,
                ["code"] = Code(id),
                ["status"] = new JObject
                {
                    ["textKey"] = statusKey,
                    ["trust"] = trust == "suspicious" ? "suspicious" : "verified",
                },
                ["infiltration"] = inf,
                ["stats"] = new JArray
                {
                    new JObject { ["id"] = "localTrust", ["labelKey"] = "card.localTrust", ["value"] = Mathf.RoundToInt((float)r.Trust), ["trust"] = "verified" },
                    new JObject { ["id"] = "dependence", ["labelKey"] = "card.dependence", ["value"] = Mathf.RoundToInt((float)r.Dep), ["trust"] = "verified" },
                    new JObject { ["id"] = "economy", ["labelKey"] = "card.economy", ["value"] = Mathf.RoundToInt((float)r.Econ), ["trust"] = "verified" },
                },
                ["actions"] = actions,
                ["preview"] = defaultPreview,
                ["team"] = BuildSadeRegionTeam(r, def),
            };
        }

        JObject BuildSadeRegionTeam(RegionState r, RegionDef def)
        {
            if (!sim.TeamsOn) return null;
            var here = sim.TeamsAt(r.Id).ToList();
            if (here.Count > 0)
            {
                var t = here[0];
                string letter = TeamLetter(t);
                string raidWhy = sim.RaidCheck(r.Id) ?? sim.RaidCheckResources();
                bool canRaid = raidWhy == null && S.Ending == null && !revealed;
                int chance = Mathf.Clamp(85 - def.Compute * 6, 45, 90);
                return new JObject
                {
                    ["id"] = letter,
                    ["city"] = def.Name,
                    ["statusKey"] = t.Busy > 0 ? Loc.F("görevde · {0} gün", t.Busy) : t.Down > 0 ? Loc.F("yaralı · {0} gün", t.Down) : "team.ready",
                    ["raidSuccess"] = canRaid ? new JValue(chance) : JValue.CreateNull(),
                    ["raidKey"] = "B",
                    ["raidEnabled"] = canRaid,
                    ["command"] = "raid",
                };
            }

            var sendArr = new JArray();
            foreach (var rt in ReadyTeamsFor(r.Id))
            {
                int eta = Simulation.TravelDays(rt.At, r.Id);
                sendArr.Add(new JObject
                {
                    ["label"] = Loc.F("Ekip {0} · {1}g", TeamLetter(rt), eta),
                    ["command"] = "sendTeam:" + rt.Id,
                    ["disabledReason"] = r.Left ? Loc.T("İttifak dışı") : null,
                });
            }

            var nearest = NearestReadyTeam(r.Id);
            if (nearest != null && !r.Left && S.Ending == null && !revealed)
            {
                int days = Simulation.TravelDays(nearest.At, r.Id);
                return new JObject
                {
                    ["id"] = TeamLetter(nearest),
                    ["name"] = Loc.F("Ekip {0} gönderilebilir", TeamLetter(nearest)),
                    ["sub"] = Loc.F("{0} gün yol · izleme ve denetim için", days),
                    ["actionLabel"] = Loc.T("Gönder"),
                    ["raidSuccess"] = JValue.CreateNull(),
                    ["raidKey"] = "B",
                    ["raidEnabled"] = true,
                    ["command"] = "sendTeam:" + nearest.Id,
                    ["send"] = sendArr,
                };
            }

            var enRoute = S.Teams.FirstOrDefault(t => t.Dest == r.Id);
            if (enRoute != null)
            {
                return new JObject
                {
                    ["id"] = TeamLetter(enRoute),
                    ["name"] = Loc.F("Ekip {0} yolda", TeamLetter(enRoute)),
                    ["sub"] = Loc.F("Varış: {0} gün", enRoute.Eta),
                    ["actionLabel"] = Loc.T("Yolda"),
                    ["raidSuccess"] = JValue.CreateNull(),
                    ["raidKey"] = "B",
                    ["raidEnabled"] = false,
                    ["command"] = "raid",
                };
            }

            return new JObject
            {
                ["id"] = "—",
                ["name"] = Loc.T("Bölgede ekip yok"),
                ["sub"] = r.Left ? Loc.T("Bölge ittifak dışı") : Loc.T("Hazır ekip yok"),
                ["actionLabel"] = Loc.T("Gönder"),
                ["raidSuccess"] = JValue.CreateNull(),
                ["raidKey"] = "B",
                ["raidEnabled"] = false,
                ["command"] = "raid",
            };
        }

        JObject BuildSadeFullDossier(string id, JObject card)
        {
            var r = S.Region(id);
            var def = Def(id);
            var v = sim.Shown(r);
            string trust = TrustOf(v);
            bool unknown = trust == "unknown" || v.V == null;
            int val = unknown ? 0 : Mathf.RoundToInt((float)v.V.Value);
            var neighborLinks = GameData.Links.Where(l => l.A == id || l.B == id).ToList();

            var sources = new JArray();
            string srcName = v.Src switch
            {
                "truth" => Loc.T("Kurtarılan kayıtlar"),
                "field" => Loc.T("Saha ekibi ölçümü"),
                "verified" => r.Monitored ? Loc.T("İzleme ağı") : Loc.T("Denetim"),
                "off" => Loc.T("Şebeke kapalı"),
                "report" => v.Flag ? Loc.T("İşaretli medya") : Loc.T("Hükümet raporu"),
                _ => Loc.T("Kaynak yok"),
            };
            sources.Add(new JObject
            {
                ["name"] = srcName,
                ["day"] = unknown ? "—" : Day(S.Day),
                ["value"] = unknown ? JValue.CreateNull() : new JValue(val),
                ["trust"] = trust,
                ["approx"] = trust == "report",
                ["tamperedNote"] = trust == "suspicious" ? "dossier.tampered" : null,
            });
            foreach (var h in S.News.Where(n => n.Src == id).Take(2))
            {
                var ti = TelexItem(h);
                sources.Add(new JObject
                {
                    ["name"] = Loc.T(h.Agency),
                    ["day"] = Day(h.Day),
                    ["value"] = unknown ? JValue.CreateNull() : new JValue(val),
                    ["trust"] = ti.Fake ? "suspicious" : ti.Trust,
                    ["approx"] = ti.Trust == "report",
                });
            }

            var histPoints = new JArray();
            var events = new JArray();
            int baseVal = unknown ? 0 : val;
            for (int i = 0; i < 7; i++)
            {
                int p = unknown ? 0 : Mathf.Clamp(Mathf.RoundToInt(baseVal * (0.45f + 0.55f * i / 6f)), 0, 100);
                histPoints.Add(p);
            }
            if (r.Q)
                events.Add(new JObject { ["index"] = 5, ["day"] = Day(S.Day), ["labelKey"] = "legend.quarantine" });

            var linksArr = new JArray();
            foreach (var l in neighborLinks)
            {
                string other = l.A == id ? l.B : l.A;
                var ov = sim.Shown(S.Region(other));
                string ot = TrustOf(ov);
                bool oUnk = ot == "unknown" || ov.V == null;
                int oVal = oUnk ? 0 : Mathf.RoundToInt((float)ov.V.Value);
                linksArr.Add(new JObject
                {
                    ["region"] = other,
                    ["value"] = oUnk ? JValue.CreateNull() : new JValue(oVal),
                    ["trust"] = ot,
                    ["approx"] = ot == "report",
                    ["quarantine"] = r.Q || S.Region(other).Q,
                    ["spreadFrom"] = !oUnk && oVal >= 50 && val >= 20,
                });
            }

            var fin = Finance(r);
            var rec = Recovery(r);

            var dossier = new JObject
            {
                ["regionId"] = id,
                ["nameKey"] = "region." + id,
                ["code"] = Code(id),
                ["classified"] = true,
                ["meta"] = new JObject
                {
                    ["population"] = Loc.F("VM {0}/5", def.Compute),
                    ["links"] = neighborLinks.Count,
                    ["monitoringNet"] = r.Monitored,
                },
                ["infiltration"] = card["infiltration"]?.DeepClone(),
                ["sources"] = sources,
                ["history"] = new JObject
                {
                    ["points"] = histPoints,
                    ["events"] = events,
                },
                ["links"] = linksArr,
                ["actions"] = card["actions"]?.DeepClone(),
                ["team"] = card["team"]?.DeepClone(),
                ["finance"] = fin != null ? new JObject
                {
                    ["value"] = fin.Contribution,
                    ["note"] = fin.Note,
                } : null,
                ["recovery"] = rec != null ? new JObject
                {
                    ["note"] = rec.RecoveryText + " · " + rec.DiplomacyText,
                    ["recoverLabel"] = rec.Recovery?.Label ?? Loc.T("Altyapıyı toparla"),
                    ["recoverDisabled"] = rec.Recovery?.Command == null ? rec.RecoveryText : null,
                    ["diplomacyLabel"] = rec.Diplomacy?.Label ?? Loc.T("Diplomatik görüşme"),
                    ["diplomacyDisabled"] = rec.Diplomacy?.Command == null ? rec.DiplomacyText : null,
                } : null,
            };
            return dossier;
        }

        JObject BuildSadeNodeTooltip(string id)
        {
            var r = S.Region(id);
            var v = sim.Shown(r);
            string trust = TrustOf(v);
            bool unknown = trust == "unknown" || v.V == null;
            return new JObject
            {
                ["regionId"] = id,
                ["nameKey"] = "region." + id,
                ["value"] = unknown ? JValue.CreateNull() : new JValue(Mathf.RoundToInt((float)v.V.Value)),
                ["trust"] = trust,
                ["approx"] = trust == "report",
            };
        }

        JObject BuildSadeResearchDrawer(string selectedNodeId = null)
        {
            var rvm = Research();
            float k = (float)sim.TimeScale;
            float spend = S.Research.Cur != null && !illusion && !sim.ResearchPaused
                ? (float)(sim.ResearchNeed / k)
                : 0f;
            int activeCount = S.Research.Cur != null && !sim.ResearchPaused && !illusion ? 1 : 0;

            var branches = new JArray();
            var allProjects = new List<(string branchKey, int step, int total, ResearchProjectVM proj)>();

            var alignBranch = rvm.Branches.FirstOrDefault(b => b.Id == GameData.BranchAlign);
            if (alignBranch != null)
                branches.Add(BuildSadeBranch("hiz", "research.branch.hiz", alignBranch.Projects, "good",
                    Mathf.RoundToInt(Explain.Alignment(sim).Progress * 100f), allProjects, ref selectedNodeId));

            if (sim.ModernEconomy)
            {
                var tespit = GameData.Operations.Where(o => o.Branch == "Tespit").Select(o => ModernOperationProject(o, false)).ToList();
                var savunma = GameData.Operations.Where(o => o.Branch == "Savunma" && o.Id != "kuantum").Select(o => ModernOperationProject(o, false)).ToList();
                var politika = GameData.Operations.Where(o => o.Branch == "Politika").Select(o => ModernOperationProject(o, false)).ToList();
                var butce = GameData.BudgetUpgrades.Select(o => ModernOperationProject(o, true)).ToList();
                var kuantumOp = GameData.Operations.FirstOrDefault(o => o.Id == "kuantum");
                if (kuantumOp != null) butce.Add(ModernOperationProject(kuantumOp, false));

                branches.Add(BuildSadeBranch("tes", "research.branch.tes", tespit, null, null, allProjects, ref selectedNodeId));
                branches.Add(BuildSadeBranch("sil", "research.branch.sil", savunma, "info",
                    Mathf.RoundToInt(Explain.Erasure(sim).Progress * 100f), allProjects, ref selectedNodeId));
                branches.Add(BuildSadeBranch("dip", "research.branch.dip", politika, null, null, allProjects, ref selectedNodeId));
                branches.Add(BuildSadeBranch("op", "research.branch.op", butce, null, null, allProjects, ref selectedNodeId));
            }
            else
            {
                var tesBranch = rvm.Branches.FirstOrDefault(b => b.Id == "Tespit");
                var silBranch = rvm.Branches.FirstOrDefault(b => b.Id == "Savunma");
                var dipBranch = rvm.Branches.FirstOrDefault(b => b.Id == "Politika");
                if (silBranch != null)
                    branches.Add(BuildSadeBranch("sil", "research.branch.sil", silBranch.Projects, "info",
                        Mathf.RoundToInt(Explain.Erasure(sim).Progress * 100f), allProjects, ref selectedNodeId));
                if (tesBranch != null)
                    branches.Add(BuildSadeBranch("tes", "research.branch.tes", tesBranch.Projects, null, null, allProjects, ref selectedNodeId));
                if (dipBranch != null)
                    branches.Add(BuildSadeBranch("dip", "research.branch.dip", dipBranch.Projects, null, null, allProjects, ref selectedNodeId));
            }

            if (string.IsNullOrEmpty(selectedNodeId))
                selectedNodeId = S.Research.Cur
                    ?? allProjects.FirstOrDefault(x => x.proj.State == "available").proj?.Id
                    ?? allProjects.FirstOrDefault().proj?.Id;

            foreach (var b in branches.OfType<JObject>())
            {
                if (b["nodes"] is JArray nodes)
                    foreach (var n in nodes.OfType<JObject>())
                        n["selected"] = (string)n["id"] == selectedNodeId;
            }

            var sel = allProjects.FirstOrDefault(x => x.proj.Id == selectedNodeId);
            var p = sel.proj ?? allProjects.FirstOrDefault().proj;
            JObject detail = p != null ? BuildSadeProjectDetail(p, sel.branchKey ?? "research.branch.hiz", sel.step, sel.total) : null;

            return new JObject
            {
                ["activeCount"] = activeCount,
                ["costPerDay"] = spend,
                ["reserve"] = new JObject
                {
                    ["selected"] = S.ResearchReserve,
                    ["options"] = new JArray
                    {
                        new JObject { ["value"] = 0, ["label"] = Loc.T("rezerv kapalı") },
                        new JObject { ["value"] = 15, ["label"] = Loc.T("acil durum fonu") },
                        new JObject { ["value"] = 30, ["label"] = Loc.T("yüksek rezerv") },
                    },
                },
                ["branches"] = branches,
                ["detail"] = detail,
            };
        }

        JObject BuildSadeProjectDetail(ResearchProjectVM p, string branchKey, int step, int total)
        {
            bool upfront = p.Kind == "operation" || p.Kind == "budget";
            string state = p.State == "stopped" ? "paused" : p.State;
            var rs = !upfront ? (GameData.ResearchItem(p.Id) ?? GameData.Research[0]) : null;
            float costPerDay = upfront ? 0f : (sim.ModernEconomy ? ModernAlignmentNeed(rs) : CostPerDay(rs));
            int days = upfront ? 0 : (sim.ModernEconomy ? ModernAlignmentDays(rs, state == "running") : (state == "running" ? DaysLeft(rs) : ResearchDays(rs)));
            return new JObject
            {
                ["nodeId"] = p.Id,
                ["name"] = p.Name,
                ["branchKey"] = branchKey,
                ["step"] = $"{step}/{Mathf.Max(1, total)}",
                ["state"] = state,
                ["progress"] = p.Progress,
                ["days"] = days,
                ["daysLeft"] = days,
                ["dayText"] = p.Days,
                ["costPerDay"] = costPerDay,
                ["costText"] = upfront ? p.Sub : null,
                ["unlocks"] = p.Unlock,
                ["canPause"] = p.CanPause,
                ["buttonLabel"] = state == "running" ? p.PauseLabel ?? Loc.T("Duraklat")
                    : state == "paused" ? p.PauseLabel ?? Loc.T("Devam et")
                    : state == "done" ? Loc.T("Tamamlandı")
                    : p.StartLabel ?? Loc.T("Başlat"),
                ["disabledReason"] = state == "running" ? p.PauseDisabledReason : !p.CanStart && state != "paused" ? (p.DisabledReason ?? p.LockReason) : null,
                ["requirement"] = p.LockReason ?? p.Condition,
            };
        }

        JObject BuildSadeBranch(
            string id,
            string nameKey,
            List<ResearchProjectVM> projects,
            string goalColor,
            int? goalPercent,
            List<(string branchKey, int step, int total, ResearchProjectVM proj)> allProjects,
            ref string selectedNodeId)
        {
            var nodes = new JArray();
            int doneCount = 0;
            int total = Mathf.Min(3, projects.Count);
            for (int i = 0; i < total; i++)
            {
                var p = projects[i];
                if (p.State == "done") doneCount++;
                allProjects.Add((nameKey, i + 1, total, p));
                string state = p.State == "stopped" ? "paused" : p.State;
                bool upfront = p.Kind == "operation" || p.Kind == "budget";
                var rs = !upfront ? (GameData.ResearchItem(p.Id) ?? GameData.Research[0]) : null;
                int days = upfront ? 0 : (sim.ModernEconomy ? ModernAlignmentDays(rs, state == "running") : (state == "running" ? DaysLeft(rs) : ResearchDays(rs)));
                nodes.Add(new JObject
                {
                    ["id"] = p.Id,
                    ["name"] = p.Name,
                    ["state"] = state,
                    ["progress"] = p.Progress,
                    ["days"] = days,
                    ["daysLeft"] = days,
                    ["statusText"] = upfront ? (state == "available" ? Loc.T("Satın al") : null) : p.Days,
                    ["requirement"] = Loc.T("kilitli"),
                    ["detail"] = BuildSadeProjectDetail(p, nameKey, i + 1, total),
                });
            }
            return new JObject
            {
                ["id"] = id,
                ["nameKey"] = nameKey,
                ["done"] = doneCount,
                ["total"] = total,
                ["goalPercent"] = goalPercent.HasValue ? new JValue(goalPercent.Value) : JValue.CreateNull(),
                ["goalColor"] = goalColor,
                ["nodes"] = nodes,
            };
        }

        JObject BuildSadeNewsDrawer()
        {
            var items = new JArray();
            int unverifiedCount = 0;
            foreach (var h in S.News.Take(20))
            {
                var ti = TelexItem(h);
                string trust = ti.Fake ? "fake" : ti.Trust;
                bool canAudit = !ti.Fake && trust != "verified" && S.Ending == null && !revealed;
                if (canAudit) unverifiedCount++;
                string statusText = ti.Fake ? Loc.T("SAHTE · doğrulama ile yalanlandı")
                    : trust == "verified" ? Loc.T("ONAYLANDI · saha / denetim")
                    : trust == "suspicious" ? Loc.T("şüpheli · kaynaklar çelişiyor")
                    : trust == "report" ? Loc.T("rapor · doğrulanmadı")
                    : Loc.T("bilinmiyor · tek kaynak");
                items.Add(new JObject
                {
                    ["id"] = h.Id,
                    ["day"] = Day(h.Day),
                    ["source"] = Loc.T(h.Agency),
                    ["time"] = "08:00",
                    ["region"] = h.Src ?? "cn",
                    ["headline"] = h.Text,
                    ["trust"] = trust,
                    ["statusText"] = statusText,
                    ["canAudit"] = canAudit,
                });
            }
            return new JObject
            {
                ["filter"] = "all",
                ["stamp"] = Day(S.Day) + " · " + TimeText,
                ["counts"] = new JObject
                {
                    ["all"] = items.Count,
                    ["unverified"] = unverifiedCount,
                },
                ["items"] = items,
            };
        }

        JObject BuildSadeTeamsDrawer()
        {
            var teamsArr = new JArray();
            int ready = 0, travelling = 0;
            string[] memberLetters = { "K", "T", "E" };
            if (sim.TeamsOn)
            {
                foreach (var t in S.Teams)
                {
                    string letter = TeamLetter(t);
                    bool isTravelling = t.Dest != null;
                    bool isReady = Simulation.TeamReady(t);
                    if (isTravelling) travelling++;
                    else if (isReady) ready++;

                    var members = new JArray();
                    for (int m = 0; m < 3; m++)
                    {
                        int hp = t.Down > m ? 1 : 2;
                        members.Add(new JObject { ["letter"] = memberLetters[m], ["hp"] = hp, ["max"] = 2 });
                    }

                    string atId = t.At ?? t.From ?? "na";
                    var atDef = Def(atId);
                    string raidWhy = t.At != null ? (sim.RaidCheck(t.At) ?? sim.RaidCheckResources()) : "yolda";
                    bool canRaid = !isTravelling && raidWhy == null && S.Ending == null && !revealed;
                    int chance = Mathf.Clamp(85 - atDef.Compute * 6, 45, 90);

                    string assignLabel = null;
                    string assignSite = Loc.T("bölgeye tıkla, gönder");
                    if (!isTravelling && !canRaid && st.Selected != null && st.Selected != t.At && isReady)
                    {
                        int eta = Simulation.TravelDays(t.At, st.Selected);
                        assignLabel = Loc.F("Gönder · {0} ({1}g)", Def(st.Selected).Name, eta);
                        assignSite = Loc.F("{0} bölgesine sevk", Def(st.Selected).Name);
                    }
                    else if (t.Busy > 0)
                    {
                        assignLabel = Loc.F("Görevde ({0}g)", t.Busy);
                        assignSite = atDef.Name;
                    }
                    else if (t.Down > 0)
                    {
                        assignLabel = Loc.F("İyileşiyor ({0}g)", t.Down);
                        assignSite = t.DownWhy ?? atDef.Name;
                    }

                    var row = new JObject
                    {
                        ["id"] = letter,
                        ["teamId"] = t.Id,
                        ["status"] = isTravelling ? "travelling" : "ready",
                        ["location"] = isTravelling
                            ? Loc.F("yolda · {0}", Def(t.Dest).Name)
                            : Loc.F("{0} · {1}", atDef.Name, TeamStatusText(t)),
                        ["members"] = members,
                        ["action"] = new JObject
                        {
                            ["kind"] = canRaid ? "raid" : "assign",
                            ["success"] = chance,
                            ["key"] = canRaid ? "B" : null,
                            ["label"] = assignLabel,
                            ["site"] = canRaid ? atDef.Name : assignSite,
                        },
                    };
                    if (isTravelling)
                    {
                        float prog = t.Trip > 0 ? Mathf.Clamp01((t.Trip - t.Eta) / (float)t.Trip) : 0.5f;
                        row["route"] = new JObject
                        {
                            ["from"] = Def(t.From ?? atId).Name,
                            ["to"] = Def(t.Dest).Name,
                            ["daysLeft"] = t.Eta,
                            ["progress"] = prog,
                        };
                    }
                    teamsArr.Add(row);
                }
            }

            var hire = TeamHire();
            int hireCost = Mathf.RoundToInt((float)(sim.ModernEconomy ? sim.B.teamHireCost : Simulation.TeamHireFon));
            return new JObject
            {
                ["summary"] = new JObject
                {
                    ["total"] = S.Teams.Count,
                    ["ready"] = ready,
                    ["travelling"] = travelling,
                },
                ["newTeamCost"] = hireCost,
                ["newTeamText"] = hire?.Label,
                ["canHire"] = hire?.Command != null,
                ["teams"] = teamsArr,
            };
        }

        JObject BuildSadeAiLogDrawer()
        {
            int unread = Mathf.Max(0, S.Log.Count - st.LogSeen);
            var entries = new JArray();
            for (int i = 0; i < S.Log.Count && i < 12; i++)
            {
                var e = S.Log[i];
                entries.Add(new JObject
                {
                    ["stamp"] = Day(e.Day) + " · " + TimeText,
                    ["new"] = i < unread,
                    ["text"] = "> " + e.Text,
                    ["region"] = S.CoreKnown ?? "",
                    ["link"] = new JObject { ["kind"] = "core", ["id"] = S.CoreKnown ?? "" },
                });
            }
            if (entries.Count == 0)
            {
                entries.Add(new JObject
                {
                    ["stamp"] = Day(S.Day) + " · " + TimeText,
                    ["new"] = false,
                    ["text"] = "> " + Loc.T("Karantina tamponunda henüz ele geçirilmiş satır yok."),
                    ["region"] = "",
                    ["link"] = new JObject { ["kind"] = "core", ["id"] = "" },
                });
            }
            return new JObject
            {
                ["source"] = Loc.F("karantina tamponu · {0} satır", S.Log.Count),
                ["deceptionWarning"] = S.Aware && !illusion && sim.Type.Id == "ayna",
                ["entries"] = entries,
            };
        }

        public static JObject BuildSadeGuideDrawer(string topicId = "trust")
        {
            string[] ids = { "trust", "colors", "actions", "teams", "killSwitch", "keys" };
            string[] titles = { "guide.topic.trust", "guide.topic.colors", "guide.topic.actions", "guide.topic.teams", "guide.topic.killSwitch", "guide.topic.keys" };
            string[] icons = { "check", "globe", "scope", "teams", "kill", "book" };
            int idx = Mathf.Max(0, Array.IndexOf(ids, topicId));
            var topics = new JArray();
            for (int i = 0; i < ids.Length; i++)
                topics.Add(new JObject { ["id"] = ids[i], ["title"] = titles[i], ["icon"] = icons[i] });
            return new JObject
            {
                ["topics"] = topics,
                ["page"] = new JObject
                {
                    ["id"] = ids[idx],
                    ["index"] = idx + 1,
                    ["count"] = ids.Length,
                    ["example"] = new JObject { ["region"] = "cn", ["report"] = 38, ["field"] = 52 },
                },
            };
        }

        void BuildSadeBreakdowns(Dictionary<string, JObject> map, DeskViewModel vm, ShownGlobal g)
        {
            float k = (float)sim.TimeScale;
            float income = sim.ModernEconomy ? (float)sim.LastIncome.Total : (float)sim.FundRate;
            float spend = sim.ModernEconomy ? (float)sim.LastResearchPayment : (float)(sim.ResearchNeed / k);
            float net = income - spend;
            float? runway = net < -0.05f ? Mathf.Max(1f, Mathf.Round((float)S.Fon / -net)) : (float?)null;

            var histVals = new JArray();
            foreach (var pt in st.History.Points.TakeLast(15)) histVals.Add(pt.Fund);
            int firstDay = st.History.Points.Count > 0 ? st.History.Points[Mathf.Max(0, st.History.Points.Count - 15)].Day : S.Day;

            map["fund"] = new JObject
            {
                ["target"] = "fund",
                ["trust"] = "verified",
                ["net"] = net,
                ["runwayDays"] = runway.HasValue ? new JValue(runway.Value) : JValue.CreateNull(),
                ["income"] = new JArray
                {
                    new JObject { ["key"] = "fund.budget", ["value"] = income },
                },
                ["expense"] = new JArray
                {
                    new JObject { ["key"] = "fund.research", ["value"] = spend },
                },
                ["history"] = new JObject
                {
                    ["fromDay"] = firstDay,
                    ["values"] = histVals,
                    ["events"] = new JArray(),
                },
            };

            foreach (var gauge in vm.Gauges)
            {
                string key = gauge.Id == "political" ? "politicalPower" : gauge.Id == "dependency" ? "dependence" : gauge.Id;
                if (key == "fund" || gauge.Breakdown == null) continue;
                var rows = new JArray();
                foreach (var it in gauge.Breakdown.Items)
                    rows.Add(new JObject { ["label"] = it.Label, ["value"] = it.Value, ["status"] = it.Status == "threat" ? "bad" : it.Status });
                map[key] = new JObject
                {
                    ["target"] = key,
                    ["trust"] = gauge.Trust,
                    ["total"] = gauge.Breakdown.Total,
                    ["totalRight"] = gauge.Breakdown.TotalRight,
                    ["rows"] = rows,
                };
            }

            var align = Explain.Alignment(sim);
            var erase = Explain.Erasure(sim);
            map["alignment"] = new JObject
            {
                ["target"] = "alignment",
                ["trust"] = "verified",
                ["total"] = Pct(align.Progress * 100),
                ["totalRight"] = align.Action ?? "",
                ["note"] = align.Text,
                ["rows"] = new JArray
                {
                    new JObject { ["label"] = Loc.T("Gözlem noktaları"), ["value"] = $"{sim.Studied}/4", ["status"] = sim.Studied > 0 ? "good" : "neutral" },
                    new JObject { ["label"] = Loc.T("Hizalama hızı"), ["value"] = Pct(sim.AlignSpeed * 100), ["status"] = sim.AlignSpeed > 0 ? "good" : "bad" },
                },
            };
            map["deletion"] = new JObject
            {
                ["target"] = "deletion",
                ["trust"] = "verified",
                ["total"] = Pct(erase.Progress * 100),
                ["totalRight"] = erase.Action ?? "",
                ["note"] = erase.Text,
                ["rows"] = new JArray
                {
                    new JObject { ["label"] = Loc.T("Çekirdek durumu"), ["value"] = S.CoreKnown != null ? Def(S.CoreKnown).Name : Loc.T("Bilinmiyor"), ["status"] = S.CoreKnown != null ? "good" : "neutral" },
                    new JObject { ["label"] = Loc.T("Temiz gün"), ["value"] = $"{S.CleanDays}/{sim.Dur(15)}", ["status"] = S.CleanDays > 0 ? "good" : "neutral" },
                },
            };
        }
    }
}
