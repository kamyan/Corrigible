using System.Collections.Generic;

namespace Corrigible.Desk
{
    /// <summary>
    /// Bina planının üç paftası (Docs/TESIS-BASKINLARI-TASARIMI.md §7.1; tesis aşama 5): görev fişinin iç yanında, fişin açık olduğu
    /// bölgenin sıradaki kanadı. Kurucu <c>FacilityPlanBuilder</c> (Bridge) yalnız tesis odasının planını okur; veride robot, niyet,
    /// robot sayısı ve uyanma karesi yoktur (bunlar baskın tohumundan gelir, plana girmez).
    /// </summary>
    public sealed class FacilityPlanData
    {
        public string RegionId, SectorId;
        /// <summary>Kanadın numarası (1..3) ve bölge cephe mi (cephe dışındaki bilgi fişinde de paftalar görünür, emir verilemez).</summary>
        public int Wing;
        public bool Front;
        /// <summary>Üç pafta: giriş, ara oda, hedef oda (soldan sağa).</summary>
        public readonly List<FacilityPlanSheetData> Sheets = new List<FacilityPlanSheetData>();
    }

    /// <summary>
    /// Bir pafta. Bilinen odada ozalit çizim (<see cref="Tiles"/>, <see cref="Marks"/>), bilinmeyende yalnız dış çerçeve ve
    /// <see cref="Stamp"/> ("PLAN YOK"). Kareler tahtayla aynı yönde: x sütun ('A' + x, batıdan doğuya), y satır (y = 0 kuzey,
    /// tahtanın uzak kenarı; satır adı 8 − y).
    /// </summary>
    public sealed class FacilityPlanSheetData
    {
        public string RoomId;
        /// <summary>Pafta sırası (1 giriş, 2 ara, 3 hedef).</summary>
        public int Index;
        public bool Known;
        /// <summary>Antetin ikinci satırı tesis şartının adı (şart rengiyle); değilse donanımın adı.</summary>
        public bool NoteIsEvent;
        /// <summary>Başlık ("PAFTA 1/3 · GİRİŞ"), antet satırları (oda adı; şart ya da donanım) ve bilinmeyen odanın damgası.</summary>
        public string Title, Caption, Note, Stamp;
        /// <summary>Üzerine gelme etiketi: plan sözle (donanım, nesne sayıları, geçitler, şart).</summary>
        public string HoverTitle, HoverLine;
        /// <summary>Bilinen odada 64 kare (y * 8 + x); bilinmeyende boş.</summary>
        public readonly List<PlanTile> Tiles = new List<PlanTile>();
        /// <summary>Tesis şartının bu kademede görünen parçaları (şartsız odada ya da olaylar kapalıyken boş).</summary>
        public readonly List<PlanMark> Marks = new List<PlanMark>();
        /// <summary>
        /// Tesis aşama 10 (K41): YZ'nin yapısal önlemleri, kırmızı kalemle (Kind "cidar": çift cidarlı duvar karesi; "yayici": koku
        /// yayıcının karesi). Önlem defteri kapalıyken, önlemsiz odada ve bilinmeyen paftada boş.
        /// </summary>
        public readonly List<PlanMark> Counters = new List<PlanMark>();
        /// <summary>Koku yayıcının erimi (Manhattan kare; kırmızı kalemin baklavası). Yayıcı yoksa okunmaz.</summary>
        public int CounterRange;
        /// <summary>
        /// Tesis aşama 11 (K39-6): kalıcı tahtanın planda görünen izleri (Kind <c>FacilityMarks</c> türü: "duvar" yıkık duvar, "kepenk"
        /// açık kepenk, "donanim" kırık donanım, "cool"/"spawner" kırılan nesne, "tel"/"kalas"/"hurda" engel; Hp/Max engelin canı, Item
        /// kırık nesnenin eski işareti). Planın karelerine uygulanmış olanlar (<c>Simulation.FacilityPlanCells</c> ile aynı); mürekkep
        /// katmanında ayırt edici çizimle. Alt bayrak kapalıyken, izsiz odada ve bilinmeyen paftada boş.
        /// </summary>
        public readonly List<PlanMark> Traces = new List<PlanMark>();
        /// <summary>
        /// Tesis aşama 12 (Katar): odanın açık kenar satırları (0 kuzey, 7 güney; Core <c>Baskin.OpenEdgeRows</c>). Çizimde o kenarın dış
        /// çerçevesi yoktur; yerine şart mürekkebiyle kesik çizgi ve dışa bakan tırnaklar. Katar dışında, giriş paftasında, bölge olayları
        /// kapalıyken ve bilinmeyen paftada boş.
        /// </summary>
        public readonly List<int> OpenEdges = new List<int>();
    }

    /// <summary>Karenin zemini: boş zemin, duvar, kablo kanalı, ağır kepenk.</summary>
    public enum PlanGround { Floor, Wall, Cable, Shutter }

    /// <summary>Karedeki sabit nesne (hedef dolap, kopya dolabı, soğutma ünitesi, sivil sunucu, donanım, enkaz, tüp, kaynak kolu; tesis
    /// aşama 11: önceki baskından kalan engel (tel, kalas) ve hurda).</summary>
    public enum PlanItem { None, Target, Copy, Cooler, Civilian, Panel, Antenna, Valve, Debris, Tank, Arm, Barrier, Scrap }

    /// <summary>Planın bir karesi.</summary>
    public sealed class PlanTile
    {
        public int X, Y;
        public PlanGround Ground;
        /// <summary>Kablonun fazı (şebeke şartı; 0 faz yok, 1 A, 2 B).</summary>
        public int Phase;
        /// <summary>Geçit: 0 yok, 1 batı girişi, 2 ileri geçit.</summary>
        public int Door;
        /// <summary>Menfez çiftinin numarası (0 yok).</summary>
        public int Vent;
        public PlanItem Item;
    }

    /// <summary>
    /// Şartın bir parçası (Core <c>PlanPart</c>'ın kopyası): tür (meme, bulut, raf, sizinti, gaz, catlak, durak, kriyo, hol, turnike,
    /// turnike-acik, kol, faz-a, faz-b, suzgec, tup, hortum, yag), kare, turnike kenarının öbür karesi (yoksa −1) ve yön (yoksa −1;
    /// 0 kuzey, 1 doğu, 2 güney, 3 batı).
    /// </summary>
    public sealed class PlanMark
    {
        public string Kind;
        public int X, Y, ToX = -1, ToY = -1, Dir = -1;
        /// <summary>Tesis aşama 11 (iz): engelin kalan ve en yüksek canı; kırık donanım/nesnede planın eski işareti.</summary>
        public int Hp, Max;
        public PlanItem Item;
    }
}
