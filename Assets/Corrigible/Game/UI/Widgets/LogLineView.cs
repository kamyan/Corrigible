using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>YZ günlüğünde bir satır (LogLine prefab'ı).</summary>
    public sealed class LogLineView : MonoBehaviour
    {
        [SerializeField] TMP_Text day;
        [SerializeField] TMP_Text text;

        public void Set(int dayNumber, string message, Color color)
        {
            day.text = $"G{dayNumber:000}";
            text.text = message;
            text.color = color;
        }
    }
}
