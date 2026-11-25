using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>Belge çekmecesinin ses olayları (köprü var olan cue'lara eşler).</summary>
    public enum PassDrawerSound { Open, Close, Switch, Deny }

    /// <summary>
    /// Kapının yanındaki çift taraflı belge çekmecesi (Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A): duvarı geçen bir çekmece, dış
    /// kapağı sığınağın dışında. Dışarıdan gelen belge (olay tutanağı, sunucu ödülü) çekmecenin tablasına konur ve lamba amber yanar;
    /// oyuncu çekmeceyi açıp belgeyi alır. Karar verilen belge yeniden tablaya konur, lamba yeşil yanıp söner; yanındaki şalter
    /// çekilince çekmece kapanır ve belge duvarın öbür yanına geçer.
    /// <para>
    /// Yalnız çizer ve olay yayar (<see cref="DrawerClicked"/>, <see cref="SwitchClicked"/>, <see cref="Sound"/>): belgeleri, komutları ve
    /// simülasyonu bilmez; durumu köprü (<c>SiginakPassDrawer</c>) her karede yazar. Parçalar <see cref="PartDriver"/>'dır: çekmece
    /// tablası kayar (dinlenme kapalı), lamba malzeme değiştirir (0 sönük, 1 amber, 2 yeşil), şalter kolu döner.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PassDrawer : MonoBehaviour
    {
        [Tooltip("Çekmecenin tablası (Slide; dinlenme kapalı, aralığın öbür ucu tam açık). Belge çapaları bunun çocuğudur.")]
        [SerializeField] PartDriver drawer;
        [Tooltip("Lamba (Swap: 0 sönük, 1 amber, 2 yeşil).")]
        [SerializeField] PartDriver lamp;
        [Tooltip("Şalter kolu (Rotate; dinlenme yukarıda, aralığın öbür ucu çekilmiş).")]
        [SerializeField] PartDriver lever;
        [Tooltip("Çekmecenin önü: tık çekmeceyi açar ya da kapar.")]
        [SerializeField] DeskTarget drawerTarget;
        [Tooltip("Şalter: tık belgeyi duvarın öbür yanına bildirir.")]
        [SerializeField] DeskTarget switchTarget;

        public enum Lamp { Off = 0, Amber = 1, Green = 2 }

        bool open;
        Lamp lampState;
        bool blink;
        float pullTimer = -1f;

        /// <summary>Çekmecenin önüne tıklandı.</summary>
        public event Action DrawerClicked;
        /// <summary>Şaltere tıklandı.</summary>
        public event Action SwitchClicked;
        public event Action<PassDrawerSound> Sound;

        /// <summary>Çekmece açık (ya da açılıyor).</summary>
        public bool IsOpen => open;
        /// <summary>Çekmece tam açık ve duruyor: içindeki belge tıklanabilir.</summary>
        public bool FullyOpen => open && drawer != null && !drawer.Moving && Mathf.Approximately(drawer.Value, OpenValue) || open && drawer == null;
        /// <summary>Çekmece tam kapalı ve duruyor.</summary>
        public bool FullyClosed => !open && (drawer == null || !drawer.Moving && Mathf.Approximately(drawer.Value, drawer.Rest));
        /// <summary>Çekmece ya da şalter hareket ediyor.</summary>
        public bool Moving => drawer != null && drawer.Moving || lever != null && lever.Moving || pullTimer >= 0f;
        /// <summary>Şalter çekiliyor (kol aşağıda ya da dönüyor).</summary>
        public bool Pulling => pullTimer >= 0f;
        public Lamp LampState => lampState;
        public bool Blinking => blink;
        public PartDriver Drawer => drawer;
        public PartDriver LampPart => lamp;
        public PartDriver Lever => lever;
        public DeskTarget DrawerTarget => drawerTarget;
        public DeskTarget SwitchTarget => switchTarget;

        float OpenValue => drawer == null ? 0f : Mathf.Approximately(drawer.Rest, drawer.RangeFrom) ? drawer.RangeTo : drawer.RangeFrom;
        float PulledValue => lever == null ? 0f : Mathf.Approximately(lever.Rest, lever.RangeFrom) ? lever.RangeTo : lever.RangeFrom;

        static float Num(string key, float fallback) => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf(key, fallback) : fallback;
        static float SlideS => Num("passDrawer.motion.slideS", 0.35f);
        static float LeverS => Num("passDrawer.motion.leverS", 0.16f);
        static float LeverHoldS => Num("passDrawer.motion.leverHoldS", 0.12f);
        static float BlinkHz => Num("passDrawer.lamp.blinkHz", 1.5f);

        /// <summary>Bağlar (tek seferlik sahne script'i ve testler).</summary>
        public void Configure(PartDriver drawerPart, PartDriver lampPart, PartDriver leverPart, DeskTarget drawerHit, DeskTarget switchHit)
        {
            drawer = drawerPart; lamp = lampPart; lever = leverPart; drawerTarget = drawerHit; switchTarget = switchHit;
        }

        void OnEnable()
        {
            if (drawerTarget != null) drawerTarget.Clicked += OnDrawerClicked;
            if (switchTarget != null) switchTarget.Clicked += OnSwitchClicked;
        }

        void OnDisable()
        {
            if (drawerTarget != null) drawerTarget.Clicked -= OnDrawerClicked;
            if (switchTarget != null) switchTarget.Clicked -= OnSwitchClicked;
        }

        void OnDrawerClicked() => DrawerClicked?.Invoke();
        void OnSwitchClicked() => SwitchClicked?.Invoke();

        /// <summary>Testler: tıkları sentetik verir.</summary>
        public void ClickDrawer() => OnDrawerClicked();
        public void ClickSwitch() => OnSwitchClicked();

        /// <summary>Çekmece açılır (açıksa bir şey olmaz).</summary>
        public void Open()
        {
            if (open) return;
            open = true;
            if (drawer != null) drawer.AnimateTo(OpenValue, SlideS);
            Sound?.Invoke(PassDrawerSound.Open);
        }

        /// <summary>Çekmece kapanır (kapalıysa bir şey olmaz).</summary>
        public void Close()
        {
            if (!open) return;
            open = false;
            if (drawer != null) drawer.AnimateTo(drawer.Rest, SlideS);
            Sound?.Invoke(PassDrawerSound.Close);
        }

        /// <summary>Animasyonsuz açık ya da kapalı (yükleme, testler); şalter dinlenmeye döner.</summary>
        public void SetOpenNow(bool on)
        {
            open = on;
            pullTimer = -1f;
            if (drawer != null) drawer.Set(on ? OpenValue : drawer.Rest);
            if (lever != null) lever.Set(lever.Rest);
        }

        /// <summary>Lamba: durum ve yanıp sönme (yanıp sönerken yarı süre sönük).</summary>
        public void SetLamp(Lamp state, bool blinking)
        {
            lampState = state;
            blink = blinking && state != Lamp.Off;
            ApplyLamp();
        }

        /// <summary>Şalter kolu çekilir: aşağı iner, kısa bekler ve geri kalkar (ses bir kez).</summary>
        public void PullSwitch()
        {
            if (pullTimer >= 0f) return;
            pullTimer = LeverS + LeverHoldS;
            if (lever != null) lever.AnimateTo(PulledValue, LeverS);
            Sound?.Invoke(PassDrawerSound.Switch);
        }

        /// <summary>Şaltere boşuna tık: kol kıpırdamaz, ret sesi.</summary>
        public void Deny() => Sound?.Invoke(PassDrawerSound.Deny);

        void Update() => Advance(Time.unscaledDeltaTime);

        /// <summary>Şalterin dönüşünü ve lambanın yanıp sönmesini ilerletir (edit modda testler sürer; parçalar kendi Update'inde).</summary>
        public void Advance(float dt)
        {
            if (pullTimer >= 0f)
            {
                pullTimer -= Mathf.Max(0f, dt);
                if (pullTimer < 0f)
                {
                    pullTimer = -1f;
                    if (lever != null) lever.AnimateTo(lever.Rest, LeverS);
                }
            }
            ApplyLamp();
        }

        void ApplyLamp()
        {
            if (lamp == null) return;
            int s = (int)lampState;
            if (blink && Application.isPlaying && Mathf.Repeat(Time.unscaledTime * BlinkHz, 1f) >= 0.5f) s = 0;
            if (lamp.SwapState != s) lamp.SetSwap(s);
        }
    }
}
