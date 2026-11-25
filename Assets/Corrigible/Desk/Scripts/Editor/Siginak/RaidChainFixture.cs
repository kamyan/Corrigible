using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Editör fikstürlerinin baskın sürücüsü (tek kip: harekâtın her baskını üç odalı tesis baskınıdır, <c>ResolveRaid</c> elle verilen
    /// sonucu yok sayar ve son oda raporlanmadan reddeder). Süren baskını oda oda yürütür ve kapatır: <c>OpenRaidRoom</c> → tahtada
    /// kurulum ve hamle (makineler susturulur, ekip kapıya, GEÇ, geri çekilme ya da tur sonu) → <c>AdvanceRaidRoom</c> →
    /// <c>ResolveRaid</c>. Simülasyona yalnız public komutlarla yazar (komutlar kayda <c>raid_room</c> ve <c>raidres</c> olarak girer,
    /// kayıttan aynen yeniden oynanır); tahta komutlar arasında yerel bir kopyadır. Baskın sahnesi yüklenmez. Hata olursa
    /// <see cref="InvalidOperationException"/>. Core testleri bu derlemeyi görmez (karşılığı <c>FacilityKit</c>); Desk testlerinin kendi kopyası <c>FacilityDeskKit.ResolveRaid</c>.
    /// </summary>
    public static class RaidChainFixture
    {
        /// <summary>Baskının sonu.</summary>
        public enum End
        {
            /// <summary>Kazanır: her odada makineler kalkar, hedef odasında kaçış başlar, ekip kapıdan çıkar.</summary>
            Win,
            /// <summary>Giriş odasında geri çekilir (başarısız).</summary>
            Retreat,
            /// <summary>Giriş odasında süre dolar: ekip geri çekilmiş sayılır (başarısız).</summary>
            Timeout,
        }

        /// <summary>Kadro üyesinin kaderi (kadro sırasıyla; verilmeyen <see cref="Returns"/>).</summary>
        public const int Returns = 0;
        /// <summary>Düşer ve tesiste kalır, ölür (tesiste "ekip ayakta döndüyse düşen kurtarılır" kuralı yok). Başarıda hedef odasında,
        /// başarısızlıkta giriş odasında düşer.</summary>
        public const int Dies = 1;
        /// <summary>Geride kalır: kaçışta süre dolar, çıkamaz (ölür). Yalnız başarılı baskında.</summary>
        public const int LeftBehind = 2;
        /// <summary>Giriş odasında düşer, dönüşte ekip temiz odadan toplar: tesisten çıkarılır, yaşar (yatar). Yalnız başarılı baskında.</summary>
        public const int Rescued = 3;

        /// <summary>Süren baskını kazanır; kadro ayakta döner.</summary>
        public static void Win(Simulation sim) => Resolve(sim, End.Win);

        /// <summary><paramref name="success"/>: kazanır; değilse giriş odasında geri çekilir. Kader <paramref name="fates"/>'te.</summary>
        public static void Resolve(Simulation sim, bool success, params int[] fates) => Resolve(sim, success ? End.Win : End.Retreat, fates);

        /// <summary>
        /// Süren baskını <paramref name="end"/> ile bitirir; kadronun kaderi <paramref name="fates"/>'te (kadro sırasıyla, verilmeyen
        /// <see cref="Returns"/>). Başarıda en az bir uzman dönmeli; başarısızlıkta yalnız <see cref="Returns"/> ve <see cref="Dies"/>.
        /// İkmal görevinde yükü dönen ilk uzman taşır (önce Teknisyen).
        /// </summary>
        public static void Resolve(Simulation sim, End end, params int[] fates)
        {
            if (sim?.S.Raid == null) throw new InvalidOperationException("Süren baskın yok.");
            if (!RaidChain.IsFacility(sim.S.Raid.Params)) throw new InvalidOperationException("Baskının odaları yok.");
            bool success = end == End.Win;
            int count = RaidChain.SquadCount(sim.S.Raid.Params);
            int Fate(int slot) => fates != null && slot < fates.Length ? fates[slot] : Returns;
            var slots = Enumerable.Range(0, count).ToList();
            if (!success && slots.Any(i => Fate(i) != Returns && Fate(i) != Dies))
                throw new InvalidOperationException("Başarısız baskında kader yalnız Returns ya da Dies olur.");
            if (success && slots.All(i => Fate(i) != Returns))
                throw new InvalidOperationException("Başarılı baskında en az bir uzman ayakta dönmeli.");
            for (int guard = 0; guard < 8 && !sim.RaidRoomsClosed; guard++)
            {
                var B = sim.OpenRaidRoom();
                Calm(B);
                Unspawn(B);
                if (!success)
                {
                    foreach (int i in slots.Where(i => Fate(i) == Dies)) Down(B, i);
                    if (end == End.Retreat) { if (B.Phase == "player" && !Baskin.Retreat(B)) throw new InvalidOperationException("Geri çekilme reddedildi: " + B.Room.Id); }
                    else RunOut(B, false);
                }
                else
                {
                    if (B.Room.Type == RoomType.Entry && !B.Escape)
                        foreach (int i in slots.Where(i => Fate(i) == Rescued)) Down(B, i);
                    if (B.Room.Type == RoomType.Target && !B.Escape)
                    {
                        foreach (int i in slots.Where(i => Fate(i) == Dies)) Down(B, i);
                        foreach (var t in Baskin.TargetsLeft(B)) Baskin.CellAt(B, t.X, t.Y).O = null;
                        if (Baskin.IsLoot(B))
                        {
                            var carriers = Standing(B).Where(u => Fate(u.Slot) == Returns).ToList();
                            if (carriers.Count == 0) throw new InvalidOperationException("Yükü taşıyacak uzman yok.");
                            B.Carrier = (carriers.FirstOrDefault(u => u.Type == "tek") ?? carriers[0]).Id;
                        }
                        B.Escape = true;
                    }
                    // Gidişte herkes kapıya; kaçışta geride kalan tahtada kalır
                    foreach (var u in Standing(B).Where(u => !B.Escape || Fate(u.Slot) != LeftBehind))
                        if (B.Phase == "player") ToGate(B, u);
                    // Geride kalan: öbürleri kapıda beklerken süre biter; çıkamayan düşmüş sayılır, kapıdakiler geçer
                    if (B.Phase == "player" && Standing(B).Count > 0) RunOut(B, true);
                    if (B.Phase == "player" && !Baskin.Pass(B)) throw new InvalidOperationException("GEÇ reddedildi: " + B.Room.Id);
                }
                var filed = sim.AdvanceRaidRoom(sim.RaidRoomReport(B));
                if (!filed.Ok) throw new InvalidOperationException("Oda raporu: " + filed.Msg);
            }
            if (!sim.RaidRoomsClosed) throw new InvalidOperationException("Oda zinciri bitmedi.");
            var resolved = sim.ResolveRaid(new RaidResult { Reason = "" });
            if (!resolved.Ok) throw new InvalidOperationException("Sonuç: " + resolved.Msg);
            if (sim.S.RaidLog[sim.S.RaidLog.Count - 1].Success != success)
                throw new InvalidOperationException(success ? "Baskın kazanılmadı." : "Baskın kaybedilmedi.");
        }

        /// <summary>Bekleyen sunucu ödülünü komutla seçer (kartlı oyunda bekleyen ödül sonraki emri kilitler); yoksa false.</summary>
        public static bool ChooseReward(Simulation sim, string choice = FacilityGear.ChoiceRecipe)
        {
            string region = sim?.Facility?.PendingReward;
            if (string.IsNullOrEmpty(region)) return false;
            var r = sim.ChooseServerReward(region, choice);
            if (!r.Ok) throw new InvalidOperationException("Sunucu ödülü: " + r.Msg);
            return true;
        }

        // ---------------------------------------------------------------- tahta (yerel kopya; komutlar arası)

        /// <summary>Makineleri devre dışı bırakır, uyanmayı kapatır: kurulan kareler bozulmasın.</summary>
        static void Calm(Baskin.BState B)
        {
            foreach (var e in B.Units.Where(u => u.Side == 'e' && !u.Dead)) { e.Dead = true; e.Intent = null; }
            B.Spawns.Clear();
            B.SpawnPerTurn = 0;
        }

        /// <summary>Kopya dolaplarını kaldırır: makinesiz oda temiz sayılır, dönüşte yeniden oynanmaz.</summary>
        static void Unspawn(Baskin.BState B)
        {
            for (int y = 0; y < Baskin.N; y++)
                for (int x = 0; x < Baskin.N; x++)
                    if (Baskin.CellAt(B, x, y).O?.K == "spawner") Baskin.CellAt(B, x, y).O = null;
        }

        /// <summary>Tahtadaki ayaktaki uzmanlar, kadro sırasıyla (köpek, aygıt ve tutsak değil).</summary>
        static List<Baskin.Unit> Standing(Baskin.BState B) =>
            B.Units.Where(u => Baskin.IsSpecialist(u) && Baskin.OnBoard(u)).OrderBy(u => u.Slot).ToList();

        /// <summary>Kadro sırasıyla uzmanı düşürür (tahtada ayaktaysa): plaka ve buz karşılamaz, can 0.</summary>
        static void Down(Baskin.BState B, int slot)
        {
            var u = B.Units.FirstOrDefault(v => Baskin.IsSpecialist(v) && v.Slot == slot && Baskin.OnBoard(v));
            if (u == null) return;
            u.Armor = 0; u.Ice = 0; u.Hp = 0; u.Dead = true;
        }

        /// <summary>Uzmanı birimsiz bir kapı karesine koyar ve kapıya girdirir.</summary>
        static void ToGate(Baskin.BState B, Baskin.Unit u)
        {
            var free = Baskin.GateTiles(B).Where(g => { var o = Baskin.UnitAt(B, g.X, g.Y); return o == null || o == u; }).ToList();
            if (free.Count == 0) throw new InvalidOperationException("Boş kapı karesi yok: " + B.Room?.Id);
            u.X = free[0].X;
            u.Y = free[0].Y;
            if (!Baskin.EnterGate(B, u.Id)) throw new InvalidOperationException("Kapıya girilemedi: " + B.Room?.Id);
        }

        /// <summary>Tur sonu verir (makineler susturulur) ta ki süre dolup evre biter (<paramref name="untilEmpty"/>: ya da tahtada
        /// ayakta uzman kalmaz). Evre bitmezse hata.</summary>
        static void RunOut(Baskin.BState B, bool untilEmpty)
        {
            for (int t = 0; t < 40 && B.Phase == "player" && (!untilEmpty || Standing(B).Count > 0); t++)
            {
                if (!Baskin.EndTurn(B)) throw new InvalidOperationException("Tur sonu reddedildi: tur " + B.Turn);
                Calm(B);
            }
            if (B.Phase == "player" && (!untilEmpty || Standing(B).Count > 0)) throw new InvalidOperationException("Süre dolmadı: " + B.Room?.Id);
        }
    }
}
