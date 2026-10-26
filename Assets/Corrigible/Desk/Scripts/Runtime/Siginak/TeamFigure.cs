using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Haritadaki 3D ekip figürü (Ekip-figuru.md): pirinç oval taban, üç ahşap piyon, bakalit “A” plakası. Hâller: hazır
    /// (dik) · üzerinde/seçili (sarı halka) · taşınıyor (25 mm havada, −6°, ayrık gölge) · yaralı (düşen üye kadar piyon
    /// yatık). Yeri bağlayıcı verir (oyun durumundan türetilir, kayda yazılmaz); burada yalnız hareket vardır:
    /// kendiliğinden taşıma 0,6 s / 10 mm yay, geri kayma 0,25 s ease-out, kalkma 0,12 s, iniş 0,15 s + 2 mm sekme.
    /// </summary>
    public sealed class TeamFigure : MonoBehaviour
    {
        [Tooltip("Kalkan ve eğilen gövde (taban, plaka, piyonlar).")]
        [SerializeField] Transform body;
        [SerializeField] Transform[] pawns = Array.Empty<Transform>();
        [SerializeField] GameObject ring;
        [SerializeField] Transform carryShadow;
        [Tooltip("ALFA etiketi (K6b): kâğıt etiket “ALFA [YARALI] n GÜN”, yalnız yaralıyken.")]
        [SerializeField] GameObject injuredTag;
        [SerializeField] TMP_Text injuredName, injuredDays;

        Vector3[] pawnRest = Array.Empty<Vector3>();
        Quaternion[] pawnRestRotation = Array.Empty<Quaternion>();
        Vector3 shadowRest;
        Vector3 moveFrom, moveTo, follow;
        float moveAt = -1, moveSeconds, moveArc;
        bool moveEaseOut, following, placed;
        float lift, liftTarget, bounceAt = -1, wobbleAt = -1;
        int down = -1;

        public bool Placed => placed;
        /// <summary>Hareketin hedefi (yoksa bugünkü yer).</summary>
        public Vector3 Destination => moveAt >= 0 ? moveTo : transform.position;
        public bool Moving => moveAt >= 0;

        public void Configure(Transform bodyRoot, Transform[] pawnRoots, GameObject selectRing, Transform shadow, GameObject tag, TMP_Text tagName, TMP_Text tagDays)
        {
            body = bodyRoot; pawns = pawnRoots; ring = selectRing; carryShadow = shadow; injuredTag = tag; injuredName = tagName; injuredDays = tagDays;
        }

        void Awake()
        {
            pawnRest = new Vector3[pawns.Length];
            pawnRestRotation = new Quaternion[pawns.Length];
            for (int i = 0; i < pawns.Length; i++) { pawnRest[i] = pawns[i].localPosition; pawnRestRotation[i] = pawns[i].localRotation; }
            if (carryShadow != null) shadowRest = carryShadow.localScale;
        }

        static SiginakTheme T => SiginakTheme.Active;
        static float Mm(string key, float fallback) => T.NumberOf(key, fallback) / 1000f;

        /// <summary>Oyun durumundan gelen yer. İlk yerleştirme ve <paramref name="instant"/> anında; sonra 0,6 s yay.</summary>
        public void Place(Vector3 world, bool instant = false)
        {
            if (!placed || instant)
            {
                placed = true;
                moveAt = -1;
                following = false;
                transform.position = world;
                return;
            }
            if ((Destination - world).sqrMagnitude < 1e-8f) return;
            Move(world, T.NumberOf("motion.team.autoMoveS", 0.6f), Mm("motion.team.autoArcMm", 10), false);
        }

        /// <summary>Geçersiz bırakma ya da vazgeçme: eski yerine 0,25 s ease-out kayar.</summary>
        public void SlideBack(Vector3 world) => Move(world, T.NumberOf("motion.team.bounceBackS", 0.25f), 0, true);

        void Move(Vector3 world, float seconds, float arc, bool easeOut)
        {
            following = false;
            moveFrom = transform.position;
            moveTo = world;
            moveSeconds = Mathf.Max(0.01f, seconds);
            moveArc = arc;
            moveEaseOut = easeOut;
            moveAt = Time.unscaledTime;
        }

        /// <summary>Taşırken imleci 0,08 s gecikmeyle izler (yay).</summary>
        public void Follow(Vector3 world)
        {
            moveAt = -1;
            following = true;
            follow = world;
        }

        /// <summary>Al / bırak: 25 mm kalkar ve −6° eğilir; bırakınca iner ve 2 mm seker.</summary>
        public void SetCarrying(bool on)
        {
            liftTarget = on ? Mm("motion.team.liftMm", 25) : 0;
            if (!on)
            {
                following = false;
                bounceAt = Time.unscaledTime + T.NumberOf("motion.team.dropS", 0.15f);
            }
        }

        public void SetHighlighted(bool on)
        {
            if (ring != null && ring.activeSelf != on) ring.SetActive(on);
        }

        /// <summary>Yatık piyon sayısı (yaralı üyeler sağdan); 0 = hepsi dik.</summary>
        public void SetDown(int count)
        {
            count = Mathf.Clamp(count, 0, pawns.Length);
            if (down == count) return;
            down = count;
            if (pawnRest.Length != pawns.Length) Awake();
            for (int i = 0; i < pawns.Length; i++)
            {
                bool lying = i >= pawns.Length - count;
                // room.js › buildTeamFigure: yatık piyon yan döner, tabana yaslanır
                pawns[i].localPosition = lying ? pawnRest[i] + new Vector3(0.004f, 0.006f, 0) : pawnRest[i];
                pawns[i].localRotation = lying ? pawnRestRotation[i] * Quaternion.Euler(0, -28.6f, 90) : pawnRestRotation[i];
            }
        }

        /// <summary>ALFA etiketi: ekip adı ve kalan gün (“1 GÜN”); <paramref name="days"/> boşsa etiket kalkar.</summary>
        public void SetInjured(string name, string days)
        {
            bool on = !string.IsNullOrEmpty(days);
            if (injuredTag != null && injuredTag.activeSelf != on) injuredTag.SetActive(on);
            if (!on) return;
            if (injuredName != null && injuredName.text != name) injuredName.text = name;
            if (injuredDays != null && injuredDays.text != days) injuredDays.text = days;
        }

        /// <summary>Yaralı ekip alınamaz: yerinde sallanır.</summary>
        public void Wobble() => wobbleAt = Time.unscaledTime;

        void Update()
        {
            float now = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            if (moveAt >= 0)
            {
                float p = Mathf.Clamp01((now - moveAt) / moveSeconds);
                float e = moveEaseOut ? 1 - (1 - p) * (1 - p) : p * p * (3 - 2 * p);
                transform.position = Vector3.Lerp(moveFrom, moveTo, e) + Vector3.up * (moveArc * 4 * e * (1 - e));
                if (p >= 1) moveAt = -1;
            }
            else if (following)
            {
                float k = 1 - Mathf.Exp(-dt / Mathf.Max(0.01f, T.NumberOf("motion.team.followLagS", 0.08f)));
                transform.position = Vector3.Lerp(transform.position, follow, k);
            }

            // Kalkma 0,12 s, iniş 0,15 s
            float rate = Mm("motion.team.liftMm", 25) / Mathf.Max(0.01f, T.NumberOf(liftTarget > lift ? "motion.team.pickUpS" : "motion.team.dropS", 0.12f));
            lift = Mathf.MoveTowards(lift, liftTarget, rate * dt);
            float liftMax = Mathf.Max(1e-4f, Mm("motion.team.liftMm", 25));
            float k01 = Mathf.Clamp01(lift / liftMax);
            float bounce = 0;
            if (bounceAt >= 0)
            {
                float b = (now - bounceAt) / 0.12f;
                if (b >= 1) bounceAt = -1;
                else if (b >= 0) bounce = Mm("motion.team.bounceMm", 2) * Mathf.Sin(b * Mathf.PI);
            }
            float wobble = 0;
            if (wobbleAt >= 0)
            {
                float w = (now - wobbleAt) / 0.4f;
                if (w >= 1) wobbleAt = -1;
                else wobble = Mathf.Sin(w * Mathf.PI * 6) * (1 - w) * 7;
            }
            float tilt = T.NumberOf("motion.team.tiltDeg", 6);
            body.localPosition = Vector3.up * (lift + bounce);
            body.localRotation = Quaternion.Euler(-4 * k01, 0, -tilt * k01 + wobble);
            if (carryShadow != null)
            {
                // Taşırken gölge ayrılır: yükseldikçe büyür ve kayar (gerçek zamanlı bulanıklık değil)
                bool show = k01 > 0.02f;
                if (carryShadow.gameObject.activeSelf != show) carryShadow.gameObject.SetActive(show);
                carryShadow.localScale = shadowRest * (1 + 0.6f * k01);
                carryShadow.localPosition = new Vector3(0.012f * k01, 0.0004f, -0.010f * k01);
            }
        }
    }
}
