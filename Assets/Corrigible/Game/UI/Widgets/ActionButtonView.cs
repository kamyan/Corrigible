using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Başlık + açıklama satırı olan büyük düğme (bölge eylemleri, hediye seçenekleri, rakip kartları).</summary>
    [RequireComponent(typeof(StyledButton))]
    public sealed class ActionButtonView : MonoBehaviour
    {
        [SerializeField] StyledButton styled;
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text sub;

        public StyledButton Styled => styled;
        public TMP_Text Title => title;
        public TMP_Text Sub => sub;

        public void Setup(string titleText, string subText, string eyebrowText = null)
        {
            title.text = titleText;
            sub.text = subText;
            if (eyebrow != null)
            {
                eyebrow.text = eyebrowText ?? "";
                eyebrow.gameObject.SetActive(!string.IsNullOrEmpty(eyebrowText));
            }
        }
    }
}
