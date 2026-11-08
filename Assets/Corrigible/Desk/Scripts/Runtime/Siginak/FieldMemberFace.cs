using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Görev fişi ve raporda aynı kişinin kimliği, değerleri ve üç eşya yuvası.</summary>
    public sealed class FieldMemberFace : MonoBehaviour
    {
        [SerializeField] TMP_Text personName, profession, stats, equipment, letter, status;
        [SerializeField] TMP_Text[] slots = Array.Empty<TMP_Text>();
        [SerializeField] Image[] slotFrames = Array.Empty<Image>(), health = Array.Empty<Image>();
        [SerializeField] Sprite occupiedFrame, emptyFrame;
        [SerializeField] Button select;
        [SerializeField] UiHoverTarget hover;
        string personId;
        public event Action<string> Clicked;
        void Awake()
        {
            if (select) { DeskUi.NoNavigation(select); select.onClick.AddListener(() => { if (!string.IsNullOrEmpty(personId)) Clicked?.Invoke(personId); }); }
        }
        public void Bind(string id, string name, string job, string values, string gear, string[] items, int hp, int maxHp, string hint, string role = null, string state = null, bool good = true)
        {
            personId = id;
            if (letter) letter.text = role ?? "";
            if (status) { status.text = state ?? ""; status.color = SiginakTheme.Active.ColorOf(good ? "stamp.teamReady" : "status.threat"); }
            if (personName) personName.text = name ?? "—";
            if (profession) profession.text = job ?? "";
            if (stats) stats.text = values ?? "";
            if (equipment) equipment.text = string.IsNullOrEmpty(gear) ? "—" : gear;
            for (int i = 0; i < slots.Length; i++)
            {
                bool filled = items != null && i < items.Length && !string.IsNullOrEmpty(items[i]);
                slots[i].text = filled ? items[i] : "—";
                if (i < slotFrames.Length && slotFrames[i]) { slotFrames[i].sprite = filled ? occupiedFrame : emptyFrame; slotFrames[i].fillCenter = false; }
            }
            for (int i = 0; i < health.Length; i++) { health[i].gameObject.SetActive(i < maxHp); health[i].color = i < hp ? SiginakTheme.Active.ColorOf("ink") : SiginakTheme.Active.ColorOf("ink3"); }
            if (hover) { hover.Title = name; hover.Line = hint; }
            if (select) select.interactable = !string.IsNullOrEmpty(id);
        }
    }
}
