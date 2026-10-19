using Corrigible.Fx.Orb;
namespace Corrigible.Fx
{
    public readonly struct OrbAppearance
    {
        public readonly OrbKind Kind;public readonly double Level,Radius;
        public OrbAppearance(OrbKind kind,double level,double radius){Kind=kind;Level=level;Radius=radius;}
    }
    /// <summary>Yalnız algı katmanının gösterdiği VM değerlerini kabul eder; gizli dünya durumuna erişmez.</summary>
    public static class OrbStateMapper
    {
        public static OrbAppearance Map(double? shownLevel,FxTrust trust,bool core,bool quarantined)
        {
            if(!shownLevel.HasValue||trust==FxTrust.Unknown)return new OrbAppearance(OrbKind.Unknown,0,27);
            double level=OrbMath.Clamp(shownLevel.Value,0,100);return new OrbAppearance(core?OrbKind.Core:quarantined?OrbKind.Quarantine:OrbKind.Normal,level,30+.16*level+(core?5:0));
        }
    }
}
