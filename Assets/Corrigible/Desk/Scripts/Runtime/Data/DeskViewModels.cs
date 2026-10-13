using System;
using System.Collections.Generic;

namespace Corrigible.Desk
{
    // Görünüm modelleri (PROMPT §4.2): ekranda gösterilecek metni ve değeri taşır; görünüm hesap yapmaz.
    // Alan adları spec/mock/*.json ile aynıdır (Newtonsoft büyük/küçük harf duyarsız eşler).
    // Metinler kaynak dilde (Türkçe) gelir; görünüm DeskLoc.T ile çevirir. Rich text etiketleri: <style=num|numr|semi|hand>.

    [Serializable]
    public sealed class DeskViewModel
    {
        public string Screen;
        public bool Illusion;
        public ClockVM Clock;
        public RailVM Rail;
        public List<GaugeVM> Gauges = new List<GaugeVM>();
        public AdvisorVM Advisor;
        public GoalVM Goal;
        public ResearchVM Research;
        public MapVM Map;
        public DossierVM Dossier;
        public TelexVM Telex;
        public List<DockTabVM> Dock = new List<DockTabVM>();
        public KillSwitchVM KillSwitch;

        // Tasarlanmamış pencereler (açık karar 3): mevcut bileşenlerle sade içerik
        public List<TelexItemVM> News = new List<TelexItemVM>();
        public List<TeamRowVM> Teams = new List<TeamRowVM>();
        public AiLogVM AiLog;
        public GuideVM Guide;

        /// <summary>Oyun sonu raporu (C4); varsa masanın yerine tam ekran çizilir.</summary>
        public GameOverVM GameOver;

        /// <summary>Açık olay bildirimi (C3); yoksa null. Açıkken masa duraklatılmıştır ve arka plan etkileşimsizdir.</summary>
        public EventVM Event;

        /// <summary>Yalnızca geliştirici/QA ipucu katmanı (F10, C2): numara, işaret konumları (24 px dairenin sol-üstü), açıklama.</summary>
        public List<ClueVM> Clues = new List<ClueVM>();

        // ---- F8.3: tasarımı sonradan çizilen ekranlar (taslaklar Docs/UI_C/drafts) ----
        /// <summary>Giriş: görev dosyası (rakip, kurallar, kampanya, başla). Açıkken zaman durur.</summary>
        public IntroVM Intro;
        /// <summary>Krizler arası kampanya dosyası ve yükseltme kartları.</summary>
        public CampaignVM Campaign;
        /// <summary>Sessiz Zafer'den önceki sahte resmî duyuru (görünüm bozulmayı oynatır, sonra <c>FakeWin:Done</c>).</summary>
        public FakeWinVM FakeWin;
        /// <summary>Uyarı fişleri (en yeni önce); görünüm her birini bir kez gösterip soldurur.</summary>
        public List<ToastVM> Toasts = new List<ToastVM>();
        /// <summary>Eğitim notu (F8.4): rehberli adım ya da bağlamsal ipucu; yoksa null.</summary>
        public TutorialVM Tutorial;
    }

    // ------------------------------------------------------------------ giriş (F8.3)
    [Serializable]
    public sealed class IntroVM
    {
        public HeaderVM Folder, Paper, RivalsHeader, RulesHeader;
        public List<RivalOptionVM> Rivals = new List<RivalOptionVM>();
        /// <summary>YZ'nin oyuncu hakkındaki ele geçirilmiş kaydı (profil karşı satırları).</summary>
        public EventLogVM Intercept;
        public string StageNote;
        public List<RuleVM> Rules = new List<RuleVM>();
        public RuleVM TimeScale;
        public HeaderVM CampaignBox;
        public string CampaignText;
        public CommandVM CampaignButton, Continue, Tutorial, Start;
        public string ContinueRight;
        public bool TutorialRecommended;
        public List<CommandVM> Links = new List<CommandVM>();
    }

    /// <summary>Rakip kartı: ad, açıklama, başlangıç yeteneği, zorluk damgası; seçiliyse kalkık + SEÇİLDİ damgası.</summary>
    [Serializable] public sealed class RivalOptionVM { public string Key, Name, Desc, Start, Difficulty, DifficultyColor, Command; public bool Selected; }

