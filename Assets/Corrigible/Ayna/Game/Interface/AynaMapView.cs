using System;
using System.Globalization;
using Corrigible.Ayna.Core;
using Corrigible.Ayna.Presentation;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Corrigible.Ayna.Game
{
    /// <summary>Harita kabı ve 1× işaretler. Bütün görsel nesneler sahnede saklanır; çalışma anında UI kurulmaz.</summary>
    public sealed class AynaMapView : MonoBehaviour
    {
        public static readonly Vector2[] DesignPoints = {
            new Vector2(176.3f, 359), new Vector2(440.7f, 709.5f), new Vector2(818.4f, 316.3f),
            new Vector2(975.7f, 287.8f), new Vector2(1353.4f, 208.3f), new Vector2(1057.6f, 465.9f),
            new Vector2(938, 627.4f), new Vector2(1293.6f, 496), new Vector2(1454.2f, 417.4f),
            new Vector2(1643, 392.3f), new Vector2(1454.2f, 557.7f), new Vector2(1643, 800.7f)
        };
        static readonly string[] mapNames = {
            Loc.N("Kuzey Amerika"), Loc.N("Güney Amerika"), Loc.N("Batı Avrupa"), Loc.N("Doğu Avrupa"),
            Loc.N("Avrasya"), Loc.N("Orta Doğu"), Loc.N("Sahra Altı Afrika"), Loc.N("Güney Asya"),
            Loc.N("Doğu Asya"), Loc.N("Japonya-Kore"), Loc.N("Güneydoğu Asya"), Loc.N("Okyanusya")
        };
        [SerializeField] Button[] buttons = Array.Empty<Button>();
        [SerializeField] TMP_Text[] nameLabels = Array.Empty<TMP_Text>();
        [SerializeField] TMP_Text[] valueLabels = Array.Empty<TMP_Text>();
        [SerializeField] AynaMapGraphic graphic;
        [SerializeField] AynaOrbGraphic[] orbs = Array.Empty<AynaOrbGraphic>();
        [SerializeField] Image coastline;
        readonly Vector2[] markerPoints = new Vector2[12];
        UnityAction[] clickHandlers;
        Button[] listeningButtons;
        AynaScreens lastScreens;
        Vector2 lastSize;
        string selectedRegion;
        bool arranging;
        public event Action<string> RegionSelected;
        public string SelectedRegion => selectedRegion;

        /// <summary>Editörde kurulan 12 butonun sırası AynaWorldData.Regions ile aynı olmalıdır.</summary>
        public void Configure(Button[] regionButtons, TMP_Text[] regionNames, TMP_Text[] regionValues,
            AynaMapGraphic mapGraphic, AynaOrbGraphic[] regionOrbs, Image coast = null)
        {
            RemoveListeners();
            buttons = regionButtons ?? Array.Empty<Button>(); nameLabels = regionNames ?? Array.Empty<TMP_Text>();
            valueLabels = regionValues ?? Array.Empty<TMP_Text>(); graphic = mapGraphic;
            orbs = regionOrbs ?? Array.Empty<AynaOrbGraphic>(); coastline = coast;
            if (isActiveAndEnabled) AddListeners();
            Render(lastScreens, selectedRegion);
        }

        void OnEnable() { AddListeners(); RefreshLayout(); }
        void OnDisable() => RemoveListeners();
        void OnRectTransformDimensionsChange() { if (!arranging) RefreshLayout(); }
        void LateUpdate()
        {
            if (((RectTransform)transform).rect.size != lastSize) RefreshLayout();
        }

        void AddListeners()
        {
            RemoveListeners();
            listeningButtons = (Button[])buttons.Clone(); clickHandlers = new UnityAction[buttons.Length];
            for (int i = 0; i < buttons.Length && i < 12; i++)
            {
                if (buttons[i] == null) continue;
                string id = AynaWorldData.Regions[i].Id;
                UnityAction handler = () => { SetSelection(id); RegionSelected?.Invoke(id); };
                clickHandlers[i] = handler; buttons[i].onClick.AddListener(handler);
            }
        }

        void RemoveListeners()
        {
            if (listeningButtons == null || clickHandlers == null) return;
            for (int i = 0; i < listeningButtons.Length && i < clickHandlers.Length; i++)
                if (listeningButtons[i] != null && clickHandlers[i] != null) listeningButtons[i].onClick.RemoveListener(clickHandlers[i]);
            listeningButtons = null; clickHandlers = null;
        }

        public void Render(AynaScreens screens, string selection)
        {
            lastScreens = screens; selectedRegion = selection;
            for (int i = 0; i < 12; i++)
            {
                string id = AynaWorldData.Regions[i].Id;
                var region = FindRegion(id);
                bool selected = id == selectedRegion;
                if (i < orbs.Length && orbs[i] != null) orbs[i].Bind(region, selected, i);
                if (i < nameLabels.Length && nameLabels[i] != null)
                {
                    var text = nameLabels[i];
                    string name = Loc.T(mapNames[i]).ToUpper(Loc.Lang == Loc.Turkish ? CultureInfo.GetCultureInfo("tr-TR") : CultureInfo.InvariantCulture);
                    text.text = name;
                    text.color = selected ? new Color32(91,228,255,255) : new Color32(172, 219, 233, 255); text.raycastTarget = false;
                    text.richText = true;
                }
                if (i < valueLabels.Length && valueLabels[i] != null)
                {
                    string enemy = region == null || region.Knowledge == Knowledge.Unknown ? "?" : region.Knowledge == Knowledge.Exact ? Loc.Num(region.EnemyMin) : Loc.F("≈{0}–{1}", region.EnemyMin, region.EnemyMax);
                    int own = region?.Own ?? 0, capacity = region?.Capacity ?? AynaWorldData.Regions[i].Capacity;
                    string badge = region?.Knowledge == Knowledge.Exact ? "<color=#60F5B0>✓</color>" : "<color=#BDA6F7>≈</color>";
                    valueLabels[i].text = Loc.F("<mark=#03101DEF><color=#FFD58C>◆{0}</color> <color=#8FB8FF>⬢{1}</color><color=#7B9CAC>/{2}</color> {3}</mark>", own, enemy, capacity, badge);
                    valueLabels[i].richText = true; valueLabels[i].raycastTarget = false;
                }
            }
            RefreshLayout();
        }

        RegionVm FindRegion(string id)
        {
            if (lastScreens?.A2?.Regions == null) return null;
            foreach (var region in lastScreens.A2.Regions) if (region != null && region.Id == id) return region;
            return null;
        }

        public void SetSelection(string id)
        {
            if (selectedRegion == id) return;
            Render(lastScreens, id);
        }

        public void RefreshLayout()
        {
            if (arranging || !(transform is RectTransform root)) return;
            arranging = true;
            try
            {
                var size = root.rect.size; lastSize = size;
                if (size.x < 1 || size.y < 1) return;
                float scale = AynaMapGraphic.CoverScale(size);
                if (coastline != null)
                {
                    // Mevcut Fx kıyı dokusu kullanılabilir; kendi Graphic'imiz de aynı konturu taşır.
                    var coastRect = coastline.rectTransform;
                    coastRect.anchorMin = coastRect.anchorMax = coastRect.pivot = Vector2.one * .5f;
                    coastRect.anchoredPosition = Vector2.zero; coastRect.sizeDelta = new Vector2(1920, 1080) * scale;
                    coastline.raycastTarget = false;
                }
                for (int i = 0; i < 12; i++)
                {
                    float radius = 24 + 2.2f * AynaWorldData.Regions[i].Capacity;
                    var point = AynaMapGraphic.Project(DesignPoints[i], size);
                    // Üst HUD ile en az 8 px açıklık: 21:9'da Avrasya kümesi aşağı itilir.
                    point.y = Mathf.Max(point.y, 102 + radius);
                    point.x = Mathf.Clamp(point.x, radius + 12, size.x - radius - 12);
                    if (AynaWorldData.Regions[i].Id == selectedRegion)
                    {
                        // Sağ y92/w364 kartı seçili düğümü örtmez. İplikler de yeni düğüm yerine bağlanır.
                        float cardLeft = size.x - 380;
                        if (point.x + 86 > cardLeft && point.y > 92 - radius && point.y - radius < 792)
                        {
                            point.x = Mathf.Max(90, cardLeft - 92);
                            // Doğu Asya ile Japonya aynı alana düşerse seçim, açık denizde biraz yukarı ayrılır.
                            if (i == 9) point.y = Mathf.Max(102 + radius, point.y - 106);
                        }
                    }
                    markerPoints[i] = point;
                    if (i >= buttons.Length || buttons[i] == null) continue;
                    var marker = (RectTransform)buttons[i].transform;
                    marker.anchorMin = marker.anchorMax = new Vector2(0, 1); marker.pivot = Vector2.one * .5f;
                    marker.anchoredPosition = new Vector2(point.x, -point.y);
                    // Yalnız küre ve dar çip hitbox'a girer; komşu bölgenin etiketini kaplamaz.
                    marker.sizeDelta = new Vector2(Mathf.Max(72, radius * 2.15f), radius * 2.15f);
                    marker.localScale = Vector3.one;
                    if (i < orbs.Length && orbs[i] != null)
                    {
                        var orb = orbs[i].rectTransform; Center(orb);
                        orb.anchoredPosition = Vector2.zero; orb.sizeDelta = Vector2.one * radius * 3.25f;
                    }
                    bool upper = i == 3;
                    if (i < valueLabels.Length && valueLabels[i] != null)
                    {
                        var value = valueLabels[i].rectTransform; Center(value);
                        value.sizeDelta = new Vector2(170, 24); value.anchoredPosition = new Vector2(0, upper ? radius + 17 : -radius - 13);
                    }
                    if (i < nameLabels.Length && nameLabels[i] != null)
                    {
                        var name = nameLabels[i].rectTransform; Center(name);
                        name.sizeDelta = new Vector2(190, 24); name.anchoredPosition = new Vector2(0, upper ? radius + 41 : -radius - 38);
                    }
                }
                if (graphic != null) graphic.Bind(lastScreens?.A2?.Regions, selectedRegion, markerPoints);
            }
            finally { arranging = false; }
        }

        static void Center(RectTransform rect) => rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
    }
}
