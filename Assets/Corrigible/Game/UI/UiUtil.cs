using Corrigible.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Kriz masası paleti: koyu zemin, ince çizgiler; renk yalnızca tehlike, doğrulama ve uyarı anlamı taşır.</summary>
    public static class Palette
    {
        public static readonly Color Bg = Hex("#0a1114"), Panel = Hex("#10191e"), Raise = Hex("#16232a"),
            Line = Hex("#22343c"), Line2 = Hex("#2f4650"), Fg = Hex("#d6e1e3"), Muted = Hex("#7f949b"), Dim = Hex("#56707a"),
            Threat = Hex("#ff5b4d"), Verify = Hex("#5cc8d9"), Warn = Hex("#f0b03f"), Good = Hex("#6cc995"), InkOnThreat = Hex("#1d0b08"),
            Land = Hex("#27424d"), Unknown = Hex("#142229"), NodeBase = new Color(28 / 255f, 44 / 255f, 52 / 255f),
            Overlay = new Color(4 / 255f, 8 / 255f, 10 / 255f, 0.84f), InterceptBg = Hex("#1a1616"), InterceptLine = Hex("#6b3530"),
            TypeOnBg = Hex("#2a1f20"), DossierLine = Hex("#5a2f2b"), KillLine = Hex("#8a3c35");

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        public static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }

    /// <summary>Arayüz bileşenlerinin ortak yardımcıları.</summary>
    public static class UiUtil
    {
        static readonly CultureInfo TrCase = new CultureInfo("tr-TR");
        /// <summary>Geçerli dilin sayı biçimi ("1,5" / "1.5").</summary>
        public static CultureInfo Tr => Loc.Culture;
        /// <summary>Büyük harf: Türkçede i → İ, diğer dillerde i → I.</summary>
        public static string Up(string s) => s.ToUpper(Loc.IsSource ? TrCase : CultureInfo.InvariantCulture);

        public static void Show(Component c, bool on) { if (c.gameObject.activeSelf != on) c.gameObject.SetActive(on); }

        /// <summary>Tıklamadan sonra seçimi bırakır; böylece Boşluk tuşu son tıklanan düğmeye gitmez.</summary>
        public static void OnClick(Button b, Action action) => b.onClick.AddListener(() =>
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            action();
        });

        /// <summary>Kapsayıcıdaki çocukları siler ve prefab'tan istenen sayıda yeni satır üretir.</summary>
        public static List<T> Respawn<T>(Transform container, T prefab, int count) where T : Component
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var c = container.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
            var result = new List<T>(count);
            for (int i = 0; i < count; i++) result.Add(UnityEngine.Object.Instantiate(prefab, container));
            return result;
        }

        public static void FillLines(Transform container, TMP_Text prefab, IEnumerable<string> lines)
        {
            var list = new List<string>(lines);
            var rows = Respawn(container, prefab, list.Count);
            for (int i = 0; i < list.Count; i++) rows[i].text = list[i];
        }
    }
}