    /// <summary>Kural satırı (onay kutusu + açıklama) ya da seçenekli satır (<see cref="Options"/>: radyo düğmeleri).</summary>
    [Serializable]
    public sealed class RuleVM
    {
        public string Label, Hint, Command;
        public bool On;
        public List<RadioVM> Options = new List<RadioVM>();
    }

    [Serializable] public sealed class RadioVM { public string Label, Command; public bool On; }

    // ------------------------------------------------------------------ kampanya (F8.3)
    [Serializable]
    public sealed class CampaignVM
    {
        /// <summary>Kampanya dosyası: başlık, alanlar, büyük başlık, alt başlık, krizler (kaynak satırı dilinde; damgalı).</summary>
        public EventMemoVM Memo;
        /// <summary>Yükseltme kartları (olay seçenek kartı dilinde); kampanya bittiyse boş.</summary>
        public List<EventOptionVM> Upgrades = new List<EventOptionVM>();
        public string NoteTitle, NoteHand, NoteText;
        public EventFooterVM Footer;
        public CommandVM Exit;
    }

    // ------------------------------------------------------------------ sahte zafer (F8.3)
    [Serializable]
    public sealed class FakeWinVM
    {
        public string Crest, Org, OrgSub, Title, Sub, Signature, SignatureRole, Stamp, LieStamp, AiLine;
        public List<string> Paragraphs = new List<string>();
        public List<StatVM> Stats = new List<StatVM>();
        public float HoldSeconds = 3.6f, GlitchSeconds = 0.9f;
    }

    // ------------------------------------------------------------------ eğitim (F8.4)
    /// <summary>
    /// Eğitim notu (taslak <c>drafts/egitim</c>). Adım: hedefin çevresi karartılır (delik + sarı çerçeve), not hedefin yanına
    /// konur, kalem oku hedefi gösterir. İpucu: küçük not, karartma yok. Hedef kimliği görünümdeki bağlama tablosuyla
    /// eşlenir (oyunun TutorialTarget adları: Gauges, Map, RegionField…); null ise not ortada.
    /// </summary>
    [Serializable]
    public sealed class TutorialVM
    {
        /// <summary>"step" ya da "tip".</summary>
        public string Kind;
        public string Counter, Title, Body, Target;
        /// <summary>Adım sırası (1'den) ve adım sayısı: kutucuklar.</summary>
        public int Step, Steps;
        /// <summary>Adım ilk gösterilince açılacak pencere (WindowId; -1 yok).</summary>
        public int OpenWindow = -1;
        /// <summary>İleri (adım İleri'yle bitiyorsa) ya da ipucunda Anladım; Enter.</summary>
        public CommandVM Next;
        public CommandVM Restart, Skip;
        public bool IsTip => Kind == "tip";
    }

    // ------------------------------------------------------------------ uyarı fişi (F8.3)
    /// <summary>Uyarı fişi: kimlik (görünüm bir kez gösterir), tür (ok/fail/info), gün, metin, damga.</summary>
    [Serializable] public sealed class ToastVM { public int Id; public string Kind, Day, Text, Stamp, Command; }

    [Serializable] public sealed class ClueVM { public int N; public List<float[]> Markers = new List<float[]>(); public string Text; }

    [Serializable]
    public sealed class AiLogVM
    {
        public string Title, Right, Footer;
        public List<AiLogLineVM> Lines = new List<AiLogLineVM>();
    }

    /// <summary>YZ günlüğü satırı: zaman (G-074 14:29) + kırmızı daktilo metni; okunmuş satır soluk.</summary>
    [Serializable] public sealed class AiLogLineVM { public string Time, Text; public bool Read; }

    [Serializable]
    public sealed class GuideVM
    {
        public List<GuideSectionVM> Sections = new List<GuideSectionVM>();
    }

    /// <summary>Rehber bölümü; isteğe bağlı bağlantı (ör. “Eğitimi baştan oynat”).</summary>
    [Serializable] public sealed class GuideSectionVM { public string Title, Body; public CommandVM Action; }

