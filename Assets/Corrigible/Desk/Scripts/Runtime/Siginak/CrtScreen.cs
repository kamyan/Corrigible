using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// CRT ekranı (K5a), telsizin göstergesi: World Space kâğıt yerine tüp — zemin #07100B, tek renk fosfor #A6E8B8, sönük
    /// #5E9C73. TELSİZ · KANAL 07, büyük durum satırı, ekip ve cephe satırları, ipucu, 0,53 s'de yanıp sönen imleç ▮, sağ altta %28 test
    /// deseni. Yaralı ekip satırı ters video (renk değil biçim). Kuruluyor/yayında statik döşenir. Tarama çizgisi ve köşe
    /// kararması sprite katmanlarıdır (shader yok). Durumu bağlayıcı verir.
    /// </summary>
    public sealed class CrtScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text channel, title, sub, team, front, hint, cursor;
        [SerializeField] Image teamBack;
        [SerializeField] RawImage noise;
        [SerializeField] GameObject testPattern;

        CrtScreenData data;
        public CrtScreenData Data => data;

        public void Configure(TMP_Text channelText, TMP_Text titleText, TMP_Text subText, TMP_Text teamText, TMP_Text frontText, TMP_Text hintText,
            TMP_Text cursorText, Image teamInverse, RawImage staticLayer, GameObject pattern)
        {
            channel = channelText; title = titleText; sub = subText; team = teamText; front = frontText; hint = hintText; cursor = cursorText;
            teamBack = teamInverse; noise = staticLayer; testPattern = pattern;
        }

        public void Bind(CrtScreenData d)
        {
            data = d;
            var t = SiginakTheme.Active;
            channel.text = d.Channel;
            title.text = d.Title;
            sub.gameObject.SetActive(!string.IsNullOrEmpty(d.Sub));
            sub.text = d.Sub ?? "";
            team.text = d.TeamLine;
            // Ters video: fosfor zemin, tüp rengi yazı (yaralı ekip)
            teamBack.enabled = d.TeamInverse;
            team.color = t.ColorOf(d.TeamInverse ? "crt.glass" : "crt.phosphorDim");
            front.gameObject.SetActive(!string.IsNullOrEmpty(d.FrontLine));
            front.text = d.FrontLine ?? "";
            hint.gameObject.SetActive(!string.IsNullOrEmpty(d.Hint));
            hint.text = d.Hint ?? "";
            bool busy = d.State == CrtState.Establishing || d.State == CrtState.Live;
            if (noise.gameObject.activeSelf != busy) noise.gameObject.SetActive(busy);
            if (testPattern.activeSelf == busy) testPattern.SetActive(!busy);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)title.transform.parent);
        }

        void Update()
        {
            // İmleç 0,53 s'de yanıp söner; statik her kare rastgele ofset
            bool on = Mathf.Repeat(Time.unscaledTime, 1.06f) < 0.53f;
            if (cursor.enabled != on) cursor.enabled = on;
            if (noise.gameObject.activeSelf)
            {
                var r = noise.uvRect;
                noise.uvRect = new Rect(Random.value, Random.value, r.width, r.height);
            }
        }
    }
}
