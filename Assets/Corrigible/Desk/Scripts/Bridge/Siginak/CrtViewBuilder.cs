using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// CRT (K5) metinleri simülasyondan. Durum: kuruluyor (baskın emri verildi, sahne henüz açılmadı) · yayın açık (sahne
    /// yüklü) · kesildi (bu vardiyada baskın oldu, sonuç teleksde) · devre dışı (Teknisyen yaralı, yedek yok) · bekleniyor.
    /// </summary>
    public static class CrtViewBuilder
    {
        public static CrtState State(Simulation sim, bool raidLoaded)
        {
            if (sim == null) return CrtState.Waiting;
            if (sim.S.Raid != null) return raidLoaded ? CrtState.Live : CrtState.Establishing;
            var log = sim.S.RaidLog.LastOrDefault();
            if (log != null && log.Day == sim.S.Day) return CrtState.Cut;
            var team = sim.S.Teams.FirstOrDefault();
            if (team != null && !sim.SquadCanRaid(team)) return CrtState.Disabled;
            return CrtState.Waiting;
        }

        /// <summary>Baskının (sürüyorsa) ya da sıradaki sektörün kaydı; dönüşte son baskın.</summary>
        static LiberationSectorState Sector(Simulation sim)
        {
            var lib = sim.S.Liberation;
            if (lib.ActiveSector != null) return sim.LiberationSector(lib.ActiveSector);
            var log = sim.S.RaidLog.LastOrDefault(l => l.SectorId != null);
            if (sim.S.Raid == null && log != null && log.Day == sim.S.Day) return sim.LiberationSector(log.SectorId);
            return sim.NextLiberationSector(lib.CurrentRegion);
        }

        static string Region(string id) => DeskText.Upper(Loc.T(GameData.Region(id).Name));

        /// <summary>Kanal YZ trafiğini dinler: alarm eşiğe ulaşınca YZ karşı saldırır.</summary>
        static string Alarm(Simulation sim) =>
            Loc.F("YZ ALARMI {0}/{1}", DeskFormat.Number(sim.S.Alarm, 0), DeskFormat.Number(sim.B.liberationAlarmThreshold, 0));

        public static CrtScreenData Screen(Simulation sim, bool raidLoaded)
        {
            var d = new CrtScreenData { Channel = Loc.T("SAHA YAYINI · KANAL 07") };
            if (sim == null) { d.Title = Loc.T("BAĞLANTI BEKLENİYOR"); return d; }
            d.State = State(sim, raidLoaded);
            var team = sim.S.Teams.FirstOrDefault();
            string name = DeskText.Upper(LiberationMapBuilder.TeamName(team));
            // Teknisyensiz kadro baskına çıkamaz: Teknisyen yatıyor ve yerine çıkacak yedek yok
            bool injured = team != null && !sim.SquadCanRaid(team);
            // Baskın vardiyayı doldurur: dönen ekip sonraki vardiyada hazır
            bool back = team != null && team.Busy > 0 && sim.S.Raid == null;
            int days = team?.Members.FirstOrDefault(m => m.Role == "tek")?.Down ?? 0;
            d.TeamLine = sim.S.Raid != null ? Loc.F("{0} · SAHADA", name)
                : back ? Loc.F("{0} · DÖNDÜ · SONRAKİ VARDİYA", name)
                : injured ? Loc.F("{0} · TEKNİSYEN YARALI · {1} GÜN", name, days) : Loc.F("{0} · HAZIR", name);
            // K5a: yaralı ekip satırı ters video (renk değil biçim)
            d.TeamInverse = injured && !back && sim.S.Raid == null;
            var sector = Sector(sim);
            var lib = sim.S.Liberation;
            d.FrontLine = sector != null
                ? Loc.F("CEPHE: {0} / {1:00}", Region(sector.Region), sector.Index + 1)
                : Loc.F("CEPHE: {0} · ÖZGÜR", Region(lib.CurrentRegion));
            switch (d.State)
            {
                case CrtState.Establishing: d.Title = Loc.T("BAĞLANTI KURULUYOR"); break;
                case CrtState.Live: d.Title = Loc.T("YAYIN AÇIK"); break;
                case CrtState.Cut: d.Title = Loc.T("BAĞLANTI KESİLDİ"); d.Sub = Loc.T("SONUÇ TELEKSTE"); break;
                case CrtState.Disabled:
                    d.Title = Loc.T("YAYIN DEVRE DIŞI");
                    d.Sub = Alarm(sim);
                    d.Hint = Loc.T("Teknisyen kalkınca ya da bir yedek uzman hazır olunca yayın açılabilir.");
                    break;
                default:
                    d.Title = Loc.T("BAĞLANTI BEKLENİYOR");
                    d.Sub = Alarm(sim);
                    d.Hint = Loc.T("Görev fişinden baskın emri verildiğinde yayın açılır.");
                    break;
            }
            return d;
        }

        public static CrtTransitionData Transition(Simulation sim)
        {
            var d = new CrtTransitionData
            {
                Channel = Loc.T("SAHA YAYINI · KANAL 07"), Title = Loc.T("BAĞLANTI KURULUYOR"), Sync = Loc.T("SENKRON"),
                SkipKey = Loc.T("Boşluk"), SkipWord = Loc.T("atla"), Footer = Loc.T("KYGA-07 · ANALOG HAT"),
                CutTitle = Loc.T("BAĞLANTI KESİLDİ"), CutSub = Loc.T("SONUÇ TELEKSTE"),
            };
            if (sim == null) return d;
            var sector = Sector(sim);
            var team = sim.S.Teams.FirstOrDefault();
            int shift = sim.S.Day + 1;
            if (sector == null) { d.Code = Loc.F("V.{0:000}", shift); return d; }
            string kind = sector.MissionKind == "loot" ? Loc.T("YAĞMA") : Loc.T("SUNUCU YIKIMI");
            d.Code = Loc.F("V.{0:000} · OP-{1:00}", shift, sector.Index + 1);
            d.Target = Loc.F("{0} · {1} / {2:00} {3}", DeskText.Upper(LiberationMapBuilder.TeamName(team)), Region(sector.Region), sector.Index + 1,
                DeskText.Upper(sector.Name));
            d.Camera = Loc.F("KAMERA 2 · TAKTİK TAHTA 8×8 · GÖREV: {0}", kind);
            return d;
        }
    }
}
