using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Game.UI
{
    /// <summary>Baskın tahtasında bir kare (RaidTile prefab'ı): zemin, vurgu, dolap, birim ve rozetler.</summary>
    public sealed class RaidTileView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
    {
        [SerializeField] Image ground;
        [SerializeField] TMP_Text groundMark;
        [SerializeField] Image tint;
        [SerializeField] Graphic targetFrame;
        [Header("Dolap")]
        [SerializeField] GameObject obj;
        [SerializeField] Image objBg;
        [SerializeField] TMP_Text objLabel;
        [SerializeField] TMP_Text objHp;
        [Header("Birim")]
        [SerializeField] GameObject unit;
        [SerializeField] ShapeGraphic unitShape;
        [SerializeField] ShapeGraphic unitRing;
        [SerializeField] TMP_Text unitLetter;
        [SerializeField] TMP_Text unitHp;
        [SerializeField] GameObject orderBadge;
        [SerializeField] TMP_Text orderText;
        [SerializeField] GameObject flagBadge;
        [SerializeField] Image flagBg;
        [SerializeField] TMP_Text flagText;
        [Header("Uyanma")]
        [SerializeField] GameObject spawn;

        public int X, Y;
        public event Action<int, int> Clicked, Hovered;

        public void OnPointerClick(PointerEventData e) => Clicked?.Invoke(X, Y);
        public void OnPointerEnter(PointerEventData e) => Hovered?.Invoke(X, Y);

        public Image Ground => ground;
        public TMP_Text GroundMark => groundMark;
        public Image Tint => tint;
        public Graphic TargetFrame => targetFrame;

        public void SetObj(bool on, Color bg, string label, string hp, Color text)
        {
            if (obj.activeSelf != on) obj.SetActive(on);
            if (!on) return;
            objBg.color = bg; objLabel.text = label; objHp.text = hp; objLabel.color = objHp.color = text;
        }

        public void SetUnit(bool on, ShapeGraphic.Kind shape, Color fill, Color ring, float ringWidth, string letter, string hp, Color text, float alpha)
        {
            if (unit.activeSelf != on) unit.SetActive(on);
            if (!on) return;
            unitShape.Shape = shape; unitShape.color = fill;
            unitRing.gameObject.SetActive(ring.a > 0);
            unitRing.color = ring; unitRing.Thickness = ringWidth;
            unitLetter.text = letter; unitHp.text = hp; unitLetter.color = unitHp.color = text;
            var g = unit.GetComponent<CanvasGroup>();
            if (g != null) g.alpha = alpha;
        }

        public void SetBadges(string order, string flag, Color flagColor)
        {
            if (orderBadge.activeSelf != (order != null)) orderBadge.SetActive(order != null);
            if (order != null) orderText.text = order;
            if (flagBadge.activeSelf != (flag != null)) flagBadge.SetActive(flag != null);
            if (flag != null) { flagText.text = flag; flagBg.color = flagColor; }
        }

        public void SetSpawn(bool on) { if (spawn.activeSelf != on) spawn.SetActive(on); }
    }
}
