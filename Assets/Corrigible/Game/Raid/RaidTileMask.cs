using System;
using Corrigible.Core;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Karo giydirmenin komşu maskesi (BASKIN-MEKANLARI Katman 3–4). Tahta (<see cref="RaidBoard3D"/>) ve editördeki kit önizlemesi
    /// aynı hesabı kullanır; böylece ikisi ayrışmaz. Bitler aynı türden 4 komşu içindir: N = 1 (y − 1, dünya +Z), E = 2 (x + 1,
    /// +X), S = 4 (y + 1, −Z), W = 8 (x − 1, −X). Tahta dışı "floor" sayılır. Yalnız okur; durumu değiştirmez.
    /// </summary>
    public static class RaidTileMask
    {
        public const int N = 1, E = 2, S = 4, W = 8, All = N | E | S | W;
        /// <summary>Kayar rafın rayı: yok, kuzey–güney (el çarkı batı yüzünde), doğu–batı (el çarkı güney yüzünde).</summary>
        public const int AxisNone = 0, AxisNS = 1, AxisEW = 2;
        /// <summary>Kenarlar maske sırasıyla (N, E, S, W): kare kayması ve bit.</summary>
        public static readonly (int dx, int dy, int bit)[] Sides = { (0, -1, N), (1, 0, E), (0, 1, S), (-1, 0, W) };

        /// <summary>Karonun türü; tahta dışı ya da boş tür "floor".</summary>
        public static string KindAt(Func<int, int, string> kind, int x, int y)
            => x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N ? "floor" : kind(x, y) ?? "floor";

        /// <summary>Aynı türden 4 komşunun maskesi (N/E/S/W bitleri).</summary>
        public static int Mask(Func<int, int, string> kind, int x, int y)
        {
            string self = KindAt(kind, x, y);
            int mask = 0;
            foreach (var (dx, dy, bit) in Sides)
                if (KindAt(kind, x + dx, y + dy) == self) mask |= bit;
            return mask;
        }

        /// <summary>Ayrık duvar: 8 komşusunun hiçbiri duvar değil (salonda kolon gibi okunur).</summary>
        public static bool Isolated(Func<int, int, string> kind, int x, int y)
        {
            if (KindAt(kind, x, y) != "wall") return false;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                    if ((dx != 0 || dy != 0) && KindAt(kind, x + dx, y + dy) == "wall") return false;
            return true;
        }

        /// <summary>
        /// Kayar rafın ray yönü komşu raf karelerinden (geçit karesi de raftır): doğu ya da batı komşusu rafsa doğu–batı, kuzey ya da
        /// güney komşusu rafsa kuzey–güney; komşu raf yoksa <paramref name="fallback"/> (ör. olayın rayından).
        /// </summary>
        public static int ShelfAxis(Func<int, int, bool> shelf, int x, int y, int fallback = AxisNS)
        {
            bool On(int cx, int cy) => cx >= 0 && cy >= 0 && cx < Baskin.N && cy < Baskin.N && shelf(cx, cy);
            if (On(x - 1, y) || On(x + 1, y)) return AxisEW;
            if (On(x, y - 1) || On(x, y + 1)) return AxisNS;
            return fallback;
        }

        /// <summary>Bir karonun giydirmesi: maske, ayrık blok ve raf yönü (<see cref="RaidTileSkin.Apply"/>'ın girdisi).</summary>
        public readonly struct Dress
        {
            public readonly int Mask;
            public readonly bool Isolated;
            public readonly int ShelfAxis;
            public Dress(int mask, bool isolated, int shelfAxis) { Mask = mask; Isolated = isolated; ShelfAxis = shelfAxis; }
            public override string ToString() => $"mask {Mask} isolated {Isolated} shelf {ShelfAxis}";
        }

        /// <summary>
        /// Karonun giydirmesi türden ve raf bayrağından. Duvarda maske yalnız raf olmayan duvar komşularını sayar (köprü yalnız iki
        /// sabit duvar arasına girer); raf duvarında maske 0, raf yönü dolu. Ayrıklık bütün duvarları sayar. Kanalda maske kanal
        /// komşularıdır. Zeminde her şey sıfır.
        /// </summary>
        public static Dress For(Func<int, int, string> kind, Func<int, int, bool> shelf, int x, int y, int fallbackAxis = AxisNS)
        {
            string self = KindAt(kind, x, y);
            if (self == "cable") return new Dress(Mask(kind, x, y), false, AxisNone);
            if (self != "wall") return new Dress(0, false, AxisNone);
            bool Shelf(int cx, int cy) => cx >= 0 && cy >= 0 && cx < Baskin.N && cy < Baskin.N && shelf != null && shelf(cx, cy);
            if (Shelf(x, y)) return new Dress(0, false, ShelfAxis(Shelf, x, y, fallbackAxis));
            string Fixed(int cx, int cy) { string k = KindAt(kind, cx, cy); return k == "wall" && Shelf(cx, cy) ? "shelf" : k; }
            return new Dress(Mask(Fixed, x, y), Isolated(kind, x, y), AxisNone);
        }

        /// <summary>Durumdan giydirme: türler ve raf bayrakları hücrelerden, yedek raf yönü olayın rayından.</summary>
        public static Dress For(Baskin.BState B, int x, int y)
            => For((cx, cy) => Baskin.CellAt(B, cx, cy).T, (cx, cy) => Baskin.CellAt(B, cx, cy).Shelf, x, y, RailAxis(B));

        /// <summary>Arşiv rafı olayının ray yönü (rayın ilk iki karesinden); olay yoksa kuzey–güney.</summary>
        public static int RailAxis(Baskin.BState B)
        {
            var area = B?.Ev != null && B.Ev.Id == "raf" ? B.Ev.Area : null;
            return area != null && area.Count >= 2 && area[0].Y == area[1].Y ? AxisEW : AxisNS;
        }
    }
}
