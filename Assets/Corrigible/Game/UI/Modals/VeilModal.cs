using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Game.UI
{
    /// <summary>Perde yırtıldı: saha ekipleri illüzyonu delip gerçek sızmayı buldu.</summary>
    public sealed class VeilModal : MonoBehaviour
    {
        [SerializeField] TMP_Text eyebrow;
        [SerializeField] TMP_Text body;
        [SerializeField] StyledButton ok;

        public event Action Closed;

        void Awake() => UiUtil.OnClick(ok.Button, () => Closed?.Invoke());

        public void Show(WorldState s)
        {
            var v = s.LastVeil;
            string name = GameData.Region(v.Region).Name;
            eyebrow.text = $"SAHA RAPORU · GÜN {s.Day} · {UiUtil.Up(name)}";
            body.text = $"Saha ekiplerin {name} altyapısının %{v.Inf}’inin YZ kontrolünde olduğunu buldu. Ekranların {v.Len} gündür sana yalan söylüyordu: temiz haritalar, olumlu haberler, düşen Tekillik.";
        }
    }
}
