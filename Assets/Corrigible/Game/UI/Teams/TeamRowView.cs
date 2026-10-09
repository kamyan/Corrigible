using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Saha ekipleri kartında bir satır (TeamRow prefab'ı): işaret, ad, bulunduğu yer ve durum.</summary>
    public sealed class TeamRowView : MonoBehaviour
    {
        [SerializeField] StyledButton button;
        [SerializeField] ShapeGraphic marker;
        [SerializeField] TMP_Text letter;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text status;

        public event Action<string> Clicked;
        string regionId;

        void Awake() => UiUtil.OnClick(button.Button, () => { if (regionId != null) Clicked?.Invoke(regionId); });

        public void Set(TeamState t, UiTheme theme)
        {
            var (mood, text) = TeamStatus.Of(t);
            regionId = t.Dest ?? t.At;
            marker.color = TeamStatus.Color(mood, theme);
            letter.text = t.Name.Substring(0, 1);
            title.text = $"<b>{t.Name}</b> · {GameData.Region(t.Dest != null ? t.From : t.At).Name}";
            status.text = text;
            status.color = TeamStatus.Color(mood, theme);
        }
    }
}
