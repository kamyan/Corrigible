using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>F3 kartlarının kâğıdı, işaretçisi ve dil yenilemesi. Yerleşim ve tek ipucu kuralı sunucudadır.</summary>
    public abstract class SadeTooltipCardBase : PopupCardBase
    {
        [SerializeField] SadePaper paper;
        [SerializeField] RectTransform pointer;
        protected JObject Data;

        public Vector2 Size => Card.sizeDelta;

        protected virtual void OnEnable()
        {
            DeskLoc.Instance.Changed += RefreshData;
            DeskTheme.Changed += RefreshData;
            RefreshData();
        }

        protected virtual void OnDisable()
        {
            DeskLoc.Instance.Changed -= RefreshData;
            DeskTheme.Changed -= RefreshData;
        }

        /// <summary>İşaretçinin ucu: kartın solundan px. Aynalanan/sıkıştırılan ipuçlarında sunucu ayarlar.</summary>
        public void SetPointerX(float x)
        {
            if (pointer != null) pointer.anchoredPosition = new Vector2(Mathf.Clamp(x, 14, Size.x - 14), 0);
        }

        public void Bind(JObject data)
        {
            Data = data;
            RefreshData();
        }

        void RefreshData()
        {
            if (paper != null) paper.Apply();
            if (Data != null) Render(Data);
        }

        protected abstract void Render(JObject data);

        protected static string Text(string source) => SadePanelText.T(source);
        protected static void Label(TMP_Text view, string source) => DeskUi.SetText(view, Text(source));
        protected static float Number(JToken token, float fallback = 0) =>
            token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float) ? token.Value<float>() : fallback;
        protected static bool HasNumber(JToken token) => token != null &&
            (token.Type == JTokenType.Integer || token.Type == JTokenType.Float);

        /// <summary>Metni en az 11 px tutar; satır sınırı yetmiyorsa çağıran kartı büyütür.</summary>
        protected static void FitText(TMP_Text view, float width, float normalSize)
        {
            if (view == null) return;
            view.fontSize = normalSize;
            float natural = SadeUi.Width(view, view.text);
            if (natural > width) view.fontSize = Mathf.Max(11, normalSize * width / natural);
        }
    }
}
