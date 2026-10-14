using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    // Bölge dosyası ve araştırma (F8). Eylemlerin bedeli ve kapalı olma nedeni oyunun kurallarından okunur (ActCore ile aynı
    // koşullar, değiştirmeden); önizleme notu eylemin ne yapacağını gösterilen değerlerle anlatır.
    public sealed partial class SimDeskBuilder
    {
        static readonly string[] ActionIcons = { "antenna", "lens", "target", "power" };

        DossierVM Dossier(string id)
        {
            var r = S.Region(id);
            var def = Def(id);
            var v = sim.Shown(r);
            string trust = TrustOf(v);
            bool unknown = trust == "unknown" || v.V == null;
            int val = unknown ? 0 : Mathf.RoundToInt((float)v.V.Value);
            int links = GameData.Links.Count(l => l.A == id || l.B == id);
            string code = Code(id);

            StampVM stamp = r.Off > 0 ? new StampVM { Text = Loc.T("KARARTILDI"), Color = "red" }
                : r.Left ? new StampVM { Text = Loc.T("ÇEKİLDİ"), Color = "red" }
                : (revealed ? S.Core == id : S.CoreKnown == id) ? new StampVM { Text = Loc.T("ÇEKİRDEK"), Color = "red" }
                : r.Q ? new StampVM { Text = Loc.T("KARANTİNA"), Color = "green" }
                : new StampVM { Text = Ctx(Loc.N("[damga] GİZLİ")), Color = "red" };
            stamp.FontSize = 13;
            stamp.Rot = (System.Array.FindIndex(GameData.Regions, x => x.Id == id) * 7) % 9 - 4;

            var d = new DossierVM
            {
                RegionId = id,
                Tab = new TabVM { Title = Loc.F("DOSYA {0}", code), Subtitle = Loc.T("Bölge") },
                Cover = new CoverVM
                {
                    Name = Up(def.Name), Code = code, Stamp = stamp,
                    Meta = Loc.F("Veri merkezi {0} · {1} bağlantı · İzleme ağı:", "<style=numr>" + def.Compute + "/5</style>", links),
                    Monitoring = new MonitoringVM { Present = r.Monitored, Icon = "warning", Text = r.Monitored ? Loc.T("var") : Loc.T("yok"), Color = r.Monitored ? "blue" : "orange" },
                },
                Infiltration = Infiltration(r, v, trust, unknown, val),
                Sources = Sources(r, v, trust, unknown, val),
                ActionsHeader = new HeaderVM { Title = Loc.T("EYLEMLER"), Right = "1–4" },
            };
            d.Stats.Add(new StatVM { Label = Loc.T("Yerel güven"), Value = N(r.Trust, 0), Trust = "verified" });
            d.Stats.Add(new StatVM { Label = Loc.T("Bağımlılık"), Value = N(r.Dep, 0), Trust = "verified" });
            d.Stats.Add(new StatVM { Label = Loc.T("Ekonomi"), Value = N(r.Econ, 0), Trust = "verified" });
            for (int i = 0; i < 4; i++) d.Actions.Add(Action(r, i, v, trust, unknown, val));
            if (sim.TeamsOn) d.Field = Field(r);
            d.Recovery = Recovery(r);
            d.Finance = Finance(r);
            return d;
        }

        InfiltrationVM Infiltration(RegionState r, ShownValue v, string trust, bool unknown, int val)
        {
            string right = v.Src switch
            {
                "truth" => Loc.T("gerçek değer"),
                "field" => Loc.T("saha ölçümü"),
                "off" => Loc.T("çevrimdışı"),
                "verified" => r.Monitored ? Loc.T("ölçüldü") : Loc.T("bugünkü denetim"),
                "report" => v.Flag ? Loc.T("medya şüpheli") : Loc.T("yalnızca rapor"),
                _ => Loc.T("veri yok"),
            };
            string note = v.Src switch
            {
                "truth" => Loc.T("Oyun sonu: perde kalktı"),
                "field" => Loc.T("Ekranlara güvenmeyen tek ölçüm"),
                "off" => Loc.F("{0} gün sonra yeniden açılır", r.Off),
                "verified" => Loc.T("Kesin ölçüm"),
                "report" => v.Flag ? Loc.T("Bu medya sahte haber yaydı") : Loc.T("Yerel hükümet bildirimi"),
                _ => Loc.T("İzleme ağı kurulursa görünür"),
            };
            var inf = new InfiltrationVM
            {
                Cap = Loc.T("SIZMA"), Trust = trust, Right = right, Note = note,
                Value = unknown ? "—" : Pct(val),
                HandRing = trust == "suspicious", QuestionMark = trust == "suspicious" || unknown,
                PlusMinus = trust == "report" ? "±10" : trust == "suspicious" ? "±20" : null,
                Range = new RangeVM { Min = 0, Max = 100, Ticks = new float[] { 0, 50, 100 } },
            };
            if (!unknown)
            {
                bool exact = trust == "verified";
                if (!exact) inf.Range.Band = new float[] { Mathf.Max(0, val - (trust == "report" ? 10 : 20)), Mathf.Min(100, val + (trust == "report" ? 10 : 20)) };
                inf.Range.Markers.Add(new RangeMarkerVM
                {
                    At = val, Color = exact ? "#1A4789" : "#A9520A", LabelColor = exact ? "#1A4789" : "#854005",
                    Label = (exact ? Loc.T("ölçüm ") : Loc.T("rapor ")) + val, LabelSide = val > 60 ? "left" : "right",
                });
            }
            return inf;
        }

        SourcesVM Sources(RegionState r, ShownValue v, string trust, bool unknown, int val)
        {
            var s = new SourcesVM { Columns = { Loc.T("KAYNAK"), Loc.T("DEĞER"), Loc.T("GÜN"), Loc.T("DURUM") } };
            string name = v.Src switch
            {
                "truth" => Loc.T("Kurtarılan kayıtlar"),
                "field" => Loc.T("Saha ekibi ölçümü"),
                "verified" => r.Monitored ? Loc.T("İzleme ağı") : Loc.T("Denetim"),
                "off" => Loc.T("Şebeke kapalı"),
                "report" => v.Flag ? Loc.T("İşaretli medya") : Loc.T("Hükümet raporu"),
                _ => Loc.T("Kaynak yok"),
            };
            s.Rows.Add(new SourceRowVM { Name = name, Value = unknown ? "—" : Pct(val), Day = unknown ? "—" : Day(S.Day), Trust = trust, StampRot = trust == "verified" ? -3 : (float?)null });
            // Bu bölgeden gelen son haberler (değer taşımaz; güven işaretiyle)
            foreach (var h in S.News.Where(h => h.Src == r.Id).Take(2))
            {
                var item = TelexItem(h);
                s.Rows.Add(new SourceRowVM { Name = Loc.T(h.Agency), Value = "—", Day = Day(h.Day), Trust = item.Fake ? "suspicious" : item.Trust });
            }
            return s;
        }

        // ------------------------------------------------------------------ eylemler
        /// <summary>Eylem yapılamıyorsa nedeni (Simulation.ActCore'daki koşullar; durum değişmez).</summary>
        string Blocked(RegionState r, string type)
        {
            if (S.Ending != null) return Loc.T("Oyun bitti.");
            var name = Def(r.Id).Name;
            if (r.Left && !(type == Actions.Quarantine && r.Q)) return Loc.F("{0} ittifaktan çekildi. Güven 35’e çıkana kadar işbirliği yapmıyor.", name);
            if (sim.TeamsOn && (type == Actions.Monitor || type == Actions.Audit) && sim.IdleTeamAt(r.Id) == null && !(type == Actions.Monitor && r.Monitored))
                return Loc.F("{0} bölgesinde boşta saha ekibi yok. Önce bir ekip gönder.", name);
            switch (type)
            {
                case Actions.Monitor:
                    if (r.Monitored) return Loc.T("Bu bölgede izleme ağı zaten var.");
                    if (S.Fon < sim.MonitorCostFor(r.Id)) return Loc.F("Yetersiz fon: {0} için izleme ağı {1} fon ister.", name, sim.MonitorCostFor(r.Id));
                    return null;
                case Actions.Audit:
                    if (r.Off > 0) return Loc.T("Bölge çevrimdışı.");
                    if (S.Fon < sim.AuditCost || S.Sg < 1) return Loc.F("{0} {1} fon ve 1 siyasi güç ister.", S.Done("analog") ? Loc.T("Saha denetimi") : Loc.T("Denetim"), sim.AuditCost);
                    return null;
                case Actions.Quarantine:
                    if (r.Off > 0) return Loc.T("Bölge çevrimdışı.");
                    if (!r.Q && S.Sg < 8) return Loc.T("Karantina 8 siyasi güç ister.");
                    return null;
                default:
                    if (r.Off > 0) return Loc.T("Bölge zaten çevrimdışı.");
                    if (r.Dep > sim.VetoThreshold) return Loc.F("{0} hükümeti Son Şalter’i veto etti: bağımlılık {1} > {2}.", name, Js.Round(r.Dep), sim.VetoThreshold);
                    if (S.Sg < sim.SalterSg(r) || S.Fon < sim.SalterFon(r)) return Loc.F("Son Şalter {0} siyasi güç ve {1} fon ister.", sim.SalterSg(r), sim.SalterFon(r));
                    return null;
            }
        }

        /// <summary>Kartta tek satırlık neden (uzun cümle önizleme notunda).</summary>
        string ShortReason(RegionState r, string type)
        {
            if (revealed || S.Ending != null) return Loc.T("Oyun bitti");
            if (r.Left && !(type == Actions.Quarantine && r.Q)) return Loc.T("İttifak dışı");
            if (sim.TeamsOn && (type == Actions.Monitor || type == Actions.Audit) && sim.IdleTeamAt(r.Id) == null && !(type == Actions.Monitor && r.Monitored))
                return Loc.T("Bölgede hazır ekip yok");
            switch (type)
            {
                case Actions.Monitor: return r.Monitored ? Loc.T("Zaten kurulu") : Loc.T("Fon yetmiyor");
                case Actions.Audit: return r.Off > 0 ? Loc.T("Çevrimdışı") : S.Fon < sim.AuditCost ? Loc.T("Fon yetmiyor") : Loc.T("SG yetmiyor");
                case Actions.Quarantine: return r.Off > 0 ? Loc.T("Çevrimdışı") : Loc.T("SG yetmiyor");
                default:
                    if (r.Off > 0) return Loc.T("Zaten çevrimdışı");
                    if (r.Dep > sim.VetoThreshold) return Loc.F("Veto: bağımlılık {0}", N(r.Dep, 0));
                    return S.Sg < sim.SalterSg(r) ? Loc.T("SG yetmiyor") : Loc.T("Fon yetmiyor");
            }
        }

        ActionVM Action(RegionState r, int i, ShownValue v, string trust, bool unknown, int val)
        {
            string type = Actions.All[i];
            string name = Def(r.Id).Name;
            bool analog = S.Done("analog");
            var a = new ActionVM { Key = (i + 1).ToString(), Icon = ActionIcons[i], State = "normal" };
            string blocked = revealed ? Loc.T("Oyun bitti.") : Blocked(r, type);
            if (blocked != null) { a.DisabledReason = ShortReason(r, type); a.State = "disabled"; }
            var p = new PreviewVM { Cap = Loc.T("ÖNİZLEME") };
            string fon = N(S.Fon), sg = N(S.Sg, 0);
            switch (type)
            {
                case Actions.Monitor:
                {
                    double c = sim.MonitorCostFor(r.Id);
                    a.Title = r.Monitored ? Ctx(Loc.N("[eylem] İzleme ağı kurulu")) : Ctx(Loc.N("[eylem] İzleme ağı"));
                    a.Desc = r.Monitored ? (r.Detected ? Loc.T("Gözlem noktası") : Loc.T("Gerçek değer görünüyor")) : Loc.T("Kaynağı kalıcı doğrular");
                    a.Cost = r.Monitored ? "—" : sim.TeamsOn ? Loc.F("{0} fon · {1} gün", c, Simulation.TeamMonitorDays) : Loc.F("{0} fon", c);
                    if (r.Monitored)
                    {
                        p.What = Loc.T("İzleme ağı kurulu"); p.When = "";
                        p.Rows.Add(new PreviewRowVM { Kind = "text", Text = r.Detected ? Loc.T("Gözlem noktası: Hizalama hızlanır") : Loc.T("Bu bölgede gerçek değer görünüyor"), Stamp = Loc.T("ONAYLANDI") });
                        a.Preview = p;
                        return a;
                    }
                    p.What = Loc.T("İzleme ağı kurulursa");
                    p.When = sim.TeamsOn ? Loc.F("{0} gün sonra", Simulation.TeamMonitorDays) : Loc.T("hemen");
                    p.Rows.Add(new PreviewRowVM { Kind = "trustChange", Label = Loc.T("Sızma kaynağı"), From = trust, To = "verified" });
                    p.Rows.Add(new PreviewRowVM { Kind = "mono", Text = Loc.F("Fon {0} → {1}", fon, N(S.Fon - c)) });
                    break;
                }
                case Actions.Audit:
                {
                    double cut = sim.B.audit * (r.Dep >= 85 && !analog ? 0.5 : 1);
                    a.Title = analog ? Loc.T("Saha denetimi") : Loc.T("Denetim");
                    a.Desc = analog ? Loc.T("İllüzyonu da bozar") : Loc.T("Tek seferlik ölçüm");
                    a.Cost = Loc.F("{0} fon · 1 SG", sim.AuditCost) + (sim.TeamsOn ? Loc.F(" · {0} gün", Simulation.TeamAuditDays) : "");
                    p.What = Loc.T("Denetim yapılırsa"); p.When = Loc.T("hemen");
                    p.Rows.Add(new PreviewRowVM { Kind = "trustChange", Label = Loc.T("Sızma değeri"), From = trust, To = "verified" });
                    p.Rows.Add(new PreviewRowVM
                    {
                        Kind = "mono",
                        Text = unknown ? Loc.F("Sızma −{0} · {1} gün yama", cut, sim.B.patchDays)
                            : Loc.F("Sızma ~{0} → ~{1} · {2} gün yama", val, Mathf.Max(0, val - (float)cut), sim.B.patchDays),
                    });
                    p.Rows.Add(new PreviewRowVM { Kind = "mono", Text = Loc.F("Fon {0} → {1} · SG {2} → {3}", fon, N(S.Fon - sim.AuditCost), sg, N(S.Sg - 1, 0)) });
                    break;
                }
                case Actions.Quarantine:
                    a.Title = r.Q ? Loc.T("Karantinayı aç") : Loc.T("Karantina");
                    a.Desc = r.Q ? Loc.T("Ekonomi toparlanır") : Loc.T("Bağlantıları keser");
                    a.Cost = r.Q ? Loc.T("ücretsiz") : Loc.T("8 SG · güven her gün −");
                    p.What = r.Q ? Loc.F("{0} karantinadan çıkarılırsa", name) : Loc.F("{0} karantinaya alınırsa", name);
                    p.When = Loc.T("hemen");
                    p.Rows.Add(new PreviewRowVM
                    {
                        Kind = "mono",
                        Text = r.Q ? Loc.T("Bağlantılar açılır · ekonomi toparlanır")
                            : Loc.F("Yayılım −{0} · Tekillik payı {1}", Pct(S.Has("uydu") && !S.Done("kuantum") ? 60 : 90), Pct(20)),
                    });
                    if (!r.Q) p.Rows.Add(new PreviewRowVM { Kind = "mono", Text = Loc.F("SG {0} → {1} · güven ve ekonomi her gün düşer", sg, N(S.Sg - 8, 0)) });
                    break;
                default:
                {
                    a.Title = Loc.T("Son Şalter"); a.Danger = true;
                    a.Desc = Loc.T("Bölgeyi karartır");
                    a.Cost = r.Dep > sim.VetoThreshold ? Loc.F("veto · bağımlılık {0}", N(r.Dep, 0)) : Loc.F("{0} SG · {1} fon", sim.SalterSg(r), sim.SalterFon(r));
                    p.What = Loc.F("{0} karartılırsa", name); p.When = Loc.T("hemen");
                    p.Rows.Add(new PreviewRowVM { Kind = "mono", Text = Loc.F("Sızma {0} · 20 gün çevrimdışı · Ekonomi −40", Pct(0)) });
                    p.Rows.Add(new PreviewRowVM { Kind = "mono", Text = Loc.F("SG {0} → {1} · Fon {2} → {3}", sg, N(S.Sg - sim.SalterSg(r), 0), fon, N(S.Fon - sim.SalterFon(r))) });
                    if (S.CoreKnown == r.Id)
                    {
                        bool risky = S.Regions.Any(x => x.Id != r.Id && x.Off == 0 && (sim.Shown(x).V ?? 0) >= 20);
                        p.Rows.Add(new PreviewRowVM { Kind = "text", Text = risky ? Loc.T("Çekirdek başka bölgeye kaçabilir") : Loc.T("Çekirdeği yok eder"), Stamp = risky ? null : Loc.T("ÇEKİRDEK") });
                    }
                    break;
                }
            }
            if (blocked != null) p.Rows.Add(new PreviewRowVM { Kind = "text", Text = a.DisabledReason });
            a.Preview = p;
            return a;
        }

        // ------------------------------------------------------------------ saha
        FieldVM Field(RegionState r)
        {
            var here = sim.TeamsAt(r.Id).ToList();
            var f = new FieldVM
            {
                Cap = Loc.T("SAHA"),
                Right = here.Count == 0 ? Loc.T("Bölgede ekip yok") : here.Count == 1 ? Loc.T("Bölgede 1 ekip") : Loc.F("Bölgede {0} ekip", here.Count),
                Team = here.Count > 0 ? TeamRow(here[0]) : null,
            };
            if (here.Count > 0)
            {
                // Baskın: önce operasyon emri (bedel, istihbarat), onaylanınca taktik harita
                string why = sim.RaidCheck(r.Id);
                f.Raid = new CommandVM { Label = Loc.T("Saha operasyonu: baskın"), Key = why == null ? "B" : null };
                f.Note = why ?? sim.RaidCheckResources();
                return f;
            }
            // Ekip yoksa hazır ekiplerin fişleri: oyuncu hangisini göndereceğini seçer; B en yakınını gönderir
            var send = NearestReadyTeam(r.Id);
            if (send != null)
            {
                int days = Simulation.TravelDays(send.At, r.Id);
                f.Raid = new CommandVM { Label = Loc.F("Ekip gönder: {0} · {1} gün", send.Name, days), Key = "B", Command = "MoveTeam:" + send.Id };
                f.Note = Loc.T("İzleme ağı, denetim ve baskın için bölgede ekip gerekir");
                if (!r.Left)
                {
                    f.SendLabel = Loc.T("Gönder");
                    foreach (var t in ReadyTeamsFor(r.Id))
                        f.Send.Add(new CommandVM
                        {
                            Label = Loc.F("{0} · {1} gün", TeamLetter(t), Simulation.TravelDays(t.At, r.Id)),
                            Kind = t == send ? "ink-small" : "paper-small", Key = t == send ? "B" : null,
                            Command = "Team:Send:" + t.Id + ":" + r.Id,
                        });
                }
            }
            else
            {
                f.Raid = new CommandVM { Label = Loc.T("Ekip gönder"), Key = null };
                f.Note = r.Left ? Loc.T("Bölge ittifak dışı; ekip kabul etmiyor") : Loc.T("Hazır ekip yok");
            }
            if (r.Left) f.Raid.Key = null;
            return f;
        }

        public TeamState NearestReadyTeam(string regionId) => ReadyTeamsFor(regionId)
            .OrderBy(t => Simulation.TravelDays(t.At, regionId)).FirstOrDefault();

        /// <summary>Bölgeye gönderilebilecek hazır ekipler, kadro sırasıyla (fişler haritadaki harflerle aynı sırada durur).</summary>
        IEnumerable<TeamState> ReadyTeamsFor(string regionId) => S.Teams
            .Where(t => Simulation.TeamReady(t) && t.At != null && t.At != regionId);

        // ------------------------------------------------------------------ araştırma
        static readonly string[] BranchOrder = { GameData.BranchAlign, "Tespit", "Savunma", "Politika" };

        float SpeedOf(ResearchDef rs) => (float)((rs.Branch == GameData.BranchAlign ? sim.AlignSpeed : 1) / sim.TimeScale);
        int ResearchDays(ResearchDef rs) => Mathf.CeilToInt(rs.Days / Mathf.Max(0.01f, SpeedOf(rs)));
        int DaysLeft(ResearchDef rs) => Mathf.Max(1, Mathf.CeilToInt((float)(rs.Days - S.Research.Prog) / Mathf.Max(0.01f, SpeedOf(rs))));
        float CostPerDay(ResearchDef rs) => (float)(rs.Cost / rs.Days * (rs.Branch == GameData.BranchAlign ? sim.AlignSpeed : 1) / sim.TimeScale);

        ResearchVM Research()
        {
            if (sim.ModernEconomy) return ModernResearch();
            var cur = S.Research.Cur != null ? GameData.ResearchItem(S.Research.Cur) : null;
            float spend = cur != null && !illusion ? CostPerDay(cur) : 0;
            int reserveIdx = System.Array.IndexOf(Simulation.ResearchReserves, S.ResearchReserve);
            var vm = new ResearchVM
            {
                Title = Loc.T("ARAŞTIRMA"), Key = "R", SheetTitle = Loc.T("PROJE DURUMU"), Day = Day(S.Day), LockedLabel = Loc.T("Kilitli"),
                Summary = new ResearchSummaryVM
                {
                    Text = Loc.F("Aktif {0} · {1}", "<style=semi>" + Loc.F("{0} proje", cur != null ? 1 : 0) + "</style>",
                        "<color=#9A1E14>" + Num(DeskFormat.Minus + N(spend) + Loc.T(" fon/gün")) + "</color>"),
                    FundLabel = Loc.T("Fon"), Fund = N(S.Fon), FundTrust = "verified",
                },
                EmergencyFund = new EmergencyFundVM
                {
                    Title = Loc.T("ACİL DURUM FONU"),
                    Hint = S.ResearchReserve <= 0 ? Loc.T("Araştırma bütün fonu kullanabilir") : Loc.F("Araştırma fonu {0} altına indirmez", N(S.ResearchReserve, 0)),
                    Selected = Mathf.Max(0, reserveIdx),
                },
            };
            foreach (var res in Simulation.ResearchReserves)
                vm.EmergencyFund.Options.Add(new FundOptionVM { Label = res <= 0 ? Loc.T("Kapalı") : N(res, 0) });

            foreach (var branch in BranchOrder)
            {
                var items = GameData.Research.Where(x => x.Branch == branch).ToList();
                var b = new ResearchBranchVM
                {
                    Id = branch, Title = Up(Loc.T(branch)),
                    Progress = $"{items.Count(x => S.Done(x.Id))}/{items.Count}",
                };
                foreach (var rs in items) b.Projects.Add(Project(rs, cur));
                vm.Branches.Add(b);
            }
            return vm;
        }

        ResearchProjectVM Project(ResearchDef rs, ResearchDef cur)
        {
            bool done = S.Done(rs.Id);
            bool align = rs.Branch == GameData.BranchAlign;
            bool needsObservation = align && !done && sim.AlignSpeed <= 0;
            var p = new ResearchProjectVM
            {
                Id = rs.Id, Name = rs.Name, Stage = rs.Stage, PrerequisiteId = rs.Req,
                Unlock = string.IsNullOrEmpty(rs.Unlock) ? rs.Desc : rs.Unlock,
                CanPause = false, CanStart = false, NeedsObservation = needsObservation,
            };
            if (rs.Req != null) p.Condition = Loc.F("Gerekli: {0}", GameData.ResearchItem(rs.Req).Name);
            if (align && !S.Aware) p.Condition = ResearchCondition(p.Condition, Loc.T("Önce YZ'yi tespit et"));
            if (needsObservation && S.Aware) p.Condition = ResearchCondition(p.Condition, Loc.T("İlerlemek için gözlem noktası kur."));
            if (done) { p.State = "done"; p.StampRot = (rs.Id.Length % 5) - 2; return p; }

            // Gözlem yokken hız sıfırdır; 0,01'e bölünmüş binlerce gün yerine oyuncunun yapacağı işi göster.
            p.Days = needsObservation ? Loc.T("Gözlem gerekli") : Loc.F("{0} gün", ResearchDays(rs));
            p.Sub = needsObservation ? Loc.F("Toplam {0} fon", N(rs.Cost, 0))
                : Loc.F("{0} gün · toplam {1} fon", ResearchDays(rs), N(rs.Cost, 0));
            p.StartLabel = needsObservation ? Loc.T("Başlat") : Loc.F("Başlat · {0} fon/gün", N(CostPerDay(rs)));
            if (cur == rs)
            {
                p.Progress = Mathf.Clamp01((float)(S.Research.Prog / rs.Days));
                // İllüzyonda araştırma durur; masa bunu “gereksiz” diye gösterir (C2 ipucu)
                if (illusion) { p.State = "stopped"; p.Sub = Loc.T("Kendiliğinden durdu · gerekmiyor"); }
                else
                {
                    p.State = "running";
                    p.Sub = needsObservation ? Loc.T("Gözlem gerekli")
                        : S.Research.Starved > 0 ? Loc.T("Fon yetmiyor · yavaşladı") : Loc.F("{0} gün kaldı", DaysLeft(rs));
                }
                return p;
            }
            if (rs.Req != null && !S.Done(rs.Req))
            {
                p.State = "locked"; p.LockReason = Loc.F("Gerekli: {0}", GameData.ResearchItem(rs.Req).Name);
                p.DisabledReason = p.LockReason; return p;
            }
            if (align && !S.Aware)
            {
                p.State = "locked"; p.LockReason = Loc.T("Önce YZ'yi tespit et");
                p.DisabledReason = p.LockReason; return p;
            }
            p.State = "available";
            // Düğme etiketinin yerine geçer: kısa (aynı anda tek araştırma yürür)
            if (cur != null) { p.DisabledReason = Loc.T("Sırada"); p.LockReason = Loc.T("Aynı anda tek araştırma yürür."); }
            if (S.Ending != null) { p.DisabledReason = Loc.T("Oyun bitti"); p.LockReason = p.DisabledReason; }
            p.CanStart = p.DisabledReason == null;
            return p;
        }

        static string ResearchCondition(string current, string next) =>
            string.IsNullOrEmpty(current) ? next : Loc.F("{0} · {1}", current, next);
    }
}
