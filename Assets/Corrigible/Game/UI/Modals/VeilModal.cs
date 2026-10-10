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
            eyebrow.text = Loc.F("SAHA RAPORU · GÜN {0} · {1}", s.Day, UiUtil.Up(name));
            body.text = Loc.F("Saha ekiplerin {0} altyapısının %{1}’inin YZ kontrolünde olduğunu buldu. Ekranların {2} gündür sana yalan söylüyordu: temiz haritalar, olumlu haberler, düşen Tekillik.", name, v.Inf, v.Len);
        }
    }
}
