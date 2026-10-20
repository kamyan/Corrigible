using UnityEngine;
using TMPro;
namespace Corrigible.Ayna.Game
{
    [CreateAssetMenu(menuName="Corrigible/AYNA/Arayüz teması")]
    public sealed class AynaInterfaceTheme : ScriptableObject
    {
        public Color Background=new Color(.008f,.025f,.045f),Panel=new Color(.018f,.045f,.075f,.98f),Text=new Color(.89f,.97f,1),Muted=new Color(.5f,.68f,.76f);
        public Color Chrome=new Color(.36f,.89f,1),Player=new Color(1,.71f,.28f),Enemy=new Color(.24f,.55f,1),Uncertain=new Color(.68f,.51f,1),Danger=new Color(1,.25f,.36f),Verified=new Color(.24f,1,.71f);
        public TMP_FontAsset HeadingFont,BodyFont,MonoFont;
        public float TypewriterDelay=.5f,TypewriterDuration=1.8f,PanelFade=.2f;
    }
}
