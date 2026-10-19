using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>FX kart/ipucu yerleşimi yalnız ekran geometrisini kullanır.</summary>
    public static class FxPanelPlacement
    {
        public static Vector2 Card(Vector2 node,float radius,Vector2 size,Vector2 viewport,Rect obstacle,Rect label)
        {
            float reach=radius*1.15f;float top=Mathf.Clamp(node.y-314,96,Mathf.Max(96,viewport.y-96-size.y));
            float[] candidates={node.x+reach+17,viewport.x-16-size.x,node.x-reach-17-size.x,label.xMin-17-size.x,label.xMax+17,obstacle.xMax+16};
            var orb=new Rect(node.x-reach,node.y-reach,reach*2,reach*2);
            foreach(float left in candidates){var rect=new Rect(left,top,size.x,size.y);if(left>=16&&rect.xMax<=viewport.x-16&&!rect.Overlaps(orb)&&!rect.Overlaps(label)&&(obstacle.width<=0||!rect.Overlaps(obstacle)))return rect.position;}
            // Dar alanlarda karşı köşe denenir; tüm içerik güvenli çerçevede tutulur.
            foreach(float left in new[]{16f,viewport.x-16-size.x})foreach(float y in new[]{96f,viewport.y-96-size.y}){var rect=new Rect(left,y,size.x,size.y);if(!rect.Overlaps(orb)&&!rect.Overlaps(label)&&(obstacle.width<=0||!rect.Overlaps(obstacle)))return rect.position;}
            return new Vector2(Mathf.Clamp(candidates[2],16,Mathf.Max(16,viewport.x-16-size.x)),top);
        }
        public static Vector2 NodeTip(Vector2 node,float radius,Vector2 size,Vector2 viewport,out float pointer)
        {
            float x=node.x-206;if(x<16)x=node.x-26; x=Mathf.Clamp(x,16,Mathf.Max(16,viewport.x-size.x-16));
            float y=Mathf.Clamp(node.y+.95f*radius+36,92,Mathf.Max(92,viewport.y-size.y-80));pointer=Mathf.Clamp(node.x-x,12,size.x-12);return new Vector2(x,y);
        }
        public static Vector2 Breakdown(Rect anchor,Vector2 size,Vector2 viewport)=>new Vector2(Mathf.Clamp(anchor.x,16,Mathf.Max(16,viewport.x-size.x-16)),Mathf.Clamp(anchor.yMax+10,90,Mathf.Max(90,viewport.y-size.y-80)));
    }
}
