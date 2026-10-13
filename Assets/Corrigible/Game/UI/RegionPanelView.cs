using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Seçili bölgenin paneli: durum etiketleri, sızma ölçümü, istatistikler ve dört bölge eylemi.</summary>
    public sealed class RegionPanelView : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] GameObject empty;
        [SerializeField] GameObject detail;
        [SerializeField] TMP_Text regionName;
        [SerializeField] TMP_Text latLon;

        [Header("Etiketler")]
        [SerializeField] ChipView status;
        [SerializeField] ChipView coreChip;
        [SerializeField] ChipView observationChip;
        [SerializeField] ChipView patchChip;

        [Header("Sızma")]
        [SerializeField] TMP_Text infValue;
        [SerializeField] TMP_Text infSource;
        [SerializeField] TMP_Text infNote;

        [Header("İstatistikler")]
        [SerializeField] TMP_Text trust;
        [SerializeField] TMP_Text trustSub;
        [SerializeField] TMP_Text dep;
        [SerializeField] TMP_Text depSub;
        [SerializeField] TMP_Text econ;

        [Header("Eylemler")]
        [SerializeField] ActionButtonView monitor;
        [SerializeField] ActionButtonView audit;
        [SerializeField] ActionButtonView quarantine;
        [SerializeField] ActionButtonView kill;
        [Tooltip("Eylemin üstüne gelince ne olacağını anlatan satır.")]
        [SerializeField] TMP_Text preview;

        [Header("Saha (deneysel)")]
        [SerializeField] GameObject field;
        [SerializeField] TMP_Text fieldHere;
        [SerializeField] Transform sendButtons;
        [SerializeField] StyledButton sendPrefab;
        [SerializeField] ActionButtonView raid;

        /// <summary>Ekibi bu bölgeye gönder: (ekip, bölge).</summary>
        public event Action<string, string> SendTeam;
        public event Action<string> RaidClicked;
        readonly List<(StyledButton b, string team)> sendPool = new List<(StyledButton, string)>();

        /// <summary>Eylem düğmesine basıldı: (eylem, bölge).</summary>
        public event Action<string, string> ActionClicked;

        Simulation sim;
        string id;

        void Awake()
        {
            Wire(monitor, Actions.Monitor);
            Wire(audit, Actions.Audit);
            Wire(quarantine, Actions.Quarantine);
            Wire(kill, Actions.KillSwitch);
            if (raid != null) UiUtil.OnClick(raid.Styled.Button, () => RaidClicked?.Invoke(id));
        }

        void Wire(ActionButtonView b, string action)
        {
            UiUtil.OnClick(b.Styled.Button, () => ActionClicked?.Invoke(action, id));
            // Önizleme: üstüne gelince eylemin sonucu (baskındaki gibi)
            var h = HoverRelay.On(b);
            h.Entered += () => { hovered = action; ShowPreview(); };
            h.Exited += () => { if (hovered == action) { hovered = null; ShowPreview(); } };
        }

        string hovered;

        void ShowPreview()
        {
            if (preview == null) return;
            string t = hovered != null && sim != null && id != null ? Explain.Action(sim, hovered, id) : null;
            preview.gameObject.SetActive(t != null);
            if (t != null) preview.text = t;
        }

        public void Bind(Simulation s, string regionId)
        {
            sim = s; id = regionId;
            empty.SetActive(id == null);
            detail.SetActive(id != null);
            if (id == null) return;
            var d = GameData.Region(id);
            regionName.text = d.Name;
            latLon.text = Loc.F("{0} · veri merkezi {1}/5", d.LatLon, d.Compute);
            Refresh();
        }

        void Chip(ChipView chip, string text, Color color) => chip.Set(text, color, color == theme.muted ? theme.line2 : color);
        void Short(ActionButtonView b, bool on) => b.Sub.color = on ? theme.threat : theme.muted;

        public void Refresh()
        {
            if (sim == null || id == null) return;
            var S = sim.S;
            var r = S.Region(id);
            var sh = sim.Shown(r);
            bool ended = sim.EndRevealed, analog = S.Done("analog");
            if (ended) Chip(status, Loc.T("GERÇEK DEĞERLER"), theme.muted);
            else if (r.Left) Chip(status, Loc.T("İTTİFAK DIŞI"), theme.muted);
            else if (r.Off > 0) Chip(status, Loc.F("ÇEVRİMDIŞI · {0} G", r.Off), theme.threat);
            else if (r.Q) Chip(status, Loc.T("KARANTİNA"), theme.warn);
            else Chip(status, Loc.T("NORMAL"), theme.muted);
            bool isCore = ended ? S.Core == r.Id : S.CoreKnown == r.Id;
            coreChip.Show(isCore);
            observationChip.Show(r.Monitored && r.Detected && !ended);
            patchChip.Show(r.Patch > 0 && !ended);
            patchChip.SetText(Loc.F("YAMA · {0} G", r.Patch));

            string val = Js.Round(sh.V ?? 0).ToString();
            bool report = true; Color srcColor = theme.muted;
            if (sh.Src == "unknown") { infValue.text = "?"; infSource.text = Loc.T("BİLİNMİYOR"); infNote.text = Loc.T("Kimse henüz bir şey aramıyor. İzleme ağı kurarsan gerçeği görürsün."); }
            else if (r.Off > 0 && !ended) { infValue.text = "—"; infSource.text = Loc.T("ÇEVRİMDIŞI"); infNote.text = Loc.F("{0} gün sonra yeniden açılacak.", r.Off); }
            else if (sh.Src == "truth") { infValue.text = val; report = false; infSource.text = Loc.T("GERÇEK"); srcColor = theme.verify; infNote.text = Loc.T("Oyun sonu: perde kalktı."); }
            else if (sh.Src == "field") { infValue.text = val; report = false; infSource.text = Loc.T("SAHA"); srcColor = theme.verify; infNote.text = Loc.T("Saha ekipleri bizzat ölçtü. Ekranlara güvenmeyen tek ölçüm bu."); }
            else if (sh.Src == "verified") { infValue.text = val; report = false; infSource.text = Loc.T("DOĞRULANMIŞ"); srcColor = theme.verify; infNote.text = r.Monitored ? Loc.T("İzleme ağı ölçüyor.") : Loc.T("Bugünkü denetim sonucu."); }
            else if (sh.Flag) { infValue.text = val; infSource.text = Loc.T("ŞÜPHELİ RAPOR"); srcColor = theme.warn; infNote.text = Loc.T("Bu bölgenin medyası sahte haber yaydı. Rapora güvenme."); }
            else { infValue.text = val; infSource.text = Loc.T("RAPOR"); infNote.text = Loc.T("Yerel hükümet ve medya bildirimi. Yalan olabilir."); }
            infValue.fontStyle = report ? FontStyles.Bold | FontStyles.Italic : FontStyles.Bold;
            infValue.color = report ? theme.muted : theme.fg;
            infSource.color = srcColor;

            double veto = sim.VetoThreshold;
            trust.text = Js.Round(r.Trust).ToString(); trustSub.text = r.Trust < 20 ? Loc.T("çekilme sınırı") : Loc.T("20 altı: çekilir");
            dep.text = Js.Round(r.Dep).ToString(); depSub.text = r.Dep > veto ? Loc.T("veto ediyor") : Loc.F("veto {0}", veto);
            dep.color = r.Dep > veto ? theme.threat : r.Dep >= 40 ? theme.warn : theme.fg;
            econ.text = Js.Round(r.Econ).ToString();

            double mc = sim.MonitorCostFor(r.Id);
            monitor.Styled.SetEnabled(!ended && !r.Monitored && !r.Left);
            monitor.Title.text = r.Monitored ? Loc.T("İzleme ağı kurulu") : Loc.T("İzleme ağı kur");
            monitor.Sub.text = r.Monitored ? (r.Detected ? Loc.T("gözlem noktası") : Loc.T("gerçek değer görünüyor")) : Loc.F("{0} fon", mc);
            Short(monitor, !r.Monitored && S.Fon < mc);

            double ac = sim.AuditCost;
            audit.Styled.SetEnabled(!ended && !r.Left && r.Off == 0);
            audit.Title.text = analog ? Loc.T("Saha denetimi") : Loc.T("Denetim");
            audit.Sub.text = Loc.F("{0} fon · 1 SG · -20, 20 g yama", ac);
            Short(audit, S.Fon < ac || S.Sg < 1);

            quarantine.Styled.SetEnabled(!ended && r.Off == 0 && !(r.Left && !r.Q));
            quarantine.Title.text = r.Q ? Loc.T("Karantinayı kaldır") : Loc.T("Karantina");
            quarantine.Sub.text = r.Q ? Loc.T("ücretsiz · ekonomi toparlanır") : Loc.T("8 SG · bağlantı -%90, Tekillik payı %20");
            Short(quarantine, !r.Q && S.Sg < 8);

            double kc = sim.SalterFon(r), ksg = sim.SalterSg(r);
            kill.Styled.SetEnabled(!ended && r.Off == 0 && !r.Left && r.Dep <= veto);
            kill.Sub.text = r.Dep > veto ? Loc.F("veto: bağımlılık {0} > {1}", Js.Round(r.Dep), veto)
                : isCore ? Loc.F("{0} SG · {1} fon · çekirdeği yok eder (başka yerde %20+ varsa kaçar)", ksg, kc)
                : Loc.F("{0} SG · {1} fon · 20 gün kapanır", ksg, kc);
            Short(kill, r.Dep <= veto && (S.Sg < ksg || S.Fon < kc));

            if (field != null) RefreshField(r, ended);
        }

        // Deneysel mod: bölgedeki ekipler, ekip gönderme düğmeleri, baskın; izleme ve denetim için ekip şartı
        void RefreshField(RegionState r, bool ended)
        {
            bool on = sim.TeamsOn;
            if (field.activeSelf != on) field.SetActive(on);
            UiUtil.Show(raid, on);
            if (!on) return;
            var here = sim.TeamsAt(r.Id).ToList();
            bool ready = here.Any(Simulation.TeamReady);
            fieldHere.text = here.Count > 0
                ? Loc.F("Burada: {0}", string.Join(", ", here.Select(t => $"<b>{t.Name}</b> ({TeamStatus.Of(t).text})")))
                : Loc.T("Bölgede ekip yok. İzleme ağı, denetim ve baskın için bir ekip gönder.");
            var others = sim.S.Teams.Where(t => t.At != r.Id).ToList();
            while (sendPool.Count < others.Count)
            {
                var b = Instantiate(sendPrefab, sendButtons);
                int idx = sendPool.Count;
                UiUtil.OnClick(b.Button, () => SendTeam?.Invoke(sendPool[idx].team, id));
                sendPool.Add((b, null));
            }
            for (int i = 0; i < sendPool.Count; i++)
            {
                var b = sendPool[i].b;
                bool used = i < others.Count;
                UiUtil.Show(b, used);
                if (!used) continue;
                var t = others[i];
                sendPool[i] = (b, t.Id);
                bool can = Simulation.TeamReady(t) && t.At != null && !ended && !r.Left;
                b.SetText(t.Dest == r.Id ? Loc.F("{0} yolda · {1} g", t.Name, t.Eta) : can ? Loc.F("{0} gönder · {1} g", t.Name, Simulation.TravelDays(t.At, r.Id)) : $"{t.Name} · {TeamStatus.Of(t).text}");
                b.SetEnabled(can);
            }
            if (!ready)
            {
                if (!r.Monitored) { monitor.Styled.SetEnabled(false); monitor.Sub.text += Loc.T(" · ekip gerekli"); Short(monitor, true); }
                audit.Styled.SetEnabled(false); audit.Sub.text += Loc.T(" · ekip gerekli"); Short(audit, true);
            }
            else
            {
                if (!r.Monitored) monitor.Sub.text += Loc.F(" · ekip {0} g", Simulation.TeamMonitorDays);
                audit.Sub.text += Loc.F(" · ekip {0} g", Simulation.TeamAuditDays);
            }
            string why = sim.RaidCheck(r.Id);
            raid.Styled.SetEnabled(why == null);
            raid.Title.text = Loc.T("Saha operasyonu: baskın");
            raid.Sub.text = why ?? Loc.F("{0} fon · {1} SG · {2} · taktik savaş", Simulation.RaidFon, Simulation.RaidSg, (r.Id == sim.S.CoreKnown ? Loc.T("çekirdek binası") : Loc.T("YZ sunucu kümesi")));
            // Yalnızca eksik olan kaynaksa (RaidCheck'in son koşulu) düğme "yetersiz" görünür
            Short(raid, why != null && (sim.S.Fon < Simulation.RaidFon || sim.S.Sg < Simulation.RaidSg) && why == sim.RaidCheckResources());
        }
    }
}