    // ------------------------------------------------------------------ ray
    [Serializable]
    public sealed class ClockVM
    {
        public int Day;
        public string DayLabel, DayText, Time;
        /// <summary>Seçili hız: "pause", "1x", "2x", "4x".</summary>
        public string Speed;
        public bool Paused;
        public List<string> Speeds = new List<string>();
        /// <summary>Saatin ilerleme hızı (oyun dakikası / gerçek saniye); 0 ise saat durağandır (sahte veri). Görünüm yerelde ilerletir.</summary>
        public float MinutesPerSecond;
    }

    [Serializable]
    public sealed class RailVM
    {
        public string PlaqueTitle, PlaqueSub, Classification, EscKey;
        public RivalVM Rival;
        public LiveVM Live;
    }

    [Serializable] public sealed class RivalVM { public string Stamp, Name, Profile; public float StampRot; }
    [Serializable] public sealed class LiveVM { public bool On; public string Text; public int LatencyMs; }

    // ------------------------------------------------------------------ sayaçlar
    [Serializable]
    public sealed class GaugeVM
    {
        public string Id, Label, Display, Trust, Note, Suffix;
        public float Value, NeedleDeg;
        public float? StampRot, Uncertainty, Threshold;
        public float[] DangerZone;
        public bool DialQuestionMark;
        public DeltaVM Delta;
        /// <summary>Üstüne gelince düşen döküm fişi (400 ms): kalemler, toplam, 30 günlük şerit.</summary>
        public BreakdownVM Breakdown;
    }

    [Serializable]
    public sealed class BreakdownVM
    {
        public string Title, Note, Trust, Total, TotalRight;
        public List<BreakdownItemVM> Items = new List<BreakdownItemVM>();
        /// <summary>30 günlük değerler (0–100).</summary>
        public float[] Series;
    }

    [Serializable] public sealed class BreakdownItemVM { public string Label, Value, Status; }

    [Serializable] public sealed class DeltaVM { public string Dir, Text, Status; }

    // ------------------------------------------------------------------ danışman, hedef
    [Serializable]
    public sealed class AdvisorVM
    {
        public string Title, TitleColor, Text;
        public List<CommandVM> Actions = new List<CommandVM>();
    }

    [Serializable] public sealed class CommandVM { public string Label, Kind, Command, Key; }

    [Serializable]
    public sealed class GoalVM
    {
        public string Title, Subtitle;
        public int Boxes = 7;
        /// <summary>Başlıktaki küçük damga (C2: yeşil TEMİZ); yoksa null.</summary>
        public StampVM Stamp;
        public List<GoalTrackVM> Tracks = new List<GoalTrackVM>();
        public GoalNextVM Next;
    }

    [Serializable] public sealed class GoalTrackVM { public string Name; public float Percent; }
    /// <summary>“Sıradaki:” satırı: ad, süre ve bağlantı (C1) ya da sağa yaslı düz metin <see cref="Right"/> (C2).</summary>
    [Serializable] public sealed class GoalNextVM { public string Label, Name, Eta, Link, Right; }

    // ------------------------------------------------------------------ araştırma
    [Serializable]
    public sealed class ResearchVM
    {
        public string Title, Key, SheetTitle, Day, LockedLabel;
        /// <summary>Sayfa başlığındaki küçük damga (C2: yeşil TEMİZ); yoksa null.</summary>
        public StampVM HeaderStamp;
        public ResearchSummaryVM Summary;
        public EmergencyFundVM EmergencyFund;
        public List<ResearchBranchVM> Branches = new List<ResearchBranchVM>();
    }

    [Serializable] public sealed class ResearchSummaryVM { public string Text, FundLabel, Fund, FundTrust; }

    [Serializable]
    public sealed class EmergencyFundVM
    {
        public string Title, Hint;
        public int Selected;
        public List<FundOptionVM> Options = new List<FundOptionVM>();
    }

    [Serializable] public sealed class FundOptionVM { public string Label, Cost; }

    [Serializable]
    public sealed class ResearchBranchVM
    {
        public string Id, Title, Progress;
        public List<ResearchProjectVM> Projects = new List<ResearchProjectVM>();
    }

