using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// CRT ekranı (K5a), telsizin göstergesi: World Space kâğıt yerine tüp — zemin #07100B, tek renk fosfor #A6E8B8, sönük
    /// #5E9C73. Revizyon 2 (S1): K1'den okunan dört satır — ters video durum (DİNLEMEDE), ekip, kısa cephe / sektör, küçük
    /// YZ ALARMI; ipucu yalnız hat kapalıyken. Yaralı ekip satırı ters video (renk değil biçim). Kuruluyor/yayında statik
    /// döşenir. Tarama çizgisi ve köşe kararması sprite katmanlarıdır (shader yok). Durumu bağlayıcı verir.
    /// </summary>
    public sealed class CrtScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text channel, title, sub, team, front, hint, cursor;
        [SerializeField] Image teamBack;
        [Tooltip("Durum satırının ters video zemini (Revizyon 2: durum hep ters video).")]
        [SerializeField] Image titleBack;
        [Tooltip("Revizyon 2 · S1: satır puntoları (1024 birimlik ekranda; durum, ekip ve cephe K1'den okunur). Satır genişliği aşarsa küçülür.")]
        [SerializeField] float titleSize = 128, teamSize = 94, frontSize = 89, subSize = 55, lineWidth = 832;
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
            if (titleBack != null) { titleBack.enabled = true; title.color = t.ColorOf("crt.glass"); }
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
            if (testPattern != null && testPattern.activeSelf == busy) testPattern.SetActive(!busy);
            Fit(title, titleSize); Fit(team, teamSize); Fit(front, frontSize); Fit(sub, subSize);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)title.transform.parent);
        }

        /// <summary>Satır kendi puntosunda; ekranın genişliğine sığmazsa orantılı küçülür (uzun ekip durumları, EN).</summary>
        void Fit(TMP_Text t, float size)
        {
            if (t == null || size <= 0) return;
            t.fontSize = size;
            if (string.IsNullOrEmpty(t.text)) return;
            float w = t.GetPreferredValues(t.text).x;
            if (w > lineWidth) t.fontSize = Mathf.Floor(size * lineWidth / w);
            // Ters video satırları (durum, ekip) kendi kutusunda: kutunun boyu puntoyla büyür, alt satıra binmez
            var row = t.transform.parent;
            if (row != null && row.GetComponent<HorizontalLayoutGroup>() != null && row.TryGetComponent<LayoutElement>(out var le))
                le.minHeight = le.preferredHeight = Mathf.Ceil(t.fontSize * 1.12f);
        }

        void Update()
        {
            // İmleç 0,53 s'de yanıp söner; statik her kare rastgele ofset
            bool on = Mathf.Repeat(Time.unscaledTime, 1.06f) < 0.53f;
            if (cursor != null && cursor.enabled != on) cursor.enabled = on;
            if (noise.gameObject.activeSelf)
            {
                var r = noise.uvRect;
                noise.uvRect = new Rect(Random.value, Random.value, r.width, r.height);
            }
        }
    }
}
