using Corrigible.Desk;
using NUnit.Framework;

namespace Corrigible.Desk.Tests
{
    /// <summary>Panel sırası ve seçimin ömrü; sahne, fare, zaman veya oyun simülasyonu gerektirmez.</summary>
    public sealed class SadeInteractionStateTests
    {
        [Test]
        public void Escape_ClosesOnlyTheHighestLayerBeforeOpeningPause()
        {
            var state = new SadeInteractionState();
            state.Select("DoA"); state.OpenDrawer("research");
            state.Confirmation = state.Tooltip = state.Legend = true;

            Assert.AreEqual(SadeEscapeLayer.Confirmation, state.Escape());
            Assert.IsFalse(state.Confirmation);
            Assert.IsTrue(state.Tooltip); Assert.IsTrue(state.Legend);
            Assert.AreEqual("research", state.Drawer); Assert.AreEqual("DoA", state.SelectedRegion);

            Assert.AreEqual(SadeEscapeLayer.Tooltip, state.Escape());
            Assert.IsFalse(state.Tooltip); Assert.IsTrue(state.Legend);
            Assert.AreEqual("research", state.Drawer); Assert.AreEqual("DoA", state.SelectedRegion);

            Assert.AreEqual(SadeEscapeLayer.Legend, state.Escape());
            Assert.IsFalse(state.Legend); Assert.AreEqual("research", state.Drawer);
            Assert.AreEqual(SadeEscapeLayer.LargePanel, state.Escape());
            Assert.IsNull(state.Drawer); Assert.AreEqual("DoA", state.SelectedRegion);
            Assert.IsFalse(state.Pause, "Büyük paneli kapatmak duraklatma menüsünü açmaz.");

            Assert.AreEqual(SadeEscapeLayer.RegionCard, state.Escape());
            Assert.IsNull(state.SelectedRegion); Assert.IsFalse(state.Pause);
            Assert.AreEqual(SadeEscapeLayer.Pause, state.Escape()); Assert.IsTrue(state.Pause);
            Assert.AreEqual(SadeEscapeLayer.Pause, state.Escape()); Assert.IsFalse(state.Pause);
        }

        [Test]
        public void DrawerAndDossier_ExcludeEachOther_AndKeepSelection()
        {
            var state = new SadeInteractionState();
            state.Select("DoA"); state.OpenDrawer("news"); state.OpenDossier();
            Assert.IsTrue(state.FullDossier); Assert.IsNull(state.Drawer);
            Assert.AreEqual("DoA", state.SelectedRegion);

            state.ToggleDrawer("teams");
            Assert.AreEqual("teams", state.Drawer); Assert.IsFalse(state.FullDossier);
            Assert.AreEqual("DoA", state.SelectedRegion);

            state.OpenDossier(); state.OpenDrawer("guide");
            Assert.AreEqual("guide", state.Drawer); Assert.IsFalse(state.FullDossier);
            Assert.AreEqual("DoA", state.SelectedRegion);
        }

        [Test]
        public void ClosingDossierOrDrawer_PreservesSelectedRegionAndEndsAssignment()
        {
            var state = new SadeInteractionState();
            state.Select("GuA"); state.OpenDossier();
            Assert.AreEqual(SadeEscapeLayer.LargePanel, state.Escape());
            Assert.IsFalse(state.FullDossier); Assert.AreEqual("GuA", state.SelectedRegion);

            state.OpenDrawer("teams"); state.AssigningTeam = "B";
            state.CloseLargePanel();
            Assert.IsNull(state.Drawer); Assert.IsNull(state.AssigningTeam);
            Assert.AreEqual("GuA", state.SelectedRegion);
        }

        [Test]
        public void SelectingRegionKeepsDrawer_AndDossierNeedsASelection()
        {
            var state = new SadeInteractionState();
            state.OpenDrawer("research"); state.OpenDossier();
            Assert.IsFalse(state.FullDossier); Assert.AreEqual("research", state.Drawer);

            state.Select("KA");
            Assert.AreEqual("KA", state.SelectedRegion); Assert.AreEqual("research", state.Drawer);
            state.Select("BA");
            Assert.AreEqual("BA", state.SelectedRegion); Assert.AreEqual("research", state.Drawer);

            state.OpenDossier(); state.ClearSelection();
            Assert.IsNull(state.SelectedRegion); Assert.IsFalse(state.FullDossier);
        }
    }
}
