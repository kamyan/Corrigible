using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Corrigible.Desk
{
    /// <summary>
    /// map_regions.json'un tipli şeması. Geometri bir sunum kaynağıdır; simülasyondaki bölge kimlikleriyle eşlemeyi
    /// veri kaynağı yapar. JSON'daki halkalar dış sınır, ardından delikler; üçgen indisleri bunların düz birleşimindedir.
    /// </summary>
    [Serializable]
    public sealed class SadeMapData
    {
        public SadeMapMeta Meta;
        public SadeMapRampStop[] Ramp = Array.Empty<SadeMapRampStop>();
        public Dictionary<string, string> RampTextColor = new Dictionary<string, string>();
        public SadeMapFills Fills;
        public SadeMapRegion[] Regions = Array.Empty<SadeMapRegion>();
        public SadeMapOverlay Overlay;
        public Dictionary<string, float[]> LonLatSamples = new Dictionary<string, float[]>();

        public static SadeMapData Parse(string json) => JsonConvert.DeserializeObject<SadeMapData>(json) ?? new SadeMapData();

        public SadeMapRegion FindRegion(string id)
        {
            foreach (var region in Regions) if (region != null && region.Id == id) return region;
            return null;
        }
    }

    [Serializable] public sealed class SadeMapMeta
    {
        public float[] Frame;
        public string Units, Source, Cover, Triangles;
        public SadeMapProjection Projection;
    }

    [Serializable] public sealed class SadeMapProjection
    {
        public string Type, Formula;
        public double CentralMeridianDeg = MapProjection.CentralMeridian;
        public double ScalePxPerRad = MapProjection.ScalePxPerRad;
        public double EquatorY = MapProjection.EquatorY;
        public double[] VisibleLonDeg;
    }

    [Serializable] public sealed class SadeMapRampStop { public float From; public string Hex; }

    [Serializable] public sealed class SadeMapRegion
    {
        public string Id, NameKey, NameTR;
        public float[] Node, Centroid;
        public float[][] Bounds;
        public SadeMapLabel Label;
        public SadeMapPolygon[] Polygons = Array.Empty<SadeMapPolygon>();
    }

    [Serializable] public sealed class SadeMapLabel { public float X, Y; public string Anchor; }
    [Serializable] public sealed class SadeMapPolygon { public float[][][] Rings; public int[] Triangles; }
    [Serializable] public class SadeMapInk { public string Hex; public float Alpha = 1; }
    [Serializable] public class SadeMapStroke : SadeMapInk { public float WidthPx; public float[] DashPx; public string Cap; }
    [Serializable] public sealed class SadeMapTexture { public string Texture, Note; public float TileDesignPx; }
    [Serializable] public sealed class SadeMapFills { public SadeMapTexture Unknown; public SadeMapQuarantine Quarantine; public SadeMapSelection Selected; }
    [Serializable] public sealed class SadeMapQuarantine { public SadeMapInk PaperOverlay; public SadeMapTexture Hatch; public SadeMapStroke Outline; }
    [Serializable] public sealed class SadeMapSelection { public SadeMapInk Fill; public SadeMapStroke[] Outline; }

    [Serializable] public sealed class SadeMapOverlay
    {
        public SadeMapThreads Threads;
        public SadeMapCoreRipple CoreRipple;
        public SadeMapTeamRoute TeamRoute;
        public SadeMapTeam[] Teams;
        public SadeMapAlert Alert;
        public SadeMapCardString CardString;
    }

    [Serializable] public sealed class SadeMapThreads { public SadeMapThreadStyle Style; public SadeMapThread[] Items; public string Rule; }
    [Serializable] public sealed class SadeMapThreadStyle : SadeMapStroke { public SadeMapThreadShadow Shadow; public string Arrowhead; }
    [Serializable] public sealed class SadeMapThreadShadow : SadeMapStroke { public float[] OffsetPx; }
    [Serializable] public sealed class SadeMapThread { public string From, To; public float[][] QuadBezier; public float ArrowAtT, ArrowDeg; public float[] ArrowPos; }
    [Serializable] public sealed class SadeMapCoreRipple { public float[] Center; public SadeMapRing[] Rings; }
    [Serializable] public sealed class SadeMapRing : SadeMapStroke { public float R; }
    [Serializable] public sealed class SadeMapTeamRoute { public string Team, Arrow; public float[][] QuadBezier; public SadeMapStroke Style; }
    [Serializable] public sealed class SadeMapTeam { public string Id, Status; public float[] Pos; public SadeMapEta Eta; }
    [Serializable] public sealed class SadeMapEta { public string Text; public float[] TagTopLeft; }
    [Serializable] public sealed class SadeMapAlert { public string Region; public float[] TopLeft; public float SizePx, RotDeg; }
    [Serializable] public sealed class SadeMapCardString { public float[] From, To; public string Note; }
}