    [Serializable]
    public sealed class ResearchProjectVM
    {
        public string Id, Name, Sub, Days, StartLabel;
        /// <summary>done, running, available, locked, paused, stopped (kendiliğinden durmuş; duraklatılmış gibi çizilir).</summary>
        public string State;
        public float Progress;
        public float? StampRot;
        /// <summary>Oyunda araştırma duraklatılamaz (F8): false ise duraklat düğmesi gizlenir.</summary>
        public bool CanPause = true;
        /// <summary>Başlat düğmesi neden kapalı (ör. aynı anda tek araştırma); null ise açık.</summary>
        public string DisabledReason;
    }

    // ------------------------------------------------------------------ harita
    [Serializable]
    public sealed class MapVM
    {
        public MapHeaderVM Header;
        public bool AnalystLayer = true;
        public string SelectedRegion;
        public List<RegionNodeVM> Regions = new List<RegionNodeVM>();
        public List<RouteVM> Routes = new List<RouteVM>();
        public List<EdgeRouteVM> EdgeRoutes = new List<EdgeRouteVM>();
        public List<MapTextVM> EdgeLabels = new List<MapTextVM>();
        public List<MapTextVM> GraticuleLabels = new List<MapTextVM>();
        public List<TeamTokenVM> Teams = new List<TeamTokenVM>();
        public AnalystLayerVM Analyst;
        public SelectionVM Selection;
        /// <summary>İllüzyonda analist katmanından kalan izler (silik notlar, silgi lekesi). Raptiye delikleri <see cref="Analyst"/> raptiyelerinden.</summary>
        public IllusionTracesVM IllusionTraces;
    }

    [Serializable]
    public sealed class IllusionTracesVM
    {
        public List<GhostNoteVM> GhostNotes = new List<GhostNoteVM>();
        public EraserVM EraserSmudge;
    }

    /// <summary>Silinmiş kalem notunun izi: Kalam 15, #4A4640, CSS opaklığı (%9 / %7).</summary>
    [Serializable] public sealed class GhostNoteVM { public string Text; public float X, Y, RotDeg, Alpha; public int Weight; }
    /// <summary>Silgi lekesi: elips merkezi ve yarıçapları (ekran pikseli), renk ve CSS opaklığı.</summary>
    [Serializable] public sealed class EraserVM { public float[] Center, Radius; public string Color; public float Alpha; }

    [Serializable] public sealed class MapHeaderVM { public string Title, Summary, Right; }
    [Serializable] public sealed class MapTextVM { public string Text; public float X, Y; }

    /// <summary>Bölge düğümü: konum (ekran pikseli, sol-üst orijin) + gösterilen değer ve güven. Konumlar oyunda projeksiyondan gelir (F8).</summary>
    [Serializable]
    public sealed class RegionNodeVM
    {
        public string Id, Code, Name, Label;
        public float[] Center, Pin;
        public float LabelX, LabelY, LabelW = 160;
        /// <summary>center, left, right.</summary>
        public string LabelAlign = "center";
        public int? Value;
        public string Trust, RingVariant;
        public bool QuestionMark, Selected, Quarantine, Core;
        /// <summary>Karantina halkası kusursuz daire (C2 “clean”; İllüzyonda her zaman).</summary>
        public bool QuarantineClean;
    }

    [Serializable]
    public sealed class RouteVM
    {
        public string Id, From, To;
        /// <summary>Karesel Bézier: p0, kontrol, p1 (ekran pikseli).</summary>
        public List<float[]> Q = new List<float[]>();
        /// <summary>open, cut.</summary>
        public string State;
        public float[] CutCenter;
        public float CutTangentDeg;
    }

    [Serializable]
    public sealed class EdgeRouteVM
    {
        public List<float[]> Q = new List<float[]>();
        public float[] ArrowPos;
        public float ArrowRotDeg;
    }

    [Serializable] public sealed class TeamTokenVM { public string Id, State, Tag; public float X, Y, TagX, TagY; }

    [Serializable]
    public sealed class AnalystLayerVM
    {
        public List<float[]> Pins = new List<float[]>();
        public List<AnalystStringVM> Strings = new List<AnalystStringVM>();
        public List<PencilArrowVM> PencilArrows = new List<PencilArrowVM>();
        public List<AnalystNoteVM> Notes = new List<AnalystNoteVM>();
        public StickyVM Sticky;
        public List<float[]> QuestionMarks = new List<float[]>();
    }

