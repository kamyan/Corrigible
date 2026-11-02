using System;
using System.Collections.Generic;
using System.Linq;

namespace Corrigible.Core
{
    /// <summary>
    /// Bölge olayları (Docs/BOLGE-OLAYLARI.md): baskın boyunca tur tur işleyen bina içi sistemler. Kural tarafa bakmaz; aynı
    /// karedeki uzman ile makine aynı sonucu alır. Olaylar RNG çekmez: kurulum Create'te bütün çekimlerden sonra yapılır, olaysız
    /// baskın ve kilitli imzalar aynı kalır. Takvimli adımlar ve olgunlaşan fitiller makine evresinin sonunda, uyanmadan sonra ve
    /// plandan önce işler (<see cref="EventEnd"/>); son turda olay adımı yoktur. Fitil: N. turda tetiklenen olay N+1. turun evre
    /// sonunda işler, arada iki taraf da bir kez hareket eder.
    /// Ortak sınırlar: hasar tavanı (birim bir olay adımından öbürüne olaylardan en çok 1 hasar alır), olay ve zinciri hiçbir
    /// nesneye hasar vermez (<see cref="BState.InEvent"/>), hiçbir olay bir zemin karesini çıkıştan koparamaz (<see cref="ExitSafe"/>).
    /// </summary>
    public static partial class Baskin
    {
        /// <summary>Olay tablosu (<see cref="RegionDef.Event"/>): ad, kısa kural, olayın geçtiği bina (görünen oda adı) ve birim
        /// uyarılarının etiketi.</summary>
        public static readonly Dictionary<string, (string Name, string Desc, string Room, string Tag)> RegionEvents =
            new Dictionary<string, (string, string, string, string)>
            {
                ["bulut"] = (Loc.N("Söndürme bulutu"), Loc.N("Tek turların sonunda tavandaki memeler bir alanı bulutla doldurur: bir tur boyunca buluta ve buluttan nişan alınmaz; bitişik nesneye iş (sabotaj, onarım) serbest."), Loc.N("Banka sunucu salonu"), Loc.N("BULUT")),
                ["raf"] = (Loc.N("Kayar arşiv rafı"), Loc.N("3. ve 5. tur sonunda raf bloğu kayar ve geçit rayın öbür ucuna atlar; kapanan geçitteki birim 1 hasar alır ve yana itilir."), Loc.N("Arşiv dosya katı"), Loc.N("RAF")),
                ["gaz"] = (Loc.N("Yanıcı gaz kaçağı"), Loc.N("Kompresör bulutu takvimle doldurup boşaltır. Buluta ateş değerse bulut bir tur sonra parlar: içindeki herkes 1 hasar alır."), Loc.N("Rafineri kompresör dairesi"), Loc.N("PARLAMA")),
                ["tavan"] = (Loc.N("Çatlak tavan"), Loc.N("Çatlağa ya da yanına darbe gelirse tavan bir tur sonra çöker: altındaki birim 1 hasar alır, kare boşsa enkaz yığını kalır."), Loc.N("Cevher işleme hangarı"), Loc.N("TAVAN")),
                ["asansor"] = (Loc.N("Yük asansörü"), Loc.N("Kabin 2. ve 4. tur sonunda kalkar, bir tur sonra öbür durağa iner: duraktaki birim yolculuk eder, iniş durağındaki birim 1 hasar alır."), Loc.N("Asansör holü"), Loc.N("KABİN")),
                ["kriyo"] = (Loc.N("Kriyo soğutma hattı"), Loc.N("Çift turların sonunda hat bölgeye boşalır: içindeki birim bir sıra yürüyemez ama eylemini yapar; buz kabuğu ilk hasarı karşılar."), Loc.N("Soğuk depo"), Loc.N("DONMA")),
                ["turnike"] = (Loc.N("Vardiya turnikeleri"), Loc.N("Hedefin çevresindeki holün turnikeleri tek yöne döner: içeri ya da dışarı. 2. ve 4. tur sonunda döner; kaçış başlayınca ya da son iki turda tahliye, hep dışarı."), Loc.N("Kasa holü"), Loc.N("TURNİKE")),
                ["kaynak"] = (Loc.N("Kaynak robotu kolu"), Loc.N("Kol her evre sonunda işaretli kareye kaynak yapar (1 hasar) ve saat yönünde döner; kola gelen her darbe dönüşe bir adım ekler."), Loc.N("Montaj hattı"), Loc.N("KAYNAK")),
                ["faz"] = (Loc.N("Şebeke faz değişimi"), Loc.N("Kablo kanalları iki faza ayrılır ve üç turda bir el değiştirir: evreyi canlı kanalda bitiren 1 hasar alır, sönük kanal güvenli."), Loc.N("Trafo merkezi"), Loc.N("ŞEBEKE")),
                ["suzgec"] = (Loc.N("Süzgeç emmesi"), Loc.N("3. ve 6. tur sonunda süzgeç dört yanındakileri içeri çeker; süzgeçte biri varsa çarpışırlar ve ikisi de 1 hasar alır."), Loc.N("Su basmış bodrum"), Loc.N("EMME")),
                ["tup"] = (Loc.N("Basınçlı tüp rafı"), Loc.N("Vurulan tüp ıslık çalar ve bir tur sonra çizili hattında uçar: hattaki ilk birime 1 hasar verir ve onu iter."), Loc.N("Türbin bakım atölyesi"), Loc.N("TÜP")),
                ["yag"] = (Loc.N("Hidrolik yağ sızıntısı"), Loc.N("2. tur sonunda hortum patlar: yağdaki birim itilince, çekilince ya da vurulunca yağ bitene kadar kayar; çarpışma 1 hasar."), Loc.N("Makine dairesi"), Loc.N("YAĞ")),
            };

        /// <summary>Fitil ya da tüp: kare, tüpün uçuş yönü (<see cref="Dirs"/>) ve işleyeceği tur (0: tetiklenmedi).</summary>
        public sealed class Fuse { public int X, Y, Dir, Due; public Fuse Clone() => (Fuse)MemberwiseClone(); }

        /// <summary>Turnike: holün içindeki kare (X, Y) ile dışındaki kare (X2, Y2) arasındaki kenar. Both: iki yöne açık geçit.</summary>
        public sealed class Gate { public int X, Y, X2, Y2; public bool Both; }

        /// <summary>Bölge olayının baskın boyunca süren durumu; kareye bağlı bayraklar <see cref="Cell"/>'de.</summary>
        public sealed class EvState
        {
            public string Id;
            /// <summary>Olayın kareleri: söndürme A alanı, arşiv rafının rayı (sırayla), gaz bulutu, kriyo bölgesi, turnike holü,
            /// yağ hattı. Area2: söndürme B alanı; turnikede turnikeli dış kareler (çıkışa holden geçmeden bağlı).</summary>
            public List<Pt> Area = new List<Pt>(), Area2 = new List<Pt>();
            /// <summary>Tek kareler: söndürme memeleri (P: A, Q: B), asansör durakları (P: A uzakta, Q: B hedefin yanında), gaz
            /// sızıntısı, kaynak kolunun kaidesi, süzgeç, yağ hortumu; turnike holünün merkezi (Q, hedef).</summary>
            public Pt P, Q;
            /// <summary>Raf: geçidin raydaki yeri (0 ya da son kare). Asansör: kabin (0 A'da, 1 B'de, 2 B'ye, 3 A'ya gidiyor).
            /// Turnike: 0 içeri, 1 dışarı. Kaynak: kolun bu evreki yönü (<see cref="Dirs"/>).</summary>
            public int Mode;
            /// <summary>Kaynak: bu evre kola gelen darbe. Asansör: kabindeki birim (0 boş).</summary>
            public int Count;
            /// <summary>Gaz: bulut alev aldı, bu turun evre sonunda parlar (0: alev almadı).</summary>
            public int Due;
            /// <summary>Raf: blok durdu (bir raf yıkıldı). Turnike: tahliye modu. Yağ: döküldü.</summary>
            public bool Stop;
            /// <summary>Tavan: çökecek çatlaklar. Tüp: tahtadaki tüpler (uçuş yönü; ıslık çaldıysa uçacağı tur).</summary>
            public List<Fuse> Fuses = new List<Fuse>();
            /// <summary>Turnike: iki yöne açık geçit (kurulumdan sonra değişmez; öteki kenarlar <see cref="HolEdges"/>).</summary>
            public List<Gate> Gates = new List<Gate>();
            /// <summary>Hasar tavanı: son olay adımından beri olaydan hasar alan birimler.</summary>
            public List<int> Hurt = new List<int>();

            public EvState Clone()
            {
                var c = (EvState)MemberwiseClone();
                c.Area = new List<Pt>(Area); c.Area2 = new List<Pt>(Area2);
                c.Fuses = Fuses.Select(f => f.Clone()).ToList();
                c.Gates = new List<Gate>(Gates);
                c.Hurt = new List<int>(Hurt);
                return c;
            }
        }

        // ---------- Ortak ----------
        static bool Is(BState B, string id) => B.Ev != null && B.Ev.Id == id;
        static bool Same(Pt a, Pt b) => a.X == b.X && a.Y == b.Y;
        static int Key(Pt p) => p.Y * N + p.X;
        static int Dist(Pt a, Pt b) => Man(a.X, a.Y, b.X, b.Y);
        /// <summary>Yürünebilir kare: duvar ve nesne yok (birimler sayılmaz).</summary>
        static bool Passable(BState B, int x, int y) => Inb(x, y) && CellAt(B, x, y).T != "wall" && CellAt(B, x, y).O == null;
        static int ExitDist(BState B, int x, int y) => B.Exits.Count == 0 ? 2 * N : B.Exits.Min(e => Man(e.X, e.Y, x, y));
        /// <summary>Bütün kareler, y sonra x sırasıyla (yer bulmada eşitliği bozan sıra).</summary>
        static IEnumerable<Pt> AllTiles() { for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) yield return new Pt(x, y); }
        static bool InCloud(BState B, int x, int y) => B.Ev != null && B.Ev.Id == "bulut" && Inb(x, y) && CellAt(B, x, y).Cloud;

