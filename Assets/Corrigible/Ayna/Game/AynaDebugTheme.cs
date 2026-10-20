using UnityEngine;
namespace Corrigible.Ayna.Game
{
    [CreateAssetMenu(menuName = "Corrigible/AYNA/Debug renkleri")]
    public sealed class AynaDebugTheme : ScriptableObject
    {
        public int RegionFontSize = 18;
        public Color Background = new Color(.02f,.035f,.055f), Panel = new Color(.035f,.06f,.085f,.98f), Text = new Color(.85f,.91f,.96f), Button = new Color(.12f,.24f,.32f);
        public Color Own = new Color(.55f,.42f,.12f), Enemy = new Color(.12f,.3f,.5f), Node = new Color(.1f,.4f,.3f), Rack = new Color(.15f,.16f,.19f), Floor = new Color(.08f,.12f,.17f);
    }
}
