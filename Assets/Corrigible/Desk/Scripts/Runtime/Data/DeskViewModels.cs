using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masanın ekran kâğıtları: baskın raporu (olay modalının belge kipi) ve oyun sonu raporu. Oda ve sayaçlar simülasyonu
    /// doğrudan okur; burada yalnız tam ekran kâğıtların içeriği durur. Konumlar 1920×1080 tasarım pikseli.
    /// </summary>
    public sealed class DeskViewModel
    {
        /// <summary>Okunmamış baskın raporu; yoksa null. Açıkken arka plan etkileşimsizdir.</summary>
        public EventVM Event;
        /// <summary>Harekât bitti: son rapor tam ekran çizilir.</summary>
        public GameOverVM GameOver;
    }

    [Serializable] public sealed class CommandVM { public string Label, Kind, Command, Key; }
    [Serializable] public sealed class HeaderVM { public string Title, Right; }
    [Serializable] public sealed class StampVM { public string Text, Color; public float FontSize; public float? Rot; }

    // ------------------------------------------------------------------ baskın raporu (olay modalı, belge kipi)
    /// <summary>
    /// Belge: karartma + bölge spotu + kalem halkası, rapor kâğıdı ve kâğıttan bölgeye iplik. Spotun yeri haritadaki bölgenin o
    /// anki yerinden okunur; buradaki merkez yalnız haritasız yedek koordinattır.
    /// </summary>
    [Serializable]
    public sealed class EventVM
    {
        public EventSpotVM Spotlight;
        public EventRingVM MarkerRing;
        public EventStringVM String;
        public EventMemoVM Memo;
        public EventDocVM Doc;
    }

    /// <summary>Belgenin alt bölümü: sonuç satırları (bedel/kazanç dili), uyarı satırı, düğmeler (ilki Enter, ikincisi Esc), yırtık alt kenar.</summary>
    [Serializable]
    public sealed class EventDocVM
    {
        public HeaderVM LinesHeader;
        public List<EventLineVM> Lines = new List<EventLineVM>();
        public string Risk;
        public List<CommandVM> Buttons = new List<CommandVM>();
        public bool Torn;
    }

    [Serializable] public sealed class EventSpotVM { public string Region; public float[] Center; public float HoleDiameter = 148, Ring = 12, ScrimAlpha = 0.78f; }
    [Serializable] public sealed class EventRingVM { public float[] Center; public string Sprite, Color; public float Alpha = 0.92f; }
    [Serializable] public sealed class EventStringVM { public float[] From, To; public string Color; public float Width = 2.2f, ShadowAlpha = 0.35f; }

    [Serializable]
    public sealed class EventMemoVM
    {
        public float[] Rect;
        public HeaderVM Header;
        public List<EventFieldVM> Fields = new List<EventFieldVM>();
        public string Title, Subtitle, Body;
    }

    /// <summary>Alan: anahtar + değer ya da damga (SONUÇ → BAŞARILI / BAŞARISIZ).</summary>
    [Serializable] public sealed class EventFieldVM { public string K, V; public StampVM Stamp; }

    /// <summary>Sonuç satırı: etiket, değer (CP 14; metinse LF 12), renk (good yeşil / threat kırmızı).</summary>
    [Serializable]
    public sealed class EventLineVM
    {
        public string Label, Value;
        public bool ValueIsText;
        public string Status;
    }

    // ------------------------------------------------------------------ oyun sonu
    /// <summary>Son rapor kâğıdı (başlık, büyük damga, KPI'lar, şerit grafik), kırılma anları zaman çizelgesi, alt çubuk.</summary>
    [Serializable]
    public sealed class GameOverVM
    {
        public GameOverReportVM Report;
        public List<KpiVM> Kpis = new List<KpiVM>();
        public ChartVM Chart;
        public TimelineVM Timeline;
        public EndbarVM Endbar;
    }

    [Serializable]
    public sealed class GameOverReportVM
    {
        public ReportHeaderVM Header;
        public string ResultCap, Title, Who, Lead;
        public BigStampVM BigStamp;
    }

    [Serializable] public sealed class ReportHeaderVM { public string Title, Meta, Right; }
    [Serializable] public sealed class BigStampVM { public string Text, Color; public float RotDeg = -7, Opacity = 0.9f; }

    /// <summary>KPI kartı: başlık, büyük değer (+ birim), alta yaslı not.</summary>
    [Serializable] public sealed class KpiVM { public string Cap, Value, Unit, Note; }

    /// <summary>Şerit grafik: çizim alanı eşlemesi, ızgara aralıkları, tek seri, kırılma işaretleri ve uç etiketi (görünüm hesap yapmaz).</summary>
    [Serializable]
    public sealed class ChartVM
    {
        public string Title, Subtitle, Footnote;
        public ChartPlotVM Plot;
        public ChartGridVM Grid;
        public float EventRuleY = 336;
        public ChartSeriesVM Series;
        public List<float> Events = new List<float>();
        public List<string> Legend = new List<string>();
        public List<ChartEndLabelVM> EndLabels = new List<ChartEndLabelVM>();
    }

    /// <summary>x = X0 + (X1 − X0)·gün / DayMax; y = Y0 − (Y0 − Y1)·değer / 100 (ekran pikseli).</summary>
    [Serializable] public sealed class ChartPlotVM { public float X0 = 168, X1 = 1776, DayMax = 125, Y0 = 618, Y1 = 342; }
    [Serializable] public sealed class ChartGridVM { public float MinorDays = 5, MajorDays = 10, MinorPct = 5, MajorPct = 25; }
    [Serializable] public sealed class ChartSeriesVM { public List<float[]> Points = new List<float[]>(); }
    /// <summary>Uç etiketi: serinin son noktası yanında (başlık ve değer).</summary>
    [Serializable] public sealed class ChartEndLabelVM { public string Text; public float Size; public string Color; }

    [Serializable]
    public sealed class TimelineVM
    {
        public string Label, LabelSub;
        public List<float> Rotations = new List<float>();
        public List<TimelineCardVM> Cards = new List<TimelineCardVM>();
        public Dictionary<string, TimelineTypeVM> Types = new Dictionary<string, TimelineTypeVM>();
    }

    [Serializable]
    public sealed class TimelineCardVM
    {
        public int N;
        public string Day, Type, Title, Text;
        public StampVM Stamp;
    }

    [Serializable] public sealed class TimelineTypeVM { public string Label, Icon, Color; }

    [Serializable]
    public sealed class EndbarVM
    {
        public string Info;
        public List<CommandVM> Actions = new List<CommandVM>();
    }
}
