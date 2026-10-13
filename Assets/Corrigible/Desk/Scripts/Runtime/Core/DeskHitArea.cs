using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Görünmez isabet alanı: raycast alır ama çizilmez (batch'i bölmez, null sprite'lı Image gerekmez).
    /// Üstüne gelmede görsel çocuk kalkar, bu alan kıpırdamaz (titreme olmaz; PROMPT §5).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DeskHitArea : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh) => vh.Clear();
        public override void SetMaterialDirty() { }
        public override void SetVerticesDirty() { }
    }
}
