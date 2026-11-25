using System.Linq;
using System.Reflection;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using NUnit.Framework;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis hamlelerinin sunum testlerinin ortak kurulumu (aşama 7): kanadın istenen odası (tohum 79: pafta 1 menfezsiz ve kepenksiz,
    /// pafta 2 menfez ve kepenk, pafta 3 büyük sandık), düz zemin (iş kareleri kalır), birim yerleştirme, gövde, ayakta tek makine.
    /// Işınlamalar test kurulumudur; hamleler gerçek yoldan (Core ya da veri kaynağı) verilir. Yalnız public API.
    /// </summary>
    public static class FacilityMovesTestKit
    {
        public static Baskin.Unit Team(Baskin.BState B, string type) => B.Units.First(u => u.Side == 'p' && u.Type == type);

        /// <summary>Kanadın <paramref name="index"/>. odası (önceki odalar betikle geçilir), makineler kalkmış. Aşama 12:
        /// <paramref name="region"/> verilirse harekât oraya komutlarla getirilir (<see cref="FacilityDeskKit.Reach"/>, tohum 79) ve o bölgenin
        /// ilk kanadı açılır; verilmezse ilk bölge (bugünkü).</summary>
        public static (Simulation sim, FacilityRaidSession session, Baskin.BState B) Room(int index, string region = null)
        {
            var sim = region == null ? FacilityDeskKit.New() : FacilityDeskKit.Reach(79, region);
            FacilityDeskKit.StartFirst(sim);
            var session = new FacilityRaidSession(sim, null);
            var B = session.Open();
            for (int guard = 0; guard < 4 && B.Room.Index < index; guard++)
            {
                FacilityDeskKit.PlayRoom(B);
                Assert.IsTrue(session.File(session.Preview(B)).Ok, "oda raporu: " + B.Room.Id);
                B = session.Open();
            }
            Assert.AreEqual(index, B.Room.Index, "oda açılmadı");
            FacilityDeskKit.Calm(B);
            return (sim, session, B);
        }

        /// <summary>Tahtayı düz zemine çevirir (nesne, kanal, sis yok); kapalı kepenk ve sandık nişi duvar kalır; menfez bayrakları
        /// kalır. Kapı kareleri odanın planından gelir (değişmez).</summary>
        public static void Floor(Baskin.BState B)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    if (c.Shutter && c.T == "wall" || c.Crate) continue;
                    // Hedef dolaplar ve ikmal kasası kalır (kaçış kendiliğinden başlamasın)
                    if (c.O != null && Baskin.IsTarget(c.O.K)) { if (c.T != "floor") c.T = "floor"; continue; }
                    c.T = "floor"; c.O = null; c.Smoke = false;
                }
        }

        /// <summary>Ekibi sol alt köşeye toplar (testin kurduğu hatlar açık kalsın): menfez, iş ve kapı kareleri atlanır.</summary>
        public static void Park(Baskin.BState B)
        {
            var gates = B.Room != null ? B.Room.Forward.Concat(B.Room.West).ToList() : new System.Collections.Generic.List<Baskin.Pt>();
            var spots = new System.Collections.Generic.List<Baskin.Pt>();
            for (int y = Baskin.N - 1; y >= 0 && spots.Count < 3; y--)
                for (int x = 0; x < Baskin.N && spots.Count < 3; x++)
                {
                    var c = Baskin.CellAt(B, x, y);
                    if (c.T != "floor" || c.O != null || c.Vent > 0 || gates.Any(g => g.X == x && g.Y == y)) continue;
                    spots.Add(new Baskin.Pt(x, y));
                }
            int i = 0;
            foreach (var u in B.Units.Where(u => u.Side == 'p')) { u.X = spots[i].X; u.Y = spots[i].Y; i++; }
        }

        public static void Put(Baskin.Unit u, int x, int y) { u.X = x; u.Y = y; }

        /// <summary>Uzman düşer (gövde: tahtada yatar).</summary>
        public static void MakeBody(Baskin.Unit u) { u.Dead = true; u.Hp = 0; }

        /// <summary>Kalkmış ilk makine ayağa kalkar ve (x, y)'ye konur.</summary>
        public static Baskin.Unit Machine(Baskin.BState B, int x, int y)
        {
            var e = B.Units.First(v => v.Side == 'e');
            e.Dead = false; e.Out = false; e.Cab = false; e.Frozen = false; e.Hp = e.Max; e.X = x; e.Y = y;
            return e;
        }

        /// <summary>Karenin tahtadaki sekiz komşusundan zemin, birimsiz ve <paramref name="avoid"/> dışında olanların ilki.</summary>
        public static Baskin.Pt FreeNeighbor(Baskin.BState B, Baskin.Pt p, bool orthogonal, params Baskin.Pt[] avoid)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0 || orthogonal && dx != 0 && dy != 0) continue;
                    int x = p.X + dx, y = p.Y + dy;
                    if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) continue;
                    if (Baskin.CellAt(B, x, y).T != "floor" || Baskin.CellAt(B, x, y).O != null || Baskin.UnitAt(B, x, y) != null || avoid.Any(a => a.X == x && a.Y == y)) continue;
                    return new Baskin.Pt(x, y);
                }
            Assert.Fail("boş komşu yok: " + p.X + "," + p.Y);
            return default;
        }

        /// <summary>Üzerine gelme kartı (EditMode'da Update çalışmaz: gecikme dolmuş sayılır).</summary>
        public static RaidInspectorModel Card(RaidDataSource source, Vector2Int tile)
        {
            source.ClearHover();
            source.Hover(tile);
            typeof(RaidDataSource).GetField("hoverVisible", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, true);
            Republish(source);
            Assert.IsNotNull(source.Current.HoverCard);
            return source.Current.HoverCard;
        }

        /// <summary>Görünüm modelini yeniden kurar (ışınlamadan sonra).</summary>
        public static void Republish(RaidDataSource source) =>
            typeof(RaidDataSource).GetMethod("Publish", BindingFlags.Instance | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null).Invoke(source, null);

        public static char Letter(Baskin.Unit u) => Baskin.UnitTypes[u.Type].Short[0];
    }
}
