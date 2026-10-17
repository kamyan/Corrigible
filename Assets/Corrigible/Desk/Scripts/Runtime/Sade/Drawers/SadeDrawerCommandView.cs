using UnityEngine;
using UnityEngine.EventSystems;

namespace Corrigible.Desk
{
    /// <summary>Önceden yazılmış satır/düğme isabet alanı; komut ve harita vurgusunu çekmece sahibine iletir.</summary>
    public sealed class SadeDrawerCommandView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] GameObject hoverVisual;
        SadeDrawerFolderView owner;
        string command, argument, region, team;
        bool preview, pointer;
        public void Bind(SadeDrawerFolderView folder, string cmd, string arg, string regionId = null, string teamId = null, bool hovered = false)
        {
            owner = folder; command = cmd; argument = arg; region = regionId; team = teamId; preview = hovered;
            DeskUi.SetActive(hoverVisual, preview || pointer);
        }
        public void OnPointerClick(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) owner?.Send(command, argument); }
        public void OnPointerEnter(PointerEventData e)
        {
            pointer = true; DeskUi.SetActive(hoverVisual, true);
            if (!string.IsNullOrEmpty(region)) owner?.HoverRegion(region);
            if (!string.IsNullOrEmpty(team)) owner?.HoverTeam(team);
        }
        public void OnPointerExit(PointerEventData e)
        {
            pointer = false; DeskUi.SetActive(hoverVisual, preview);
            if (!string.IsNullOrEmpty(region)) owner?.HoverRegion(null);
            if (!string.IsNullOrEmpty(team)) owner?.HoverTeam(null);
        }
        void OnDisable()
        {
            if (pointer && !string.IsNullOrEmpty(region)) owner?.HoverRegion(null);
            if (pointer && !string.IsNullOrEmpty(team)) owner?.HoverTeam(null);
            pointer = false;
        }
    }
}
