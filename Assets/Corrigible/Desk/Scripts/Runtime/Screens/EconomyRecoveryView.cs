using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Bölge dosyasındaki toparlanma emirleri; koşulları görünüm modeli taşır.</summary>
    public sealed class EconomyRecoveryView : MonoBehaviour
    {
        [SerializeField] TMP_Text title, recoveryText, diplomacyText;
        [SerializeField] DeskButton recovery, diplomacy;
        EconomyRecoveryVM data;
        public event Action<string> Invoked;

        void Awake()
        {
            recovery.onClick.AddListener(() => { if (data?.Recovery?.Command != null) Invoked?.Invoke(data.Recovery.Command); });
            diplomacy.onClick.AddListener(() => { if (data?.Diplomacy?.Command != null) Invoked?.Invoke(data.Diplomacy.Command); });
        }

        public void Bind(EconomyRecoveryVM vm)
        {
            data = vm;
            gameObject.SetActive(vm != null);
            if (vm == null) return;
            title.text = vm.Title;
            recoveryText.text = vm.RecoveryText;
            diplomacyText.text = vm.DiplomacyText;
            BindButton(recovery, vm.Recovery);
            BindButton(diplomacy, vm.Diplomacy);
        }

        static void BindButton(DeskButton button, CommandVM vm)
        {
            button.SetLabel(vm?.Label);
            button.SetDisabled(string.IsNullOrEmpty(vm?.Command) ? vm?.Label : null);
        }
    }
}
