using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sabit arayüz metni (lejant, başlık, sütun adı): Türkçe kaynak saklanır, dil değişince <see cref="DeskLoc"/> ile çevrilir.
    /// Kaynak metin TMP'nin içindeki metindir (ilk açılışta alınır).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class DeskLocText : MonoBehaviour
    {
        [SerializeField] string source;

        public string Source => source;

        void OnEnable()
        {
            var t = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(source)) source = t.text;
            DeskLoc.Instance.Changed += Apply;
            Apply();
        }

        void OnDisable() => DeskLoc.Instance.Changed -= Apply;

        public void Apply()
        {
            if (!Application.isPlaying) return;   // Editörde kaynak metin görünür kalır
            DeskUi.SetText(GetComponent<TMP_Text>(), DeskLoc.Instance.T(source));
        }
    }
}
