using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.EditorTools;
using Corrigible.Game;
using Corrigible.Game.Raid;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Revizyon 2'nin tam görüntü seti (Aşama 10): referans ekranlarının (<c>reference/screens/S*.jpg</c>) oyundaki karşılıkları,
    /// Overlay dahil 1920×1080. Durumlar fikstürlerden ve oyunun kendi komutlarından kurulur; oyunun kaydı değişir (önce yedekle).
    /// <list type="bullet">
    /// <item><see cref="Start"/> (DeskGame, Play): S1a V.001 + eğitim notu · S1b V.031 + kapıda bekleyen ziyaretçi · S2 görev fişi ·
    /// S3 sığınak (atölye seçili) · S4a gözetleme · S4b açık kapı · S4c VURULDU · S5a vardiya raporu · S5b karşı saldırı ·
    /// S7a/S7c başarılı baskının kartı ve raporu · S7b/S7d geri çekilme · S8a kazanılan harekât · S8b Dağılma.</item>
    /// <item><see cref="StartRaid"/> (Baskin tek başına, Play): S6a plan (Teknisyen seçili) · S6b makine evresi · S6c son tur;
    /// ikmal baskını, tohum 914, DAR KORİDOR, kilitleyici imzası.</item>
    /// </list>
    /// Çıktı <see cref="OutDir"/>: TR <c>S*.png</c>, EN <c>S*_EN.png</c>, TR koşusunun sonunda gri kopyalar <c>S*_gray.png</c>
    /// (renk körlüğü denetimi). İlerleme <c>Temp/capture_set.txt</c>'de.
    /// </summary>
    public static class SiginakCaptureSet
    {
        public const string OutDir = "Docs/UI_Siginak_Rev2/screens_unity_rev2";
        const string StatusPath = "Temp/capture_set.txt";
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [MenuItem("Corrigible/Sığınak/Tam görüntü seti (TR, Play)")]
        static void MenuTr() => Debug.Log(Start("tr"));

        [MenuItem("Corrigible/Sığınak/Tam görüntü seti (EN, Play)")]
        static void MenuEn() => Debug.Log(Start("en"));

        [MenuItem("Corrigible/Sığınak/Baskın görüntüleri (TR, Baskin tek başına)")]
        static void MenuRaidTr() => Debug.Log(StartRaid("tr"));

        [MenuItem("Corrigible/Sığınak/Baskın görüntüleri (EN, Baskin tek başına)")]
        static void MenuRaidEn() => Debug.Log(StartRaid("en"));

        [MenuItem("Corrigible/Sığınak/Gri kopyalar")]
        static void MenuGray() => Debug.Log(Gray());

        /// <summary>DeskGame Play'de masa, kapı, rapor ve son rapor görüntüleri.</summary>
        public static string Start(string lang)
        {
            if (!Application.isPlaying) return "Play gerekli (DeskGame)";
            var game = Object.FindFirstObjectByType<GameController>();
            if (game?.Sim == null) return "Kurtuluş oyunu yok";
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(StatusPath, "başladı\n");
            game.StartCoroutine(Run(game, lang == "en"));
            return "başladı: " + lang + " · " + StatusPath;
        }

        /// <summary>Baskin tek başına Play'de S6a, S6b, S6c.</summary>
        public static string StartRaid(string lang)
        {
            if (!Application.isPlaying) return "Play gerekli (Baskin tek başına)";
            var src = Object.FindFirstObjectByType<LiveRaidDataSource>();
            var hud = Object.FindFirstObjectByType<RaidHudView>();
            if (src == null || hud == null) return "Baskın tahtası yok";
            Directory.CreateDirectory(OutDir);
            File.WriteAllText(StatusPath, "başladı\n");
            hud.StartCoroutine(RunRaid(src, lang == "en"));
            return "başladı: " + lang + " · " + StatusPath;
        }

        /// <summary>TR görüntülerinin gri tonlu kopyaları (<c>S*_gray.png</c>).</summary>
        public static string Gray()
        {
            if (!Directory.Exists(OutDir)) return "Görüntü yok: " + OutDir;
            int n = 0;
            foreach (var path in Directory.GetFiles(OutDir, "S*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.EndsWith("_EN") || name.EndsWith("_gray")) continue;
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                try
                {
                    tex.LoadImage(File.ReadAllBytes(path));
                    var px = tex.GetPixels32();
                    for (int i = 0; i < px.Length; i++)
                    {
                        byte g = (byte)Mathf.RoundToInt(((Color)px[i]).grayscale * 255);
                        px[i] = new Color32(g, g, g, 255);
                    }
                    tex.SetPixels32(px);
                    File.WriteAllBytes(Path.Combine(OutDir, name + "_gray.png"), tex.EncodeToPNG());
                    n++;
                }
                finally { Object.DestroyImmediate(tex); }
            }
            return n + " gri kopya · " + OutDir;
        }

        static IEnumerator Wait(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

        static object Call(object target, string method) =>
            target.GetType().GetMethod(method, Any, null, Type.EmptyTypes, null)?.Invoke(target, null);

        static void Raise(GameController game, string evt) =>
            (typeof(GameController).GetField(evt, Any)?.GetValue(game) as Action)?.Invoke();

        sealed class Log
        {
            readonly StringBuilder sb = new StringBuilder();
            readonly string suffix;
            public Log(bool en) { suffix = en ? "_EN" : ""; }
            public void Line(string s) { sb.AppendLine(s); File.WriteAllText(StatusPath, sb.ToString()); }
            public void Shot(string name) => Line(name + suffix + " → " + CorrigibleDebug.Capture(Path.Combine(OutDir, name + suffix + ".png")));
        }

        static IEnumerator Run(GameController game, bool en)
        {
            var log = new Log(en);
            var surface = Object.FindFirstObjectByType<LiberationDeskView>();
            var rig = Object.FindFirstObjectByType<SiginakCameraRig>();
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var shelter = Object.FindFirstObjectByType<SiginakShelter>();
            var door = Object.FindFirstObjectByType<SiginakDoor>();
            var shift = Object.FindFirstObjectByType<SiginakShift>();
            var op = Object.FindFirstObjectByType<SiginakOperation>(FindObjectsInactive.Include);
            var briefing = Object.FindFirstObjectByType<SiginakBriefing>();
            var card = Object.FindFirstObjectByType<DoorCardView>(FindObjectsInactive.Include);
            var startLang = DeskLoc.Instance.Lang;
            bool tutorialDone = SiginakTutorial.Done;
            DeskLoc.Instance.SetLanguage(en ? DeskLang.En : DeskLang.Tr);
            // Editördeki gerçek imleç görüntüye üzerine gelme etiketi düşürmesin: odanın fizik ışını kapalı
            var physics = Camera.main != null ? Camera.main.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() : null;
            bool physicsWas = physics != null && physics.enabled;
            if (physics != null) physics.enabled = false;

            IEnumerator Fresh(string fixture)
            {
                game.NewGame();
                yield return Wait(0.3f);
                log.Line(SiginakFixtures.Load(fixture));
                yield return Wait(1.2f);
                Settle();
                yield return Wait(0.6f);
            }
            // Kapıda bekleyen ziyaretçi ve sıradaki vuruş görüntüye karışmasın
            void Settle()
            {
                if (game.Sim.DoorVisitor != null) Call(door, "Close");
                game.Sim.S.Door.Visit = null;
                game.Sim.S.Door.NextKnock = game.Sim.S.Day + 5;
                Raise(game, "Changed");
            }
            void Knock(string id)
            {
                game.Sim.S.Door.Visit = new DoorVisit { Day = game.Sim.S.Day, Card = id };
                Raise(game, "Changed");
            }

            // S1a · V.001: açılış belgesinde BAŞLA, masa pozu, eğitim notu ADIM 1
            SiginakTutorial.Done = false;
            game.NewGame();
            yield return Wait(0.3f);
            briefing.Skip();
            yield return Wait(1.0f);
            for (int i = 0; i < 6 && surface.InputBlocked; i++) { Call(briefing, "Action"); yield return Wait(1.6f); }
            rig.Focus(SiginakPose.Desk, true);
            yield return Wait(1.2f);
            log.Shot("S1a");
            SiginakTutorial.Done = true;

            // S1b · V.031: ziyaretçi kapıda bekliyor (kâğıt kapatıldı), masa pozu
            yield return Fresh("V031");
            Knock("anne");
            yield return Wait(3.0f);
            Call(door, "Close");
            yield return Wait(1.6f);
            rig.Focus(SiginakPose.Desk, true);
            yield return Wait(1.2f);
            log.Shot("S1b");

            // S2 · V.014: görev fişi (Doğu Avrupa)
            yield return Fresh("V014");
            map.ShowMissions();
            yield return Wait(1.8f);
            log.Shot("S2");
            map.HandleEscape();
            yield return Wait(0.6f);

            // S3 · sığınak pozu, atölye seçili
            rig.Focus(SiginakPose.Shelf, true);
            shelter.Select(ShelterLogic.Workshop);
            yield return Wait(1.4f);
            log.Shot("S3");
            rig.Focus(SiginakPose.Desk, true);
            yield return Wait(0.6f);

            // S4a · alt kattan bir anne: gözetleme deliğinden bakıldı; S4b · kapı açık (revirde görevli yok)
            game.Sim.Shelter.Room(ShelterLogic.Infirmary).Workers = 0;
            Knock("anne");
            yield return Wait(2.8f);
            card.PressKey(0);
            yield return Wait(0.9f);
            log.Shot("S4a");
            card.PressKey(2);
            yield return Wait(2.4f);
            log.Shot("S4b");

            // S4c · silahlı sakin: kapıyı aç → VURULDU
            yield return Fresh("V014");
            Knock("silahli");
            yield return Wait(3.0f);
            card.PressKey(3);
            yield return Wait(2.6f);
            log.Shot("S4c");

            // S5a · V.014 → 015 vardiya raporu; S5b · karşı saldırılı rapor
            yield return Fresh("V014");
            Call(shift, "Throw");
            yield return Wait(4.8f);
            log.Shot("S5a");
            Call(shift, "File");
            yield return Wait(1.8f);
            yield return Fresh("V014");
            game.Sim.S.Alarm = game.Sim.B.liberationAlarmThreshold - game.Sim.AlarmPerShift;
            Call(shift, "Throw");
            yield return Wait(5.0f);
            log.Shot("S5b");
            Call(shift, "File");
            yield return Wait(1.8f);

            // S7a, S7c · Doğu Avrupa 02 geri alındı; S7b, S7d · geri çekilme
            foreach (bool win in new[] { true, false })
            {
                yield return Fresh("V014");
                var r = game.StartLiberationRaid("eeu_2");
                log.Line("baskın emri: " + r.Ok + " " + r.Msg);
                LiveRaidDataSource src = null;
                for (int i = 0; i < 80 && (src == null || src.State == null); i++)
                {
                    yield return Wait(0.25f);
                    src = Object.FindFirstObjectByType<LiveRaidDataSource>();
                }
                if (src?.State == null) { log.Line("baskın açılmadı"); break; }
                yield return Wait(1.5f);
                if (win) Win(src);
                else
                {
                    src.EndTurn(); yield return Wait(0.3f); src.EndTurn(); yield return Wait(0.5f);
                    for (int i = 0; i < 40 && (src.Busy || src.Executing); i++) yield return Wait(0.5f);
                    src.Retreat(); yield return Wait(0.2f); src.Retreat();
                }
                yield return Wait(1.3f);
                log.Shot(win ? "S7a" : "S7b");
                src.Continue();
                for (int i = 0; i < 40 && RaidLauncher.IsRaidLoaded; i++) yield return Wait(0.25f);
                yield return Wait(2.6f);
                log.Shot(win ? "S7c" : "S7d");
                Call(op, "File");
                yield return Wait(1.0f);
            }

            // S8a · kazanılan harekât (karşı saldırılı); S8b · Dağılma (29 vardiya)
            yield return Fresh("V014");
            Campaign(game.Sim);
            Raise(game, "Changed");
            yield return Wait(4.8f);
            log.Shot("S8a");
            yield return Fresh("V014");
            Disband(game.Sim);
            Raise(game, "Changed");
            yield return Wait(4.8f);
            log.Shot("S8b");

            SiginakTutorial.Done = tutorialDone;
            DeskLoc.Instance.SetLanguage(startLang);
            if (physics != null) physics.enabled = physicsWas;
            if (!en) log.Line(Gray());
            log.Line("bitti");
        }

        /// <summary>Tahtayı kazanılmış hâle getirir (yalnız görüntü): Teknisyen hedefin yanında, hedef alınmış.</summary>
        static void Win(RaidDataSource src)
        {
            var st = src.State;
            int cx = -1, cy = -1;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    if (st.Cells[y, x].O != null && Baskin.IsTarget(st.Cells[y, x].O.K)) { cx = x; cy = y; }
            if (cx >= 0)
            {
                var tek = st.Units.First(u => u.Type == "tek");
                tek.X = cx > 0 ? cx - 1 : cx + 1;
                tek.Y = cy;
                st.Cells[cy, cx].O = null;
            }
            st.Phase = "won";
            st.Turn = 6;
            // Görünüm durumu yeniden kurulsun
            var l = DeskLoc.Instance;
            var cur = l.Lang;
            l.SetLanguage(cur == DeskLang.En ? DeskLang.Tr : DeskLang.En);
            l.SetLanguage(cur);
        }

        /// <summary>V.014'ten bütün bölgeler: her sektör ilk baskında alınır, güven tavanda tutulur; vardiya 30'da karşı saldırı.</summary>
        static void Campaign(Simulation sim)
        {
            for (int guard = 0; sim.S.Ending == null && guard < 400; guard++)
            {
                var region = sim.LiberationRegion(sim.S.Liberation.CurrentRegion);
                if (region != null && region.Completed)
                {
                    var next = sim.LiberationFrontier.FirstOrDefault();
                    if (next == null) break;
                    sim.SelectLiberationRegion(next);
                    continue;
                }
                while (!sim.S.Teams.Any(Simulation.TeamReady) && sim.S.Ending == null) { sim.S.Trust = sim.TrustMax; sim.Step(); }
                if (sim.S.Ending != null) break;
                if (!sim.StartLiberationRaid(sim.NextLiberationSector(sim.S.Liberation.CurrentRegion).Id).Ok) { sim.Step(); continue; }
                sim.ResolveRaid(new RaidResult { Success = true, Turns = 5 });
                sim.Step();
                sim.S.Trust = sim.TrustMax;
                if (sim.S.Day == 30) sim.S.Alarm = sim.B.liberationAlarmThreshold - 0.5;
            }
            sim.DrainEvents();
        }

        /// <summary>V.014'ten vardiya 29: son vardiyada karşı saldırı son güveni sıfırlar.</summary>
        static void Disband(Simulation sim)
        {
            while (sim.S.Day < 27 && sim.S.Ending == null) { sim.S.Alarm = 0; sim.Step(); }
            sim.S.Trust = 1;
            sim.S.Alarm = sim.B.liberationAlarmThreshold;
            sim.Step();
            sim.DrainEvents();
        }

        static IEnumerator RunRaid(LiveRaidDataSource src, bool en)
        {
            var log = new Log(en);
            var startLang = DeskLoc.Instance.Lang;
            DeskLoc.Instance.SetLanguage(en ? DeskLang.En : DeskLang.Tr);
            yield return Wait(0.5f);
            var p = new RaidParams
            {
                Seed = 914, Region = "eeu", RegionName = GameData.Region("eeu").Name, MissionKind = "loot", Tier = 2, Compute = 3, RaidNumber = 2,
                Traits = new System.Collections.Generic.List<string> { "dar" }, Signature = "kilit",
            };
            src.OpenForValidation(Baskin.Create(p));
            yield return Wait(1.0f);
            src.Select('T');
            yield return Wait(0.8f);
            log.Shot("S6a");
            src.EndTurn(); yield return Wait(0.3f); src.EndTurn();
            yield return Wait(1.1f);
            log.Shot("S6b");
            for (int i = 0; i < 40 && (src.Busy || src.Executing); i++) yield return Wait(0.5f);
            src.State.Turn = src.State.MaxTurns;
            src.Select('T'); yield return Wait(0.3f);
            src.Arm();
            yield return Wait(0.8f);
            log.Shot("S6c");
            DeskLoc.Instance.SetLanguage(startLang);
            if (!en) log.Line(Gray());
            log.Line("bitti");
        }
    }
}
