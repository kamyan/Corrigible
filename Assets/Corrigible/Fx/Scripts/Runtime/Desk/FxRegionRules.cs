using Corrigible.Fx.Orb;
using UnityEngine;
namespace Corrigible.Fx
{
    /// <summary>Yalnız sunum VM'sindeki gösterilen değerler; oyun durumuna erişmez.</summary>
    public static class FxRegionRules
    {
        public static FxTrust Trust(string value)=>value=="verified"?FxTrust.Verified:value=="report"?FxTrust.Report:value=="suspicious"?FxTrust.Suspicious:value=="fake"?FxTrust.Fake:FxTrust.Unknown;
        public static Color AiColor(double level)
        {
            double t=OrbMath.Smooth(.24,.80,OrbMath.Clamp(level,0,100)/100);
            return new Color(OrbMath.Round(OrbMath.Lerp(255,58,t))/255f,OrbMath.Round(OrbMath.Lerp(176,140,t))/255f,OrbMath.Round(OrbMath.Lerp(64,255,t))/255f,1);
        }
        public static Vector2 ChipOffset(double radius,bool eastEurope)=>new Vector2(0,(float)(eastEurope?-.95*radius-26:.95*radius+4));
        public static Vector2 LabelOffset(double radius,bool eastEurope)=>new Vector2(0,(float)(eastEurope?-.95*radius-46:.95*radius+32));
        public static Vector2[] Spread(Vector2 a,Vector2 b,float ra,float rb,float scale=1)
        {
            var d=(b-a).normalized;var p=a+d*(ra+8)/scale;var q=b-d*(rb+8)/scale;
            return new[]{p,(p+q)*.5f+new Vector2(0,10),q};
        }
        public static Vector2[] Route(Vector2 a,Vector2 b,float radius,float scale=1)
        {
            var d=(b-a).normalized;var p=a+d*18/scale;var q=b-d*(radius+10)/scale;
            return new[]{p,(p+q)*.5f+new Vector2(0,-18),q};
        }
    }
}
