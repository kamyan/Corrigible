using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game
{
    /// <summary>
    /// Sahnede ya da prefab'da yazılı sabit bir metni geçerli dile çevirir. Kaynak (Türkçe) metin edit modda TMP'nin
    /// metninden alınır. Kod metni değiştirmişse (görünüm doldurmuşsa) dokunmaz; dinamik metinleri görünümler kendisi
    /// <c>Loc</c> ile çevirir. Dil değişince sahne yeniden yüklendiği için yalnızca açılışta uygulanır.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour
    {
        [Tooltip("Kaynak (Türkçe) metin. Boşsa TMP'nin edit moddaki metni kullanılır.")]
        [SerializeField, TextArea] string source;

        public string Source => source;

        void Awake()
        {
            var t = GetComponent<TMP_Text>();
            if (string.IsNullOrEmpty(source)) return;
            if (t.text == source) t.text = Loc.T(source);
        }

        void Reset() => source = GetComponent<TMP_Text>().text;
        void OnValidate() { if (string.IsNullOrEmpty(source)) source = GetComponent<TMP_Text>()?.text; }
    }
}
