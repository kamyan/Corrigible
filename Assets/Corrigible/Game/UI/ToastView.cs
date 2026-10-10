using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Ekranın altında kısa süreli bildirimler (Bildirim canvas'ı). Bildirim kutusu CanvasGroup ile solup belirir.</summary>
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] float seconds = 3.6f;
        [SerializeField, Min(0.01f)] float fade = 0.2f;

        [SerializeField] UiTheme theme;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text text;
        [SerializeField] Graphic accent;

        float hideAt = -1;

        void Awake()
        {
            group.alpha = 0;
            group.blocksRaycasts = false;
        }

        public void Show(string msg, bool ok = false)
        {
            if (string.IsNullOrEmpty(msg) || text == null) return;
            text.text = msg;
            accent.color = ok ? theme.verify : theme.warn;
            if (!ok) Audio.Sfx.Play(Audio.Cue.Deny);
            hideAt = Time.unscaledTime + seconds;
        }

        void Update()
        {
            float target = Time.unscaledTime < hideAt ? 1 : 0;
            group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / fade);
        }
    }
}
