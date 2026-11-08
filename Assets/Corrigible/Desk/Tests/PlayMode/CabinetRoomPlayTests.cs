using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace Corrigible.Desk.Tests
{
    /// <summary>R3f: menü kolu dolabın kendisini kilit nedeni saymaz; açık emir belgesi ise gerçek kilittir.</summary>
    public sealed class CabinetRoomPlayTests
    {
        [UnityTearDown] public IEnumerator TearDown(){yield return BoardPlayKit.TearDown();}
        [UnityTest] public IEnumerator OpenDocument_LocksExit_AndDeniedClickDoesNotSaveOrChangeTheGame()
        {
            yield return BoardPlayKit.LoadDesk(passBriefing:false);var c=Object.FindFirstObjectByType<SettingsCabinet>();c.RequestOpen();yield return BoardPlayKit.WaitSeconds(1.5f);var before=BoardPlayKit.Snapshot();int denied=0;c.Exit.Denied+=()=>denied++;
            Assert.AreEqual(ShiftSwitch.State.Disabled,c.Exit.Current);Assert.IsTrue(c.Exit.Target.Interactable);Assert.IsNotEmpty(c.Exit.LockLine);c.Exit.Press();yield return BoardPlayKit.WaitSeconds(.03f);Assert.AreEqual(1,denied);Assert.AreEqual(ShiftSwitch.State.Disabled,c.Exit.Current);Assert.AreEqual(before,BoardPlayKit.Snapshot());
        }
        [UnityTest] public IEnumerator IdleDesk_AllowsTheExitCover_WhenCabinetIsOpen()
        {
            yield return BoardPlayKit.LoadDesk();var c=Object.FindFirstObjectByType<SettingsCabinet>();c.RequestOpen();yield return BoardPlayKit.WaitSeconds(1.5f);Assert.AreEqual(ShiftSwitch.State.Idle,c.Exit.Current);c.Exit.Press();yield return BoardPlayKit.WaitSeconds(.3f);Assert.AreEqual(ShiftSwitch.State.CoverOpen,c.Exit.Current);Assert.Greater(c.Exit.CoverDeg,110);Assert.IsTrue(c.Exit.Cancel());
        }
    }
}
