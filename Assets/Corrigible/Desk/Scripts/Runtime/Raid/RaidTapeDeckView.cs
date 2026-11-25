using Corrigible.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Makaralı teyp (tesis aşama 5, Docs/TESIS-BASKINLARI-TASARIMI.md §7.2–7.3): tesis baskını boyunca iki makara döner; oda bitip
    /// kayda geçince etiket “KAYIT · PAFTA n” yazılır, sayaç penceresinde kayıtlı paftaların numarası koyulaşır. Kayıt, odanın planının
    /// bilindiğinin görünür karşılığıdır: liste <see cref="RaidViewModel.TapeSheets"/>'ten (Bridge, Core'un bilinen odaları) gelir, görünüm
    /// hiçbir şey hesaplamaz. Görünürlüğü telsiz masası belirler (yalnız tesis baskını, <see cref="RaidHudView"/>). Telsiz masasının sağ
    /// yarısı; renk ve süreler raid_tokens.json › radioDesk.tape.
    /// </summary>
    public sealed class RaidTapeDeckView : MonoBehaviour
    {
        [Tooltip("Dönen iki makara (sol besleme, sağ sarma).")]
        [SerializeField] RectTransform[] reels = Array.Empty<RectTransform>();
        [SerializeField] Graphic[] reelBodies = Array.Empty<Graphic>(), reelSpokes = Array.Empty<Graphic>(), reelHubs = Array.Empty<Graphic>();
        [SerializeField] Graphic plate, tapeLine, window, labelPaper;
        [Tooltip("Sayaç penceresinin üç hücresi ve numaraları (PAFTA 1–3).")]
        [SerializeField] Graphic[] sheetCells = Array.Empty<Graphic>();
        [SerializeField] TMP_Text[] sheetNumbers = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text label;

        int shown = -1;
        float writeAt = -1;

        /// <summary>Makaralar dönüyor (tesis baskını sürüyor, sonuç kartı yok).</summary>
        public bool Spinning { get; private set; }
        /// <summary>Etiketin yazısı (testler için).</summary>
        public TMP_Text Label => label;
        /// <summary>Sayaç hücresi koyu mu (o pafta kayıtlı).</summary>
        public bool SheetRecorded(int sheet) => sheet >= 1 && sheet <= recorded.Length && recorded[sheet - 1];
        readonly bool[] recorded = new bool[3];

        static float Number(string key, float fallback) => RaidTheme.Active != null ? RaidTheme.Active.Number(key, fallback) : fallback;

        public void Render(RaidViewModel vm)
        {
            var sheets = vm.TapeSheets ?? Array.Empty<int>();
            Spinning = vm.BlueprintWing > 0 && !vm.Finished;
            if (label != null)
            {
                label.text = sheets.Length == 0 ? Loc.T("KAYIT") : Loc.F("KAYIT · PAFTA {0}", sheets[sheets.Length - 1]);
                label.color = RaidHudView.C("radioDesk.tape.label");
            }
            // Yeni pafta kayda geçti: etiket kalemle yazılır gibi açılır (ilk çizimde, kayıttan devamda ve düzenleme modunda yazılı gelir)
            if (shown >= 0 && sheets.Length > shown && Application.isPlaying) writeAt = Time.unscaledTime;
            shown = sheets.Length;
            for (int i = 0; i < recorded.Length; i++) recorded[i] = Array.IndexOf(sheets, i + 1) >= 0;
            for (int i = 0; i < sheetCells.Length; i++)
                if (sheetCells[i] != null) sheetCells[i].color = RaidHudView.C(SheetRecorded(i + 1) ? "radioDesk.tape.cellOn" : "radioDesk.tape.cellOff");
            for (int i = 0; i < sheetNumbers.Length; i++)
            {
                if (sheetNumbers[i] == null) continue;
                sheetNumbers[i].text = (i + 1).ToString();
                sheetNumbers[i].color = RaidHudView.C(SheetRecorded(i + 1) ? "radioDesk.tape.sheetOn" : "radioDesk.tape.sheetOff");
            }
            foreach (var g in reelBodies) if (g != null) g.color = RaidHudView.C("radioDesk.tape.reel");
            foreach (var g in reelSpokes) if (g != null) g.color = RaidHudView.C("radioDesk.tape.spoke");
            foreach (var g in reelHubs) if (g != null) g.color = RaidHudView.C("radioDesk.tape.hub");
            if (plate != null) plate.color = RaidHudView.C("radioDesk.tape.plate");
            if (tapeLine != null) tapeLine.color = RaidHudView.C("radioDesk.tape.tape");
            if (window != null) window.color = RaidHudView.C("radioDesk.tape.window");
            if (labelPaper != null) labelPaper.color = RaidHudView.C("radioDesk.tape.labelPaper");
            Write();
        }

        void Update()
        {
            if (Spinning)
            {
                // İki makara da saat yönünde döner (bant soldan sağa sarılır); oyun hızından bağımsız
                float step = -Number("radioDesk.tape.reelDegPerS", 120) * Time.unscaledDeltaTime;
                foreach (var r in reels) if (r != null) r.Rotate(0, 0, step);
            }
            if (writeAt >= 0) Write();
        }

        /// <summary>Etiketin kalemle yazılışı: görünen harf sayısı <c>radioDesk.tape.writeS</c> boyunca artar.</summary>
        void Write()
        {
            if (label == null) return;
            if (writeAt < 0) { label.maxVisibleCharacters = 99999; return; }
            float p = Mathf.Clamp01((Time.unscaledTime - writeAt) / Mathf.Max(.01f, Number("radioDesk.tape.writeS", .45f)));
            label.maxVisibleCharacters = Mathf.CeilToInt(p * label.text.Length);
            if (p >= 1) { writeAt = -1; label.maxVisibleCharacters = 99999; }
        }

        void OnDisable() { if (writeAt >= 0) { writeAt = -1; if (label != null) label.maxVisibleCharacters = 99999; } }
    }
}
