using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>R10 iki sayfalık kişi dosyası. Uzun kayıtlar ek yapraklara bölünür; hiçbir satır sessizce düşmez.</summary>
    public sealed class PersonDossierFace : MonoBehaviour
    {
        [SerializeField] RectTransform left, right;
        [SerializeField] TMP_Text template, pageNumber;
        [SerializeField] RawImage portrait;
        [SerializeField] PortraitLibrary portraits;
        PageRows leftRows, rightRows;
        FolderData data;
        readonly List<List<FolderLine>> pages = new List<List<FolderLine>>();
        public int Page { get; private set; }
        public int PageCount => pages.Count;
        public bool Overflowing { get; private set; }
        static Color Ink => SiginakTheme.Active.ColorOf("ink");
        static Color Mute => SiginakTheme.Active.ColorOf("ink2");
        static Color Red => SiginakTheme.Active.ColorOf("cards.material.reservedRing");
        TMP_FontAsset Mono => DeskTheme.Active.Font(DeskFont.CpBold);
        TMP_FontAsset Body => DeskTheme.Active.Font(DeskFont.LfRegular);
        TMP_FontAsset Bold => DeskTheme.Active.Font(DeskFont.LfBold);

        public void Bind(FolderData d)
        {
            bool changed = data == null || data.PersonId != d.PersonId;
            data = d;
            leftRows ??= new PageRows(left, template); rightRows ??= new PageRows(right, template);
            LayoutLeft(); Paginate();
            Page = changed ? 0 : Mathf.Clamp(Page, 0, Mathf.Max(0, pages.Count - 1));
            LayoutRight();
        }

        public void Turn(int direction) { Page = Mathf.Clamp(Page + direction, 0, Mathf.Max(0, pages.Count - 1)); LayoutRight(); }

        void Put(PageRows rows, string text, float x, float y, float width, float size, TMP_FontAsset font, Color color)
        {
            rows.Cursor(y);
            if (rows.Put(text, x, width, size, color, font, FontStyles.Normal, true, rows.Height - 2) < 0) Overflowing = true;
        }

        void LayoutLeft()
        {
            Overflowing = false; leftRows.Begin(0);
            Put(leftRows, data.Header, 5, 4, 110, 3.4f, Mono, Mute);
            Put(leftRows, data.Name, 5, 12, 110, 6.5f, Bold, Ink);
            if (portrait)
            {
                var tex = portraits ? portraits.TextureFor(data.PortraitKey) : null;
                portrait.texture = tex ? tex : portraits ? portraits.Placeholder : null;
                portrait.uvRect = tex ? new Rect(PortraitLibrary.CropOffset, PortraitLibrary.CropScale) : new Rect(0, 0, 1, 1);
            }
            string[] labels = { Loc.T("MESLEK"), Loc.T("PROFİL"), Loc.T("GÖREV"), Loc.T("DURUM") };
            string[] values = { data.Profession, data.Trait, data.Job, data.Card?.Strip };
            for (int i = 0; i < labels.Length; i++)
            {
                Put(leftRows, labels[i], 48, 27 + i * 11, 66, 3.1f, Mono, Mute);
                Put(leftRows, values[i], 48, 31 + i * 11, 66, 3.6f, Body, Ink);
            }
            if (data.Card != null)
                for (int i = 0; i < 3; i++)
                {
                    Put(leftRows, data.Card.StatLabels[i], 6 + i * 37, 73, 34, 3.4f, Mono, Mute);
                    Put(leftRows, data.Card.StatValues[i], 6 + i * 37, 78, 34, 5.7f, Mono, Ink);
                }
            Put(leftRows, Loc.T("USTALIK"), 5, 90, 110, 3.5f, Mono, Mute);
            for (int i = 0; i < data.Mastery.Count; i++)
            {
                Put(leftRows, data.Mastery[i].Label, 5, 97 + i * 6, 43, 3.5f, Body, Ink);
                Put(leftRows, data.Mastery[i].Value, 49, 97 + i * 6, 66, 3.5f, Mono, Mute);
            }
            Put(leftRows, Loc.T("ÜZERİNDE"), 5, 141, 110, 3.4f, Mono, Mute);
            string[] gear = { Loc.T("KİT"), Loc.T("ZIRH"), Loc.T("YARDIMCI") };
            for (int i = 0; i < 3; i++)
            {
                Put(leftRows, gear[i], 6 + i * 37, 148, 34, 3.1f, Mono, Mute);
                Put(leftRows, string.IsNullOrEmpty(data.Gear[i]) ? "—" : data.Gear[i], 6 + i * 37, 153, 34, 3.5f, Body, Ink);
            }
            leftRows.End();
        }

        float Height(FolderLine line)
        {
            string text = line.Section ? line.Label : string.IsNullOrEmpty(line.Label) ? line.Value : Loc.F("{0} · {1}", line.Label, line.Value);
            return rightRows.Measure(text, 110, line.Section ? 3.5f : 3.8f, line.Section ? Mono : Body, true) + (line.Section ? 3 : 1.5f);
        }

        void Paginate()
        {
            pages.Clear(); var page = new List<FolderLine>(); pages.Add(page); float y = 25;
            var lines = data.DossierRight.Count > 0 ? data.DossierRight : data.Right;
            foreach (var line in lines)
            {
                float h = Height(line);
                if (y + h > right.rect.height - 42 && page.Count > 0) { page = new List<FolderLine>(); pages.Add(page); y = 25; }
                page.Add(line); y += h;
            }
        }

        void LayoutRight()
        {
            rightRows.Begin(0);
            Put(rightRows, data.AdmissionHeader, 5, 4, 110, 3.4f, Mono, Mute);
            Put(rightRows, data.Name, 5, 12, 110, 5.5f, Bold, Ink);
            float y = 25;
            if (Page < pages.Count) foreach (var line in pages[Page])
            {
                string text = line.Section ? line.Label : string.IsNullOrEmpty(line.Label) ? line.Value : Loc.F("{0} · {1}", line.Label, line.Value);
                Put(rightRows, text, 5, y, 110, line.Section ? 3.5f : 3.8f, line.Section ? Mono : Body,
                    line.Section ? Red : line.Tone == StripTone.Red ? Red : Ink);
                y += Height(line);
            }
            rightRows.End();
            if (pageNumber) pageNumber.text = Loc.F("{0} / {1}", Page + 1, pages.Count);
        }
    }
}
