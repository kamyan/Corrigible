using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınak özeti panosu (Revizyon 2 · S3): ayaklı panodaki World Space kâğıt (400 × 594). Başta asıl soru “ERZAK YETER ·
    /// 15 VARDİYA” (3 ve altı kırmızı + AZ KALDI), sonra TOPLULUK · UZMANLAR (diskler) · PLAKA; altında seçili odanın sayfası:
    /// ad + SEÇİLİ, seviye/personel, açıklama, analistin kurşun kalem notu (yalnız öneri), personel adımlayıcısı [−] 2/6 [+],
    /// GELİŞTİRME (en üst seviyede yeşil damga; değilse mürekkep düğme, yetersiz kaynakta açık ve eksik kalem çizik).
    /// Düğmeler devre dışı değilken her zaman tıklanır: yapılamayan eylem komutu dener, gerekçe teleksten basılır.
    /// </summary>
    public sealed class ShelterClipboard : MonoBehaviour
    {
        [Serializable] public sealed class PaperButton
        {
            public Button button;
            public UiHoverTarget hover;
            [Tooltip("Yüz (dolgu + kenar + yazı): üzerinde kalkar, basılıyken iner; gölge altında kalır.")]
            public RectTransform face;
            public Image fill, edge, shadow;
            public TMP_Text label;
            [Tooltip("Mürekkep düğme (GELİŞTİR); değilse çerçeve düğme (adımlayıcı).")]
            public bool ink;
        }

        [SerializeField] TMP_Text title, shift;
        [SerializeField] TMP_Text suppliesLabel, suppliesBig, suppliesLine;
        [SerializeField] RectTransform suppliesStamp;
        [SerializeField] TMP_Text suppliesStampText;
        [SerializeField] TMP_Text community, plates;
        [SerializeField] OperationSlip.Member[] squad = Array.Empty<OperationSlip.Member>();
        [Tooltip("Seçili odanın sayfası: oda değişince kayarak yenilenir.")]
        [SerializeField] RectTransform page;
        [SerializeField] CanvasGroup pageGroup;
        [SerializeField] TMP_Text roomName, chip, roomLine, description, note;
        [SerializeField] TMP_Text staff, staffNote;
        [SerializeField] PaperButton minus, plus, action;
        [SerializeField] GameObject topStamp;
        [SerializeField] TMP_Text actionNote;
        [SerializeField] RectTransform doneStamp;
        [SerializeField] Sprite frameSolid, frameDashed;

        string roomId;
        float swapAt = -1, stampAt = -1, shakeAt = -1;
        PaperButton shaking;
        readonly SlipButtonState[] states = new SlipButtonState[3];

        public event Action ActionClicked, MinusClicked, PlusClicked;

        void Awake()
        {
            action.button.onClick.AddListener(() => ActionClicked?.Invoke());
            minus.button.onClick.AddListener(() => MinusClicked?.Invoke());
            plus.button.onClick.AddListener(() => PlusClicked?.Invoke());
        }

        public void Bind(ShelterBoardData d)
        {
            var t = SiginakTheme.Active;
            title.text = d.Title;
            shift.text = d.Shift;
            suppliesLabel.text = d.SuppliesLabel;
            suppliesBig.text = d.SuppliesBig;
            suppliesBig.color = t.ColorOf(d.SuppliesLow ? "status.threat" : "ink");
            suppliesLine.text = d.SuppliesLine;
            suppliesStamp.gameObject.SetActive(!string.IsNullOrEmpty(d.SuppliesStamp));
            suppliesStampText.text = d.SuppliesStamp ?? "";
            community.text = d.Community;
            plates.text = d.Plates;
            for (int i = 0; i < squad.Length; i++)
            {
                var m = squad[i];
                var data = i < d.Squad.Count ? d.Squad[i] : null;
                m.root.SetActive(data != null);
                if (data == null) continue;
                m.letter.text = data.Letter;
                m.hp.text = data.Hp;
                // Yatan uzmanın diski soluk, yanında kalkmasına kalan gün
                m.disc.color = data.Missing ? t.ColorOf("ui.disabledFill") : Color.white;
                m.hp.color = t.ColorOf(data.Missing ? "ink3" : "ink");
            }

            if (roomId != d.RoomId)
            {
                // Sayfa 0,15 s'de değişir; EMİR UYGULANDI damgası yeni sayfada görünmez
                if (roomId != null) swapAt = Time.unscaledTime;
                roomId = d.RoomId;
                doneStamp.gameObject.SetActive(false);
            }
            roomName.text = d.RoomName;
            chip.text = d.SelectedChip;
            roomLine.text = d.RoomLine;
            description.text = d.Description;
            note.gameObject.SetActive(!string.IsNullOrEmpty(d.Note));
            note.text = d.Note ?? "";
            staff.text = d.Staff;
            staffNote.gameObject.SetActive(!string.IsNullOrEmpty(d.StaffNote));
            staffNote.text = d.StaffNote ?? "";
            Style(minus, d.Minus, 1);
            Style(plus, d.Plus, 2);
            topStamp.SetActive(d.Top);
            action.button.gameObject.SetActive(!d.Top);
            if (!d.Top) Style(action, d.Action, 0);
            actionNote.gameObject.SetActive(!d.Top && !string.IsNullOrEmpty(d.ActionNote));
            actionNote.text = d.ActionNote ?? "";
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)page.parent);
            Update();
        }

        void Style(PaperButton b, ShelterButtonData d, int index)
        {
            var t = SiginakTheme.Active;
            states[index] = d.State;
            b.label.text = d.Label ?? "";
            bool disabled = d.State == SlipButtonState.Disabled;
            // Devre dışı tıklanmaz (üzerine gelince neden); yetersiz kaynak açık kalır, basınca komut denenir ve teleks basar
            b.button.interactable = !disabled;
            b.hover.Title = disabled || d.State == SlipButtonState.Short ? d.Hover : null;
            b.hover.Line = null;
            if (b.ink)
            {
                b.fill.color = t.ColorOf(disabled ? "slip.buttonDisabled" : "ui.inkButton");
                b.label.color = t.ColorOf(disabled ? "slip.buttonDisabledText" : "ui.inkButtonText");
                b.edge.enabled = false;
                b.shadow.enabled = !disabled;
                return;
            }
            // Çerçeve düğme: kenar 2 px #463F34, gölge #8E8166; devre dışında kesik #8A8374
            b.fill.color = t.ColorOf("ui.frameButton");
            b.edge.enabled = true;
            b.edge.sprite = disabled ? frameDashed : frameSolid;
            b.edge.type = disabled ? Image.Type.Tiled : Image.Type.Sliced;
            b.edge.fillCenter = false;
            b.edge.color = t.ColorOf(disabled ? "slip.buttonDisabled" : "ink2");
            b.label.color = t.ColorOf(disabled ? "slip.buttonDisabled" : "ink");
            b.shadow.enabled = !disabled;
        }

        /// <summary>Yapılamadı: düğme ±3 px sarsılır (0,2 s). 0 GELİŞTİR, 1 [−], 2 [+].</summary>
        public void Shake(int which)
        {
            shaking = which == 0 ? action : which == 1 ? minus : plus;
            shakeAt = Time.unscaledTime;
        }

        /// <summary>ONAR / GELİŞTİR uygulandı: sayfada “EMİR UYGULANDI” damgası iner.</summary>
        public void StampDone()
        {
            doneStamp.gameObject.SetActive(true);
            stampAt = Time.unscaledTime;
        }

        void Update()
        {
            var t = SiginakTheme.Active;
            float now = Time.unscaledTime;
            if (swapAt >= 0)
            {
                // Sayfa değişimi (S3 › oda seçimi 0,15 s): yeni sayfa soluktan belirir (yeri düzen grubunun)
                float p = Mathf.Clamp01((now - swapAt) / 0.15f);
                pageGroup.alpha = 0.35f + 0.65f * (1 - (1 - p) * (1 - p));
                if (p >= 1) swapAt = -1;
            }
            if (stampAt >= 0)
            {
                float p = Mathf.Clamp01((now - stampAt) / Mathf.Max(0.01f, t.NumberOf("motion.stampS", 0.1f)));
                doneStamp.localScale = Vector3.one * Mathf.Lerp(1.4f, 1, p);
                if (p >= 1) stampAt = -1;
            }
            Press(action, states[0], t);
            Press(minus, states[1], t);
            Press(plus, states[2], t);
            if (shakeAt >= 0 && shaking != null)
            {
                float p = Mathf.Clamp01((now - shakeAt) / 0.2f);
                var f = shaking.face.anchoredPosition;
                shaking.face.anchoredPosition = new Vector2(Mathf.Sin(p * Mathf.PI * 6) * 3 * (1 - p), f.y);
                if (p >= 1) { shaking.face.anchoredPosition = new Vector2(0, f.y); shakeAt = -1; shaking = null; }
            }
        }

        /// <summary>Üzerinde: zemin açılır, 2 px kalkar, gölge 4 px · basılı: 2 px iner, gölge 1 px (S3 › [−] [+]).</summary>
        void Press(PaperButton b, SlipButtonState state, SiginakTheme t)
        {
            if (!b.button.gameObject.activeInHierarchy) return;
            bool disabled = state == SlipButtonState.Disabled;
            bool pressed = !disabled && b.hover.Pressed, over = !disabled && b.hover.Over && !pressed;
            float lift = pressed ? -2 : over ? 2 : 0, shadow = pressed ? 1 : over ? 4 : 3;
            if (shaking != b) b.face.anchoredPosition = new Vector2(0, lift);
            else b.face.anchoredPosition = new Vector2(b.face.anchoredPosition.x, lift);
            b.shadow.rectTransform.anchoredPosition = new Vector2(0, lift - shadow);
            if (!b.ink && !disabled) b.fill.color = t.ColorOf(over ? "ui.frameButtonHover" : "ui.frameButton");
        }
    }
}
