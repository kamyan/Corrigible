using Corrigible.Game.Raid;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Sığınak odasının baskın bağlantısı (Aşama 7, Aşama 10'da sunum anahtarından ayrıldı). Açılışta
    /// <see cref="RaidLauncher"/>'a CRT geçiş kancasını (<see cref="IRaidTransition"/>) ve emirden sonraki bekleme süresini
    /// verir; baskın sahnesi ek yüklüyken odayı gizler (odanın ışıkları taktik tahtayı aydınlatmasın). Odanın kökünün
    /// dışında durur: kök gizliyken de çalışır.
    /// </summary>
    public sealed class SiginakRaidLink : MonoBehaviour
    {
        [SerializeField] GameObject room;
        [SerializeField] RaidLauncher launcher;
        [Tooltip("Baskın geçişi (SiginakCrt: IRaidTransition).")]
        [SerializeField] MonoBehaviour transition;

        public void Configure(GameObject siginakRoom, RaidLauncher raidLauncher, MonoBehaviour raidTransition)
        {
            room = siginakRoom; launcher = raidLauncher; transition = raidTransition;
        }

        void Awake()
        {
            if (launcher == null) return;
            // K2: “EMİR VERİLDİ” damgası iner, 0,3 s sonra kamera CRT'ye döner
            launcher.TransitionDelaySeconds = SiginakTheme.Active.NumberOf("motion.raidOrderDelayS", 0.3f);
            launcher.Transition = transition as IRaidTransition;
        }

        void Update()
        {
            if (room == null) return;
            bool show = !RaidLauncher.IsRaidLoaded;
            if (room.activeSelf != show) room.SetActive(show);
        }
    }
}
