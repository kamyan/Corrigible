using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using Corrigible.EditorTools;
using Corrigible.Game;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Tam görüntü seti (PROMPT §7.2 “Capture Poses”, Aşama 10): Play'de, yeni görünüm açıkken bütün pozları durumlarıyla
    /// kurar ve 1920×1080 çeker (Overlay dahil): K7 açılış belgesi, V.014'te K1 · K2 (görev fişi) · K4 · K5a · K5b (CRT geçişi),
    /// V.019'da K3 (özgürleşme + aday seçimi), V.014 → 015'te K6a ve K6b. Çıktı <c>screens_unity/K*.png</c> (TR) ve
    /// <c>K*_EN.png</c>. Durumlar fikstürlerden (oyunun kaydı değişir: önce yedekle). İlerleme Temp/capture_set.txt'de.
    /// </summary>
    public static class SiginakCaptureSet
    {
        const string StatusPath = "Temp/capture_set.txt";

        [MenuItem("Corrigible/Sığınak/Tam görüntü seti (TR, Play)")]
        static void MenuTr() => Debug.Log(Start("tr"));

        [MenuItem("Corrigible/Sığınak/Tam görüntü seti (EN, Play)")]
        static void MenuEn() => Debug.Log(Start("en"));

        public static string Start(string lang)
        {
            if (!Application.isPlaying) return "Play gerekli (yeni görünüm açık)";
            var game = Object.FindFirstObjectByType<GameController>();
            if (game?.Sim == null) return "Kurtuluş oyunu yok";
            if (Object.FindFirstObjectByType<SiginakBriefing>() == null) return "Yeni görünüm kapalı";
            File.WriteAllText(StatusPath, "başladı\n");
            game.StartCoroutine(Run(game, lang == "en"));
            return "başladı: " + lang + " · " + StatusPath;
        }

        static IEnumerator Run(GameController game, bool en)
        {
            var log = new StringBuilder();
            string suffix = en ? "_EN" : "";
            void Shot(string name)
            {
                string path = Path.Combine(SiginakCapture.OutDir, name + suffix + ".png");
                log.AppendLine(name + suffix + " → " + CorrigibleDebug.Capture(path));
                File.WriteAllText(StatusPath, log.ToString());
            }
            static IEnumerator Wait(float seconds) { yield return new WaitForSecondsRealtime(seconds); }

            var briefing = Object.FindFirstObjectByType<SiginakBriefing>();
            var rig = Object.FindFirstObjectByType<SiginakCameraRig>();
            var map = Object.FindFirstObjectByType<SiginakMap>();
            var layer = Object.FindFirstObjectByType<LiberationMapLayer>();
            var crt = Object.FindFirstObjectByType<CrtTransition>(FindObjectsInactive.Include);
            var lever = Object.FindFirstObjectByType<ShiftSwitch>();
            var source = Object.FindFirstObjectByType<SimDeskDataSource>();
            var startLang = DeskLoc.Instance.Lang;
            DeskLoc.Instance.SetLanguage(en ? DeskLang.En : DeskLang.Tr);
            // Editördeki gerçek imleç görüntüye üzerine gelme etiketi düşürmesin: odanın fizik ışını kapalı
            var physics = Camera.main != null ? Camera.main.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() : null;
            bool physicsWas = physics != null && physics.enabled;
            if (physics != null) physics.enabled = false;

            // K7: açılış ve emir belgesi
            briefing.OpenOpening();
            yield return Wait(2.6f);
            Shot("K7");

            // V.014: K1, K2, K4, K5a, K5b
            game.NewGame();
            yield return null;
            log.AppendLine(SiginakFixtures.Load("V014"));
            yield return Wait(0.6f);
            rig.Focus(SiginakPose.Desk, true);
            yield return Wait(0.4f);
            Shot("K1");
            map.ShowMissions();
            yield return Wait(1.0f);
            Shot("K2");
            rig.Focus(SiginakPose.Shelf);
            yield return Wait(1.0f);
            Shot("K4");
            rig.Focus(SiginakPose.Crt);
            yield return Wait(1.0f);
            Shot("K5a");
            crt.BeginApproach(0);
            var run = game.StartCoroutine(crt.Run(CrtViewBuilder.Transition(game.Sim)));
            yield return Wait(1.95f);
            Shot("K5b");
            game.StopCoroutine(run);
            crt.Hide();

            // V.019: Doğu Avrupa özgürleşir (K3 dizisi), sonra figür ilk adaya taşınır (onay fişi)
            rig.Focus(SiginakPose.Map, true);
            log.AppendLine(SiginakFixtures.Load("V019"));
            // Baskın sonucu modalı açılmasın: rapor okunmuş sayılır
            source.State.RaidLogSeen = game.Sim.S.RaidLog.Count;
            yield return Wait(3.0f);
            var candidate = map.State?.Regions.FirstOrDefault(r => r.Kind == MapRegionKind.Candidate);
            if (candidate != null)
            {
                var drop = typeof(SiginakMap).GetMethod("Dropped", BindingFlags.Instance | BindingFlags.NonPublic);
                drop.Invoke(map, new object[] { layer.MapToWorld(layer.TeamAnchor(candidate.Id)) });
            }
            yield return Wait(1.0f);
            Shot("K3");

            // V.014 → 015: baskın başarısız, şalter iki adım
            game.NewGame();
            yield return null;
            log.AppendLine(SiginakFixtures.Load("K6"));
            yield return Wait(0.6f);
            rig.Focus(SiginakPose.Desk, true);
            yield return Wait(0.3f);
            lever.Press();
            yield return Wait(0.4f);
            lever.Press();
            yield return Wait(0.85f);
            Shot("K6a");
            yield return Wait(2.4f);
            Shot("K6b");

            DeskLoc.Instance.SetLanguage(startLang);
            if (physics != null) physics.enabled = physicsWas;
            log.AppendLine("bitti");
            File.WriteAllText(StatusPath, log.ToString());
        }
    }
}
