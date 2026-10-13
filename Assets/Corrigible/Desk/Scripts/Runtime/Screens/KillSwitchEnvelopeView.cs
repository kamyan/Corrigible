using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Küresel Son Şalter zarfı (C1 §7.1, §8): üstüne gelince 3 px kalkar ve gölge büyür; 3 sn basılı tutulunca ilerleme
    /// halkası (üstten, saat yönü) dolar, %50'den sonra mumda çatlak görünür, alt yazı “Bırakmayın: N sn” olur. Erken
    /// bırakılırsa halka 200 ms'de geri sarılır. Dolunca <see cref="Completed"/> → onay kartı.
    /// </summary>
    public sealed class KillSwitchEnvelopeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IIllusionAware
    {
        [SerializeField] RectTransform visual;
        [SerializeField] Image body, edge, flap, seal;
        [SerializeField] TMP_Text title, sub;
        [SerializeField] Image progress, crack;

        public event Action Completed;

        string subText;
        bool dim, disabled, hover, holding;
        KillSwitchVM last;

        /// <summary>İllüzyon: soluk gövde, sönük kapak ve mühür; “gerekli değil” fişi. Hâlâ basılabilir (PROMPT §7.2).</summary>
        public void SetIllusion(bool on) { if (last != null) Bind(last); }
        float hold, holdSeconds = 3, rewindRate;
        int shownSeconds = -1;
        DeskTween lift;
        Shadow bodyShadow;

        public void Bind(KillSwitchVM k)
        {
            if (k == null) return;
            last = k;
            disabled = k.State == "disabled";
            dim = DeskTheme.Illusion || disabled;
            holdSeconds = k.HoldSeconds > 0 ? k.HoldSeconds : 3;
            DeskUi.Tint(body, DeskTheme.Illusion ? "envelope.illusionBody" : dim ? "envelope.disabledBody" : "envelope.body");
            DeskUi.Tint(edge, DeskTheme.Illusion ? "envelope.illusionEdge" : dim ? "envelope.disabledEdge" : "envelope.bodyEdge");
            DeskSprites.Apply(flap, dim ? "env_flap_dim" : "env_flap");
            DeskSprites.Apply(seal, dim ? "env_seal_dim" : "env_seal");
            DeskUi.SetText(title, DeskLoc.Instance.T(k.Title));
            subText = DeskLoc.Instance.T(k.Sub);
            DeskUi.SetText(sub, subText);
            Reset();
        }

        void Reset()
        {
            holding = false; hold = 0; shownSeconds = -1;
            DeskUi.SetActive(progress, false);
            DeskUi.SetActive(crack, false);
            if (subText != null) DeskUi.SetText(sub, subText);
            Lift();
        }

        public void OnPointerEnter(PointerEventData e) { hover = !disabled; Lift(); }
        public void OnPointerExit(PointerEventData e) { hover = false; Lift(); }

        public void OnPointerDown(PointerEventData e)
        {
            if (disabled || e.button != PointerEventData.InputButton.Left) return;
            holding = true;
            DeskUi.SetActive(progress, true);
        }

        /// <summary>Erken bırakma: halka, doluluğundan bağımsız olarak <c>killSwitchRewind</c> (200 ms) içinde geri sarılır.</summary>
        public void OnPointerUp(PointerEventData e)
        {
            if (!holding) return;
            holding = false;
            float rewind = DeskTheme.Active != null ? DeskTheme.Active.killSwitchRewind : 0.2f;
            rewindRate = hold / Mathf.Max(0.01f, rewind);
        }

        /// <summary>Üstüne gelince 3 px kalkar, gölge (3,4) → (4,8) büyür; ~80 ms ease-out.</summary>
        void Lift()
        {
            if (visual == null) return;
            float y = hover ? 3 : 0;
            if (Application.isPlaying) lift.To(y, DeskTheme.Active != null ? DeskTheme.Active.hoverLift : 0.08f); else lift.Snap(y);
            ApplyLift();
        }

        void ApplyLift()
        {
            DeskUi.SetY(visual, lift.Value);
            if (bodyShadow == null) bodyShadow = body.GetComponent<Shadow>();
            if (bodyShadow != null) bodyShadow.effectDistance = Vector2.Lerp(new Vector2(3, -4), new Vector2(4, -8), lift.Value / 3f);
        }

        void Update()
        {
            if (lift.Tick(Time.unscaledDeltaTime)) ApplyLift();
            if (!holding && hold <= 0) return;
            hold = holding ? hold + Time.unscaledDeltaTime : Mathf.Max(0, hold - Time.unscaledDeltaTime * rewindRate);
            float f = Mathf.Clamp01(hold / holdSeconds);
            progress.fillAmount = f;
            DeskUi.SetActive(crack, f >= 0.5f);
            if (holding)
            {
                int left = Mathf.CeilToInt(holdSeconds - hold);
                if (left != shownSeconds) { shownSeconds = left; DeskUi.SetText(sub, Loc.F("Bırakmayın: {0} sn", Mathf.Max(0, left))); }
            }
            else if (hold <= 0) Reset();
            if (f >= 1 && holding)
            {
                Reset();
                Completed?.Invoke();
            }
        }
    }
}