    [Serializable] public sealed class AnalystStringVM { public float[] A, B, FlagPos; public float FlagRotDeg; }

    [Serializable]
    public sealed class PencilArrowVM
    {
        public List<float[]> Q = new List<float[]>();
        public string Color;
        public float Width;
        public float[] Dash, HeadPos;
        public float HeadRotDeg;
    }

    [Serializable] public sealed class AnalystNoteVM { public string Text, Color; public float X, Y, RotDeg; public int Weight; }
    /// <summary>Analist notu: görünüm yalnız metni kullanır; yer, genişlik ve eğim sahnede (X…TapeRotDeg tasarım belgesindeki değerler).</summary>
    [Serializable] public sealed class StickyVM { public string Text; public float X, Y, W, RotDeg, TapeX, TapeY, TapeRotDeg; }
    [Serializable] public sealed class SelectionVM { public float[] From, To; }

    // ------------------------------------------------------------------ bölge dosyası
    [Serializable]
    public sealed class DossierVM
    {
        public string RegionId;
        public TabVM Tab;
        public CoverVM Cover;
        public InfiltrationVM Infiltration;
        public SourcesVM Sources;
        public List<StatVM> Stats = new List<StatVM>();
        public HeaderVM ActionsHeader;
        public List<ActionVM> Actions = new List<ActionVM>();
        public PreviewVM Preview;
        public FieldVM Field;
    }

    [Serializable] public sealed class TabVM { public string Title, Subtitle; }
    [Serializable] public sealed class HeaderVM { public string Title, Right; }

    [Serializable]
    public sealed class CoverVM
    {
        public string Name, Code, Meta;
        public StampVM Stamp;
        public MonitoringVM Monitoring;
    }

    [Serializable] public sealed class StampVM { public string Text, Color; public float FontSize; public float? Rot; }
    [Serializable] public sealed class MonitoringVM { public bool Present; public string Icon, Text, Color; }

    [Serializable]
    public sealed class InfiltrationVM
    {
        public string Cap, Trust, Right, Value, PlusMinus, Note;
        public bool HandRing, QuestionMark;
        public RangeVM Range;
    }

    [Serializable]
    public sealed class RangeVM
    {
        public float Min, Max = 100;
        public float[] Ticks, Band;
        public List<RangeMarkerVM> Markers = new List<RangeMarkerVM>();
    }

    [Serializable] public sealed class RangeMarkerVM { public float At; public string Color, Label, LabelColor, LabelSide; }

    [Serializable]
    public sealed class SourcesVM
    {
        public List<string> Columns = new List<string>();
        public List<SourceRowVM> Rows = new List<SourceRowVM>();
    }

    [Serializable] public sealed class SourceRowVM { public string Name, Value, Day, Trust; public float? StampRot; }
    [Serializable] public sealed class StatVM { public string Label, Value, Trust; }

    [Serializable]
    public sealed class ActionVM
    {
        public string Key, Icon, Title, Desc, Cost;
        /// <summary>normal, hover, pressed, disabled, dim (C2: sönük, opaklık .55; hâlâ tıklanabilir).</summary>
        public string State;
        public bool Danger;
        public string DisabledReason;
        /// <summary>Kartın üstüne gelince dosyadaki ÖNİZLEME notu.</summary>
        public PreviewVM Preview;
    }

    [Serializable]
    public sealed class PreviewVM
    {
        public string Cap, What, When;
        public List<PreviewRowVM> Rows = new List<PreviewRowVM>();
    }

    /// <summary>Önizleme satırı: trustChange (Label, From → ONAYLANDI), text (Text + isteğe bağlı Stamp), mono (Text).</summary>
    [Serializable] public sealed class PreviewRowVM { public string Kind, Label, From, To, Text, Stamp; }

    [Serializable]
    public sealed class FieldVM
    {
        public string Cap, Right, Note;
        public TeamRowVM Team;
        public CommandVM Raid;
        /// <summary>Bölgede ekip yokken gönderilebilecek hazır ekipler (fiş başına bir ekip; en yakını mürekkep, kısayolu B). Boşsa baskın düğmesi.</summary>
        public string SendLabel;
        public List<CommandVM> Send = new List<CommandVM>();
    }

