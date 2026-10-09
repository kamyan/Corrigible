using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>
    /// Nokta matrisi dünya haritası. Kara noktaları, bağlantılar ve bölge düğümleri (RegionNode prefab örnekleri)
    /// sahnede ayrı objelerdir; düğümler Inspector'daki listeden gelir.
    /// </summary>
    public sealed class MapView : MonoBehaviour
    {
        [SerializeField] UiTheme theme;
        [SerializeField] MapLinksGraphic links;
        [SerializeField] RegionNode[] nodes = Array.Empty<RegionNode>();
        [Header("Saha ekipleri (deneysel)")]
        [SerializeField] MapTeamsGraphic teamPaths;
        [SerializeField] RectTransform teamLayer;
        [SerializeField] TeamMarkerView markerPrefab;

        readonly List<TeamMarkerView> markers = new List<TeamMarkerView>();

        readonly Dictionary<string, RegionNode> byId = new Dictionary<string, RegionNode>();
        Simulation sim;

        public string Selected { get; set; }
        public event Action<string> RegionClicked;

        void Awake()
        {
            foreach (var n in nodes)
            {
                byId[n.RegionId] = n;
                string id = n.RegionId;
                UiUtil.OnClick(n.Button, () => RegionClicked?.Invoke(id));
            }
            foreach (var d in GameData.Regions)
                if (!byId.ContainsKey(d.Id)) Debug.LogError($"Haritada '{d.Id}' bölgesinin düğümü yok (MapView > Nodes).", this);
        }

        public void Bind(Simulation s)
        {
            sim = s;
            links.Bind(s);
            if (teamPaths != null) teamPaths.Bind(s);
            Refresh();
        }

        /// <summary>Her gün çağrılır: düğüm renkleri, değerler, rozetler ve bağlantılar güncellenir.</summary>
        public void Refresh()
        {
            if (sim == null) return;
            foreach (var r in sim.S.Regions)
                if (byId.TryGetValue(r.Id, out var n)) n.Refresh(sim, r, Selected == r.Id, theme);
            links.SetVerticesDirty();
            RefreshTeams();
        }

        // Bölgede duran ekipler düğümün sağ üstünde yan yana; yoldakiler iki bölge arasında ilerlemeye göre
        void RefreshTeams()
        {
            if (teamLayer == null) return;
            var teams = sim.TeamsOn ? sim.S.Teams : new List<TeamState>();
            while (markers.Count < teams.Count) markers.Add(Instantiate(markerPrefab, teamLayer));
            var slot = new Dictionary<string, int>();
            for (int i = 0; i < markers.Count; i++)
            {
                bool used = i < teams.Count;
                if (markers[i].gameObject.activeSelf != used) markers[i].gameObject.SetActive(used);
                if (!used) continue;
                var t = teams[i];
                var p = MapTeamsGraphic.MapPos(t);
                if (t.Dest == null)
                {
                    slot.TryGetValue(t.At, out int k);
                    slot[t.At] = k + 1;
                    float r = MapSpace.Radius(GameData.Region(t.At)) + 9, a = -Mathf.PI / 2 + 0.35f + k * 0.62f;
                    p += new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                }
                markers[i].Set(t, p, theme);
            }
            if (teamPaths != null) teamPaths.SetVerticesDirty();
        }
    }
}
