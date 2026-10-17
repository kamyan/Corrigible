using System;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>F3 çekmecesi. İçerik ve etkileşimler yalnız gösterim modelinden okunur; hazır yuvalar yeniden bağlanır.</summary>
    public sealed class SadeDrawerFolderView : PopupCardBase
    {
        [SerializeField] string drawerId;
        [SerializeField] KeyChipView key;
        [SerializeField] RectTransform folderTab;
        [SerializeField] SadeResearchDrawerView research;
        [SerializeField] SadeNewsDrawerView news;
        [SerializeField] SadeTeamsDrawerView teams;
        [SerializeField] SadeAiLogDrawerView aiLog;
        [SerializeField] SadeGuideDrawerView guide;
        public string DrawerId => drawerId;
        public event Action<string, string> CommandRequested;
        public event Action<string> RegionHovered;
        public event Action<string> TeamHovered;

        public void Bind(JObject drawerData)
        {
            if (drawerData == null) return;
            drawerId = (string)drawerData["id"] ?? drawerId;
            string label = drawerId switch { "news" => "dock.news", "teams" => "dock.teams", "aiLog" => "dock.aiLog", "guide" => "dock.guide", _ => "dock.research" };
            string shortcut = drawerId switch { "news" => "N", "teams" => "T", "aiLog" => "L", "guide" => "H", _ => "R" };
            DeskUi.SetText(title, SadePanelText.T(label));
            if (key != null) key.Set(shortcut);
            if (folderTab != null)
            {
                float tw = title != null ? title.GetPreferredValues(title.text).x : 100;
                float w = Mathf.Max(244, 38 + tw + 8 + 17 + 62);
                folderTab.sizeDelta = new Vector2(w, 38);
                if (key != null) SadeUi.Place(key, 38 + tw + 8, 10.5f, 17, 17);
                if (close != null) SadeUi.Place(close, w - 30, 8, 22, 22);
            }
            research?.Bind(drawerData["research"] as JObject, this);
            news?.Bind(drawerData["news"] as JObject, this);
            teams?.Bind(drawerData["teams"] as JObject, this);
            aiLog?.Bind(drawerData["aiLog"] as JObject, this);
            guide?.Bind(drawerData["guide"] as JObject, this);
        }
        public void Send(string command, string arg) { if (command == "newsFilter") { news?.SetFilter(arg); return; } if (!string.IsNullOrEmpty(command)) CommandRequested?.Invoke(command, arg); }
        public void HoverRegion(string id) => RegionHovered?.Invoke(id);
        public void HoverTeam(string id) => TeamHovered?.Invoke(id);
        void OnDisable() { HoverRegion(null); HoverTeam(null); }
    }

    /// <summary>Görünüm verisinin güvenli okumaları. Modeldeki oranlar yalnız çizim için ölçeklenir.</summary>
    public static class SadeDrawerData
    {
        static JToken Field(JToken j, string key) => j is JObject o ? o[key] : null;
        public static string S(JToken j, string key, string fallback = "") => (string)Field(j, key) ?? fallback;
        public static int I(JToken j, string key, int fallback = 0) => (int?)Field(j, key) ?? fallback;
        public static float N(JToken j, string key, float fallback = 0) => (float?)Field(j, key) ?? fallback;
        public static bool B(JToken j, string key, bool fallback = false) => (bool?)Field(j, key) ?? fallback;
        public static JArray A(JToken j, string key) => Field(j, key) as JArray;
        public static void Text(TMP_Text t, string s) => DeskUi.SetText(t, s ?? "");
        public static string T(string s) => SadePanelText.T(s ?? "");
        public static void Size(RectTransform r, float h) { if (r != null) r.sizeDelta = new Vector2(r.sizeDelta.x, h); }
        public static float Wrapped(TMP_Text t, string text, float width, float min)
        {
            Text(t, text);
            return t == null ? min : Mathf.Max(min, t.GetPreferredValues(text, width, 10000).y);
        }
        public static void Fill(Image image, float value)
        {
            if (image == null) return;
            image.fillAmount = Mathf.Clamp01(value);
        }
        public static string Region(string id) => T("region.short." + id);
    }
}