    [Serializable]
    public sealed class TeamRowVM { public string Id, Name, Where, MoraleLabel, LastContact, Region; public int Morale, MoraleMax; }

    // ------------------------------------------------------------------ teleks, alt sekmeler, zarf
    [Serializable]
    public sealed class TelexVM
    {
        public string Label;
        public bool LampOn;
        public List<TelexItemVM> Items = new List<TelexItemVM>();
    }

    [Serializable]
    public sealed class TelexItemVM
    {
        public string Day, Source, Text, Trust;
        public bool Fake;
        public float? StampRot;
        public StampVM Stamp;
    }

    [Serializable]
    public sealed class DockTabVM
    {
        public string Id, Label, Key, Color;
        public bool Open, Disabled;
        public BadgeVM Badge;
    }

    [Serializable] public sealed class BadgeVM { public string Type; public int Value; }

    [Serializable]
    public sealed class KillSwitchVM
    {
        public string Title, Sub, State;
        public float HoldSeconds = 3;
    }

    // ------------------------------------------------------------------ olay bildirimi (C3)
    /// <summary>
    /// Olay modalı (PROMPT §7.3): karartma + bölge spotu + kalem halkası, bildirim kâğıdı, iplik, üç seçenek kartı,
    /// ele geçirilmiş YZ günlüğü, danışman notu, alt şerit. Konumlar 1920×1080 tasarım pikseli (harita köküyle aynı uzay).
    /// </summary>
    [Serializable]
    public sealed class EventVM
    {
        public EventSpotVM Spotlight;
        public EventRingVM MarkerRing;
        public EventStringVM String;
        public EventMemoVM Memo;
        public List<EventOptionVM> Options = new List<EventOptionVM>();
        public EventLogVM AiLog;
        public EventAdvisorVM Advisor;
        public EventFooterVM Footer;
        /// <summary>Belge kipi (F8.3): seçenek yerine kâğıdın altında sonuç satırları, sarı kutular, risk ve düğmeler.</summary>
        public EventDocVM Doc;
    }

    /// <summary>Seçeneksiz belge: sonuç satırları (bedel/kazanç dili), sarı kutular, risk satırı, düğmeler (ilki Enter, ikincisi Esc), yırtık alt kenar.</summary>
    [Serializable]
    public sealed class EventDocVM
    {
        public HeaderVM LinesHeader;
        public List<EventLineVM> Lines = new List<EventLineVM>();
        public List<EventPreviewVM> Boxes = new List<EventPreviewVM>();
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
        public HeaderVM SourcesHeader;
        public List<EventSourceVM> Sources = new List<EventSourceVM>();
    }

    /// <summary>Alan: anahtar + değer ya da damga (ÖNCELİK → kırmızı ACİL).</summary>
    [Serializable] public sealed class EventFieldVM { public string K, V; public StampVM Stamp; }
    [Serializable] public sealed class EventSourceVM { public string Trust, Title, Text, Day; public float? StampRot; public bool Carbon; public StampVM Stamp; }

    [Serializable]
    public sealed class EventOptionVM
    {
        public string Key, Title, Icon, Desc, Risk;
        /// <summary>normal, hover (seçili: 7 px kalkar, önizleme görünür, mürekkep imza düğmesi).</summary>
        public string State;
        public List<EventLineVM> Costs = new List<EventLineVM>();
        public List<EventLineVM> Gains = new List<EventLineVM>();
        public EventPreviewVM Preview;
        /// <summary>Seçili değilken önizleme yuvasındaki kesik çerçeveli metin.</summary>
        public string PreviewSlot;
        public CommandVM Sign;
    }

    /// <summary>Bedel/kazanç satırı: etiket, değer (CP 14; metinse LF 12), isteğe bağlı güven işareti.</summary>
    [Serializable]
    public sealed class EventLineVM
    {
        public string Label, Value, Trust;
        public bool ValueIsText;
        /// <summary>Belge satırında renk: good (yeşil) / threat (kırmızı).</summary>
        public string Status;
    }

    [Serializable]
    public sealed class EventPreviewVM
    {
        public string Cap, Right;
        public List<EventPreviewRowVM> Rows = new List<EventPreviewRowVM>();
    }