        /// <summary>Çıkışlardan yürüyüş uzaklığı (duvar ve nesne geçilmez, birimler sayılmaz; ulaşılamayan −1).</summary>
        static int[,] ExitSteps(BState B, HashSet<int> blocked = null)
        {
            var dist = new int[N, N];
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) dist[y, x] = -1;
            var q = new Queue<Pt>();
            foreach (var e in B.Exits) if (Passable(B, e.X, e.Y) && dist[e.Y, e.X] < 0) { dist[e.Y, e.X] = 0; q.Enqueue(e); }
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (!Passable(B, x, y) || dist[y, x] >= 0 || blocked != null && blocked.Contains(y * N + x)) continue;
                    dist[y, x] = dist[p.Y, p.X] + 1; q.Enqueue(new Pt(x, y));
                }
            }
            return dist;
        }

        /// <summary>
        /// Kaçış güvencesi: değişiklikten önce çıkışa bağlı olan ve hâlâ açık her kare bağlı kalır, bağlı bir komşusu olan hedef
        /// onu korur. <paramref name="before"/> değişiklikten önceki <see cref="ExitSteps"/>.
        /// </summary>
        static bool ExitSafe(BState B, int[,] before)
        {
            var after = ExitSteps(B);
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (before[y, x] >= 0 && Passable(B, x, y) && after[y, x] < 0) return false;
            foreach (var t in TargetsLeft(B))
            {
                bool Had(int[,] r) => Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && r[t.Y + d[1], t.X + d[0]] >= 0);
                if (Had(before) && !Had(after)) return false;
            }
            return true;
        }

        /// <summary>Girişten hedefin yanına en kısa yürüyüş yolu (birimler sayılmaz; eşitlikte Dirs sırası).</summary>
        static HashSet<int> TargetPath(BState B, Pt tg)
        {
            var dist = ExitSteps(B);
            var path = new HashSet<int>();
            Pt? end = null;
            foreach (var d in Dirs)
            {
                int x = tg.X + d[0], y = tg.Y + d[1];
                if (Inb(x, y) && dist[y, x] >= 0 && (end == null || dist[y, x] < dist[end.Value.Y, end.Value.X])) end = new Pt(x, y);
            }
            if (end == null) return path;
            var p = end.Value;
            path.Add(Key(p));
            while (dist[p.Y, p.X] > 0)
            {
                foreach (var d in Dirs)
                {
                    int x = p.X + d[0], y = p.Y + d[1];
                    if (Inb(x, y) && dist[y, x] == dist[p.Y, p.X] - 1) { p = new Pt(x, y); break; }
                }
                path.Add(Key(p));
            }
            return path;
        }

        /// <summary>Kopya dolaplarının yanındaki açık kareler (uyanma adayları).</summary>
        static HashSet<int> SpawnerSides(BState B)
        {
            var o = new HashSet<int>();
            foreach (var p in AllTiles())
                if (CellAt(B, p.X, p.Y).O?.K == "spawner")
                    foreach (var d in Dirs) if (Passable(B, p.X + d[0], p.Y + d[1])) o.Add((p.Y + d[1]) * N + p.X + d[0]);
            return o;
        }

        /// <summary>Olay hasarı (1): hasar tavanına tabi, nesnelere işlemez.</summary>
        static void EventHurt(BState B, Unit u, string why)
        {
            bool was = B.InEvent;
            B.InEvent = true;
            Hurt(B, u, 1, why);
            B.InEvent = was;
        }

        /// <summary>
        /// İtilen, çekilen ya da atılan birim kabloya girdi: 1 hasar. Şebeke olayında yalnız canlı fazın kanalı yakar (sönük kanal
        /// sıradan zemin) ve hasar olaydır.
        /// </summary>
        static void CableHurt(BState B, Unit u)
        {
            var c = CellAt(B, u.X, u.Y);
            if (c.T != "cable") return;
            if (!Is(B, "faz")) Hurt(B, u, 1, Loc.T("elektrik"));
            else if (c.Phase == LivePhase(B.Turn)) EventHurt(B, u, Loc.T("elektrik"));
        }

        /// <summary>
        /// Adım olaya uyuyor mu: turnikenin kapalı yönü geçilmez (yürüyüş, itme, kanca çekişi ve atılma). Olay yoksa hep açık.
        /// Komşu iki kare için.
        /// </summary>
        public static bool CanStep(BState B, int fx, int fy, int tx, int ty)
        {
            if (!Is(B, "turnike")) return true;
            bool inF = InHol(B.Ev, fx, fy), inT = InHol(B.Ev, tx, ty);
            if (inF == inT) return true;
            // Dışarıdaki kare çıkışa holden geçmeden bağlıysa kenar turnikedir (çıkmaz cebin kenarı serbest); iki yöne açık geçit hariç
            int ox = inF ? tx : fx, oy = inF ? ty : fy;
            if (!B.Ev.Area2.Any(p => p.X == ox && p.Y == oy)) return true;
            if (B.Ev.Gates.Any(g => g.Both && (g.X == fx && g.Y == fy && g.X2 == tx && g.Y2 == ty || g.X == tx && g.Y == ty && g.X2 == fx && g.Y2 == fy))) return true;
            return B.Ev.Mode == 1 ? inF : inT;
        }

        /// <summary>Turnike holü: hedefin (Q) çevresindeki 3×3.</summary>
        static bool InHol(EvState ev, int x, int y) => Math.Abs(x - ev.Q.X) <= 1 && Math.Abs(y - ev.Q.Y) <= 1;

        /// <summary>Turnike kenarları bugünkü tahtada: holün açık bir karesi ile turnikeli dış kare arasındaki her kenar.</summary>
        static IEnumerable<Gate> HolEdges(BState B)
        {
            var ev = B.Ev;
            for (int y = ev.Q.Y - 1; y <= ev.Q.Y + 1; y++)
                for (int x = ev.Q.X - 1; x <= ev.Q.X + 1; x++)
                {
                    if (!Passable(B, x, y)) continue;
                    foreach (var d in Dirs)
                    {
                        int ox = x + d[0], oy = y + d[1];
                        if (InHol(ev, ox, oy) || !Passable(B, ox, oy) || !ev.Area2.Any(p => p.X == ox && p.Y == oy)) continue;
                        yield return new Gate { X = x, Y = y, X2 = ox, Y2 = oy, Both = ev.Gates.Any(g => g.Both && g.X == x && g.Y == y && g.X2 == ox && g.Y2 == oy) };
                    }
                }
        }

        /// <summary>Hidrolik yağ: yağdaki birim yağ bitene ya da önü kapanana kadar kayar. Çarpışma itme kuralıyla ve olay hasarıdır;
        /// yağdan kabloya kayan 1 hasar alır.</summary>
        static void Slide(BState B, Unit u, int[] d)
        {
            if (!Is(B, "yag")) return;
            for (int step = 0; step < N && !u.Dead && CellAt(B, u.X, u.Y).Oil; step++)
            {
                int nx = u.X + d[0], ny = u.Y + d[1];
                if (!Inb(nx, ny) || CellAt(B, nx, ny).T == "wall") { EventHurt(B, u, Loc.T("yağda kaydı, duvara çarptı")); return; }
                if (CellAt(B, nx, ny).O != null) { EventHurt(B, u, Loc.T("yağda kaydı, dolaba çarptı")); return; }
                var o = UnitAt(B, nx, ny);
                if (o != null) { EventHurt(B, u, Loc.T("yağda kaydı, çarpıştı")); EventHurt(B, o, Loc.T("çarpışma")); return; }
                AddFx(B, new Fx { K = "push", X = u.X, Y = u.Y, Dx = d[0], Dy = d[1], Unit = u.Id });
                u.X = nx; u.Y = ny;
                if (CellAt(B, nx, ny).T == "cable") EventHurt(B, u, Loc.T("elektrik"));
            }
        }

        /// <summary>İsabet alan birim yağdaysa isabet yönünde kayar (atış, vuruş, havan).</summary>
        static void HitSlide(BState B, Unit v, int dir)
        {
            if (v != null && !v.Dead && Is(B, "yag")) Slide(B, v, Dirs[dir]);
        }

        static bool SlideCrashes(BState B, Unit u, int[] d)
        {
            int x = u.X, y = u.Y;
            for (int step = 0; step < N && CellAt(B, x, y).Oil; step++)
            {
                int nx = x + d[0], ny = y + d[1];
                if (!Inb(nx, ny) || CellAt(B, nx, ny).T == "wall" || CellAt(B, nx, ny).O != null || UnitAt(B, nx, ny) != null) return true;
                x = nx; y = ny;
            }
            return false;
        }

        /// <summary>Ateş (yanıcı gaz): hasar veren atışın hattı ve vurduğu kare, havanın düştüğü kare, patlama alanı, EMP'nin düştüğü
        /// kare. Dolu ve alev almamış buluta değerse bulut bir sonraki turun evre sonunda parlar.</summary>
        static void Fire(BState B, int x, int y)
        {
            if (!Is(B, "gaz") || B.InEvent || B.Ev.Due != 0 || !Inb(x, y) || !CellAt(B, x, y).Gas) return;
            B.Ev.Due = B.Turn + 1;
            AddFx(B, new Fx { K = "ev", Obj = "gaz_alev", X = x, Y = y });
            Say(B, Loc.T("Gaz bulutu alev aldı: bir tur sonra parlayacak."), "sys");
        }

        /// <summary>Darbe (tavan): karede ya da dört komşusunda bulunan çatlak bir sonraki turun evre sonunda çöker.</summary>
        static void CrackHit(BState B, int x, int y)
        {
            if (!Is(B, "tavan") || B.InEvent || !Inb(x, y)) return;
            foreach (var p in CrackNear(B, x, y))
            {
                B.Ev.Fuses.Add(new Fuse { X = p.X, Y = p.Y, Due = B.Turn + 1 });
                AddFx(B, new Fx { K = "ev", Obj = "catlak", X = p.X, Y = p.Y });
                Say(B, Loc.T("Tavan çatlağı çökecek: bir tur sonra."), "sys");
            }
        }

        /// <summary>Karede ya da dört komşusunda tetiklenmemiş çatlaklar (kare, kuzey, doğu, güney, batı sırası).</summary>
        static List<Pt> CrackNear(BState B, int x, int y)
        {
            var o = new List<Pt>();
            if (!Is(B, "tavan")) return o;
            foreach (var p in new[] { new Pt(x, y) }.Concat(Dirs.Select(d => new Pt(x + d[0], y + d[1]))))
                if (Inb(p.X, p.Y) && CellAt(B, p.X, p.Y).Crack && !B.Ev.Fuses.Any(f => f.X == p.X && f.Y == p.Y)) o.Add(p);
            return o;
        }

        /// <summary>Patlama alanı ve havanın düştüğü kare: yanıcı gazı tutuşturur, tavan çatlağını tetikler.</summary>
        static void Blasted(BState B, int x, int y)
        {
            if (B.Ev == null) return;
            Fire(B, x, y);
            CrackHit(B, x, y);
        }

        /// <summary>Darbe: itme çarpışması (itilenin ve çarptığının karesi), dinamitle yıkılan duvar ve moloz.</summary>
        static void Struck(BState B, int ax, int ay, int bx, int by)
        {
            if (!Is(B, "tavan")) return;
            CrackHit(B, ax, ay);
            CrackHit(B, bx, by);
        }

        /// <summary>Madenci arşiv rafını yıktı: blok kalıcı olarak durur.</summary>
        static void ShelfBroken(BState B, int x, int y)
        {
            if (!Is(B, "raf") || !CellAt(B, x, y).Shelf) return;
            CellAt(B, x, y).Shelf = false;
            B.Ev.Stop = true;
            Say(B, Loc.T("Arşiv rafı yıkıldı: blok artık kaymaz."), "sys");
        }

        /// <summary>Tüp ve kaynak kolu darbeyi kendi kuralıyla karşılar (hasar almaz): tüp ıslık çalar, kol bir adım fazla döner.</summary>
        static bool EventObjectHit(BState B, int x, int y, Obj o)
        {
            if (o.K == "tup" && Is(B, "tup"))
            {
                var f = B.Ev.Fuses.FirstOrDefault(t => t.X == x && t.Y == y);
                if (f != null && f.Due == 0)
                {
                    f.Due = B.Turn + 1;
                    AddFx(B, new Fx { K = "ev", Obj = "islik", X = x, Y = y, Dx = Dirs[f.Dir][0], Dy = Dirs[f.Dir][1] });
                    Say(B, Loc.T("Tüpün vanası çatladı, ıslık çalıyor: bir tur sonra çizili hattında uçacak."), "sys");
                }
                return true;
            }
            if (o.K == "kol" && Is(B, "kaynak"))
            {
                B.Ev.Count++;
                AddFx(B, new Fx { K = "ev", Obj = "kol", X = x, Y = y });
                Say(B, Loc.T("Kaynak kolu sarsıldı: evre sonunda bir adım fazla dönecek."), "sys");
                return true;
            }
            return false;
        }

        // ---------- Takvim ----------
        /// <summary>Şebeke: turun canlı fazı (A 1.–3., B 4.–6., A 7.–8. tur).</summary>
        static int LivePhase(int turn) => (Math.Max(1, turn) - 1) / 3 % 2 == 0 ? 1 : 2;
        static bool ShiftAt(BState B, int t) => Is(B, "raf") && !B.Ev.Stop && (t == 3 || t == 5) && t < B.MaxTurns - 1;
        static bool GasFillAt(int t) => t % 3 == 1;
        static bool GasVentAt(int t) => t % 3 == 0;
        static bool LiftDeparts(BState B, int t) => B.Ev.Mode < 2 && t % 2 == 0 && t >= 2 && t <= B.MaxTurns - 2;
        static bool FreezeAt(BState B, int t) => t % 2 == 0 && t + 1 < B.MaxTurns;
        static bool GateTurnAt(BState B, int t) => !B.Ev.Stop && (t == 2 || t == 4) && t < B.MaxTurns - 2;
        static bool DrainAt(int t) => t == 3 || t == 6;

        static Pt ArmTile(EvState ev, int dir) => new Pt(ev.P.X + Dirs[dir][0], ev.P.Y + Dirs[dir][1]);
        /// <summary>Kol bu yöne kaynak yapabilir mi: duvara, tahta kenarına ve hedefe bakan yönler atlanır.</summary>
        static bool ArmFaces(BState B, int dir)
        {
            var t = ArmTile(B.Ev, dir);
            return Inb(t.X, t.Y) && CellAt(B, t.X, t.Y).T != "wall" && !(CellAt(B, t.X, t.Y).O is Obj o && IsTarget(o.K));
        }
        /// <summary>Kol saat yönünde <paramref name="steps"/> geçerli adım döner (geçerli yön yoksa yerinde kalır).</summary>
        static int ArmTurn(BState B, int dir, int steps)
        {
            for (int s = 0; s < steps; s++)
                for (int i = 1; i <= 4; i++) if (ArmFaces(B, (dir + i) % 4)) { dir = (dir + i) % 4; break; }
            return dir;
        }

        /// <summary>Tüpün uçuş hattı: tüpün önünden ilk duvara, nesneye ya da tahta kenarına kadar (birimler hattı kesmez).</summary>
        static List<Pt> TubeLine(BState B, Fuse f)
        {
            var o = new List<Pt>();
            for (int k = 1; k < N; k++)
            {
                int x = f.X + Dirs[f.Dir][0] * k, y = f.Y + Dirs[f.Dir][1] * k;
                if (!Passable(B, x, y)) break;
                o.Add(new Pt(x, y));
            }
            return o;
        }

        /// <summary>Kareye en yakın boş zemin (kabin dolu durağa inemezse son adımda yolcuyu yanına bırakır); yürüyerek bulunmazsa
        /// tahtadaki en yakın boş açık kare, o da yoksa null.</summary>
        static Pt? NearestFree(BState B, Pt from)
        {
            var seen = new HashSet<int> { Key(from) };
            var q = new Queue<Pt>();
            q.Enqueue(from);
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                foreach (var d in Dirs)
                {
                    var n = new Pt(p.X + d[0], p.Y + d[1]);
                    if (!Passable(B, n.X, n.Y) || !seen.Add(Key(n))) continue;
                    if (UnitAt(B, n.X, n.Y) == null) return n;
                    q.Enqueue(n);
                }
            }
            Pt? best = null;
            foreach (var p in AllTiles())
                if (Passable(B, p.X, p.Y) && UnitAt(B, p.X, p.Y) == null && (best == null || Dist(p, from) < Dist(best.Value, from))) best = p;
            return best;
        }

        // ---------- Kurulum ----------
        /// <summary>
        /// Olayı tahtaya kurar: RNG çekmez, birimli kareye nesne ya da duvar koymaz, çıkışlara ve komşularına olay karesi koymaz.
        /// Koşullar kademe kademe gevşer; yer bulunamazsa olay kurulmaz (RaidEventTests her harita türünde kilitler).
        /// </summary>
        static void EventSetup(BState B, RaidParams p, string[] rows)
        {
            B.Ev = new EvState { Id = p.Event };
            var targets = TargetsLeft(B);
            var tg = targets.Count > 0 ? new Pt(targets[0].X, targets[0].Y) : new Pt(N - 2, N / 2);
            bool ok;
            switch (p.Event)
            {
                case "bulut": ok = SetupCloud(B, tg); break;
                case "raf": ok = SetupShelf(B, tg); break;
                case "gaz": ok = SetupGas(B, tg); break;
                case "tavan": ok = SetupCracks(B, tg, rows); break;
                case "asansor": ok = SetupLift(B, tg); break;
                case "kriyo": ok = SetupFreeze(B, tg); break;
                case "turnike": ok = SetupGates(B, tg); break;
                case "kaynak": ok = SetupArm(B, tg); break;
                case "faz": ok = SetupPhases(B); break;
                case "suzgec": ok = SetupDrain(B, tg); break;
                case "tup": ok = SetupTubes(B, tg); break;
                case "yag": ok = SetupOil(B, tg); break;
                default: ok = false; break;
            }
            if (!ok) { B.Ev = null; return; }
            var def = RegionEvents[p.Event];
            Say(B, Loc.F("Saha raporu · {0}: {1}", Loc.T(def.Name), Loc.T(def.Desc)), "sys");
        }

        /// <summary>Söndürme memesinin alanı: meme karesi ve duvar ya da nesne olmayan dört komşusu.</summary>
        static List<Pt> CloudArea(BState B, Pt n)
        {
            var a = new List<Pt> { n };
            foreach (var d in Dirs) if (Passable(B, n.X + d[0], n.Y + d[1])) a.Add(new Pt(n.X + d[0], n.Y + d[1]));
            return a;
        }

        /// <summary>Batı Avrupa: iki meme (B'nin alanı hedefin bir komşusunu kapsar), alanlar birbirine değmez. Sıra: en geniş alan,
        /// merkeze yakınlık, y, x.</summary>
        static bool SetupCloud(BState B, Pt tg)
        {
            for (int tier = 0; tier < 3; tier++)
            {
                int minExit = tier == 0 ? 3 : 2, minSize = tier == 0 ? 4 : tier == 1 ? 3 : 2, lo = tier == 0 ? 2 : 1, hi = tier == 0 ? 5 : 6;
                bool Fits(Pt n, List<Pt> a) => n.X >= lo && n.X <= hi && Passable(B, n.X, n.Y) && a.Count >= minSize && a.All(t => ExitDist(B, t.X, t.Y) >= minExit);
                double Score(Pt n, List<Pt> a) => a.Count * 10 - 0.1 * (Math.Abs(2 * n.X - 7) + Math.Abs(2 * n.Y - 7));
                Pt? bn = null; List<Pt> ba = null; double bs = double.NegativeInfinity;
                foreach (var n in AllTiles())
                {
                    if (!Passable(B, n.X, n.Y)) continue;
                    var a = CloudArea(B, n);
                    if (!Fits(n, a) || tier < 2 && !a.Any(t => Dist(t, tg) == 1)) continue;
                    if (Score(n, a) > bs) { bs = Score(n, a); bn = n; ba = a; }
                }
                if (bn == null) continue;
                Pt? an = null; List<Pt> aa = null; bs = double.NegativeInfinity;
                foreach (var n in AllTiles())
                {
                    if (!Passable(B, n.X, n.Y)) continue;
                    var a = CloudArea(B, n);
                    if (!Fits(n, a) || a.Any(t => ba.Any(s => Dist(s, t) <= (tier < 2 ? 1 : 0)))) continue;
                    if (Score(n, a) > bs) { bs = Score(n, a); an = n; aa = a; }
                }
                B.Ev.Q = bn.Value; B.Ev.Area2 = ba;
                // İkinci meme yoksa iki dolum aynı alana
                B.Ev.P = an ?? bn.Value; B.Ev.Area = aa ?? new List<Pt>(ba);
                return true;
            }
            return false;
        }

        /// <summary>Doğu Avrupa: 4 karelik ray girişlerle hedef arasındaki ana ateş hattına dik; iki konumda da kaçış yolu açık.</summary>
        static bool SetupShelf(BState B, Pt tg)
        {
            var before = ExitSteps(B);
            double ex = B.Exits.Count > 0 ? B.Exits.Average(e => e.X) : 0, ey = B.Exits.Count > 0 ? B.Exits.Average(e => e.Y) : 0;
            bool vertical = Math.Abs(tg.X - ex) >= Math.Abs(tg.Y - ey);
            // Kademeler: 4 karelik dik ray; iki yön; 3 kare; dar koridorda 2 kare (tek raf, tek geçit)
            for (int tier = 0; tier < 4; tier++)
            {
                int len = tier < 2 ? 4 : tier == 2 ? 3 : 2;
                List<Pt> best = null; double bs = double.NegativeInfinity;
                foreach (bool vert in tier == 0 ? new[] { vertical } : new[] { vertical, !vertical })
                    foreach (var s in AllTiles())
                    {
                        var rail = Enumerable.Range(0, len).Select(i => new Pt(s.X + (vert ? 0 : i), s.Y + (vert ? i : 0))).ToList();
                        if (!rail.All(t => Inb(t.X, t.Y) && CellAt(B, t.X, t.Y).T == "floor"
                            && CellAt(B, t.X, t.Y).O == null && UnitAt(B, t.X, t.Y) == null
                            && t.X >= (tier == 0 ? 2 : 1) && t.X <= (tier < 2 ? 6 : 7) && ExitDist(B, t.X, t.Y) >= 2 && Dist(t, tg) > 1)) continue;
                        if (!ShelfSafe(B, rail, 0, before) || !ShelfSafe(B, rail, len - 1, before)) continue;
                        // Ray hedefin satırını (sütununu) keser ve giriş ile hedefin ortasına yakın durur
                        double mid = vert ? (ex + tg.X) / 2.0 : (ey + tg.Y) / 2.0;
                        int along = vert ? tg.Y : tg.X, start = vert ? s.Y : s.X;
                        double sc = (start <= along && along < start + len ? 10 : 0) - Math.Abs((vert ? s.X : s.Y) - mid);
                        if (sc > bs) { bs = sc; best = rail; }
                    }
                if (best == null) continue;
                B.Ev.Area = best;
                B.Ev.Mode = 0;
                for (int i = 0; i < best.Count; i++)
                {
                    var c = CellAt(B, best[i].X, best[i].Y);
                    c.Shelf = true;
                    c.T = i == 0 ? "floor" : "wall";
                }
                return true;
            }
            return false;
        }

        static bool ShelfSafe(BState B, List<Pt> rail, int gap, int[,] before)
        {
            var saved = rail.Select(t => CellAt(B, t.X, t.Y).T).ToList();
            for (int i = 0; i < rail.Count; i++) if (i != gap) CellAt(B, rail[i].X, rail[i].Y).T = "wall";
            bool ok = ExitSafe(B, before);
            for (int i = 0; i < rail.Count; i++) CellAt(B, rail[i].X, rail[i].Y).T = saved[i];
            return ok;
        }

        /// <summary>Orta Doğu: sızıntı hedefe 2 kare uzakta, duvar dibinde; 5 karelik bulut önce hedefin komşularını alır.</summary>
        static bool SetupGas(BState B, Pt tg)
        {
            for (int tier = 0; tier < 3; tier++)
            {
                List<Pt> best = null; Pt leak = default; double bs = double.NegativeInfinity;
                foreach (var s in AllTiles())
                {
                    int dt = Dist(s, tg);
                    if (!Passable(B, s.X, s.Y) || ExitDist(B, s.X, s.Y) < (tier == 0 ? 3 : 2)) continue;
                    if (tier == 0 ? dt != 2 : tier == 1 ? dt < 2 || dt > 3 : dt < 1 || dt > 4) continue;
                    if (tier < 2 && !Dirs.Any(d => Inb(s.X + d[0], s.Y + d[1]) ? CellAt(B, s.X + d[0], s.Y + d[1]).T == "wall" : tier == 1)) continue;
                    var cloud = GrowGas(B, s, tg);
                    if (cloud.Count < (tier < 2 ? 4 : 2)) continue;
                    double sc = cloud.Count(t => Dist(t, tg) == 1) * 10 + cloud.Count;
                    if (sc > bs) { bs = sc; best = cloud; leak = s; }
                }
                if (best == null) continue;
                B.Ev.P = leak;
                B.Ev.Area = best;
                return true;
            }
            return false;
        }

        static List<Pt> GrowGas(BState B, Pt leak, Pt tg)
        {
            var cloud = new List<Pt> { leak };
            while (cloud.Count < 5)
            {
                Pt? pick = null; double ps = double.NegativeInfinity;
                foreach (var c in cloud)
                    foreach (var d in Dirs)
                    {
                        var n = new Pt(c.X + d[0], c.Y + d[1]);
                        if (!Passable(B, n.X, n.Y) || ExitDist(B, n.X, n.Y) < 2 || cloud.Any(t => Same(t, n))) continue;
                        double s = (Dist(n, tg) == 1 ? 100 : 0) - Dist(n, leak) * 10 - n.Y * 0.1 - n.X * 0.01;
                        if (s > ps) { ps = s; pick = n; }
                    }
                if (pick == null) break;
                cloud.Add(pick.Value);
            }
            return cloud;
        }

        /// <summary>Afrika: 3 çatlak (3. kademeden 4); önce açık arazinin yıktığı duvarlar, sonra duvar dibi. Çıkışların, hedefin ve
        /// kopya dolabının yanı ile girişten hedefe giden yol çatlak olmaz.</summary>
        static bool SetupCracks(BState B, Pt tg, string[] rows)
        {
            int count = 3 + (B.Params.Tier >= 3 ? 1 : 0);
            var path = TargetPath(B, tg);
            var spawn = SpawnerSides(B);
            for (int tier = 0; tier < 3; tier++)
            {
                bool Ok(Pt t) => CellAt(B, t.X, t.Y).T == "floor" && CellAt(B, t.X, t.Y).O == null && UnitAt(B, t.X, t.Y) == null
                    && ExitDist(B, t.X, t.Y) >= 2 && Dist(t, tg) > 1 && !spawn.Contains(Key(t)) && (tier == 2 || !path.Contains(Key(t)));
                var broken = AllTiles().Where(t => rows != null && rows[t.Y][t.X] == '#' && Ok(t));
                var near = AllTiles().Where(t => Ok(t) && t.X >= 2 && t.X <= 6
                    && (tier > 0 || Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && CellAt(B, t.X + d[0], t.Y + d[1]).T == "wall"))).OrderBy(t => Dist(t, tg));
                var pick = new List<Pt>();
                foreach (var t in broken.Concat(near))
                {
                    if (pick.Count >= count) break;
                    if (pick.Any(p => Dist(p, t) < (tier == 0 ? 2 : 1))) continue;
                    pick.Add(t);
                }
                if (pick.Count < (tier < 2 ? count : 1)) continue;
                foreach (var t in pick) CellAt(B, t.X, t.Y).Crack = true;
                return true;
            }
            return false;
        }

        /// <summary>Kuzey Amerika: B durağı hedefin komşusu, A uzak yarıda bir kopya dolabının yanında.</summary>
        static bool SetupLift(BState B, Pt tg)
        {
            var spawn = SpawnerSides(B);
            var steps = ExitSteps(B);
            // İki durak da çıkışa yürüyerek bağlı: yolcu kapalı bir cebe inmez
            bool Stop(Pt t) => CellAt(B, t.X, t.Y).T == "floor" && CellAt(B, t.X, t.Y).O == null && ExitDist(B, t.X, t.Y) >= 2 && steps[t.Y, t.X] >= 0;
            Pt? b = null; double bs = double.NegativeInfinity;
            foreach (var t in AllTiles())
            {
                int dt = Dist(t, tg);
                if (!Stop(t) || dt > 2) continue;
                double s = (dt == 1 ? 10 : 0) + (UnitAt(B, t.X, t.Y) == null ? 2 : 0) + 0.1 * ExitDist(B, t.X, t.Y);
                if (s > bs) { bs = s; b = t; }
            }
            if (b == null) return false;
            Pt? a = null; bs = double.NegativeInfinity;
            foreach (var t in AllTiles())
            {
                if (!Stop(t) || Dist(t, b.Value) < 3) continue;
                double s = (spawn.Contains(Key(t)) ? 10 : 0) + (t.X >= 4 ? 5 : 0) + 0.5 * Dist(t, b.Value) + (UnitAt(B, t.X, t.Y) == null ? 1 : 0);
                if (s > bs) { bs = s; a = t; }
            }
            if (a == null) return false;
            B.Ev.P = a.Value; B.Ev.Q = b.Value; B.Ev.Mode = 0;
            return true;
        }

        /// <summary>Rusya: 2×3 ya da 3×2 bölge hedefin bir komşusunu içerir, soğutma ünitelerine en yakın olan seçilir.</summary>
        static bool SetupFreeze(BState B, Pt tg)
        {
            var cools = AllTiles().Where(t => CellAt(B, t.X, t.Y).O?.K == "cool").ToList();
            for (int tier = 0; tier < 3; tier++)
            {
                List<Pt> best = null; double bs = double.NegativeInfinity;
                foreach (var (w, h) in new[] { (2, 3), (3, 2) })
                    foreach (var s in AllTiles())
                    {
                        if (s.X + w > N || s.Y + h > N) continue;
                        var rect = new List<Pt>();
                        for (int y = s.Y; y < s.Y + h; y++) for (int x = s.X; x < s.X + w; x++) rect.Add(new Pt(x, y));
                        if (tier == 0 && rect.Any(t => t.X < 2 || t.X > 6) || tier == 1 && rect.Any(t => t.X < 1)) continue;
                        if (rect.Any(t => Same(t, tg))) continue;
                        var zone = rect.Where(t => Passable(B, t.X, t.Y)).ToList();
                        if (zone.Count < (tier == 0 ? 4 : tier == 1 ? 3 : 2) || zone.Any(t => ExitDist(B, t.X, t.Y) < (tier == 0 ? 3 : 2))) continue;
                        if (tier < 2 && !zone.Any(t => Dist(t, tg) == 1)) continue;
                        double near = cools.Count > 0 ? zone.Min(t => cools.Min(c => Dist(c, t))) : 0;
                        double sc = zone.Count - 2 * near;
                        if (sc > bs) { bs = sc; best = zone; }
                    }
                if (best == null) continue;
                B.Ev.Area = best;
                return true;
            }
            return false;
        }

        /// <summary>Hindistan: hol hedef ve sekiz komşusu (Q hedef). Turnikeli dış kareler (Area2) çıkışa holden geçmeden bağlı olanlar:
        /// yalnız holden girilen çıkmaz cebin kenarı serbest kalır, kimse dışarıda kapalı kalmaz. Üç ve daha çok kenarda çıkışlardan
        /// en uzak kenar iki yöne açık geçit (Gates). Çıkışa ya da komşusuna değen kare turnikeli olmaz.</summary>
        static bool SetupGates(BState B, Pt tg)
        {
            var ev = B.Ev;
            ev.Q = tg;
            var holKeys = new HashSet<int>();
            for (int y = tg.Y - 1; y <= tg.Y + 1; y++) for (int x = tg.X - 1; x <= tg.X + 1; x++) if (Inb(x, y)) holKeys.Add(y * N + x);
            var around = ExitSteps(B, holKeys);
            ev.Area = AllTiles().Where(p => holKeys.Contains(Key(p)) && Passable(B, p.X, p.Y)).ToList();
            ev.Area2 = AllTiles().Where(p => !holKeys.Contains(Key(p)) && Passable(B, p.X, p.Y) && ExitDist(B, p.X, p.Y) >= 2 && around[p.Y, p.X] >= 0
                && Dirs.Any(d => holKeys.Contains((p.Y + d[1]) * N + p.X + d[0]) && Passable(B, p.X + d[0], p.Y + d[1]))).ToList();
            var edges = HolEdges(B).ToList();
            if (ev.Area.Count == 0 || edges.Count == 0) return false;
            // İki yöne açık geçit en az iki tek yönlü turnike kalırsa konur; dar holde her kenar turnikedir (son iki turun tahliye
            // modu çıkışı yine güvenceye alır)
            ev.Gates = new List<Gate>();
            if (edges.Count >= 3)
            {
                Gate far = null;
                foreach (var g in edges) if (far == null || around[g.Y2, g.X2] > around[far.Y2, far.X2]) far = g;
                far.Both = true;
                ev.Gates.Add(far);
            }
            ev.Mode = 0;
            return true;
        }

        /// <summary>Çin: kaide hedefle aynı satır ya da sütunda tam 2 kare uzakta (aradaki kare açık); kol önce hedefe bakan yönün
        /// saat yönündeki komşusuna kaynak yapar.</summary>
        static bool SetupArm(BState B, Pt tg)
        {
            var before = ExitSteps(B);
            for (int tier = 0; tier < 3; tier++)
            {
                Pt? best = null; double bs = double.NegativeInfinity;
                foreach (var t in AllTiles())
                {
                    int dt = Dist(t, tg);
                    var c = CellAt(B, t.X, t.Y);
                    if (c.T != "floor" || c.O != null || UnitAt(B, t.X, t.Y) != null || ExitDist(B, t.X, t.Y) < (tier == 0 ? 3 : 2)) continue;
                    if (tier == 0 ? dt != 2 || t.X != tg.X && t.Y != tg.Y || !Passable(B, (t.X + tg.X) / 2, (t.Y + tg.Y) / 2)
                        : tier == 1 ? dt != 2 : dt < 2 || dt > 4) continue;
                    c.O = NewObj("kol", 1);
                    B.Ev.P = t;
                    bool ok = ExitSafe(B, before) && Enumerable.Range(0, 4).Any(d => ArmFaces(B, d));
                    c.O = null;
                    if (!ok) continue;
                    double sc = ExitDist(B, t.X, t.Y) + (Dirs.Any(d => Same(new Pt(t.X + d[0], t.Y + d[1]), tg) || Dist(new Pt(t.X + d[0], t.Y + d[1]), tg) == 1) ? 5 : 0);
                    if (sc > bs) { bs = sc; best = t; }
                }
                if (best == null) continue;
                var p = best.Value;
                CellAt(B, p.X, p.Y).O = NewObj("kol", 1);
                B.Ev.P = p;
                int toward = 0;
                for (int i = 0; i < 4; i++) if (Sign(tg.X - p.X) == Dirs[i][0] && Sign(tg.Y - p.Y) == Dirs[i][1]) toward = i;
                B.Ev.Mode = ArmTurn(B, toward, 1);
                return true;
            }
            return false;
        }

        /// <summary>Japonya-Kore: birbirine değen kanallar grup olur, gruplar sırayla A ve B; tek grup uzun ekseninden ikiye bölünür.</summary>
        static bool SetupPhases(BState B)
        {
            var seen = new bool[N, N];
            var groups = new List<List<Pt>>();
            foreach (var s in AllTiles())
            {
                if (CellAt(B, s.X, s.Y).T != "cable" || seen[s.Y, s.X]) continue;
                var g = new List<Pt>();
                var q = new Queue<Pt>();
                q.Enqueue(s); seen[s.Y, s.X] = true;
                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    g.Add(p);
                    foreach (var d in Dirs)
                    {
                        int x = p.X + d[0], y = p.Y + d[1];
                        if (!Inb(x, y) || seen[y, x] || CellAt(B, x, y).T != "cable") continue;
                        seen[y, x] = true; q.Enqueue(new Pt(x, y));
                    }
                }
                groups.Add(g);
            }
            if (groups.Count == 0) return false;
            if (groups.Count == 1)
            {
                var g = groups[0];
                int x0 = g.Min(t => t.X), x1 = g.Max(t => t.X), y0 = g.Min(t => t.Y), y1 = g.Max(t => t.Y);
                bool byY = y1 - y0 >= x1 - x0;
                int cut = byY ? (y0 + y1) / 2 : (x0 + x1) / 2;
                foreach (var t in g) CellAt(B, t.X, t.Y).Phase = (byY ? t.Y : t.X) <= cut ? 1 : 2;
            }
            else
                for (int i = 0; i < groups.Count; i++) foreach (var t in groups[i]) CellAt(B, t.X, t.Y).Phase = i % 2 == 0 ? 1 : 2;
            return true;
        }

        /// <summary>Güneydoğu Asya: süzgeç hedefe bitişik değil ama bir komşusu hedefin komşusu; kopya dolaplarından uzak.</summary>
        static bool SetupDrain(BState B, Pt tg)
        {
            var spawn = SpawnerSides(B);
            for (int tier = 0; tier < 3; tier++)
            {
                Pt? best = null; double bs = double.NegativeInfinity;
                int lo = tier == 0 ? 2 : tier == 1 ? 1 : 0, hi = tier == 0 ? 5 : tier == 1 ? 6 : 7;
                foreach (var t in AllTiles())
                {
                    var c = CellAt(B, t.X, t.Y);
                    int dt = Dist(t, tg);
                    if (c.T != "floor" || c.O != null || t.X < lo || t.X > hi || ExitDist(B, t.X, t.Y) < (tier == 0 ? 3 : 2) || dt < 2 || dt > (tier < 2 ? 2 : 4)) continue;
                    var sides = Dirs.Select(d => new Pt(t.X + d[0], t.Y + d[1])).Where(p => Passable(B, p.X, p.Y)).ToList();
                    if (sides.Count < 2 || tier < 2 && !sides.Any(p => Dist(p, tg) == 1)) continue;
                    int sd = spawn.Count == 0 ? 2 * N : spawn.Min(k => Man(k % N, k / N, t.X, t.Y));
                    if (sd < (tier == 0 ? 2 : tier == 1 ? 1 : 0)) continue;
                    double sc = sides.Count + (UnitAt(B, t.X, t.Y) == null ? 0.5 : 0);
                    if (sc > bs) { bs = sc; best = t; }
                }
                if (best == null) continue;
                B.Ev.P = best.Value;
                return true;
            }
            return false;
        }

        /// <summary>Güney Amerika: 3 tüp (3. kademeden 4) duvar dibinde, vanası duvardan dışarı bakar; en az birinin hattı hedefin
        /// bir komşusundan geçer. Tüpler nesnedir: yolu kapatan tüp konmaz.</summary>
        static bool SetupTubes(BState B, Pt tg)
        {
            int count = 3 + (B.Params.Tier >= 3 ? 1 : 0);
            var before = ExitSteps(B);
            for (int tier = 0; tier < 3; tier++)
            {
                var cands = new List<Fuse>();
                foreach (var t in AllTiles())
                {
                    var c = CellAt(B, t.X, t.Y);
                    if (c.T != "floor" || c.O != null || UnitAt(B, t.X, t.Y) != null || ExitDist(B, t.X, t.Y) < 2 || tier < 2 && (t.X < 2 || t.X > 6)) continue;
                    if (Dirs.Any(d => Inb(t.X + d[0], t.Y + d[1]) && CellAt(B, t.X + d[0], t.Y + d[1]).O is Obj o && (IsTarget(o.K) || o.K == "civ"))) continue;
                    Fuse bestF = null; int bestLen = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        int bx = t.X - Dirs[i][0], by = t.Y - Dirs[i][1];
                        if (!(Inb(bx, by) ? CellAt(B, bx, by).T == "wall" : tier == 2)) continue;
                        var f = new Fuse { X = t.X, Y = t.Y, Dir = i };
                        int len = TubeLine(B, f).Count;
                        if (len > bestLen) { bestLen = len; bestF = f; }
                    }
                    if (bestF != null && bestLen >= 2) cands.Add(bestF);
                }
                bool Hot(Fuse f) => TubeLine(B, f).Any(l => Dist(l, tg) == 1);
                var hot = cands.Where(Hot).OrderByDescending(f => TubeLine(B, f).Count).ToList();
                if (tier < 2 && hot.Count == 0) continue;
                var pick = new List<Fuse>();
                foreach (var f in hot.Take(1).Concat(cands.OrderByDescending(f => TubeLine(B, f).Count)))
                {
                    if (pick.Count >= count) break;
                    if (pick.Any(p => Man(p.X, p.Y, f.X, f.Y) < (tier == 0 ? 3 : 2))) continue;
                    if (tier == 0 && pick.Any(p => TubeLine(B, p).Any(l => l.X == f.X && l.Y == f.Y))) continue;
                    CellAt(B, f.X, f.Y).O = NewObj("tup", 1);
                    if (!ExitSafe(B, before)) { CellAt(B, f.X, f.Y).O = null; continue; }
                    pick.Add(f);
                }
                if (pick.Count == 0 || tier < 2 && pick.Count < count)
                {
                    foreach (var f in pick) CellAt(B, f.X, f.Y).O = null;
                    continue;
                }
                B.Ev.Fuses = pick;
                return true;
            }
            return false;
        }

        /// <summary>Okyanusya: 4–5 karelik düz yağ hattı x 3–5'ten başlar, hedefin bir komşusundan ya da girişten hedefe giden yoldan
        /// geçer; hortum hattın başı.</summary>
        static bool SetupOil(BState B, Pt tg)
        {
            var path = TargetPath(B, tg);
            for (int tier = 0; tier < 3; tier++)
            {
                List<Pt> best = null; double bs = double.NegativeInfinity;
                int maxLen = tier == 0 ? 5 : tier == 1 ? 4 : 3, minLen = tier == 0 ? 4 : tier == 1 ? 3 : 2;
                foreach (var s in AllTiles())
                {
                    if (tier < 2 && (s.X < 3 || s.X > 5)) continue;
                    for (int i = 0; i < 4; i++)
                        for (int len = maxLen; len >= minLen; len--)
                        {
                            var line = Enumerable.Range(0, len).Select(k => new Pt(s.X + Dirs[i][0] * k, s.Y + Dirs[i][1] * k)).ToList();
                            if (!line.All(t => Inb(t.X, t.Y) && CellAt(B, t.X, t.Y).T == "floor" && CellAt(B, t.X, t.Y).O == null && ExitDist(B, t.X, t.Y) >= 2)) continue;
                            bool nearT = line.Any(t => Dist(t, tg) == 1), onPath = line.Any(t => path.Contains(Key(t)));
                            if (tier < 2 && !nearT && !onPath) continue;
                            double sc = (nearT ? 20 : 0) + (onPath ? 10 : 0) + len;
                            if (sc > bs) { bs = sc; best = line; }
                            break;
                        }
                }
                if (best == null) continue;
                B.Ev.Area = best;
                B.Ev.P = best[0];
                return true;
            }
            return false;
        }

        // ---------- Evre sonu ----------
        /// <summary>
        /// Olay adımı: makine evresinin sonunda, uyanmadan sonra, plandan önce (son turda yok). Adım boyunca her hasar olay hasarıdır
        /// (tavan, nesne muafiyeti); hasar tavanı adımın sonunda yeniden sayılır.
        /// </summary>
        static void EventEnd(BState B)
        {
            int t = B.Turn;
            bool was = B.InEvent;
            B.InEvent = true;
            switch (B.Ev.Id)
            {
                case "bulut": EndCloud(B, t); break;
                case "raf": EndShelf(B, t); break;
                case "gaz": EndGas(B, t); break;
                case "tavan": EndCracks(B, t); break;
                case "asansor": EndLift(B, t); break;
                case "kriyo": EndFreeze(B, t); break;
                case "turnike": EndGates(B, t); break;
                case "kaynak": EndArm(B); break;
                case "faz": EndPhases(B, t); break;
                case "suzgec": EndDrain(B, t); break;
                case "tup": EndTubes(B, t); break;
                case "yag": EndOil(B, t); break;
            }
            B.InEvent = was;
            B.Ev.Hurt.Clear();
        }

        static void EndCloud(BState B, int t)
        {
            bool had = false;
            foreach (var p in AllTiles()) { var c = CellAt(B, p.X, p.Y); if (c.Cloud) { c.Cloud = false; had = true; } }
            if (had) AddFx(B, new Fx { K = "ev", Obj = "bulut_dagil" });
            if (t % 2 == 0) return;
            // Dolum sırası A, B, A, B (1., 3., 5., 7. tur sonu)
            bool first = (t - 1) / 2 % 2 == 0;
            var nozzle = first ? B.Ev.P : B.Ev.Q;
            foreach (var p in first ? B.Ev.Area : B.Ev.Area2) CellAt(B, p.X, p.Y).Cloud = true;
            AddFx(B, new Fx { K = "ev", Obj = "bulut", X = nozzle.X, Y = nozzle.Y });
            Say(B, Loc.T("Söndürme memeleri açıldı: bulut bir tur boyunca nişanı kesecek."), "sys");
        }

        static void EndShelf(BState B, int t)
        {
            if (!ShiftAt(B, t)) return;
            var ev = B.Ev;
            var rail = ev.Area;
            int g = ev.Mode, ng = g == 0 ? rail.Count - 1 : 0;
            var close = rail[g];
            // Kayma bugünkü tahtada da kaçış yolunu kesmemeli (kırılan nesne, yıkılan duvar yolları değiştirmiş olabilir)
            var before = ExitSteps(B);
            CellAt(B, close.X, close.Y).T = "wall"; CellAt(B, rail[ng].X, rail[ng].Y).T = "floor";
            bool safe = ExitSafe(B, before);
            CellAt(B, close.X, close.Y).T = "floor"; CellAt(B, rail[ng].X, rail[ng].Y).T = "wall";
            if (!safe)
            {
                AddFx(B, new Fx { K = "ev", Obj = "raf_sikisti", X = close.X, Y = close.Y });
                Say(B, Loc.T("Arşiv rafı sıkıştı: blok bu evre kaymadı."), "sys");
                return;
            }
            var u = UnitAt(B, close.X, close.Y);
            if (u != null)
            {
                Hurt(B, u, 1, Loc.T("arşiv rafı"));
                if (!u.Dead)
                {
                    // Dik yanlar: dikey rayda batı ve doğu, yatay rayda kuzey ve güney; ikisi de boşsa batı ya da kuzey
                    bool vert = rail[0].X == rail[1].X;
                    var side = (vert ? new[] { Dirs[3], Dirs[1] } : new[] { Dirs[0], Dirs[2] })
                        .FirstOrDefault(d => Passable(B, close.X + d[0], close.Y + d[1]) && UnitAt(B, close.X + d[0], close.Y + d[1]) == null);
                    if (side == null)
                    {
                        AddFx(B, new Fx { K = "ev", Obj = "raf_sikisti", X = close.X, Y = close.Y });
                        Say(B, Loc.T("Arşiv rafı sıkıştı: geçitte biri var, blok bu evre kaymadı."), "sys");
                        return;
                    }
                    AddFx(B, new Fx { K = "push", X = u.X, Y = u.Y, Dx = side[0], Dy = side[1], Unit = u.Id });
                    u.X += side[0]; u.Y += side[1];
                    CableHurt(B, u);
                }
            }
            CellAt(B, close.X, close.Y).T = "wall";
            CellAt(B, rail[ng].X, rail[ng].Y).T = "floor";
            ev.Mode = ng;
            AddFx(B, new Fx { K = "ev", Obj = "raf", X = close.X, Y = close.Y, Dx = rail[ng].X, Dy = rail[ng].Y });
            Say(B, Loc.T("Arşiv rafı kaydı: geçit rayın öbür ucuna atladı."), "sys");
        }

        static void EndGas(BState B, int t)
        {
            var ev = B.Ev;
            if (ev.Due != 0)
            {
                // Alev almış bulut boşalma zamanı gelse de boşalmaz, parlar; kompresör sonraki dolum zamanına kadar doldurmaz
                if (ev.Due > t) return;
                AddFx(B, new Fx { K = "ev", Obj = "gaz_parla", X = ev.P.X, Y = ev.P.Y });
                Say(B, Loc.T("Gaz bulutu parladı."), "sys");
                foreach (var p in ev.Area)
                {
                    var u = UnitAt(B, p.X, p.Y);
                    if (u != null && CellAt(B, p.X, p.Y).Gas) Hurt(B, u, 1, Loc.T("parlama"));
                }
                foreach (var p in ev.Area) CellAt(B, p.X, p.Y).Gas = false;
                ev.Due = 0;
                return;
            }
            if (GasVentAt(t) && ev.Area.Any(p => CellAt(B, p.X, p.Y).Gas))
            {
                foreach (var p in ev.Area) CellAt(B, p.X, p.Y).Gas = false;
                AddFx(B, new Fx { K = "ev", Obj = "gaz_bosal", X = ev.P.X, Y = ev.P.Y });
                Say(B, Loc.T("Kompresör gazı boşalttı."), "sys");
            }
            else if (GasFillAt(t))
            {
                foreach (var p in ev.Area) CellAt(B, p.X, p.Y).Gas = true;
                AddFx(B, new Fx { K = "ev", Obj = "gaz_dol", X = ev.P.X, Y = ev.P.Y });
                Say(B, Loc.T("Kompresör çalıştı: yanıcı gaz hedefin önüne yayıldı."), "sys");
            }
        }

        static void EndCracks(BState B, int t)
        {
            foreach (var f in B.Ev.Fuses.Where(f => f.Due <= t).ToList())
            {
                B.Ev.Fuses.Remove(f);
                var c = CellAt(B, f.X, f.Y);
                c.Crack = false;
                var u = UnitAt(B, f.X, f.Y);
                if (u != null)
                {
                    AddFx(B, new Fx { K = "ev", Obj = "tavan", X = f.X, Y = f.Y });
                    Say(B, Loc.F("Tavan çöktü: {0} altında kaldı.", LowerName(UnitTypes[u.Type].Name)), "sys");
                    Hurt(B, u, 1, Loc.T("tavan"));
                    continue;
                }
                if (!Passable(B, f.X, f.Y)) continue;
                // Enkaz bir zemin karesini çıkıştan ya da hedefi girişten koparacaksa kurulmaz, toz dağılır; son turdan önceki evrede
                // çıkış yolu değişmez
                var before = ExitSteps(B);
                c.O = NewObj("enkaz", 2);
                if (t < B.MaxTurns - 1 && ExitSafe(B, before))
                {
                    AddFx(B, new Fx { K = "ev", Obj = "tavan", X = f.X, Y = f.Y });
                    Say(B, Loc.T("Tavan çöktü: enkaz yığını hattı kesti."), "sys");
                }
                else
                {
                    c.O = null;
                    AddFx(B, new Fx { K = "ev", Obj = "toz", X = f.X, Y = f.Y });
                    Say(B, Loc.T("Tavan çöktü, toz dağıldı."), "sys");
                }
            }
        }

        static void EndLift(BState B, int t)
        {
            var ev = B.Ev;
            if (ev.Mode >= 2)
            {
                var dest = ev.Mode == 2 ? ev.Q : ev.P;
                var occ = UnitAt(B, dest.X, dest.Y);
                // Son olay adımında kabin her hâlde iner (yolcu baskının son turunu tahtada oynar)
                bool last = t >= B.MaxTurns - 1;
                if (occ != null)
                {
                    Hurt(B, occ, 1, Loc.T("kabin"));
                    if (UnitAt(B, dest.X, dest.Y) != null && !last)
                    {
                        AddFx(B, new Fx { K = "ev", Obj = "kabin_bekle", X = dest.X, Y = dest.Y });
                        Say(B, Loc.T("Kabin inemedi: durakta biri var, bir evre daha bekleyecek."), "sys");
                        return;
                    }
                }
                var rider = ev.Count != 0 ? ById(B, ev.Count) : null;
                ev.Mode = ev.Mode == 2 ? 1 : 0;
                ev.Count = 0;
                if (rider == null || rider.Dead) { AddFx(B, new Fx { K = "ev", Obj = "kabin_in", X = dest.X, Y = dest.Y }); return; }
                var spot = UnitAt(B, dest.X, dest.Y) == null ? dest : NearestFree(B, dest);
                if (spot == null) { ev.Mode = ev.Mode == 1 ? 2 : 3; ev.Count = rider.Id; return; }
                var at = spot.Value;
                rider.Cab = false; rider.X = at.X; rider.Y = at.Y;
                AddFx(B, new Fx { K = "ev", Obj = "kabin_in", X = at.X, Y = at.Y, Unit = rider.Id });
                Say(B, Loc.F("Kabin indi: {0} öbür durakta.", LowerName(UnitTypes[rider.Type].Name)), "sys");
                return;
            }
            if (!LiftDeparts(B, t)) return;
            var stop = ev.Mode == 0 ? ev.P : ev.Q;
            var u = UnitAt(B, stop.X, stop.Y);
            ev.Mode = ev.Mode == 0 ? 2 : 3;
            AddFx(B, new Fx { K = "ev", Obj = "kabin_kalk", X = stop.X, Y = stop.Y, Unit = u?.Id ?? 0 });
            if (u == null) return;
            u.Cab = true;
            ev.Count = u.Id;
            Say(B, Loc.F("Kabin kalktı: {0} bir evre yolculukta.", LowerName(UnitTypes[u.Type].Name)), "sys");
        }

        static void EndFreeze(BState B, int t)
        {
            // Önceki dondurmanın buzu çözülür (birim sırasını ve bir makine evresini donmuş geçirdi)
            foreach (var u in B.Units) { u.Frozen = false; u.Ice = 0; }
            if (!FreezeAt(B, t)) return;
            AddFx(B, new Fx { K = "ev", Obj = "donma", X = B.Ev.Area[0].X, Y = B.Ev.Area[0].Y });
            bool any = false;
            foreach (var p in B.Ev.Area)
            {
                var u = UnitAt(B, p.X, p.Y);
                if (u == null) continue;
                u.Frozen = true; u.Ice = 1; any = true;
                AddFx(B, new Fx { K = "ev", Obj = "buz", X = p.X, Y = p.Y, Unit = u.Id });
            }
            Say(B, any ? Loc.T("Kriyo hattı boşaldı: bölgedekiler dondu.") : Loc.T("Kriyo hattı boşaldı."), "sys");
        }

        static void EndGates(BState B, int t)
        {
            var ev = B.Ev;
            if (ev.Stop) return;
            // Kaçış başlayınca (kasa açıldı, hedefler kırıldı) o evrenin sonunda tahliye: holde kapalı kalan Teknisyen kaçışı
            // yitirmesin
            if (B.Escape || t >= B.MaxTurns - 2)
            {
                ev.Stop = true; ev.Mode = 1;
                AddFx(B, new Fx { K = "ev", Obj = "turnike", X = ev.Area.Count > 0 ? ev.Area[0].X : 0, Y = ev.Area.Count > 0 ? ev.Area[0].Y : 0 });
                Say(B, Loc.T("Tahliye: turnikeler baskın bitene kadar dışarı döner."), "sys");
                return;
            }
            if (!GateTurnAt(B, t)) return;
            ev.Mode ^= 1;
            AddFx(B, new Fx { K = "ev", Obj = "turnike", X = ev.Area.Count > 0 ? ev.Area[0].X : 0, Y = ev.Area.Count > 0 ? ev.Area[0].Y : 0 });
            Say(B, ev.Mode == 0 ? Loc.T("Vardiya değişti: turnikeler içeri döner.") : Loc.T("Vardiya değişti: turnikeler dışarı döner."), "sys");
        }

        static void EndArm(BState B)
        {
            var ev = B.Ev;
            var w = ArmTile(ev, ev.Mode);
            if (!ArmFaces(B, ev.Mode)) { ev.Mode = ArmTurn(B, ev.Mode, 1 + ev.Count); ev.Count = 0; return; }
            AddFx(B, new Fx { K = "ev", Obj = "kaynak", X = w.X, Y = w.Y });
            var u = UnitAt(B, w.X, w.Y);
            if (u != null)
            {
                Say(B, Loc.F("Kaynak kolu {0} yaktı.", LowerName(UnitTypes[u.Type].Name)), "sys");
                Hurt(B, u, 1, Loc.T("kaynak"));
            }
            ev.Mode = ArmTurn(B, ev.Mode, 1 + ev.Count);
            ev.Count = 0;
        }

        static void EndPhases(BState B, int t)
        {
            int live = LivePhase(t);
            foreach (var u in B.Units.Where(OnBoard).ToList())
            {
                var c = CellAt(B, u.X, u.Y);
                if (c.T == "cable" && c.Phase == live) Hurt(B, u, 1, Loc.T("şebeke"));
            }
            if (LivePhase(t + 1) == live) return;
            AddFx(B, new Fx { K = "ev", Obj = "faz" });
            Say(B, LivePhase(t + 1) == 1 ? Loc.T("Faz değişti: A kanalları canlı, B sönük.") : Loc.T("Faz değişti: B kanalları canlı, A sönük."), "sys");
        }

        static void EndDrain(BState B, int t)
        {
            if (!DrainAt(t)) return;
            var p = B.Ev.P;
            AddFx(B, new Fx { K = "ev", Obj = "emme", X = p.X, Y = p.Y });
            bool any = false;
            // Sıra kuzey, doğu, güney, batı: ilk çekilen boş süzgece girer, sonrakiler süzgeçtekine çarpar
            for (int i = 0; i < 4; i++)
            {
                int nx = p.X + Dirs[i][0], ny = p.Y + Dirs[i][1];
                var u = Inb(nx, ny) ? UnitAt(B, nx, ny) : null;
                if (u == null) continue;
                any = true;
                var occ = UnitAt(B, p.X, p.Y);
                AddFx(B, new Fx { K = "push", X = u.X, Y = u.Y, Dx = -Dirs[i][0], Dy = -Dirs[i][1], Unit = u.Id });
                if (occ != null) { Hurt(B, u, 1, Loc.T("çarpışma")); Hurt(B, occ, 1, Loc.T("çarpışma")); continue; }
                u.X = p.X; u.Y = p.Y;
                CableHurt(B, u);
            }
            Say(B, any ? Loc.T("Süzgeç çekti: çevresindekiler içeri sürüklendi.") : Loc.T("Süzgeç çekti."), "sys");
        }

        static void EndTubes(BState B, int t)
        {
            foreach (var f in B.Ev.Fuses.Where(f => f.Due != 0 && f.Due <= t).ToList())
            {
                if (!B.Ev.Fuses.Contains(f)) continue;
                B.Ev.Fuses.Remove(f);
                if (CellAt(B, f.X, f.Y).O?.K == "tup") CellAt(B, f.X, f.Y).O = null;
                var d = Dirs[f.Dir];
                int x = f.X, y = f.Y;
                for (int k = 1; k < 2 * N; k++)
                {
                    int nx = f.X + d[0] * k, ny = f.Y + d[1] * k;
                    if (!Inb(nx, ny) || CellAt(B, nx, ny).T == "wall")
                    {
                        AddFx(B, new Fx { K = "ev", Obj = "tup_uc", X = f.X, Y = f.Y, Dx = x, Dy = y });
                        Say(B, Loc.T("Tüp uçtu, duvara çarpıp düştü."), "sys");
                        break;
                    }
                    var u = UnitAt(B, nx, ny);
                    if (u != null)
                    {
                        AddFx(B, new Fx { K = "ev", Obj = "tup_uc", X = f.X, Y = f.Y, Dx = nx, Dy = ny });
                        Say(B, Loc.F("Uçan tüp {0} vurdu.", LowerName(UnitTypes[u.Type].Name)), "sys");
                        Hurt(B, u, 1, Loc.T("tüp"));
                        Push(B, u, d);
                        break;
                    }
                    var o = CellAt(B, nx, ny).O;
                    if (o != null)
                    {
                        AddFx(B, new Fx { K = "ev", Obj = "tup_uc", X = f.X, Y = f.Y, Dx = nx, Dy = ny });
                        // Zincir tek halka: çarpılan tüp kırılır, uçmaz; öteki nesnelere bir şey olmaz
                        if (o.K == "tup")
                        {
                            CellAt(B, nx, ny).O = null;
                            B.Ev.Fuses.RemoveAll(o2 => o2.X == nx && o2.Y == ny);
                            AddFx(B, new Fx { K = "break", X = nx, Y = ny, Obj = "tup" });
                            Say(B, Loc.T("Uçan tüp öbür tüpü kırdı."), "sys");
                        }
                        else Say(B, Loc.T("Tüp uçtu, nesneye çarpıp düştü."), "sys");
                        break;
                    }
                    x = nx; y = ny;
                }
            }
        }

        static void EndOil(BState B, int t)
        {
            if (t != 2 || B.Ev.Stop) return;
            B.Ev.Stop = true;
            foreach (var p in B.Ev.Area) CellAt(B, p.X, p.Y).Oil = true;
            AddFx(B, new Fx { K = "ev", Obj = "yag", X = B.Ev.P.X, Y = B.Ev.P.Y });
            Say(B, Loc.T("Hidrolik hortum patladı: yağ yere yayıldı."), "sys");
        }

        // ---------- Makine aklı (yalnız gelişmiş akılda) ----------
        /// <summary>Konum puanı: plan bu evrenin sonundaki olay adımını (B.Turn) bilir; kaynak karesinde, canlı kanalda, çökecek
        /// tavanın altında durmaz.</summary>
        static double EventPos(BState B, Unit e, Pt pos)
        {
            var ev = B.Ev;
            int t = B.Turn;
            if (t >= B.MaxTurns) return 0;
            var type = UnitTypes[e.Type];
            var c = CellAt(B, pos.X, pos.Y);
            switch (ev.Id)
            {
                case "bulut":
                    // Bulutta kimse vurulamaz: bakım robotu hasarlı hedefin yanında bulutta onarır
                    return c.Cloud && type.Kinds.Contains("repair") && TargetsLeft(B).Any(g => g.O.Hp < g.O.Max && Man(g.X, g.Y, pos.X, pos.Y) == 1) ? 2 : 0;
                case "raf": return ShiftAt(B, t) && Same(pos, ev.Area[ev.Mode]) ? -1.5 : 0;
                case "gaz": return ev.Due != 0 && ev.Due <= t && c.Gas ? -2 : 0;
                case "tavan": return ev.Fuses.Any(f => f.Due <= t && f.X == pos.X && f.Y == pos.Y) ? -2 : 0;
                case "asansor":
                    if (ev.Mode >= 2) return Same(pos, ev.Mode == 2 ? ev.Q : ev.P) ? -2 : 0;
                    if (!LiftDeparts(B, t) || !Same(pos, ev.Mode == 0 ? ev.P : ev.Q)) return 0;
                    // A'dan kalkan kabin hedefin dibine iner; B'den kalkan hedeften uzağa götürür
                    return ev.Mode == 0 ? (type.Ranged ? 0.5 : 1.5) : -1.5;
                case "kriyo":
                    if (!FreezeAt(B, t) || !ev.Area.Any(a => Same(a, pos))) return 0;
                    // Atıcılar ve bakım robotu yerinde çalışır (buz kabuğu ödül); yakın dövüşçü ve kalkan dronu yürümek zorunda
                    return type.Kinds.Contains("repair") || type.Ranged && !type.Kinds.Contains("shield") ? 0.5 : -1.5;
                case "kaynak": return Same(pos, ArmTile(ev, ev.Mode)) ? -3 : 0;
                case "faz": return c.T == "cable" && c.Phase == LivePhase(t) ? -3 : 0;
                case "suzgec": return DrainAt(t) && Man(pos.X, pos.Y, ev.P.X, ev.P.Y) == 1 ? (type.Ranged ? -1.5 : -0.5) : 0;
                case "tup":
                    foreach (var f in ev.Fuses) if (f.Due != 0 && f.Due <= t && TubeLine(B, f).Any(a => Same(a, pos))) return -3;
                    return 0;
                case "yag": return c.Oil ? -0.5 : 0;
            }
            return 0;
        }

        /// <summary>Bir planlama turunda önceki makinelerin seçtiği olay tetikleri: aynı tüp, aynı gaz, aynı çatlak bir kez sayılır,
        /// kola planlanan darbeler sonraki makinelerin dönüş hesabına girer.</summary>
        sealed class EventClaims { public int ArmHits; public bool Gas; public HashSet<int> Tubes = new HashSet<int>(), Cracks = new HashSet<int>(); }

        /// <summary>Niyetin değdiği kareler: atışın hattı ve vurduğu kare, havanın düştüğü kare, patlamanın alanı, yakın vuruşun ve
        /// kilidin hedefi. Niyet kimseye ulaşmıyorsa null.</summary>
        static List<Pt> AimTiles(BState B, Unit e, string kind, int dir, int px, int py, out ITarget it)
        {
            it = null;
            if (kind == "blast") return dir == 0 ? BlastArea(px, py) : null;
            if (kind != "shoot" && kind != "melee" && kind != "lob" && kind != "lock") return null;
            it = IntentTarget(B, e, kind, dir, px, py);
            if (it == null || it.None) return null;
            var tiles = new List<Pt>();
            if (kind == "shoot") for (int k = 1; k <= it.K; k++) tiles.Add(new Pt(px + Dirs[dir][0] * k, py + Dirs[dir][1] * k));
            else tiles.Add(new Pt(it.X, it.Y));
            return tiles;
        }

        /// <summary>Niyet puanı: olayı kullanan saldırı (tüpü uzmana uçurmak, gazı uzmanın üstünde tutuşturmak, kolu uzmana
        /// çevirmek, kayma ya da canlı faz öncesi uzmanı kilitlemek). Değerler kendi makinesine eksi. Fitilli sonuç bir sonraki
        /// olay adımında gelir: o adım son tura düşüyorsa puan yok.</summary>
        static double EventAim(BState B, Unit e, string kind, int dir, int px, int py, EventClaims claims)
        {
            var ev = B.Ev;
            int t = B.Turn;
            if (ev.Id != "yag" && t + 1 >= B.MaxTurns) return 0;
            var tiles = AimTiles(B, e, kind, dir, px, py, out var it);
            if (tiles == null) return 0;
            double V(Unit v, double team, double machine) => v == null || v.Id == e.Id ? 0 : v.Side == 'p' ? team : machine;
            switch (ev.Id)
            {
                case "raf":
                    // Kilitlenen uzman sonraki tur yürüyemez: kaymadan önce geçitteki uzmanı kilitle
                    return kind == "lock" && ShiftAt(B, t + 1) && Same(new Pt(it.X, it.Y), ev.Area[ev.Mode]) ? V(UnitAt(B, it.X, it.Y), 2, 0) : 0;
                case "faz":
                {
                    if (kind != "lock") return 0;
                    var c = CellAt(B, it.X, it.Y);
                    return c.T == "cable" && c.Phase == LivePhase(t + 1) ? V(UnitAt(B, it.X, it.Y), 3, 0) : 0;
                }
                case "gaz":
                    if (kind == "melee" || kind == "lock" || ev.Due != 0 || claims.Gas || !tiles.Any(h => Inb(h.X, h.Y) && CellAt(B, h.X, h.Y).Gas)) return 0;
                    return ev.Area.Where(a => CellAt(B, a.X, a.Y).Gas).Sum(a => V(UnitAt(B, a.X, a.Y), 2, -1));
                case "tavan":
                    if (kind != "lob" && kind != "blast") return 0;
                    return tiles.SelectMany(h => CrackNear(B, h.X, h.Y)).Select(Key).Distinct().Where(k => !claims.Cracks.Contains(k))
                        .Sum(k => V(UnitAt(B, k % N, k / N), 2, -1));
                case "kaynak":
                {
                    if (kind == "lock" || !tiles.Any(h => Same(h, ev.P))) return 0;
                    int hits = ev.Count + claims.ArmHits;
                    double W(Pt p) => Inb(p.X, p.Y) ? V(UnitAt(B, p.X, p.Y), 9, -3) : 0;
                    return 0.5 * (W(ArmTile(ev, ArmTurn(B, ev.Mode, 2 + hits))) - W(ArmTile(ev, ArmTurn(B, ev.Mode, 1 + hits))));
                }
                case "tup":
                {
                    if (kind == "lock") return 0;
                    double s = 0;
                    foreach (var f in ev.Fuses)
                    {
                        if (f.Due != 0 || claims.Tubes.Contains(f.Y * N + f.X) || !tiles.Any(h => h.X == f.X && h.Y == f.Y)) continue;
                        var first = TubeLine(B, f).Select(a => UnitAt(B, a.X, a.Y)).FirstOrDefault(v => v != null);
                        s += V(first, 4, -2);
                    }
                    return s;
                }
                case "yag":
                {
                    if (kind == "blast" || kind == "lock") return 0;
                    var v = UnitAt(B, it.X, it.Y);
                    return v != null && v.Side == 'p' && CellAt(B, v.X, v.Y).Oil && SlideCrashes(B, v, Dirs[dir]) ? 2 : 0;
                }
            }
            return 0;
        }

        /// <summary>Seçilen niyetin olay tetiklerini planlama turunun kaydına yazar.</summary>
        static void Claim(BState B, Unit e, string kind, int dir, EventClaims claims)
        {
            var tiles = AimTiles(B, e, kind, dir, e.X, e.Y, out _);
            if (tiles == null || kind == "lock") return;
            var ev = B.Ev;
            if (ev.Id == "kaynak" && tiles.Any(h => Same(h, ev.P))) claims.ArmHits++;
            if (ev.Id == "gaz" && kind != "melee" && tiles.Any(h => Inb(h.X, h.Y) && CellAt(B, h.X, h.Y).Gas)) claims.Gas = true;
            if (ev.Id == "tavan" && (kind == "lob" || kind == "blast")) foreach (var p in tiles.SelectMany(h => CrackNear(B, h.X, h.Y))) claims.Cracks.Add(Key(p));
            if (ev.Id == "tup") foreach (var f in ev.Fuses) if (tiles.Any(h => h.X == f.X && h.Y == f.Y)) claims.Tubes.Add(f.Y * N + f.X);
        }

        // ---------- Sunum ----------
        /// <summary>Bölge olayının sunumu: kare işaretleri, turnike kenarları, hatlar, birim uyarıları ve tur şeridi.</summary>
        public sealed class HazardView
        {
            /// <summary>Olayın kimliği ve görünen adı (olay yoksa null).</summary>
            public string Event, Name;
            public List<HazardMark> Marks = new List<HazardMark>();
            public List<HazardEdge> Edges = new List<HazardEdge>();
            public List<HazardLine> Lines = new List<HazardLine>();
            public List<HazardWarn> Warnings = new List<HazardWarn>();
            /// <summary>Tur şeridi: [tur] o turun sonunda olacak olayın kısa adı (yoksa null; dizin 1..MaxTurns). Şebekede turun
            /// canlı fazı.</summary>
            public string[] Turns = new string[0];
            /// <summary>Bu turun sonunda olacak olayın kısa adı (yoksa null).</summary>
            public string Next;
        }

        /// <summary>
        /// Kare işareti. Kind sunumun katmanı: cloud, cloud_next, nozzle; rail, rail_gap, rail_close, rail_open; gas, gas_next,
        /// gas_lit, leak; crack, crack_due; stop, stop_cab, ghost (Unit: varacak birim); freeze, freeze_next; hol; weld, weld_next;
        /// live, dead; drain, drain_next; tube_hot; oil, oil_next, hose. Label: kısa etiket (çoğu işarette null).
        /// </summary>
        public sealed class HazardMark { public int X, Y, Unit; public string Kind, Label; }
        /// <summary>Turnike kenarı: (X, Y) holün içi, (X2, Y2) dışı. Kind: in (yalnız içeri), out (yalnız dışarı), both. Soon: bu tur
        /// sonunda döner.</summary>
        public sealed class HazardEdge { public int X, Y, X2, Y2; public string Kind; public bool Soon; }
        /// <summary>Hat: tüpün uçuş hattı (tube; ıslık çalan tube_hot), süzgecin çekişi (pull; Hit: süzgeçte biri var, çarpışır).</summary>
        public sealed class HazardLine { public int X, Y, X2, Y2; public string Kind; public bool Hit; }
        /// <summary>Birim uyarısı: bu turun evre sonunda olayın birime yapacağı (Damage: hasar; birim düşecekse Down).</summary>
        public sealed class HazardWarn { public int Unit, Damage; public bool Down; public string Text; }

        /// <summary>Görünen oda adı: olaylı baskında olayın binası (yalnız sunum; harita seçimi aynı).</summary>
        public static string RoomName(BState B) => B.Ev != null ? Loc.T(RegionEvents[B.Ev.Id].Room) : Loc.T(B.Layout);

        /// <summary>
        /// Bu turun evre sonunda olacakların önizlemesi: kopyada gösterilen niyetler sırayla uygulanır, işaretli uyanma kareleri dolu
        /// sayılır (kum fırtınasında sayılmaz), sonra olay adımı işler. RNG çekmez. <paramref name="post"/> olay adımından sonrası.
        /// </summary>
        static BState Forecast(BState B, out BState post)
        {
            var pre = B.Clone();
            pre.FxList.Clear();
            foreach (var e in Alive(pre, 'e').OrderBy(u => u.Order).ToList())
            {
                if (e.Dead || e.Intent == null || pre.Phase != "player") continue;
                var t = IntentTarget(pre, e, e.Intent.Kind, e.Intent.Dir, e.X, e.Y);
                if (t != null && !t.None) ApplyIntent(pre, e, t);
            }
            for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) pre.Cells[y, x].Smoke = false;
            // EndTurn gibi: kaçışta itilen uzman çıkışta tahliye olur; uyanma karesinde duran 1 hasar alır, boş karede makine uyanır
            if (pre.Escape) CheckEscape(pre);
            if (!pre.Fog)
                foreach (var s in pre.Spawns)
                {
                    if (pre.Phase != "player") break;
                    var v = UnitAt(pre, s.X, s.Y);
                    if (v != null) { Hurt(pre, v, 1, Loc.T("uyanmayı engelledi")); continue; }
                    if (CellAt(pre, s.X, s.Y).O == null) pre.Units.Add(NewUnit(pre, "dron", s.X, s.Y));
                }
            pre.Spawns = new List<Pt>();
            post = pre.Clone();
            post.FxList.Clear();
            if (post.Phase == "player" && post.Ev != null && post.Turn < post.MaxTurns) EventEnd(post);
            return pre;
        }

        /// <summary>Bölge olayının işaretleri ve bu turun evre sonu için birim uyarıları (olay yoksa boş).</summary>
        public static HazardView Hazards(BState B)
        {
            var hv = new HazardView();
            if (B.Ev == null) return hv;
            var def = RegionEvents[B.Ev.Id];
            hv.Event = B.Ev.Id;
            hv.Name = Loc.T(def.Name);
            int t = B.Turn;
            bool step = B.Phase == "player" && t < B.MaxTurns;
            BState pre = B, post = null;
            if (step) pre = Forecast(B, out post);
            var ev = pre.Ev;
            void Mark(Pt p, string kind, string label = null, int unit = 0) => hv.Marks.Add(new HazardMark { X = p.X, Y = p.Y, Kind = kind, Label = label, Unit = unit });
            string In(int due) => Math.Max(1, due - t + 1).ToString();
            switch (ev.Id)
            {
                case "bulut":
                {
                    bool fillA = t % 2 == 1 && (t - 1) / 2 % 2 == 0;
                    var cur = AllTiles().Where(p => CellAt(pre, p.X, p.Y).Cloud).ToList();
                    foreach (var p in cur) Mark(p, "cloud");
                    bool next = step && t % 2 == 1;
                    if (next) foreach (var p in fillA ? ev.Area : ev.Area2) Mark(p, "cloud_next");
                    foreach (var (n, area) in new[] { (ev.P, ev.Area), (ev.Q, ev.Area2) })
                    {
                        string label = next && Same(n, fillA ? ev.P : ev.Q) ? Loc.F("BULUT {0}", 1)
                            : cur.Count > 0 && area.Any(a => cur.Any(c => Same(a, c))) && step ? Loc.T("DAĞILIR") : null;
                        Mark(n, "nozzle", label);
                        if (Same(ev.P, ev.Q)) break;
                    }
                    break;
                }
                case "raf":
                {
                    bool shift = step && ShiftAt(pre, t);
                    int other = ev.Mode == 0 ? ev.Area.Count - 1 : 0;
                    for (int i = 0; i < ev.Area.Count; i++)
                    {
                        if (!CellAt(pre, ev.Area[i].X, ev.Area[i].Y).Shelf && i != ev.Mode) continue;
                        if (shift && i == ev.Mode) Mark(ev.Area[i], "rail_close", Loc.F("RAF {0}", 1));
                        else if (shift && i == other) Mark(ev.Area[i], "rail_open");
                        else Mark(ev.Area[i], i == ev.Mode ? "rail_gap" : "rail");
                    }
                    break;
                }
                case "gaz":
                {
                    bool full = ev.Area.Any(p => CellAt(pre, p.X, p.Y).Gas);
                    // Son turda olay adımı yok: o tura kalan parlama söner
                    bool lit = ev.Due != 0 && ev.Due < B.MaxTurns;
                    string label = lit ? Loc.F("PARLAMA {0}", In(ev.Due)) : step && full && GasVentAt(t) ? Loc.T("BOŞALIR")
                        : step && !full && GasFillAt(t) ? Loc.F("DOLUM {0}", 1) : null;
                    Mark(ev.P, "leak", label);
                    if (full) foreach (var p in ev.Area) Mark(p, lit ? "gas_lit" : "gas");
                    else if (step && GasFillAt(t)) foreach (var p in ev.Area) Mark(p, "gas_next");
                    break;
                }
                case "tavan":
                    foreach (var p in AllTiles())
                    {
                        if (!CellAt(pre, p.X, p.Y).Crack) continue;
                        var f = ev.Fuses.FirstOrDefault(x => x.X == p.X && x.Y == p.Y);
                        if (f != null && f.Due < B.MaxTurns) Mark(p, "crack_due", Loc.F("ÇÖKECEK {0}", In(f.Due)));
                        else Mark(p, "crack");
                    }
                    break;
                case "asansor":
                {
                    bool departs = step && LiftDeparts(pre, t);
                    foreach (var (s, name, mode) in new[] { (ev.P, "A", 0), (ev.Q, "B", 1) })
                    {
                        bool dest = step && ev.Mode == (mode == 1 ? 2 : 3);
                        string label = departs && ev.Mode == mode ? Loc.F("KALKIŞ {0}", 1) : dest ? Loc.F("İNİŞ {0}", 1) : name;
                        Mark(s, ev.Mode == mode ? "stop_cab" : "stop", label);
                    }
                    if (departs && UnitAt(pre, (ev.Mode == 0 ? ev.P : ev.Q).X, (ev.Mode == 0 ? ev.P : ev.Q).Y) is Unit rider)
                        Mark(ev.Mode == 0 ? ev.Q : ev.P, "ghost", null, rider.Id);
                    if (ev.Mode >= 2 && ev.Count != 0) Mark(ev.Mode == 2 ? ev.Q : ev.P, "ghost", null, ev.Count);
                    break;
                }
                case "kriyo":
                {
                    bool fire = step && FreezeAt(pre, t);
                    for (int i = 0; i < ev.Area.Count; i++) Mark(ev.Area[i], fire ? "freeze_next" : "freeze", fire && i == 0 ? Loc.F("DONDURMA {0}", 1) : null);
                    break;
                }
                case "turnike":
                {
                    bool evac = pre.Escape || t >= pre.MaxTurns - 2;
                    bool turn = step && !ev.Stop && (GateTurnAt(pre, t) || evac);
                    // Ok yalnız yönü gerçekten değişecekse kesik: dışarı vardiyasında başlayan tahliye yönü değiştirmez
                    bool flips = turn && (!evac || ev.Mode != 1);
                    string label = !step ? null : ev.Stop ? Loc.T("TAHLİYE MODU") : evac ? Loc.F("TAHLİYE {0}", 1) : turn ? Loc.F("VARDİYA {0}", 1) : null;
                    for (int i = 0; i < ev.Area.Count; i++) Mark(ev.Area[i], "hol", i == 0 ? label : null);
                    foreach (var g in HolEdges(pre))
                        hv.Edges.Add(new HazardEdge { X = g.X, Y = g.Y, X2 = g.X2, Y2 = g.Y2, Kind = g.Both ? "both" : ev.Mode == 0 ? "in" : "out", Soon = flips && !g.Both });
                    break;
                }
                case "kaynak":
                    if (step)
                    {
                        var w = ArmTile(ev, ev.Mode);
                        if (Inb(w.X, w.Y)) Mark(w, "weld", Loc.T("KAYNAK"));
                        var n = ArmTile(ev, ArmTurn(pre, ev.Mode, 1 + ev.Count));
                        if (Inb(n.X, n.Y) && !Same(n, w)) Mark(n, "weld_next");
                    }
                    break;
                case "faz":
                {
                    int live = LivePhase(t);
                    foreach (var p in AllTiles())
                    {
                        var c = CellAt(pre, p.X, p.Y);
                        if (c.T == "cable" && c.Phase != 0) Mark(p, c.Phase == live ? "live" : "dead");
                    }
                    break;
                }
                case "suzgec":
                {
                    bool suck = step && DrainAt(t);
                    Mark(ev.P, suck ? "drain_next" : "drain", suck ? Loc.F("EMME {0}", 1) : null);
                    if (suck)
                    {
                        bool full = UnitAt(pre, ev.P.X, ev.P.Y) != null;
                        for (int i = 0; i < 4; i++)
                        {
                            int nx = ev.P.X + Dirs[i][0], ny = ev.P.Y + Dirs[i][1];
                            if (!Inb(nx, ny) || UnitAt(pre, nx, ny) == null) continue;
                            hv.Lines.Add(new HazardLine { X = nx, Y = ny, X2 = ev.P.X, Y2 = ev.P.Y, Kind = "pull", Hit = full });
                            full = true;
                        }
                    }
                    break;
                }
                case "tup":
                    foreach (var f in ev.Fuses)
                    {
                        var line = TubeLine(pre, f);
                        var hitAt = line.FindIndex(p => UnitAt(pre, p.X, p.Y) != null);
                        var end = hitAt >= 0 ? line[hitAt] : line.Count > 0 ? line[line.Count - 1] : new Pt(f.X, f.Y);
                        bool hot = f.Due != 0 && f.Due < B.MaxTurns;
                        hv.Lines.Add(new HazardLine { X = f.X, Y = f.Y, X2 = end.X, Y2 = end.Y, Kind = hot ? "tube_hot" : "tube", Hit = hitAt >= 0 });
                        if (hot) Mark(new Pt(f.X, f.Y), "tube_hot", Loc.F("TÜP {0}", In(f.Due)));
                    }
                    break;
                case "yag":
                    if (ev.Stop) foreach (var p in ev.Area) Mark(p, "oil");
                    else
                    {
                        foreach (var p in ev.Area) if (!Same(p, ev.P)) Mark(p, "oil_next");
                        Mark(ev.P, "hose", t <= 2 ? Loc.F("HORTUM {0}", In(2)) : null);
                    }
                    break;
            }
            hv.Turns = new string[B.MaxTurns + 1];
            for (int i = Math.Max(1, t); i <= B.MaxTurns; i++) hv.Turns[i] = Upcoming(pre, i);
            hv.Next = step ? hv.Turns[t] : null;
            if (post != null) Warn(B, pre, post, hv, Loc.T(def.Tag));
            return hv;
        }

        /// <summary>Tur şeridinin çipi: <paramref name="i"/>. turun sonunda olacak olay (gelecek turlar bugünkü duruma göre).</summary>
        static string Upcoming(BState B, int i)
        {
            var ev = B.Ev;
            if (ev.Id == "faz") return LivePhase(i) == 1 ? "A" : "B";
            if (i >= B.MaxTurns) return null;
            switch (ev.Id)
            {
                // Bulut: dolduğu turlar (dağılması bulutun sonu, tahtada DAĞILIR yazar)
                case "bulut": return i % 2 == 1 ? Loc.T("BULUT") : null;
                case "raf": return ShiftAt(B, i) ? Loc.T("RAF") : null;
                case "gaz": return ev.Due != 0 ? (ev.Due == i ? Loc.T("PARLAMA") : null) : GasFillAt(i) ? Loc.T("DOLUM") : GasVentAt(i) ? Loc.T("BOŞALIR") : null;
                case "tavan": return ev.Fuses.Any(f => f.Due == i) ? Loc.T("ÇÖKME") : null;
                case "asansor":
                {
                    // Bekleme olmadan kabinin takvimi
                    int mode = ev.Mode;
                    for (int k = B.Turn; k <= i; k++)
                    {
                        string what = null;
                        if (mode >= 2) { what = Loc.T("İNİŞ"); mode = mode == 2 ? 1 : 0; }
                        else if (mode < 2 && k % 2 == 0 && k >= 2 && k <= B.MaxTurns - 2) { what = Loc.T("KALKIŞ"); mode = mode == 0 ? 2 : 3; }
                        if (k == i) return what;
                    }
                    return null;
                }
                case "kriyo": return FreezeAt(B, i) ? Loc.T("DONDURMA") : null;
                case "turnike": return ev.Stop ? null : B.Escape ? (i == B.Turn ? Loc.T("TAHLİYE MODU") : null)
                    : i == B.MaxTurns - 2 ? Loc.T("TAHLİYE MODU") : GateTurnAt(B, i) ? Loc.T("VARDİYA") : null;
                // Kaynak her evre işler: şeritte işaret bilgi taşımaz (tahtada KAYNAK karesi)
                case "kaynak": return null;
                case "suzgec": return DrainAt(i) ? Loc.T("EMME") : null;
                case "tup": return ev.Fuses.Any(f => f.Due == i) ? Loc.T("TÜP") : null;
                case "yag": return i == 2 && !ev.Stop ? Loc.T("HORTUM") : null;
            }
            return null;
        }

        /// <summary>Birim uyarıları: olay adımından önceki ve sonraki kopyanın farkı (tahtadaki birimler).</summary>
        static void Warn(BState B, BState pre, BState post, HazardView hv, string tag)
        {
            foreach (var u in B.Units.Where(OnBoard))
            {
                var a = ById(pre, u.Id);
                var b = ById(post, u.Id);
                if (a == null || b == null || !OnBoard(a)) continue;
                int dmg = a.Hp - b.Hp;
                bool moved = !b.Cab && (a.X != b.X || a.Y != b.Y);
                bool soaked = post.FxList.Any(f => f.Unit == u.Id && (f.K == "ice" || f.K == "plate"));
                string text = b.Cab ? Loc.T("KABİN: biner, bir evre tahtada yok")
                    : b.Dead ? Loc.F("{0}: düşer", tag)
                    : dmg > 0 && moved ? Loc.F("{0}: {1} hasar ve itilir", tag, dmg)
                    : dmg > 0 ? Loc.F("{0}: {1} hasar", tag, dmg)
                    : soaked ? Loc.F("{0}: buz ya da zırh karşılar", tag)
                    : moved ? Loc.F("{0}: yer değiştirir", tag)
                    : b.Frozen ? Loc.T("DONACAK: yürüyemez, buz 1 hasar emer")
                    : null;
                if (text != null) hv.Warnings.Add(new HazardWarn { Unit = u.Id, Damage = Math.Max(0, dmg), Down = b.Dead, Text = text });
            }
        }

        /// <summary>Yürüyüş önizlemesi: birim (x, y)'ye yürürse olayın işaretleri ve evre sonunda ona yapacağı (olay yoksa boş).</summary>
        public static HazardView PreviewMove(BState B, int uid, int x, int y)
        {
            if (B.Ev == null) return new HazardView();
            var C = B.Clone();
            var u = ById(C, uid);
            if (u == null || !Inb(x, y)) return new HazardView();
            u.X = x; u.Y = y; u.Moved = true;
            return Hazards(C);
        }

        /// <summary>İmzanın olay eki (RaidEngineTests): durum, kare bayrakları, fitiller ve olay hasarı.</summary>
        static string EventSignature(BState B)
        {
            var ev = B.Ev;
            var cells = new List<string>();
            foreach (var p in AllTiles())
            {
                var c = CellAt(B, p.X, p.Y);
                string f = (c.Cloud ? "b" : "") + (c.Gas ? "g" : "") + (c.Crack ? "k" : "") + (c.Oil ? "y" : "");
                if (f.Length > 0) cells.Add($"{p.X}{p.Y}{f}");
            }
            return $"{ev.Id}:{ev.Mode},{ev.Count},{ev.Due}{(ev.Stop ? "s" : "")}|" + string.Join(",", cells)
                + (ev.Fuses.Count > 0 ? "|f" + string.Join(",", ev.Fuses.Select(f => $"{f.X}{f.Y}:{f.Due}")) : "")
                + $"|d{B.EvDamageTeam}/{B.EvDamageMachines}";
        }
    }
}
