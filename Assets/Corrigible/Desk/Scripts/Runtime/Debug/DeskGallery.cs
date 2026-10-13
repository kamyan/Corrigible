using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// <c>DeskUI_Gallery</c> sahnesi (PROMPT §7.5): bileşenleri her durumuyla gösteren bölümler. ←/→ bölüm değiştirir,
    /// F9 dili (TR↔EN) değiştirir. Bölümler C5 dilimleriyle aynı koordinatlardadır; yakalama + karşılaştırma için.
    /// </summary>
    public sealed class DeskGallery : MonoBehaviour
    {
        [SerializeField] GameObject[] sections = System.Array.Empty<GameObject>();
        [SerializeField] int index;
        [SerializeField] TMP_Text pager;

        public int Count => sections.Length;

        void OnEnable() => Show(index);

        public void Show(int i)
        {
            if (sections.Length == 0) return;
            index = (i % sections.Length + sections.Length) % sections.Length;
            for (int k = 0; k < sections.Length; k++) DeskUi.SetActive(sections[k], k == index);
            if (pager != null) DeskUi.SetText(pager, (index + 1) + "/" + sections.Length + " · " + sections[index].name + "   ←/→ bölüm · F9 dil");
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.rightArrowKey.wasPressedThisFrame) Show(index + 1);
            if (kb.leftArrowKey.wasPressedThisFrame) Show(index - 1);
            if (kb.f9Key.wasPressedThisFrame) DeskLoc.Instance.Toggle();
        }
    }
}
