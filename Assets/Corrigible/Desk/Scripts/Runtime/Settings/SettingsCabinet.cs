using System;
using Corrigible.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Corrigible.Desk
{
    /// <summary>
    /// Ayarlar dolabı (Docs/AYARLAR-DOLABI-VE-MENU.md Katman 1–4): duvara asılı çelik hat dolabı. Kapalıyken dolaba tıklamak
    /// açılmayı ister (<see cref="OpenRequested"/>; sahnenin bağlayıcısı kamerayı dolabın pozuna götürür ve
    /// <see cref="Open"/> der). Açılırken mandal döner (0,15 s), kanat açılır (0,45 s), iç ampul kırpışarak yanar. İçeride dört
    /// kollu sürgü (genel, müzik, ortam, efekt), sessiz mod anahtarı, dil kademe anahtarı ve isteğe bağlı kapaklı bir kol
    /// (<see cref="Exit"/>: odada menüye dönüş) vardır. Değerler <see cref="IDeskSettings"/>'ten okunur ve ona yazılır.
    /// Kapanır: Esc (bağlayıcı), açık kanada ya da panelin dışına tıklama. Kontroller yalnız dolap açıkken çalışır
    /// (kendi kilitleri, odanınkinden bağımsız). Dolap açıkken Tab kontroller arasında gezer, oklar odaktakini çevirir
    /// (ekranda ipucu yazmaz). Sesleri bağlayıcı çalar (<see cref="Sound"/>). Çalışırken hiyerarşi yaratmaz.
    /// </summary>
    public sealed class SettingsCabinet : MonoBehaviour
    {
        public enum State { Closed, Opening, Open, Closing }
        public enum Noise { Open, Close, Detent, DetentSoft, Switch, Selector }

        [Header("Kasa")]
        [Tooltip("Kapalı dolabın tıklama hedefi (“AYARLAR”); açıkken çarpıştırıcısı kapanır, kontroller tıklanır.")]
        [SerializeField] DeskTarget closedTarget;
        [Tooltip("Açık kanadın tıklama hedefi (kapatır).")]
        [SerializeField] DeskTarget leafTarget;
        [Tooltip("Kanadın menteşe pivotu (yerel y ekseninde döner).")]
        [SerializeField] Transform leafHinge;
        [SerializeField] float leafOpenDeg = -105;
        [Tooltip("Çeyrek tur mandal (yerel z ekseninde döner).")]
        [SerializeField] Transform latch;
        [SerializeField] float latchOpenDeg = 90;
        [Tooltip("İç parçalar: dolap kapalıyken çizilmez.")]
        [SerializeField] GameObject interior;
        [SerializeField] Light bulb;
        [SerializeField] Renderer bulbGlass;
        [SerializeField] Material bulbOn, bulbOff;
        [Tooltip("Kamera pozunun çapası (sahnenin kamerası buraya bakar).")]
        [SerializeField] Transform cameraAnchor;
        [SerializeField] float cameraFieldOfView = 45;
        [Tooltip("Sahnenin üzerine gelme etiketi (prefab'da boş; sahne bağlar).")]
        [SerializeField] HoverTag hoverTag;

        [Header("Kontroller")]
        [Tooltip("Sıra IDeskSettings kanallarıyla aynı: genel, müzik, ortam, efekt.")]
        [SerializeField] CabinetFader[] channels = Array.Empty<CabinetFader>();
        [SerializeField] CabinetToggle sound;
        [SerializeField] CabinetSelector language;
        [Tooltip("Kapaklı kol (odada MENÜYE DÖN); yoksa boş.")]
        [SerializeField] ShiftSwitch exit;

        static readonly string SoundOnLine = Loc.N("Açık · kapatınca bütün kanallar susar; ayarlar korunur."),
            SoundOffLine = Loc.N("Kapalı · sürgüler yerinde kalır."),
            LanguageLine = Loc.N("Türkçe · English");
        static readonly DeskLang[] Languages = { DeskLang.Tr, DeskLang.En };

        IDeskSettings settings;
        State state = State.Closed;
        float since, leaf, latchTurn, glow;
        int focus = -1;

        public static SettingsCabinet Instance { get; private set; }
        public State Current => state;
        /// <summary>Açık ya da açılıyor: oda kilitli.</summary>
        public bool IsOpen => state == State.Opening || state == State.Open;
        public Transform CameraAnchor => cameraAnchor;
        public float CameraFieldOfView => cameraFieldOfView;
        public ShiftSwitch Exit => exit;

        /// <summary>Kapalı dolaba tıklandı ya da biri açılmasını istedi (<see cref="RequestOpen"/>).</summary>
        public event Action OpenRequested;
        /// <summary>Kapanmaya başladı (kamera önceki poza döner).</summary>
        public event Action Closing;
        public event Action<Noise> Sound;
        /// <summary>Dil değişti (oda teleksten bir satır basar).</summary>
        public event Action<DeskLang> LanguageChanged;

        static SiginakTheme T => SiginakTheme.Active;
        static float LeafS => Mathf.Max(0.05f, T.NumberOf("motion.cabinetLeafS", 0.45f));
        static float LatchS => Mathf.Max(0.02f, T.NumberOf("motion.cabinetLatchS", 0.15f));
        static float CloseS => Mathf.Max(0.05f, T.NumberOf("motion.cabinetCloseS", 0.35f));

        public void Init(IDeskSettings s)
        {
            settings = s;
            Refresh();
        }

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        void Awake()
        {
            Func<bool> locked = () => state != State.Open;
            foreach (var t in Targets())
            {
                if (t == null) continue;
                if (hoverTag != null) t.Tag = hoverTag;
                if (t != closedTarget) t.BlockedOverride = locked;
            }
            if (closedTarget != null && hoverTag != null) closedTarget.Tag = hoverTag;
            if (closedTarget != null) closedTarget.Clicked += RequestOpen;
            if (leafTarget != null) leafTarget.Clicked += Close;
            for (int i = 0; i < channels.Length; i++)
            {
                int k = i;
                channels[i].Changed += v => { settings?.Set(k, v); settings?.Preview(k); };
                channels[i].Released += () => settings?.Save();
                channels[i].Detent += big => Sound?.Invoke(big ? Noise.Detent : Noise.DetentSoft);
            }
            if (sound != null) sound.Changed += on =>
            {
                if (settings != null) settings.Muted = !on;
                Sound?.Invoke(Noise.Switch);
                Refresh();
                settings?.Save();
            };
            if (language != null) language.Changed += i =>
            {
                Sound?.Invoke(Noise.Selector);
                var lang = Languages[Mathf.Clamp(i, 0, Languages.Length - 1)];
                settings?.SetLanguage(lang);
                Flicker();
                LanguageChanged?.Invoke(lang);
                Refresh();
            };
            DeskLoc.Instance.Changed += Relabel;
            Pose(0, 0, 0);
            Show(false);
        }

        void OnDestroy() => DeskLoc.Instance.Changed -= Relabel;

        DeskTarget[] Targets()
        {
            var list = new System.Collections.Generic.List<DeskTarget> { closedTarget, leafTarget };
            foreach (var c in channels) if (c != null) list.Add(c.Target);
            if (sound != null) list.Add(sound.Target);
            if (language != null) list.Add(language.Target);
            if (exit != null) list.Add(exit.Target);
            return list.ToArray();
        }

        // ---------- Açma ve kapama ----------

        /// <summary>Açılmayı ister: bağlayıcı varsa kamerayı getirip <see cref="Open"/> der, yoksa hemen açılır.</summary>
        public void RequestOpen()
        {
            if (IsOpen) return;
            if (OpenRequested != null) OpenRequested.Invoke();
            else Open();
        }

        /// <summary>Kapak <paramref name="delay"/> saniye sonra (kamera varınca) açılır.</summary>
        public void Open(float delay = 0)
        {
            if (IsOpen) return;
            Refresh();
            Enter(State.Opening);
            since = Time.unscaledTime + Mathf.Max(0, delay);
            Show(true);
            if (closedTarget != null) closedTarget.GetComponent<Collider>().enabled = false;
        }

        public void Close()
        {
            if (!IsOpen) return;
            Enter(State.Closing);
            since = Time.unscaledTime;
            focus = -1;
            if (exit != null) exit.Cancel();
            settings?.Save();
            Sound?.Invoke(Noise.Close);
            Closing?.Invoke();
        }

        public void Toggle() { if (IsOpen) Close(); else RequestOpen(); }

        /// <summary>Animasyonsuz kapalı (sahne açılışı).</summary>
        public void ResetClosed()
        {
            Enter(State.Closed);
            leaf = latchTurn = glow = 0;
            Pose(0, 0, 0);
            Show(false);
        }

        void Enter(State s) => state = s;

        void Show(bool open)
        {
            if (interior != null && interior.activeSelf != open) interior.SetActive(open);
            if (closedTarget != null) closedTarget.GetComponent<Collider>().enabled = !open;
            if (leafTarget != null) leafTarget.GetComponent<Collider>().enabled = open;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float now = Time.unscaledTime;
            switch (state)
            {
                case State.Opening:
                    if (now < since) break;
                    if (latchTurn < 1)
                    {
                        if (latchTurn == 0) Sound?.Invoke(Noise.Open);
                        latchTurn = Mathf.MoveTowards(latchTurn, 1, dt / LatchS);
                    }
                    else leaf = Mathf.MoveTowards(leaf, 1, dt / LeafS);
                    // Ampul kanat aralanınca 0,2 s'de iki kırpışmayla yanar
                    glow = leaf > 0 ? Mathf.MoveTowards(glow, 1, dt / 0.2f) : 0;
                    if (leaf >= 1 && glow >= 1) Enter(State.Open);
                    break;
                case State.Closing:
                    leaf = Mathf.MoveTowards(leaf, 0, dt / CloseS);
                    glow = Mathf.Min(glow, leaf);
                    if (leaf <= 0) latchTurn = Mathf.MoveTowards(latchTurn, 0, dt / LatchS);
                    if (leaf <= 0 && latchTurn <= 0) { Enter(State.Closed); Show(false); }
                    break;
                case State.Open:
                    Keys();
                    ClickOutside();
                    break;
            }
            Pose(leaf, latchTurn, glow);
        }

        void Pose(float leafT, float latchT, float glowT)
        {
            if (leafHinge != null) leafHinge.localRotation = Quaternion.Euler(0, leafOpenDeg * Ease(leafT), 0);
            if (latch != null) latch.localRotation = Quaternion.Euler(0, 0, latchOpenDeg * Ease(latchT));
            // İki kırpışma: 0,25 ve 0,6'da kısa sönme
            float level = glowT >= 1 ? 1 : glowT <= 0 ? 0 : glowT < 0.25f || glowT > 0.35f && glowT < 0.6f || glowT > 0.7f ? glowT : 0.1f;
            if (bulb != null)
            {
                bool lit = level > 0.001f;
                if (bulb.enabled != lit) bulb.enabled = lit;
                bulb.intensity = T.NumberOf("cabinet.bulbIntensity", 0.9f) * level;
            }
            if (bulbGlass != null && bulbOn != null && bulbOff != null)
            {
                var m = level > 0.5f ? bulbOn : bulbOff;
                if (bulbGlass.sharedMaterial != m) bulbGlass.sharedMaterial = m;
            }
        }

        float flickerAt = -1;

        /// <summary>Dil değişince ampul bir kez kırpışır.</summary>
        void Flicker() => flickerAt = Time.unscaledTime;

        void LateUpdate()
        {
            if (flickerAt < 0 || bulb == null || state != State.Open) return;
            float t = Time.unscaledTime - flickerAt;
            if (t > 0.18f) { flickerAt = -1; bulb.intensity = T.NumberOf("cabinet.bulbIntensity", 0.9f); return; }
            bulb.intensity = T.NumberOf("cabinet.bulbIntensity", 0.9f) * (t < 0.08f ? 0.15f : 1);
        }

        static float Ease(float t) => t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;

        // Panelin (ve kanadın) dışına tıklama kapatır; Overlay yüzeyleri (kenar satırı, plaka) kendi işini görür
        void ClickOutside()
        {
            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;
            var events = EventSystem.current;
            if (events != null && events.IsPointerOverGameObject()) return;
            Close();
        }

        // ---------- Klavye (ipucu yazmaz) ----------

        void Keys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            int count = ControlCount;
            if (count == 0) return;
            if (kb.tabKey.wasPressedThisFrame)
            {
                bool back = kb.shiftKey.isPressed;
                focus = focus < 0 ? (back ? count - 1 : 0) : (focus + (back ? count - 1 : 1)) % count;
                if (hoverTag != null) hoverTag.Show(ControlTarget(focus));
            }
            int hovered = HoveredControl();
            if (hovered >= 0) focus = hovered;
            if (focus < 0) return;
            int v = kb.upArrowKey.wasPressedThisFrame ? 1 : kb.downArrowKey.wasPressedThisFrame ? -1 : 0;
            int h = kb.rightArrowKey.wasPressedThisFrame ? 1 : kb.leftArrowKey.wasPressedThisFrame ? -1 : 0;
            if (v == 0 && h == 0) return;
            if (focus < channels.Length) channels[focus].Nudge(v != 0 ? v : h);
            else if (focus == channels.Length && sound != null) sound.Nudge(v != 0 ? v : h);
            else if (language != null) language.Nudge(h != 0 ? h : -v);
        }

        int ControlCount => channels.Length + (sound != null ? 1 : 0) + (language != null ? 1 : 0);

        DeskTarget ControlTarget(int i)
        {
            if (i < channels.Length) return channels[i].Target;
            if (i == channels.Length && sound != null) return sound.Target;
            return language != null ? language.Target : null;
        }

        int HoveredControl()
        {
            for (int i = 0; i < ControlCount; i++)
            {
                var t = ControlTarget(i);
                if (t != null && t.IsHovered) return i;
            }
            return -1;
        }

        // ---------- Değerler ----------

        void Refresh()
        {
            if (settings == null) return;
            for (int i = 0; i < channels.Length; i++)
            {
                channels[i].Set(settings.Get(i), state == State.Closed);
                channels[i].SetDimmed(settings.Muted);
            }
            if (sound != null) sound.Set(!settings.Muted, state == State.Closed);
            if (language != null) language.Set(Array.IndexOf(Languages, settings.Language), state == State.Closed);
            Relabel();
        }

        void Relabel()
        {
            if (sound != null && sound.Target != null) sound.Target.LineOverride = Loc.T(sound.On ? SoundOnLine : SoundOffLine);
            if (language != null && language.Target != null) language.Target.LineOverride = Loc.T(LanguageLine);
        }
    }
}
