using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Son şalterin sunum metinleri (KOS aşama 6; Docs/KABUL-OLAYLAR-VE-SONLAR-TASARIMI.md §4, §7): kilit gerekçesi, çekilirse hangi
    /// çeşidin geleceği (düzenli çekilme ya da tahliye) ve ÇEKİLME / TAHLİYE EMRİ kâğıdı. Saf ve salt okunur: yalnız Core'un public
    /// okuyucularından (<see cref="Simulation.LastSwitchCheck"/>, <see cref="Simulation.LastSwitchReasons"/>, pano, güven, ifşa) kurar;
    /// gizli kimliği, <c>EndReport</c>'u ve <c>VictoryTone</c>'u okumaz, çekim yapmaz, komut yazmaz. Bütün metinler o anki dilde.
    /// </summary>
    public static class LastSwitchViewBuilder
    {
        /// <summary>
        /// Emir kâğıdında çıkacakların ad listesinin en çok karakteri ("… +n" kesimi); kâğıdın yerleşim ölçüsü olduğu için temada
        /// (<c>lastSwitch.order.namesMaxChars</c>), tema yoksa 170.
        /// </summary>
        public static int OrderNamesMaxChars => SiginakTheme.Active != null ? (int)SiginakTheme.Active.NumberOf("lastSwitch.order.namesMaxChars", 170) : 170;

        /// <summary>Son şalterin adı (plaka, teleks durumu): "SON ŞALTER" / "LAST SWITCH". Oyunun adı olan "SON ŞALTER" anahtarından ayrı.</summary>
        public static string SwitchName => LastSwitchPlate.Name;

        /// <summary>Tahliye damgası (emir kâğıdı ve son rapor): "TAHLİYE" / "EVACUATED". Baskının "TAHLİYE" açılır yazısından ayrı anahtar.</summary>
        public static string EvacuatedStamp => DeskText.Upper(Loc.T("tahliye"));

        /// <summary>Core'un bugün döndürebildiği tahliye nedenleri (<see cref="Simulation.LastSwitchReasons"/>); testler bunu Core'la karşılaştırır.</summary>
        public static readonly string[] KnownReasons = { "konum", "kadro", "aclik", "guven" };

        /// <summary>Kilidin anlık girdileri (sunumun durumu; simülasyon dışı).</summary>
        public struct LockInputs
        {
            /// <summary>Baskın sahnesi yüklü (Core'un <c>S.Raid</c>'i de ayrıca sınanır).</summary>
            public bool RaidLoaded;
            /// <summary>Rehber sürüyor (<c>SiginakTutorial.Active</c>).</summary>
            public bool Tutorial;
            /// <summary>Açılış ya da emir belgesi açık (<c>StoryHold</c>, <c>SiginakBriefing.Busy</c>).</summary>
            public bool Story;
            /// <summary>Masa meşgul: fiş, tutanak ya da kabul elde; rapor ya da ayarlar dolabı açık.</summary>
            public bool DeskBusy;
        }

        /// <summary>
        /// Kilit (ilk tutan kazanır): harekât bitti, baskın sürüyor, rehber sürüyor, belge açık, masa meşgul, Core'un gerekçesi (ör. 5.
        /// vardiyadan önce). Kilit yoksa (null, null). Açık olay, kilitli plan ve basılmış fiş engel değildir (K6).
        /// </summary>
        public static (string plate, string line) Lock(Simulation sim, LockInputs ui)
        {
            if (sim == null) return (Loc.T("harekât bitti"), Loc.T("Harekât bitti."));
            if (sim.S.Ending != null) return (Loc.T("harekât bitti"), Loc.T("Harekât bitti."));
            if (sim.S.Raid != null || ui.RaidLoaded) return (Loc.T("baskın sürüyor"), Loc.T("Baskın sürüyor: son şalter çekilemez."));
            if (ui.Tutorial) return (Loc.T("mühürlü · rehber"), Loc.T("Rehber bitene kadar son şalter mühürlü."));
            if (ui.Story) return (Loc.T("belge açık"), Loc.T("Önce açılış belgesini kapat."));
            if (ui.DeskBusy) return (Loc.T("masa meşgul"), Loc.T("Masa meşgul: önce elindeki kâğıdı bırak."));
            string why = sim.LastSwitchCheck();
            if (why == null) return (null, null);
            string plate = sim.S.Day + 1 < sim.B.lastSwitchMinShift
                ? Loc.F("mühürlü · {0}. vardiya", sim.B.lastSwitchMinShift)
                : Loc.T("mühürlü");
            return (plate, why);
        }

        /// <summary>Şalter şimdi çekilse gelecek çeşit: plaka (kısa), üzerine gelme satırı ve tahliye mi.</summary>
        public static (string plate, string line, bool evac) Variant(Simulation sim)
        {
            var reasons = sim?.LastSwitchReasons() ?? new List<string>();
            if (reasons.Count == 0)
                return (Loc.T("çekilirse: ÇEKİLME"), Loc.T("Çekilirse: düzenli çekilme. Yenilgi sayılmaz."), false);
            return (Loc.T("çekilirse: TAHLİYE"), Loc.F("Çekilirse: TAHLİYE ({0}). Yenilgi sayılır.", ReasonList(sim, reasons)), true);
        }

        /// <summary>Nedenlerin kısa adlarının " · " ile birleşimi ("konum çözüldü · sığınak eksik 2/3 · güven 1").</summary>
        public static string ReasonList(Simulation sim, IEnumerable<string> reasons)
        {
            string line = null;
            foreach (string id in reasons)
            {
                string part = ReasonShort(sim, id);
                line = line == null ? part : Loc.F("{0} · {1}", line, part);
            }
            return line ?? string.Empty;
        }

        /// <summary>Tahliye nedeninin kısa adı (plaka, teleks, ton satırı).</summary>
        public static string ReasonShort(Simulation sim, string id) => id switch
        {
            "konum" => Loc.T("konum çözüldü"),
            "kadro" => Loc.F("sığınak eksik {0}/{1}", sim != null ? sim.UnderstaffedAtClose : 0, sim != null ? sim.B.abandonUnderstaffedShifts : 0),
            "aclik" => Loc.T("açlıktan ayrılmalar"),
            "guven" => Loc.F("güven {0}", sim != null ? sim.S.Trust : 0),
            _ => Loc.T("başka neden"),
        };

        /// <summary>Tahliye nedeninin emir kâğıdındaki satırı.</summary>
        public static string ReasonText(Simulation sim, string id) => id switch
        {
            "konum" => Loc.T("Konum çözüldü: YZ kapıya yürüyor."),
            "kadro" => Loc.F("Sığınak eksik: {0}/{1}.", sim != null ? sim.UnderstaffedAtClose : 0, sim != null ? sim.B.abandonUnderstaffedShifts : 0),
            "aclik" => Loc.F("Açlık {0} vardiya: insanlar birer birer kapıdan çıkıyor.", sim?.Shelter != null ? sim.Shelter.HungryStreak : 0),
            "guven" => Loc.F("Güven {0}: halk sırtını döndü.", sim != null ? sim.S.Trust : 0),
            _ => Loc.T("Başka bir tahliye nedeni."),
        };

        /// <summary>Tahliyenin neden damgası (son rapor).</summary>
        public static string ReasonStamp(string id) => id switch
        {
            "konum" => Loc.T("KONUM ÇÖZÜLMÜŞTÜ"),
            "kadro" => Loc.T("SIĞINAK EKSİKTİ"),
            "aclik" => Loc.T("AÇLIK SÜRÜYORDU"),
            "guven" => Loc.T("GÜVEN DÜŞÜKTÜ"),
            _ => EvacuatedStamp,
        };

        /// <summary>
        /// ÇEKİLME EMRİ ya da TAHLİYE EMRİ kâğıdı (emir belgesinin düzeninde, manila dosyasız). Çıkacaklar canlı panodan (ad ve sedye;
        /// kimlik yok), bırakılan bölgeler ve cephe, tahliyede her neden ayrı satırda; son satır "Bu karar geri alınmaz."
        /// </summary>
        public static BriefingData Order(Simulation sim)
        {
            var reasons = sim.LastSwitchReasons();
            bool evac = reasons.Count > 0;
            var lib = sim.S.Liberation;
            var people = sim.Board != null ? sim.Board.People.Where(p => p.Alive).ToList() : new List<ResidentState>();
            var names = people.Select(p => p.Down > 0 ? Loc.F("{0} (sedyeyle)", p.Name) : p.Name).ToList();
            int free = lib.Regions.Count(r => r.Completed), sectors = lib.Regions.Sum(r => r.CapturedCount);
            var front = lib.Regions.FirstOrDefault(r => r.Id == lib.CurrentRegion);
            string frontName = front != null ? sim.RegionName(front.Id) : "—";

            string verdict;
            if (evac)
            {
                var lines = reasons.Select(id => ReasonText(sim, id)).ToList();
                lines.Add(Loc.T("Tahliye yenilgi sayılır."));
                verdict = string.Join("\n", lines);
            }
            else verdict = Loc.T("Düzenli çekilme yenilgi sayılmaz.");
            // Görünür vazgeçiş (kol inikken şaltere tıklanmaz): Esc ya da sürüklemesiz sağ tık
            verdict = Loc.F("{0}\n{1}", verdict, Loc.T("Damgalamadan vazgeç: Esc ya da sağ tık."));

            return new BriefingData
            {
                Agency = Loc.T("KYGA · SIĞINAK 07"),
                Number = Loc.F("EMİR NO. V.{0:000}-S", sim.S.Day + 1),
                Title = evac ? Loc.T("TAHLİYE EMRİ") : Loc.T("ÇEKİLME EMRİ"),
                ToLabel = Loc.T("KİME"),
                To = Loc.T("Sığınak 07 kadrosu"),
                ReLabel = Loc.T("KONU"),
                Re = evac ? Loc.F("Tahliye · {0}", ReasonList(sim, reasons)) : Loc.T("Son şalter · sığınağın boşaltılması"),
                Stamp = Loc.T("SON"),
                Paragraphs = new[]
                {
                    Loc.F("Çıkacaklar ({0}): {1}", people.Count, FinalReportBuilder.Names(names, OrderNamesMaxChars)),
                    Loc.F("Bırakılan: {0} özgür bölge, {1} sektör. Cephe: {2}.", free, sectors, frontName),
                    verdict,
                },
                Closing = Loc.T("Bu karar geri alınmaz."),
                SignOffice = Loc.T("SIĞINAK 07 · KOMUTA"),
                SignLine = Loc.F("Vardiya {0:000} · analog hat", sim.S.Day + 1),
                Received = evac ? EvacuatedStamp : Loc.T("ÇEKİLDİ"),
                Action = Loc.T("DAMGALA"),
                Reading = false,
                NoDossier = true,
                HasSave = false,
                TabTitle = string.Empty, TabSummary = string.Empty, TabHint = string.Empty, NoSave = string.Empty,
            };
        }
    }
}