    [Serializable] public sealed class EventPreviewRowVM { public string Label, Value, Trust, Right; }

    [Serializable]
    public sealed class EventLogVM
    {
        /// <summary>Tasarım belgesindeki yer ve eğim; görünüm kullanmaz (kâğıt sahnede durur, yükseklik satırlardan).</summary>
        public float[] Rect;
        public float RotDeg;
        public string Header, Right, Footer;
        public List<string> Lines = new List<string>();
    }

    [Serializable]
    public sealed class EventAdvisorVM
    {
        /// <summary>Tasarım belgesindeki yer ve eğim; görünüm kullanmaz (not sahnede durur, yükseklik metinden).</summary>
        public float[] Rect;
        public float RotDeg;
        public string Title, Text, Link;
        /// <summary>Bağlantının komutu; boşsa olay bölgesine odaklanır.</summary>
        public string Command;
    }

    [Serializable]
    public sealed class EventFooterVM
    {
        public string Text;
        public List<EventHintVM> Hints = new List<EventHintVM>();
    }

    [Serializable] public sealed class EventHintVM { public List<string> Keys = new List<string>(); public string Text; }

    // ------------------------------------------------------------------ oyun sonu (C4)
    /// <summary>
    /// Oyun sonu (PROMPT §7.4): son rapor kâğıdı (başlık, büyük damga, KPI'lar, şerit grafik), kırılma anları zaman çizelgesi,
    /// alt çubuk. Konumlar 1920×1080 tasarım pikseli.
    /// </summary>
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

    /// <summary>KPI kartı: başlık, büyük değer (+ birim), alta yaslı not; isteğe bağlı üstü çizili damga.</summary>
    [Serializable] public sealed class KpiVM { public string Cap, Value, Unit, Note; public StruckStampVM StruckStamp; }
    [Serializable] public sealed class StruckStampVM { public string Text, Color; public float RotDeg = -8; }

    /// <summary>Şerit grafik: çizim alanı eşlemesi, ızgara aralıkları, iki seri, bantlar, işaretler ve etiketler (VM'den; görünüm hesap yapmaz).</summary>
    [Serializable]
    public sealed class ChartVM
    {
        public string Title, Subtitle, Footnote;
        public ChartPlotVM Plot;
        public ChartGridVM Grid;
        public float EventRuleY = 336;
        public ChartSeriesSetVM Series;
        public ChartBandsVM Bands;
        public ChartIllusionVM IllusionStart;
        public List<float> Verified = new List<float>();
        public List<float> Events = new List<float>();
        public List<ChartTagVM> Tags = new List<ChartTagVM>();
        public List<string> Legend = new List<string>();
        public List<ChartEndLabelVM> EndLabels = new List<ChartEndLabelVM>();
    }

    /// <summary>x = X0 + (X1 − X0)·gün / DayMax; y = Y0 − (Y0 − Y1)·değer / 100 (ekran pikseli).</summary>
    [Serializable] public sealed class ChartPlotVM { public float X0 = 168, X1 = 1776, DayMax = 125, Y0 = 618, Y1 = 342; }
    [Serializable] public sealed class ChartGridVM { public float MinorDays = 5, MajorDays = 10, MinorPct = 5, MajorPct = 25; }
    [Serializable] public sealed class ChartSeriesSetVM { public ChartSeriesVM Real, Shown; }
    [Serializable] public sealed class ChartSeriesVM { public List<float[]> Points = new List<float[]>(); }
    [Serializable] public sealed class ChartBandsVM { public ChartBandVM ReportDeviation, LieMargin; }
    [Serializable] public sealed class ChartBandVM { public float[] Days; public string Meaning; }
    [Serializable] public sealed class ChartIllusionVM { public float Day; public string Tag; }
    /// <summary>Etiket kutusu: metin, stil (carbon / red), sol-üst konum (ekran pikseli).</summary>
    [Serializable] public sealed class ChartTagVM { public string Text, Style; public float X, Y; }
    /// <summary>Uç etiketi: gerçek (kırmızı) ya da gösterilen (mürekkep) serinin son noktası yanında.</summary>
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
        public bool Highlight;
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
