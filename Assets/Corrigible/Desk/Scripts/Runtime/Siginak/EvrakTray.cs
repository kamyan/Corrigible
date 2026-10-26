using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masadaki EVRAK tepsisi (K6 notu › çıkış, K7): son 5 vardiya raporu ve en altta emir belgesi (açılışta BAŞLA ya da
    /// KAYITTAN DEVAM ile dosyalanır). En üstte son raporun A5 teleks kâğıdı (rapor yoksa emir belgesi) durur; okuma
    /// görünümü açıkken kâğıt tepside değildir, dosyalanınca geri gelir. Raporlar kayda girmez: bağlayıcı yan dosyaya yazar.
    /// </summary>
    public sealed class EvrakTray : MonoBehaviour
    {
        [SerializeField, Min(1)] int keep = 5;
        [Tooltip("Son raporun masadaki A5 kâğıdı.")]
        [SerializeField] GameObject sheet;
        [SerializeField] TMP_Text sheetTitle;

        readonly List<ShiftReportInput> reports = new List<ShiftReportInput>();
        bool sheetOut, briefing, briefingOut;

        public IReadOnlyList<ShiftReportInput> Reports => reports;
        /// <summary>Emir belgesi tepside (açılış bitti).</summary>
        public bool HasBriefing => briefing;
        /// <summary>Okuma görünümü dosyalanırken kâğıdın kaydığı nokta.</summary>
        public Vector3 DropPoint => sheet != null ? sheet.transform.position : transform.position;
        public event Action Changed;

        public void Configure(GameObject topSheet, TMP_Text title) { sheet = topSheet; sheetTitle = title; }

        void Awake() => Apply();

        /// <summary>Yeni rapor (en sona); en eskisi düşer.</summary>
        public void Add(ShiftReportInput report)
        {
            if (report == null) return;
            reports.Add(report);
            if (reports.Count > keep) reports.RemoveRange(0, reports.Count - keep);
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Kayıttan devam: raporları animasyonsuz geri koyar.</summary>
        public void Restore(IEnumerable<ShiftReportInput> saved)
        {
            reports.Clear();
            if (saved != null) reports.AddRange(saved);
            if (reports.Count > keep) reports.RemoveRange(0, reports.Count - keep);
            sheetOut = false;
            Apply();
        }

        public void Clear() => Restore(null);

        /// <summary>Son rapor okuma görünümünde (tepside değil).</summary>
        public void SetSheetOut(bool taken)
        {
            sheetOut = taken;
            Apply();
        }

        /// <summary>Emir belgesi tepsiye dosyalandı (yeni oyunda açılışa kadar yok).</summary>
        public void SetBriefing(bool filed)
        {
            briefing = filed;
            briefingOut = false;
            Apply();
        }

        /// <summary>Emir belgesi okuma görünümünde (H); rapor yoksa üstteki kâğıt odur.</summary>
        public void SetBriefingOut(bool taken)
        {
            briefingOut = taken;
            Apply();
        }

        /// <summary>Dil değişince kâğıttaki başlık.</summary>
        public void Relabel() => Apply();

        void Apply()
        {
            bool report = reports.Count > 0;
            bool show = report ? !sheetOut : briefing && !briefingOut;
            if (sheet != null && sheet.activeSelf != show) sheet.SetActive(show);
            if (show && sheetTitle != null)
                sheetTitle.text = report ? Corrigible.Core.Loc.F("VARDİYA RAPORU · {0:000}", reports[reports.Count - 1].before.shift)
                    : Corrigible.Core.Loc.T("HAREKÂT EMRİ");
        }
    }
}
