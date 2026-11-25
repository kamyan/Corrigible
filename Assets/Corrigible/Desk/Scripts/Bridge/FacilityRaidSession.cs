using System;
using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Tesis baskınının sunumdaki oturumu (aşama 3; Docs/TESIS-BASKINLARI-TASARIMI.md §3.5, §11.5): sıradaki odayı açar, biten odanın
    /// raporunu hazırlar ve dosyalar, baskının sonucunu raporlardan okur. UnityEngine kullanmaz. Simülasyona tek yazma yolu kurucuya
    /// verilen <c>advance</c>'tır (canlı oyunda <c>GameController.AdvanceRaidRoom</c>: raid_room kaydedilir); oturumun kendi durumu
    /// yalnız açılan odanın adımı ve açılıştaki kırılan sivil toplamıdır, gerisi her an simülasyondan okunur (kayıttan devam aynı yoldan).
    /// Harekâtın her baskını tesis baskınıdır; koddan kurulan odasız örnek tahtada kurulmaz (<see cref="Applies"/>).
    /// </summary>
    public sealed class FacilityRaidSession
    {
        static readonly IReadOnlyList<RoomReport> None = Array.Empty<RoomReport>();
        readonly Func<RoomReport, ActResult> advance;

        /// <param name="advance">Oda raporunu simülasyona veren komut; null ise doğrudan <see cref="Simulation.AdvanceRaidRoom"/>
        /// (testler, GameController'sız sahne).</param>
        public FacilityRaidSession(Simulation sim, Func<RoomReport, ActResult> advance)
        {
            Sim = sim ?? throw new ArgumentNullException(nameof(sim));
            this.advance = advance ?? sim.AdvanceRaidRoom;
            Resumed = Filed.Count > 0;
        }

        /// <summary>Süren baskının odaları emirde yazılı (Core her emre kanadın üç odasını yazar; baskın yoksa false).</summary>
        public static bool Applies(Simulation sim) => sim != null && RaidChain.IsFacility(sim.S.Raid?.Params);

        public Simulation Sim { get; }
        public RaidParams Params => Sim.S.Raid?.Params;
        /// <summary>Dosyalanmış oda raporları (simülasyonun listesi; baskın yoksa boş).</summary>
        public IReadOnlyList<RoomReport> Filed => (IReadOnlyList<RoomReport>)Sim.S.Raid?.Reports ?? None;
        /// <summary>Son oda raporlandı: sonuç bekleniyor, tahta açılmaz.</summary>
        public bool Closed => Sim.RaidRoomsClosed;
        /// <summary>Oturum kurulurken en az bir oda raporu vardı (kayıttan devam: içinde bulunulan oda baştan açılır).</summary>
        public bool Resumed { get; }
        /// <summary>En son <see cref="Open"/>'ın açtığı oda.</summary>
        public RoomStep Step { get; private set; }
        /// <summary>Sıradaki oda (dosyalamadan sonra okunur); baskının odaları bittiyse null.</summary>
        public RoomStep? NextStep => Sim.RaidRoom;
        /// <summary>Açılan odadan önce dosyalanmış raporların kırdığı sivil sunucu (güven göstergesi: açılan odanın kırdıkları tahtadan
        /// eklenir; son oda dosyalanınca çift sayılmaz).</summary>
        public int CollateralBefore { get; private set; }

        /// <summary>Sıradaki odanın tahtası (raporlardan; kayıttan devam aynı tahtayı açar). Açılacak oda yoksa InvalidOperationException.</summary>
        public Baskin.BState Open()
        {
            var step = Sim.RaidRoom ?? throw new InvalidOperationException("Açılacak oda yok");
            var B = Sim.OpenRaidRoom();
            Step = step;
            CollateralBefore = Filed.Sum(r => r.Collateral);
            return B;
        }

        /// <summary>Biten tahtanın raporu (evre pass, won ya da lost); oda sürüyorsa null. Tahta beklenen oda değilse
        /// InvalidOperationException (olmamalı).</summary>
        public RoomReport Preview(Baskin.BState B) =>
            B != null && (B.Phase == "pass" || B.Phase == "won" || B.Phase == "lost") ? Sim.RaidRoomReport(B) : null;

        /// <summary>Raporu simülasyona verir (raid_room); simülasyonun denetimi reddederse Ok false, hiçbir şey değişmez.</summary>
        public ActResult File(RoomReport r) => advance(r);

        /// <summary>Baskının sonucu, raporlardan; son oda raporlanmadıysa null.</summary>
        public RaidResult Result() => Closed ? Sim.FacilityRaidResult() : null;

        /// <summary>Son dosyalanmış raporda kadronun <paramref name="slot"/>. kişisi; rapor yoksa null.</summary>
        public RoomMember Member(int slot)
        {
            var f = Filed;
            if (f.Count == 0) return null;
            var team = f[f.Count - 1].Team;
            return team != null && slot >= 0 && slot < team.Count ? team[slot] : null;
        }

        /// <summary>
        /// Raporu dosyalanan odadan sonra yürünerek geçilen (temiz) odalar, batıya doğru azalan sırayla: <see cref="RaidChain.Next"/>'in
        /// atladıkları. Geçişsiz biten odada, gidişte (hedef öncesi) ve atlanan oda yokken boş.
        /// </summary>
        public static IReadOnlyList<int> Skipped(RoomReport closed, RoomStep? next)
        {
            var o = new List<int>();
            if (closed == null || closed.End != "pass" || (!closed.Return && closed.Room < 3)) return o;
            int stop = next?.Room ?? 0;
            for (int k = closed.Room - 1; k > stop; k--) o.Add(k);
            return o;
        }
    }
}
