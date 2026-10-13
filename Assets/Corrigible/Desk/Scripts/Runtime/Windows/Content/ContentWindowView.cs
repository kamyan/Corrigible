using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    public enum ContentKind { News, Teams, AiLog, Guide }

    /// <summary>
    /// Haberler, Saha Ekipleri, YZ Günlüğü ve Rehber pencereleri (açık karar 3): aynı klasör + mevcut bileşenlerle sade
    /// içerik. Haberler teleks öğelerinin listesi (güven işaretli), YZ Günlüğü C3/C5'teki kırmızı daktilo satırları.
    /// </summary>
    public sealed class ContentWindowView : MonoBehaviour
    {
        [SerializeField] ContentKind kind;
        [SerializeField] FolderWindow window;
        [SerializeField] RectTransform content;
        [SerializeField] float gap = 6;
        [Header("Şablonlar (türe göre biri)")]
        [SerializeField] NewsRowView newsTemplate;
        [SerializeField] TeamCardView teamTemplate;
        [SerializeField] AiLogLineView logTemplate;
        [SerializeField] GuideSectionView guideTemplate;
        [Header("YZ günlüğü başlığı")]
        [SerializeField] TMP_Text logTitle, logRight, logFooter;

        DeskPool<NewsRowView> news;
        DeskPool<TeamCardView> teams;
        DeskPool<AiLogLineView> logs;
        DeskPool<GuideSectionView> guide;

        public FolderWindow Window => window;
        /// <summary>İçerikteki bir bağlantı (rehber: “Eğitimi baştan oynat”) komutunu yayınlar.</summary>
        public event Action<string> Invoked;

        static readonly string[] Titles = { "HABERLER", "SAHA EKİPLERİ", "YZ GÜNLÜĞÜ", "REHBER" };
        static readonly string[] Keys = { "N", "T", "L", "H" };

        public void Bind(DeskViewModel vm)
        {
            window.SetTitle(Titles[(int)kind], null, Keys[(int)kind]);
            if (!Application.isPlaying && !DeskUi.EditPreview) return;
            float w = content.rect.width;
            switch (kind)
            {
                case ContentKind.News:
                    news ??= new DeskPool<NewsRowView>(newsTemplate, content);
                    var items = vm.News ?? new List<TelexItemVM>();
                    for (int i = 0; i < items.Count; i++) news.Get(i).Bind(items[i], w);
                    DeskList.Stack(news, items.Count, content, gap, r => r.Height);
                    break;
                case ContentKind.Teams:
                    teams ??= new DeskPool<TeamCardView>(teamTemplate, content);
                    var t = vm.Teams ?? new List<TeamRowVM>();
                    for (int i = 0; i < t.Count; i++) teams.Get(i).Bind(t[i]);
                    DeskList.Stack(teams, t.Count, content, gap, r => 49);
                    break;
                case ContentKind.AiLog:
                    logs ??= new DeskPool<AiLogLineView>(logTemplate, content);
                    var log = vm.AiLog;
                    if (log == null) break;
                    DeskUi.SetText(logTitle, DeskLoc.Instance.T(log.Title));
                    DeskUi.SetText(logRight, log.Right);
                    DeskUi.SetText(logFooter, DeskLoc.Instance.T(log.Footer));
                    for (int i = 0; i < log.Lines.Count; i++) logs.Get(i).Bind(log.Lines[i], w);
                    DeskList.Stack(logs, log.Lines.Count, content, 0, r => r.Height);
                    break;
                default:
                    guide ??= new DeskPool<GuideSectionView>(guideTemplate, content);
                    var g = vm.Guide?.Sections ?? new List<GuideSectionVM>();
                    for (int i = 0; i < g.Count; i++)
                    {
                        var sv = guide.Get(i);
                        if (!sv.Wired) { sv.Wired = true; sv.Invoked += c => Invoked?.Invoke(c); }
                        sv.Bind(g[i], w);
                    }
                    DeskList.Stack(guide, g.Count, content, 14, r => r.Height);
                    break;
            }
        }

        /// <summary>Çeviri tablosuna toplanan sabit başlıklar.</summary>
        internal static readonly string[] Words = { Corrigible.Core.Loc.N("HABERLER"), Corrigible.Core.Loc.N("SAHA EKİPLERİ"), Corrigible.Core.Loc.N("YZ GÜNLÜĞÜ"), Corrigible.Core.Loc.N("REHBER") };
    }
}
