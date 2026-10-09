using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Haritada bir saha ekibinin işareti (TeamMarker prefab'ı). Haritaya oranlı anchor ile yerleşir.</summary>
    public sealed class TeamMarkerView : MonoBehaviour
    {
        [SerializeField] ShapeGraphic shape;
        [SerializeField] TMP_Text letter;

        public void Set(TeamState t, Vector2 mapPos, UiTheme theme)
        {
            var rt = (RectTransform)transform;
            var a = new Vector2(mapPos.x / MapSpace.W, 1 - mapPos.y / MapSpace.H);
            rt.anchorMin = rt.anchorMax = a;
            rt.anchoredPosition = Vector2.zero;
            shape.color = TeamStatus.Color(TeamStatus.Of(t).mood, theme);
            letter.text = t.Name.Substring(0, 1);
        }
    }
}
