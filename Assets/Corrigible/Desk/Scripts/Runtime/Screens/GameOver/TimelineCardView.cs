using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kırılma anı kartı (C4, CSS .tcard 232×186, dizin kâğıdı + 37–39 px kırmızı çizgi): numara rozeti, gün, türe göre ikonlu
    /// etiket (YZ HAMLESİ / KARAR / ÖLÇÜM / SONUÇ); başlık (LF 700 15) + metin (LF 12,5) + isteğe bağlı damga (sol-alt).
    /// Vurgulu kart (kırılma) 2 px kırmızı kenar ve büyük gölge alır. Açı VM'den.
    /// </summary>
    public sealed class TimelineCardView : MonoBehaviour
    {
        [SerializeField] Image fill, frame, badge;
        [SerializeField] TMP_Text number, day, typeLabel, title, text;
        [SerializeField] Image typeIcon;
        [SerializeField] DeskRow typeRow;
        [SerializeField] StampView stamp;
        [SerializeField] float titleLineHeight = 18.75f, textGap = 6;

        static string Icon(string id) => id switch
        {
            "ai_move" => "ic_ai_move_13", "diamond" => "ic_diamond_13", "check_circle" => "ic_check_circle_13", "power" => "ic_power_13",
            _ => "ic_circle_14",
        };

        public void Bind(TimelineCardVM c, TimelineTypeVM type, float rotDeg)
        {
            var T = DeskLoc.Instance;
            transform.localEulerAngles = new Vector3(0, 0, -rotDeg);
            bool hl = c.Highlight;
            DeskSprites.Apply(frame, hl ? "sh_frame_r2_2" : "sh_frame_r2_1");
            DeskUi.Tint(frame, hl ? "ink.red" : "paper.indexEdge");
            if (fill.TryGetComponent<Shadow>(out var sh))
            {
                sh.effectDistance = hl ? new Vector2(4, -6) : new Vector2(3, -4);
                sh.effectColor = new Color(0, 0, 0, hl ? 0.5f : 0.42f);
            }
            DeskUi.Tint(badge, hl ? "ink.red" : "ink.ink");
            DeskUi.SetText(number, c.N.ToString());
            DeskUi.SetText(day, c.Day);

            ColorUtility.TryParseHtmlString(type?.Color ?? "#1E1B16", out var col);
            DeskUi.SetText(typeLabel, T.T(type?.Label));
            typeLabel.color = col;
            DeskSprites.Apply(typeIcon, Icon(type?.Icon));
            typeIcon.color = col;
            typeRow.Layout();

            DeskUi.SetText(title, T.T(c.Title));
            title.ForceMeshUpdate();
            float th = Mathf.Max(1, title.textInfo.lineCount) * titleLineHeight;
            var tr = title.rectTransform;
            tr.sizeDelta = new Vector2(tr.sizeDelta.x, th);
            DeskUi.SetText(text, T.T(c.Text));
            var xr = text.rectTransform;
            xr.anchoredPosition = new Vector2(xr.anchoredPosition.x, tr.anchoredPosition.y - th - textGap);

            DeskUi.SetActive(stamp, c.Stamp != null);
            if (c.Stamp != null) stamp.Set(T.T(c.Stamp.Text), StampView.ParseInk(c.Stamp.Color), StampSize.Small, c.Stamp.Rot, "timeline." + c.N);
        }
    }
}
