using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Menü sahnesinin puantaj saati ve kart rafı (Docs/AYARLAR-DOLABI-VE-MENU.md Katman 7). Kayıt varsa rafta komutanın
    /// vardiya kartı durur: başlık, kaydın vardiyası ve özgür bölgesi, her vardiya için bir zımba deliği. Karta tıklamak
    /// kayıttan devamdır (<see cref="ContinueChosen"/>). Rafın altında boş kart destesi: kayıt yoksa tek tıklama yeni
    /// harekâttır; kayıt varsa destenin kıskacı vardiya şalterinin kapağı gibidir: 1. tıklama kıskacı kaldırır ve eski kart
    /// “İPTAL EDİLİR” fişiyle öne eğilir, 4 s içindeki 2. tıklama onaylar (<see cref="NewChosen"/>), süre dolarsa kıskaç
    /// kapanır. <see cref="Punch"/> seçilen kartı saatin yarığına sürer ve damgalar. Sesleri bağlayıcı çalar. Çalışırken
    /// hiyerarşi yaratmaz.
    /// </summary>
    public sealed class TimeClock : MonoBehaviour
    {
        [SerializeField] DeskTarget clockTarget;
        [Header("Komutanın kartı")]
        [SerializeField] DeskTarget cardTarget;
        [SerializeField] Transform card;
        [SerializeField] TMP_Text cardSummary;
        [Tooltip("Zımba delikleri (soldan sağa, yukarıdan aşağı); vardiya sayısı kadarı görünür.")]
        [SerializeField] GameObject[] punches = Array.Empty<GameObject>();
        [Tooltip("Eski kartın öne eğilme pivotu ve fişi (yeni harekât kurulurken).")]
        [SerializeField] Transform cardTilt;
        [SerializeField] GameObject voidTag;
        [SerializeField] TMP_Text voidText;
        [Header("Boş kartlar")]
        [SerializeField] DeskTarget stackTarget;
        [SerializeField] Transform blank;
        [Tooltip("Destenin kıskacı (yerel x ekseninde kalkar).")]
        [SerializeField] Transform clip;
        [SerializeField] float clipOpenDeg = -70;
        [Header("Saat")]
        [Tooltip("Kartın yarığın üstünde durduğu ve yarığa girdiği yerler.")]
        [SerializeField] Transform slotAbove, slotIn;
        [Tooltip("Damga kolu (yerel y ekseninde iner).")]
        [SerializeField] Transform plunger;
        [SerializeField] float plungerDrop = 0.012f;

        static readonly string VoidSource = Corrigible.Core.Loc.N("İPTAL EDİLİR · V.{0:000}"),
            ArmLine = Corrigible.Core.Loc.N("Kıskacı kaldır, yeniden bas: eski kart iptal edilir."),
            ConfirmLine = Corrigible.Core.Loc.N("Onaylamak için yeniden bas.");

        bool hasCard, armed, punching;
        float armedAt, clipAngle, tilt, plungerRest;
        int shift;

        public bool HasCard => hasCard;
        public bool Armed => armed;
        public bool Punching => punching;

        public event Action ClockClicked, ContinueChosen, NewChosen;
        /// <summary>Kıskaç kalktı (true) ya da kapandı (false).</summary>
        public event Action<bool> ArmChanged;
        /// <summary>Kart yarığa girdi, damga indi.</summary>
        public event Action Stamped;

        static SiginakTheme T => SiginakTheme.Active;

        void Awake()
        {
            if (plunger != null) plungerRest = plunger.localPosition.y;
            if (clockTarget != null) clockTarget.Clicked += () => ClockClicked?.Invoke();
            if (cardTarget != null) cardTarget.Clicked += () => { if (hasCard && !punching) { Disarm(); ContinueChosen?.Invoke(); } };
            if (stackTarget != null) stackTarget.Clicked += Stack;
            if (voidTag != null) voidTag.SetActive(false);
        }

        /// <summary>Kayıttaki harekâtın kartı (<paramref name="summary"/> gösterime hazır); biten her vardiya (<paramref name="shifts"/>) bir delik.</summary>
        public void ShowCard(string summary, int shifts)
        {
            hasCard = true;
            shift = shifts + 1;
            if (card != null) card.gameObject.SetActive(true);
            if (cardSummary != null) cardSummary.text = summary;
            if (cardTarget != null) cardTarget.LineOverride = summary;
            for (int i = 0; i < punches.Length; i++) if (punches[i] != null) punches[i].SetActive(i < shifts);
            StackLine();
        }

        void StackLine()
        {
            if (stackTarget != null) stackTarget.LineOverride = !hasCard ? null : Corrigible.Core.Loc.T(armed ? ConfirmLine : ArmLine);
        }

        /// <summary>Kayıt yok: rafın yuvası boş.</summary>
        public void HideCard()
        {
            hasCard = false;
            if (card != null) card.gameObject.SetActive(false);
            StackLine();
        }

        void Stack()
        {
            if (punching) return;
            if (!hasCard || armed) { Disarm(); NewChosen?.Invoke(); return; }
            armed = true;
            armedAt = Time.unscaledTime;
            if (voidText != null) voidText.text = Corrigible.Core.Loc.F(VoidSource, shift);
            if (voidTag != null) voidTag.SetActive(true);
            StackLine();
            ArmChanged?.Invoke(true);
        }

        /// <summary>Kıskaç kapanır (Esc, süre doldu ya da başka bir seçim).</summary>
        public void Disarm()
        {
            if (!armed) return;
            armed = false;
            if (voidTag != null) voidTag.SetActive(false);
            StackLine();
            ArmChanged?.Invoke(false);
        }

        /// <summary>Seçilen kart (yeni harekâtta boş kart) saatin yarığına kayar, iner ve damgalanır; sonra <paramref name="done"/>.</summary>
        public void Punch(bool fresh, Action done)
        {
            var c = fresh ? blank : card;
            if (c == null || slotAbove == null || slotIn == null) { done?.Invoke(); return; }
            punching = true;
            StartCoroutine(PunchRoutine(c, done));
        }

        System.Collections.IEnumerator PunchRoutine(Transform c, Action done)
        {
            Vector3 p0 = c.position;
            Quaternion r0 = c.rotation;
            float carry = T.NumberOf("motion.cardCarryS", 0.4f), push = T.NumberOf("motion.cardPushS", 0.14f);
            for (float t = 0; t < carry; t += Time.unscaledDeltaTime)
            {
                float k = Ease(t / carry);
                // Hafif bir yay: kart raftan kalkar, yarığın üstüne iner
                c.SetPositionAndRotation(Vector3.Lerp(p0, slotAbove.position, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 0.04f,
                    Quaternion.Slerp(r0, slotAbove.rotation, k));
                yield return null;
            }
            for (float t = 0; t < push; t += Time.unscaledDeltaTime)
            {
                c.SetPositionAndRotation(Vector3.Lerp(slotAbove.position, slotIn.position, t / push), slotIn.rotation);
                yield return null;
            }
            c.SetPositionAndRotation(slotIn.position, slotIn.rotation);
            Stamped?.Invoke();
            // Damga kolu iner ve kalkar (0,08 + 0,12 s)
            for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                SetPlunger(t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.12f);
                yield return null;
            }
            SetPlunger(0);
            punching = false;
            done?.Invoke();
        }

        void SetPlunger(float k)
        {
            if (plunger == null) return;
            var p = plunger.localPosition;
            p.y = plungerRest - plungerDrop * Mathf.Clamp01(k);
            plunger.localPosition = p;
        }

        void Update()
        {
            if (armed && Time.unscaledTime - armedAt > T.NumberOf("motion.coverAutoCloseS", 4)) Disarm();
            float dt = Time.unscaledDeltaTime;
            clipAngle = Mathf.MoveTowards(clipAngle, armed ? clipOpenDeg : 0, Mathf.Abs(clipOpenDeg) / 0.15f * dt);
            if (clip != null) clip.localRotation = Quaternion.Euler(clipAngle, 0, 0);
            tilt = Mathf.MoveTowards(tilt, armed ? 1 : 0, dt / 0.15f);
            if (cardTilt != null) cardTilt.localRotation = Quaternion.Euler(-14 * tilt, 0, 0);
        }

        static float Ease(float t) => t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
    }
}
