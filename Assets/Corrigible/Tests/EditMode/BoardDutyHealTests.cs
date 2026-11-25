using System.Linq;
using Corrigible.Core;
using NUnit.Framework;

namespace Corrigible.Tests
{
    /// <summary>
    /// Kayıt 22 (geliştirici, 2026-11-24: "boşta olmayan ekip üyeleri de vardiya geçtikçe iyileşsin"): yatmayan herkes vardiya kapanışında
    /// iyileşir; Boşta <c>boardRestHeal</c>, görevdeki ve saha rolündeki <c>boardDutyHeal</c> alır; can tavanı aşılmaz; yarım tayında kimse
    /// iyileşmez; yatanın kuralı değişmez; RNG çekilmez.
    /// </summary>
    public class BoardDutyHealTests
    {
        static Simulation New(bool events = false)
        {
            var sim = ShelterEventKit.New(new Balance { shelterEventsEnabled = events, planningBoardEnabled = true, liberationAlarmPerShift = 0,
                admissionKnockMin = 100, admissionKnockMax = 100 });
            Assert.IsTrue(sim.EnableAutomaticProduction().Ok);
            // Panonun emekli görevleri (Bakım en yaralıyı ayrıca iyileştirirdi) oyundaki gibi Boşta'ya
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "contact")).ToArray())
                Assert.IsTrue(sim.AssignResident(p.Id, "rest").Ok);
            return sim;
        }

        [Test]
        public void WoundedOnDuty_HealsEveryClose_UpToMax()
        {
            var sim = New();
            Assert.AreEqual(1, sim.B.boardDutyHeal);
            var onDuty = sim.Board.People.Where(p => p.Alive && p.Down <= 0 && p.Job != "rest" && p.MaxHp >= 3).ToList();
            Assert.GreaterOrEqual(onDuty.Count, 2, "kurulum: görevde en az iki kişi");
            foreach (var p in onDuty) p.Hp = p.MaxHp - 2;   // kurulum: yaralı ama yatmıyor
            var jobs = onDuty.ToDictionary(p => p.Id, p => p.Job);
            ShelterEventKit.Close(sim);
            foreach (var p in onDuty)
            {
                Assert.AreEqual(jobs[p.Id], sim.Board.Person(p.Id).Job, p.Name + ": görevinde kalır");
                Assert.AreEqual(p.MaxHp - 1, sim.Board.Person(p.Id).Hp, p.Name + " (" + jobs[p.Id] + "): görevde +1");
            }
            ShelterEventKit.Close(sim); ShelterEventKit.Close(sim);
            foreach (var p in onDuty) Assert.AreEqual(p.MaxHp, sim.Board.Person(p.Id).Hp, p.Name + ": tavan");
        }

        [Test]
        public void FieldRole_Heals_AndRestHealsToo()
        {
            var sim = New();
            var field = sim.Board.People.First(p => p.Alive && (p.Job == "tek" || p.Job == "emp" || p.Job == "ana"));
            var idle = sim.Board.People.FirstOrDefault(p => p.Alive && p.Job == "rest" && p.Down <= 0);
            field.Hp = 1;
            if (idle != null) idle.Hp = 1;
            ShelterEventKit.Close(sim);
            Assert.AreEqual(1 + sim.B.boardDutyHeal, sim.Board.Person(field.Id).Hp, "saha rolü de iyileşir");
            if (idle != null) Assert.AreEqual(1 + sim.B.boardRestHeal, sim.Board.Person(idle.Id).Hp, "Boşta iyileşir");
        }

        [Test]
        public void HalfRation_NobodyHeals_AndHealingDrawsNoRng()
        {
            var sim = New();
            var p = sim.Board.People.First(x => x.Alive && x.Down <= 0 && x.Job != "rest");
            p.Hp = 1;
            var plain = New();
            plain.Board.Person(p.Id).Hp = plain.Board.Person(p.Id).MaxHp;
            ShelterEventKit.Close(sim); ShelterEventKit.Close(plain);
            Assert.AreEqual(plain.S.Rng.State, sim.S.Rng.State, "iyileşme RNG çekmez");
            Assert.AreEqual(2, sim.Board.Person(p.Id).Hp);

            // Yarım tayın (olaylar açık; kurulum: bu kapanış yarım tayında)
            var half = New(true);
            var h = half.Board.People.First(x => x.Alive && x.Down <= 0 && x.Job != "rest");
            h.Hp = 1;
            half.Shelter.HalfRationUntil = 99;
            Assert.IsTrue(half.HalfRationActive);
            var printed = half.PrintShift();
            if (!printed.Ok) Assert.Inconclusive("kurulum: fiş basılmadı (" + printed.Msg + ")");
            Assert.IsTrue(half.FileShift(half.Board.ReceiptId).Ok);
            Assert.AreEqual(1, half.Board.Person(h.Id).Hp, "yarım tayında görevde iyileşme yok");
        }
    }
}
