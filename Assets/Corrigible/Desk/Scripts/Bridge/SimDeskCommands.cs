using System.Globalization;
using System.Linq;
using Corrigible.Core;
using Corrigible.Game;
using Corrigible.Game.Audio;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Masanın niyetlerini oyuna iletir (F8): kuralı <see cref="GameController"/>/Simulation uygular. Sonuç mesajları
    /// (başarısızlık nedeni, olay sonucu) masanın uyarı kuyruğuna girer. Ses yönetmeni eski masadaki gibi haberdar edilir.
    /// </summary>
    public sealed class SimDeskCommands : MonoBehaviour, IDeskCommands
    {
        [SerializeField] SimDeskDataSource source;
        [Tooltip("Eğitim yürütücüsü (F8.4): “Tutorial:…” komutları.")]
        [SerializeField] DeskTutorialRunner tutorial;

        GameController Game => source.Game;
        Simulation Sim => source.Game.Sim;
        DeskBridgeState State => source.State;

        void Report(ActResult r, string okKind = "ok")
        {
            if (r == null) return;
            if (!r.Ok) Fail(r.Msg ?? Loc.T("Yapılamadı."));
            else if (r.Msg != null) State.Push(r.Msg, Sim.S.Day, okKind, source.Builder.TimeNow);
            source.MarkDirty();
        }

        void Fail(string msg) { State.Push(msg, Sim.S.Day, "fail"); source.MarkDirty(); }

        public void SelectRegion(string regionId)
        {
            if (regionId != null && regionId != State.Selected) AudioDirector.Instance?.RegionSelected(regionId);
            State.Selected = regionId;
            source.MarkDirty();
        }

        public void ToggleWindow(WindowId id)
        {
            var S = Sim.S;
            if (id == WindowId.AiLog) State.LogSeen = S.Log.Count;
            else if (id == WindowId.News) State.NewsSeen = S.Seq;
            else if (id == WindowId.Teams) State.TeamsBadge = false;
            source.MarkDirty();
        }

        public void ExecuteRegionAction(string regionId, int index)
        {
            if (index < 0 || index >= Actions.All.Length || regionId == null) return;
            string action = Actions.All[index];
            var r0 = Sim.S.Region(regionId);
            bool q = r0.Q, mon = r0.Monitored;
            var res = Game.Act(action, regionId);
            AudioDirector.Instance?.ActionResult(action, regionId, res, q, mon);
            Report(res);
        }

        /// <summary>B: bölgede ekip varsa baskın emri (onaylanınca taktik harita), yoksa en yakın hazır ekibi gönderir.</summary>
        public void LaunchFieldOperation(string regionId)
        {
            if (!Sim.TeamsOn || regionId == null) return;
            if (Sim.TeamsAt(regionId).Any())
            {
                var why = Sim.RaidCheck(regionId);
                if (why != null) { Fail(why); return; }
                State.RaidBrief = regionId;
                source.MarkDirty();
                return;
            }
            var team = source.Builder.NearestReadyTeam(regionId);
            if (team == null) { Fail(Loc.T("Hazır ekip yok.")); return; }
            Report(Game.MoveTeam(team.Id, regionId));
        }

        public void StartResearch(string projectId) => Report(Game.StartResearch(projectId));

        /// <summary>Oyunda araştırma duraklatılamaz; düğme gizli (CanPause = false).</summary>
        public void PauseResearch(string projectId) { }

        public void SetEmergencyFund(int option)
        {
            if (option < 0 || option >= Simulation.ResearchReserves.Length) return;
            var r = Game.SetResearchReserve(Simulation.ResearchReserves[option]);
            if (!r.Ok) Report(r); else source.MarkDirty();
        }

        public void TogglePause()
        {
            if (Sim.S.Ending != null) return;
            int last = State.LastSpeed == "4x" ? 4 : State.LastSpeed == "2x" ? 2 : 1;
            Game.SetSpeed(Game.Speed == 0 ? last : 0);
        }

        public void SetSpeed(GameSpeed speed)
        {
            if (Sim.S.Ending != null) return;
            Game.SetSpeed(speed switch { GameSpeed.X1 => 1, GameSpeed.X2 => 2, GameSpeed.X4 => 4, _ => 0 });
        }

        /// <summary>Danışman notunu kapatır (uyarılar fişte, kendiliğinden solar).</summary>
        public void DismissAdvisor() => Game.DismissAdvice();

        public void RunAdvisorAction(string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            if (command == "DismissAdvisor") DismissAdvisor();
            else if (command == "ShowReport") { State.Review = false; source.MarkDirty(); }
            else if (command == "Restart") Restart();
            else if (command.StartsWith("FocusRegion:")) SelectRegion(command.Substring("FocusRegion:".Length));
            else if (command.StartsWith("SetSpeed:"))
            {
                var sp = command.Substring("SetSpeed:".Length);
                SetSpeed(sp == "4x" ? GameSpeed.X4 : sp == "2x" ? GameSpeed.X2 : sp == "pause" ? GameSpeed.Pause : GameSpeed.X1);
            }
        }

        public void ConfirmGlobalKillSwitch()
        {
            AudioDirector.Instance?.GlobalKillArmed();
            Report(Game.GlobalKillSwitch());
        }

        /// <summary>Olay kartı ya da hediye teklifi: seçenek sırası 1/2/3.</summary>
        public void SignEventOption(int index)
        {
            var S = Sim.S;
            if (S.PendingGift != null)
            {
                if (index < 0 || index >= SimDeskBuilder.GiftChoices.Length) return;
                string choice = SimDeskBuilder.GiftChoices[index];
                var r = Game.ResolveGift(choice);
                if (r.Ok) AudioDirector.Instance?.GiftResolved(choice);
                Report(r);
                return;
            }
            if (S.PendingEvent != null) Report(Game.ResolveEvent(index), "info");
        }

        /// <summary>Ana menü: aynı rakiple yeni oyun; giriş dosyası açılır (yeni oyunda her zaman).</summary>
        public void ReturnToMainMenu() => Game.NewGame(Game.TypeId);

        /// <summary>Rapor kapanır; masa oyun sonu gerçek değerleriyle açık kalır (harita gerçeği gösterir).</summary>
        public void ReviewLogs()
        {
            State.Review = true;
            source.MarkDirty();
        }

        public void RetrySameSeed() => Game.NewGame(Game.TypeId, Sim.S.Seed);

        public void Restart() => Game.NewGame(Game.TypeId);

        /// <summary>
        /// F8.3 ekranlarının niyetleri (VM komut dizisi “Ekran:Eylem[:bağımsız değişken]”). Oyun kuralını GameController
        /// uygular; yeni oyun giriş dosyasını yeniden açar (SimDeskDataSource.OnNewGame), eski ModalView akışıyla aynı.
        /// </summary>
        public void Run(string command)
        {
            if (string.IsNullOrEmpty(command)) return;
            var p = command.Split(new[] { ':' }, 3);
            string screen = p[0], verb = p.Length > 1 ? p[1] : "", arg = p.Length > 2 ? p[2] : null;
            switch (screen)
            {
                case "Intro": RunIntro(verb, arg); break;
                case "Campaign": RunCampaign(verb, arg); break;
                case "Toast":
                    if (verb == "Dismiss" && int.TryParse(arg, out int id)) State.Remove(id);
                    break;
                case "Doc": if (verb == "Close") CloseDoc(); break;
                case "Team":
                    // Dosyadaki ekip fişi “Team:Send:<ekip>:<bölge>”: seçilen ekip yola çıkar (koşulları oyun denetler)
                    if (verb == "Send" && arg != null && Sim.TeamsOn)
                    {
                        var ta = arg.Split(new[] { ':' }, 2);
                        if (ta.Length == 2) Report(Game.MoveTeam(ta[0], ta[1]));
                    }
                    break;
                case "Tutorial":
                    if (tutorial == null) break;
                    if (verb == "Next") tutorial.Next();
                    else if (verb == "Skip") tutorial.Skip();
                    else if (verb == "Hide") tutorial.Hide();
                    else if (verb == "Restart") tutorial.Restart();
                    else if (verb == "Replay") tutorial.Replay();
                    break;
                case "Raid":
                    if (verb == "Start" && arg != null)
                    {
                        // Onaylanan emir: baskın başlar, RaidLauncher taktik haritayı (Baskin sahnesi) yükler
                        State.RaidBrief = null;
                        var r = Game.StartRaid(arg);
                        if (!r.Ok) Report(r); else source.MarkDirty();
                    }
                    break;
                case "FakeWin":
                    if (verb == "Glitch") AudioDirector.Instance?.FakeWinBroken();
                    else if (verb == "Done")
                    {
                        // Duyuru bozuldu: arkadaki masa artık gerçeği gösterir, oyun sonu raporu açılır
                        State.FakeWin = false;
                        Sim.HideEnding = false;
                        source.MarkDirty();
                    }
                    break;
                default: Debug.LogWarning("[Desk] Bilinmeyen komut: " + command); break;
            }
        }

        void RunIntro(string verb, string arg)
        {
            switch (verb)
            {
                case "Rival": if (arg != null) Game.NewGame(arg); break;
                case "Onboarding": Game.SetOnboardingOff(!Game.OnboardingOff); break;
                case "Events": Game.SetEventsEnabled(!Game.EventsEnabled); break;
                case "Teams": Game.SetExperimental(!Game.ExpTeams, Game.ExpTimeScale); break;
                case "Scale":
                    if (double.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out double k)) Game.SetExperimental(Game.ExpTeams, k);
                    break;
                case "Campaign": if (Game.Campaign != null) Game.EndCampaign(); else Game.StartCampaign(); break;
                case "Continue":
                    if (Game.ContinueSaved())
                    {
                        State.IntroOpen = false;
                        State.Push(Loc.F("Kayıt yüklendi: {0}. gün. Oyun duraklatıldı.", Sim.S.Day), Sim.S.Day, "ok", source.Builder.TimeNow);
                        source.MarkDirty();
                    }
                    else Fail(Loc.T("Kayıt yüklenemedi."));
                    break;
                case "Tutorial":
                    // Eğitim zamanı kendisi başlatır (“Zamanı başlat” adımı); yürütücü isteği bir sonraki karede alır
                    State.IntroOpen = false;
                    State.TutorialRequested = true;
                    Game.SetSpeed(0);
                    source.MarkDirty();
                    break;
                case "Start":
                    State.IntroOpen = false;
                    Game.SetSpeed(1);
                    source.MarkDirty();
                    break;
                case "Language":
                    BridgeSettings.ApplyLanguage(DeskLoc.Instance.Lang == DeskLang.Tr ? DeskLang.En : DeskLang.Tr, Game);
                    break;
            }
        }

        void RunCampaign(string verb, string arg)
        {
            switch (verb)
            {
                case "Open":
                    if (Game.Campaign == null) return;
                    State.CampaignOpen = true;
                    source.MarkDirty();
                    break;
                case "Upgrade":
                    State.CampaignOpen = false;
                    Game.NextCrisis(arg);
                    break;
                case "End":
                    State.CampaignOpen = false;
                    Game.EndCampaign();
                    break;
            }
        }

        /// <summary>Belgeyi kapatır: perde raporu, operasyon raporu ya da baskın emri (açık olan).</summary>
        void CloseDoc()
        {
            var S = Sim.S;
            if (State.VeilOpen) State.VeilOpen = false;
            else if (S.Raid == null && S.RaidLog.Count > State.RaidLogSeen) State.RaidLogSeen = S.RaidLog.Count;
            else State.RaidBrief = null;
            source.MarkDirty();
        }
    }
}
