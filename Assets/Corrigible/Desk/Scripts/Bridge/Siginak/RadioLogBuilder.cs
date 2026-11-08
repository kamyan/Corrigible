using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// CRT önündeki telsiz kayıt defteri (Revizyon 2 · S1: “son dört kayıt”). Hat sahayla tek bağ olduğu için defter hattın
    /// geçmişidir ve simülasyondan türetilir: her baskının dönüşü (alınan sektör ya da geri çekilme, düşen uzman, kırılan sivil
    /// sunucu), son karşı saldırı ve bugünün satırları (ekip durumu, dinleyen telsiz odası, hat durumu). Kod “vardiya/sıra”
    /// (014/02; vardiya sayaçtaki gibi gün + 1); kayıt el yazısıyla, kısa bölge adıyla (“D. Avrupa 02”).
    /// </summary>
    public static class RadioLogBuilder
    {
        public const int Shown = 4;

        public static List<(string code, string text)> Entries(Simulation sim, bool raidLoaded)
        {
            var raw = new List<(int day, string text)>();
            if (sim == null) return new List<(string, string)>();
            var team = sim.S.Teams.FirstOrDefault();
            string name = LiberationMapBuilder.TeamName(team);
            foreach (var r in sim.S.RaidLog)
            {
                string place = Place(r.Region, r.SectorId != null ? sim.LiberationSector(r.SectorId) : null);
                raw.Add((r.Day, r.Success ? Loc.F("{0} döndü · {1} alındı", name, place) : Loc.F("{0} geri çekildi · {1}", name, place)));
                if (r.Down > 0) raw.Add((r.Day, Loc.F("{0} uzman düştü", r.Down)));
                if (r.Collateral > 0) raw.Add((r.Day, Loc.F("{0} sivil sunucu kırıldı", r.Collateral)));
            }
            // Karşı saldırı vardiyanın sonunda gelir: o günün baskın satırlarından sonra
            if (sim.S.LastCounterattackDay >= 0)
            {
                var sector = sim.S.LastCounterattack != null ? sim.LiberationSector(sim.S.LastCounterattack) : null;
                string text = sector != null ? Loc.F("YZ karşı saldırısı · {0} yeniden işgal", Place(sector.Region, sector)) : Loc.T("YZ karşı saldırısı");
                int at = raw.FindLastIndex(e => e.day <= sim.S.LastCounterattackDay) + 1;
                raw.Insert(at, (sim.S.LastCounterattackDay, text));
            }
            // Bugün: ekip, telsiz odası, hat
            int today = sim.S.Day;
            var state = CrtViewBuilder.State(sim, raidLoaded);
            if (team != null)
            {
                bool back = team.Busy > 0 && sim.S.Raid == null;
                var active = sim.S.Liberation?.ActiveSector != null ? sim.LiberationSector(sim.S.Liberation.ActiveSector) : null;
                if (sim.S.Raid != null) raw.Add((today, Loc.F("{0} sahada · {1}", name, active != null ? Place(active.Region, active) : "—")));
                // Kartlı harekâtta kadro panodan gelir (CrtViewBuilder.Screen ile aynı denetim); ekip kaydının yaralısı yanıltır
                else if (!back && sim.Board != null && sim.PlanningRaidCheck() != null)
                    raw.Add((today, Loc.T("Kadro hazır değil")));
                else if (!back && sim.Board == null && !sim.SquadCanRaid(team))
                    raw.Add((today, Loc.F("Teknisyen yaralı · {0} gün, yedek yok", LiberationMapBuilder.TechnicianDown(team))));
                else if (!back)
                {
                    var next = sim.S.Liberation != null ? sim.NextLiberationSector(sim.S.Liberation.CurrentRegion) : null;
                    raw.Add((today, Loc.F("{0} hazır · cephe {1}", name, Short(next != null ? next.Region : sim.S.Liberation?.CurrentRegion ?? Simulation.HQ))));
                }
            }
            var radio = sim.Shelter?.Room(ShelterLogic.Radio);
            if (radio != null && radio.Level > 0 && radio.Workers > 0) raw.Add((today, Loc.T("Telsiz odası YZ trafiğini dinliyor")));
            raw.Add((today, state switch
            {
                CrtState.Establishing => Loc.T("Hat açılıyor"),
                CrtState.Live => Loc.T("Hat açık — ekip sahada"),
                CrtState.Cut => Loc.T("Hat kapandı — sonuç tekste"),
                CrtState.Disabled => Loc.T("Hat kapalı — ekip çıkamaz"),
                _ => Loc.T("Dinlemede — emir bekleniyor"),
            }));

            var entries = new List<(string, string)>();
            int day = int.MinValue, seq = 0;
            foreach (var e in raw)
            {
                seq = e.day == day ? seq + 1 : 1; day = e.day;
                entries.Add((string.Format(CultureInfo.InvariantCulture, "{0:000}/{1:00}", e.day + 1, seq), e.text));
            }
            return entries.Skip(System.Math.Max(0, entries.Count - Shown)).ToList();
        }

        /// <summary>El yazısında kısa bölge adı: iki sözcükte ilki baş harfiyle (“D. Avrupa”, “W. Europe”).</summary>
        public static string Short(string regionId)
        {
            var words = GameData.Region(regionId).Name.Split(' ');
            return words.Length == 2 ? DeskText.Upper(words[0].Substring(0, 1)) + ". " + words[1] : string.Join(" ", words);
        }

        static string Place(string region, LiberationSectorState sector) =>
            sector != null ? Loc.F("{0} {1:00}", Short(sector.Region), sector.Index + 1) : Short(region);
    }
}
