using System.Collections.Generic;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>Fiziksel masa aynı görünüm modelini okur ve mevcut sunum komutlarını kullanır.</summary>
    public sealed class CommandPostDesk : MonoBehaviour
    {
        [SerializeField] DeskPresenter presenter;
        [SerializeField] Camera view;
        [SerializeField] CommandPostCamera cameraRig;
        [SerializeField] GraphicRaycaster[] worldRaycasters = System.Array.Empty<GraphicRaycaster>();
        [SerializeField] Renderer[] roomRenderers = System.Array.Empty<Renderer>();
        [SerializeField] TMP_Text ledger, faxPaper, faxStatus, radioNote, help;
        IDeskDataSource source;
        PointerEventData pointer;
        EventSystem pointerEvents;
        readonly List<RaycastResult> hits = new List<RaycastResult>(24);
        string hoverLabel;
        bool[] rendererVisibility;
        bool renderersHidden;

        public bool InteractionBlocked => !isActiveAndEnabled || view == null || !view.enabled ||
            presenter == null || presenter.Commands == null ||
            (IntroView.Instance != null && IntroView.Instance.IsOpen) ||
            (CampaignView.Instance != null && CampaignView.Instance.IsOpen) ||
            (SettingsView.Instance != null && SettingsView.Instance.IsOpen) ||
            (FakeWinView.Instance != null && FakeWinView.Instance.IsOpen) ||
            (GameOverView.Instance != null && GameOverView.Instance.IsOpen) ||
            (EventModalView.Instance != null && EventModalView.Instance.IsOpen) ||
            (ConfirmModalView.Instance != null && ConfirmModalView.Instance.IsOpen) ||
            (presenter.Sade != null && presenter.Sade.State.Pause);

        /// <summary>Kaydırılabilir dosya üstünde tekerlek yalnız dosyayı kaydırır.</summary>
        public bool PointerOverOverlay
        {
            get
            {
                var events = EventSystem.current;
                if (events == null || Mouse.current == null) return false;
                if (pointer == null || pointerEvents != events) { pointerEvents = events; pointer = new PointerEventData(events); }
                pointer.Reset();
                pointer.position = Mouse.current.position.ReadValue();
                hits.Clear();
                events.RaycastAll(pointer, hits);
                // EventSystem sırası ekranda en üstteki tıklanabilir yüzeyi verir.
                foreach (var hit in hits)
                {
                    if (!(hit.module is GraphicRaycaster raycaster)) continue;
                    var canvas = raycaster.GetComponent<Canvas>();
                    return canvas != null && canvas.rootCanvas.renderMode != RenderMode.WorldSpace;
                }
                return false;
            }
        }

        void OnEnable()
        {
            if (renderersHidden)
                for (int i = 0; i < roomRenderers.Length; i++)
                    if (roomRenderers[i] != null) roomRenderers[i].enabled = rendererVisibility[i];
            renderersHidden = false;
            DeskLoc.Instance.Changed += Refresh;
        }

        void Update()
        {
            if (source == null && presenter != null && presenter.Source != null)
            {
                source = presenter.Source;
                source.Changed += Bind;
                Refresh();
            }
            bool enabledInput = !InteractionBlocked;
            foreach (var raycaster in worldRaycasters)
                if (raycaster != null && raycaster.enabled != enabledInput) raycaster.enabled = enabledInput;
            if (!enabledInput && hoverLabel != null) SetHover(null);
        }

        void OnDisable()
        {
            DeskLoc.Instance.Changed -= Refresh;
            if (source != null) source.Changed -= Bind;
            source = null;
            foreach (var raycaster in worldRaycasters) if (raycaster != null) raycaster.enabled = false;
            if (rendererVisibility == null || rendererVisibility.Length != roomRenderers.Length)
                rendererVisibility = new bool[roomRenderers.Length];
            for (int i = 0; i < roomRenderers.Length; i++)
                if (roomRenderers[i] != null) { rendererVisibility[i] = roomRenderers[i].enabled; roomRenderers[i].enabled = false; }
            renderersHidden = true;
            hoverLabel = null;
        }

        void Refresh() { if (source != null) Bind(source.Current); SetHover(hoverLabel); }

        void Bind(DeskViewModel vm)
        {
            if (vm == null) return;
            var resources = vm.Sade?.View?.Resources;
            var fund = resources?.Find(r => r.Id == "fund");
            var power = resources?.Find(r => r.Id == "politicalPower");
            DeskUi.SetText(ledger, Loc.F("KAYNAK DEFTERİ\n\nGün {0:000}\nFon: {1}\nSiyasi güç: {2}",
                vm.Clock?.Day ?? 0, fund != null ? DeskFormat.Number(fund.Value, fund.Decimals) : "—",
                power != null ? DeskFormat.Number(power.Value, power.Decimals) : "—"));
            DeskUi.SetText(faxStatus, Loc.F("GELEN: {0:00}", vm.News.Count));
            DeskUi.SetText(faxPaper, vm.News.Count > 0
                ? Loc.F("SON HABER\n\n{0}", DeskLoc.Instance.T(vm.News[0].Text))
                : Loc.T("SAHA FAKSI\n\nYeni rapor bekleniyor."));
            DeskUi.SetText(radioNote, Loc.F("SAHA TELSİZİ\n\nEkipler: {0}\nHaberler için DİNLE.", vm.Teams.Count));
        }

        public void SetHover(string sourceLabel)
        {
            hoverLabel = sourceLabel;
            DeskUi.SetText(help, string.IsNullOrEmpty(sourceLabel)
                ? Loc.T("Sağ tuş: bakış · Tekerlek: yaklaş · F: haritaya odaklan · Home: masaya dön")
                : Loc.F("{0} · Sol tık: kullan", DeskLoc.Instance.T(sourceLabel)));
        }

        /// <summary>Yalnız sunum niyeti; simülasyon ve kayıt zinciri DeskPresenter'da kalır.</summary>
        public bool Execute(CommandPostAction action)
        {
            if (InteractionBlocked) return false;
            switch (action)
            {
                case CommandPostAction.Research: presenter.ToggleWindow(WindowId.Research); break;
                case CommandPostAction.News: presenter.ToggleWindow(WindowId.News); break;
                case CommandPostAction.Teams: presenter.ToggleWindow(WindowId.Teams); break;
                case CommandPostAction.AiLog: presenter.ToggleWindow(WindowId.AiLog); break;
                case CommandPostAction.Guide: presenter.ToggleWindow(WindowId.Guide); break;
                case CommandPostAction.Pause: presenter.Commands.TogglePause(); break;
                case CommandPostAction.FocusMap: if (cameraRig != null) { if (cameraRig.IsFocused) cameraRig.ResetView(); else cameraRig.FocusMap(); } break;
                default: return false;
            }
            return true;
        }
    }

    public enum CommandPostAction { Research, News, Teams, AiLog, Guide, Pause, FocusMap }
}
