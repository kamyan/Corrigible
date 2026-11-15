using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// KOS aşama 6 masa testlerinin küçük kurulum kiti. Core test assembly'sindeki <c>ShelterEventKit</c> <c>internal</c> olduğu için
    /// gereken yardımcılar burada (kaynak: <c>Assets/Corrigible/Tests/EditMode/ShelterEventKit.cs</c> ve
    /// <c>ShelterEventPlayTests.Quiet</c>). Kurulumlar (ifşa, güven, günlük girdisi) hata ayıklama kurulumudur: komut değildir, kayda girmez.
    /// </summary>
    public static class KosDeskKit
    {
        /// <summary>Olaylar açık kartlı oyun; olağan olaylar susturulmuş, alarm artmaz, kapıya kimse gelmez, açlıktan ayrılma yok.</summary>
        public static Simulation New(uint seed = 79, System.Action<Balance> tweak = null)
        {
            var b = new Balance
            {
                shelterEventsEnabled = true, planningBoardEnabled = true, liberationAlarmPerShift = 0,
                admissionKnockMin = 100, admissionKnockMax = 100, desertStreak = 0,
                eventBigTheftMax = 0, eventDeserterMax = 0, eventLowRationMax = 0, eventTheftMax = 0, eventWoundedMax = 0, eventFeudMax = 0,
                eventMemorialMax = 0, eventBlackoutMax = 0, eventTradeMax = 0, eventCallMax = 0, eventConvoyMax = 0,
            };
            tweak?.Invoke(b);
            var sim = Simulation.NewGame(seed, b);
            Assert.IsNotNull(sim.Board, "kartlı pano");
            Assert.IsNotNull(sim.Admission, "topluluk kabulü");
            return sim;
        }

        /// <summary>Tam kapanış (<c>board_print</c> + <c>board_file</c>).</summary>
        public static void Close(Simulation sim)
        {
            var p = sim.PrintShift();
            Assert.IsTrue(p.Ok, "board_print: " + p.Msg);
            var f = sim.FileShift(sim.Board.ReceiptId);
            Assert.IsTrue(f.Ok, "board_file: " + f.Msg);
        }

        /// <summary>Oyuncunun gördüğü vardiyaya (<c>S.Day + 1</c>) kapanışlarla.</summary>
        public static void CloseTo(Simulation sim, int shift)
        {
            for (int guard = 0; sim.S.Day + 1 < shift && guard < 64; guard++) Close(sim);
            Assert.AreEqual(shift, sim.S.Day + 1, "vardiya");
        }

        /// <summary>Hata ayıklama kurulumu: ifşa günlüğüne bir karar girdisi ve değer (Core'un <c>ChangeExposure</c>'ının yazdığı biçim).</summary>
        public static void Expose(Simulation sim, string cause, int delta, string personId = null)
        {
            personId = personId ?? sim.Board.People.First(p => p.Alive).Id;
            sim.ShelterEvents.Log.Add(new ShelterEventLogEntry { Shift = sim.S.Day, Kind = "ifsa", Option = cause, PersonId = personId, Amount = delta });
            sim.Shelter.Exposure = System.Math.Max(0, sim.Shelter.Exposure + delta);
        }

        /// <summary>Hata ayıklama kurulumu: sığınak eksik sayacı işler (ayrılma dönemi, canlı acil eşiğinin altında, sayaç 1).</summary>
        public static void Understaffed(Simulation sim, int streak = 1)
        {
            foreach (var p in sim.Board.People.Where(p => p.Alive).Skip(sim.B.candidateEmergencyBelow - 1).ToArray()) p.Alive = false;
            sim.Shelter.DesertedSinceFull = true;
            sim.Shelter.UnderstaffedStreak = streak;
            Assert.IsTrue(sim.UnderstaffedCounting, "kurulum: sayaç işliyor");
        }
    }
}
