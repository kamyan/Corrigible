using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis ekipmanının sunum testlerinin ortak kurulumu (aşama 9): Core'un test kiti (<c>FacilityKit</c>) internal olduğu için
    /// karşılıkları yalnız public alanlarla (<see cref="Baskin.Unit.Gear"/>, <see cref="Baskin.Unit.Class"/>, Owner, Dir, Turns):
    /// uzmana ekipman, tahtaya aygıt, köpek ve tutsak. Kurulum kayda girmez; eylemler gerçek yoldan (Core ya da veri kaynağı) verilir.
    /// </summary>
    public static class FacilityGearTestKit
    {
        /// <summary>Uzmana ekipman ve kalan hak (emrin yazdığı gibi).</summary>
        public static void Gear(Baskin.Unit u, string kind, int left) { u.Gear = kind; u.GearLeft = left; }

        static Baskin.Unit New(Baskin.BState B, string type, int x, int y)
        {
            var t = Baskin.UnitTypes[type];
            var u = new Baskin.Unit { Id = B.NextId++, Type = type, Side = 'p', X = x, Y = y, Hp = t.Hp, Max = t.Hp };
            B.Units.Add(u);
            return u;
        }

        /// <summary>Aygıt (taret, yem, vinç): yön (<see cref="Baskin.Dirs"/> dizini), kalan çalışma, kuranın kadro sırası; can dengeden.</summary>
        public static Baskin.Unit AddGadget(Baskin.BState B, string type, int x, int y, int dir, int turns, int owner)
        {
            var g = New(B, type, x, y);
            g.Class = Baskin.ClassGadget; g.Dir = dir; g.Turns = turns; g.Owner = owner;
            g.Max = g.Hp = B.Moves?.DeviceHp ?? 2;
            return g;
        }

        /// <summary>Köpek: sahibin kadro sırası; can ve yürüyüş dengeden (<see cref="MoveRules.DogHp"/>, <see cref="MoveRules.DogMove"/>).</summary>
        public static Baskin.Unit AddDog(Baskin.BState B, int owner, int x, int y)
        {
            var d = New(B, "kopek", x, y);
            d.Class = Baskin.ClassDog; d.Owner = owner;
            d.Max = d.Hp = B.Moves?.DogHp ?? 2;
            d.MoveRange = B.Moves?.DogMove ?? 5;
            return d;
        }

        /// <summary>Makine tutsak olur (Core'un kelepçesinin alanları; yakalandığı turda değil: hamleleri açık).</summary>
        public static Baskin.Unit Captive(Baskin.BState B, Baskin.Unit machine, int owner, int turns)
        {
            machine.Side = 'p'; machine.Class = Baskin.ClassCaptive; machine.Owner = owner; machine.Turns = turns;
            machine.Intent = null; machine.Order = 0; machine.Idle = false; machine.Locked = false;
            machine.Dead = false; machine.Out = false; machine.Hp = System.Math.Max(1, machine.Hp);
            return machine;
        }

        /// <summary>Yeni ayakta makine (türün canı, zırhsız, niyetsiz).</summary>
        public static Baskin.Unit AddMachine(Baskin.BState B, string type, int x, int y)
        {
            var e = New(B, type, x, y);
            e.Side = 'e';
            return e;
        }

        /// <summary>Kadro sırasıyla uzman (köpeğin sahibi, kuran).</summary>
        public static Baskin.Unit Specialist(Baskin.BState B, int slot) => B.Units.First(u => Baskin.IsSpecialist(u) && u.Slot == slot);

        /// <summary>Karede boş zemin mi (birim, nesne, kapı, menfez yok).</summary>
        public static bool Open(Baskin.BState B, int x, int y)
        {
            if (x < 0 || y < 0 || x >= Baskin.N || y >= Baskin.N) return false;
            var c = Baskin.CellAt(B, x, y);
            return c.T == "floor" && c.O == null && c.Door == 0 && c.Vent == 0 && Baskin.UnitAt(B, x, y) == null;
        }

        /// <summary>Uzmanı dört dik komşusu da boş zemin olan bir kareye koyar (kapı ve menfezden uzak); yoksa Assert.</summary>
        public static void Clear(Baskin.BState B, Baskin.Unit u)
        {
            for (int y = 1; y < Baskin.N - 1; y++)
                for (int x = 1; x < Baskin.N - 1; x++)
                {
                    if (!Open(B, x, y) && !(u.X == x && u.Y == y)) continue;
                    if (Baskin.Dirs.All(d => Open(B, x + d[0], y + d[1]) || u.X == x + d[0] && u.Y == y + d[1])) { u.X = x; u.Y = y; return; }
                }
            Assert.Fail("açık kare yok");
        }
    }
}
