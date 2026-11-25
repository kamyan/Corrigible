using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    [Flags]
    public enum FacilityTitle { None = 0, Marksman = 1, Carrier = 2, Ram = 4, Handler = 8, Builder = 16, Thrower = 32 }

    /// <summary>Unvan sırası aynı baskında aşılan eşiklerin önceliğini de belirler.</summary>
    public static class FacilityTitles
    {
        public static readonly IReadOnlyList<FacilityTitle> Order = Array.AsReadOnly(new[]
        {
            FacilityTitle.Marksman, FacilityTitle.Carrier, FacilityTitle.Ram, FacilityTitle.Handler, FacilityTitle.Builder, FacilityTitle.Thrower,
        });
        internal const FacilityTitle RoomWorks = FacilityTitle.Marksman | FacilityTitle.Ram | FacilityTitle.Builder | FacilityTitle.Thrower;
        public static bool Has(FacilityTitle mask, FacilityTitle title) => title != FacilityTitle.None && (mask & title) == title;

        public static string Id(FacilityTitle title) => title switch
        {
            FacilityTitle.Marksman => "nisanci", FacilityTitle.Carrier => "sirtlayan", FacilityTitle.Ram => "kocbasi",
            FacilityTitle.Handler => "kopekci", FacilityTitle.Builder => "kurucu", FacilityTitle.Thrower => "atici", _ => "",
        };

        public static string Name(FacilityTitle title) => title switch
        {
            FacilityTitle.Marksman => Loc.T("Nişancı"), FacilityTitle.Carrier => Loc.T("Sırtlayan"), FacilityTitle.Ram => Loc.T("Koçbaşı"),
            FacilityTitle.Handler => Loc.T("Köpekçi"), FacilityTitle.Builder => Loc.T("Kurucu"), FacilityTitle.Thrower => Loc.T("Atıcı"), _ => "",
        };

        public static string Effect(Balance b, FacilityTitle title) => title switch
        {
            FacilityTitle.Marksman => Loc.F("Pusu hattı +{0} kare", Math.Max(0, b.facilityTitleAmbushBonus)),
            FacilityTitle.Carrier => Loc.T("Sürüklerken tam yürüyüş menzili"),
            FacilityTitle.Ram => Loc.F("Koşu darbesi +{0} kare iter", Math.Max(0, b.facilityTitleRushBonus)),
            FacilityTitle.Handler => Loc.F("Köpeğe tur başına {0} komut", Math.Max(1, b.facilityTitleDogCommands)),
            FacilityTitle.Builder => Loc.F("Aygıt süresi +{0} tur", Math.Max(0, b.facilityTitleDeviceBonus)),
            FacilityTitle.Thrower => Loc.F("Yük fırlatma menzili +{0} kare", Math.Max(0, b.facilityTitleThrowBonus)), _ => "",
        };

        static int Threshold(Balance b, FacilityTitle title) => Math.Max(1, title switch
        {
            FacilityTitle.Marksman => b.facilityMarksmanAt, FacilityTitle.Carrier => b.facilityCarrierAt, FacilityTitle.Ram => b.facilityRamTitleAt,
            FacilityTitle.Handler => b.facilityHandlerAt, FacilityTitle.Builder => b.facilityBuilderAt, _ => b.facilityThrowerAt,
        });

        internal static FacilityTitle RaidWorks(int slot, IReadOnlyList<RoomReport> reports, RaidResult result)
        {
            FacilityTitle works = FacilityTitle.None;
            int extracted = 0;
            foreach (int body in result.Extracted ?? new List<int>())
                if (body >= 0 && body < 31) extracted |= 1 << body;
            foreach (var report in reports)
            {
                var member = report.Team[slot];
                works |= member.Work;
                if ((member.Dragged & extracted) != 0) works |= FacilityTitle.Carrier;
            }
            if (reports[reports.Count - 1].Units?.Any(u => u.Class == Baskin.ClassDog && u.Owner == slot && u.State == "a") == true)
                works |= FacilityTitle.Handler;
            return works;
        }

        internal static FacilityTitle Credit(ResidentState person, FacilityTitle works, Balance b)
        {
            if (works == FacilityTitle.None) return FacilityTitle.None;
            Array.Resize(ref person.Deeds, Order.Count);
            int remaining = Math.Max(0, b.facilityMaxTitles) - Order.Count(t => Has(person.Titles, t));
            FacilityTitle awarded = FacilityTitle.None;
            for (int i = 0; i < Order.Count; i++)
            {
                var title = Order[i];
                if (!Has(works, title)) continue;
                if (person.Deeds[i] < int.MaxValue) person.Deeds[i]++;
                if (remaining <= 0 || Has(person.Titles, title) || person.Deeds[i] < Threshold(b, title)) continue;
                person.Titles |= title;
                awarded |= title;
                remaining--;
            }
            return awarded;
        }
    }

    public static partial class Baskin
    {
        static bool TitleOn(BState b, Unit u, FacilityTitle title) => b?.Moves?.Titles == true && u != null && FacilityTitles.Has(u.Titles, title);
        internal static void RecordWork(BState b, Unit u, FacilityTitle work)
        {
            if (b.Moves.Titles && u.Slot >= 0 && !string.IsNullOrEmpty(u.PersonId)) u.Work |= work;
        }

        public static int AmbushRangeFor(BState b, Unit u) => b.Moves.AmbushRange + (TitleOn(b, u, FacilityTitle.Marksman) ? b.Moves.TitleAmbushBonus : 0);
        public static int RushPushFor(BState b, Unit u) => b.Moves.RushPush + (TitleOn(b, u, FacilityTitle.Ram) ? b.Moves.TitleRushBonus : 0);
        public static int DragCostFor(BState b, Unit u) => TitleOn(b, u, FacilityTitle.Carrier) ? 0 : b.Moves.DragCost;
        public static int ThrowRangeFor(BState b, Unit u) => b.Moves.ThrowRange + (TitleOn(b, u, FacilityTitle.Thrower) ? b.Moves.TitleThrowBonus : 0);
        public static int DogCommandsFor(BState b, Unit u) => TitleOn(b, u, FacilityTitle.Handler) ? b.Moves.TitleDogCommands : 1;
    }
}
