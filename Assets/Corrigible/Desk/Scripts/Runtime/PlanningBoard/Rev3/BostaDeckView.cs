using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kartlı panonun BOŞTA destesi (Revizyon 3 · R15c, PLAN §3.7). Kapalıyken j. kart (0 = üst) desteden 1 mm sağ-aşağı
    /// basamaklıdır, yalnız üst kart sürüklenir (çarpıştırıcıları <see cref="BoardView3D"/> açar). Açık elde üst kart tepside kalır,
    /// diğerleri sola dizilir (aralık <see cref="BoardLayout.HandStep"/>). Açılış üstten başlar (kart başına 0,32 s, j. kart j·0,07 s
    /// sonra), toplama alttan (en soldaki önce). Hareket yarıda tersine dönerse her kart o anki yerinden yeni hedefine gider; süresi
    /// kalan yolun payıyla kısalır (en az 0,08 s), böylece kare sıçraması olmaz. Bir kart alınınca (<see cref="TakeOne"/>) öbürleri
    /// alttan başlayarak desteye toplanır ve alınanın yeri boş kalır (geri dönerse aynı yere oturur); BOŞTA'ya bırakılan kart
    /// (<see cref="AcceptDrop"/>) bırakıldığı yerden sırasındaki yere 0,32 s'de kayar (eski cebinden geçmez). Destede yeni olan kart
    /// (takasta cepten gelen) 6 mm kalkışlı düz yolla 0,32 s'de gelir. Kilitli planda da açılır ve kapanır.
    /// Simülasyona dokunmaz; sırayı anlık görüntü verir (<see cref="Layout"/>).
    /// </summary>
    public sealed class BostaDeckView : MonoBehaviour
    {
        enum Wave { Relayout, Open, Collect }

        struct Track
        {
            public float Start, End;
        }

        const float Eps = 1e-10f;

        readonly List<BoardCardView> cards = new List<BoardCardView>();
        readonly HashSet<BoardCardView> members = new HashSet<BoardCardView>();
        readonly HashSet<BoardCardView> previous = new HashSet<BoardCardView>();
        readonly HashSet<BoardCardView> incoming = new HashSet<BoardCardView>();
        readonly Dictionary<BoardCardView, Track> tracks = new Dictionary<BoardCardView, Track>();

        bool open;
        // TakeOne'ın bıraktığı boşluk (bir sonraki Layout'a dek): öbür kartlar asıl yerlerinde kalır
        int gapIndex = -1, gapCount;
        // Alınan kart ve geri dönüş yeri
        BoardCardView taken;
        int takenIndex = -1, takenCount;
        Predicate<BoardCardView> notMember;

        bool NotMember(BoardCardView c) => !members.Contains(c);

        public bool IsOpen => open;
        /// <summary>Destedeki kartlar, üstten alta (alınan kart dahil değil).</summary>
        public IReadOnlyList<BoardCardView> Cards => cards;
        public BoardCardView Top => cards.Count > 0 ? cards[0] : null;
        public bool Contains(BoardCardView c) => c != null && cards.Contains(c);
        public int IndexOf(BoardCardView c) => c != null ? cards.IndexOf(c) : -1;
        /// <summary>Son <see cref="TakeOne"/> ile alınan kart (geri dönene ya da yeni düzene dek).</summary>
        public BoardCardView Taken => taken;

        public event Action<BoardSound> Sound;

        static float SwapLiftMm => DeckMotion.Num("cards.swapLiftMm", 6f);

        // ------------------------------------------------------------------ açma, kapama

        public void Toggle()
        {
            if (open) Close(true);
            else Open(true);
        }

        /// <summary>Eli açar: üst kart tepside kalır, diğerleri sırayla (j·0,07 s) sola kayar.</summary>
        public void Open(bool animate = true)
        {
            if (open) return;
            open = true;
            ApplyLayout(animate, Wave.Open, members);
            if (animate && cards.Count > 1) Sound?.Invoke(BoardSound.DeckSlide);
        }

        /// <summary>Eli toplar: en soldaki (alttaki) kart önce.</summary>
        public void Close(bool animate = true)
        {
            if (!open) return;
            open = false;
            ApplyLayout(animate, Wave.Collect, members);
            if (animate && cards.Count > 1) Sound?.Invoke(BoardSound.DeckCollect);
        }

        // ------------------------------------------------------------------ düzen

        /// <summary>
        /// Destenin sırası (anlık görüntünün <c>DeckOrder</c>'ı, üstten alta; sürükleme denetleyicisinin tuttuğu kart yok).
        /// <paramref name="animate"/> yanlışsa her kart anında yerine konur; doğruysa yeri değişen kart 0,32 s'de gider (süren
        /// hareket o anki yerinden devam eder). Önceki <see cref="TakeOne"/> boşluğu kapanır.
        /// </summary>
        public void Layout(IReadOnlyList<BoardCardView> topToBottom, bool animate)
        {
            previous.Clear();
            for (int i = 0; i < cards.Count; i++) if (cards[i] != null) previous.Add(cards[i]);
            if (taken != null) previous.Add(taken);

            cards.Clear();
            members.Clear();
            if (topToBottom != null)
                for (int i = 0; i < topToBottom.Count; i++)
                {
                    var c = topToBottom[i];
                    if (c == null || members.Contains(c)) continue;
                    cards.Add(c);
                    members.Add(c);
                }
            gapIndex = -1;
            if (taken != null && members.Contains(taken)) { taken = null; takenIndex = -1; }
            foreach (var c in previous) if (!members.Contains(c)) tracks.Remove(c);
            // Kabul edilip bu düzende yer almayan kart artık beklenmiyor
            if (incoming.Count > 0)
            {
                if (notMember == null) notMember = NotMember;
                incoming.RemoveWhere(notMember);
            }

            ApplyLayout(animate, Wave.Relayout, previous);
        }

        /// <summary>
        /// Kart desteden alındı (sürükleme başı): sıradan çıkar, açık el alttan başlayarak toplanır; öbür kartlar asıl yerlerinde
        /// kalır (alınanın yeri boş). Geri dönüş yeri <see cref="TryReturnPose"/>.
        /// </summary>
        public void TakeOne(BoardCardView c)
        {
            int i = IndexOf(c);
            if (i < 0) return;
            int n = SlotCount;
            int j = SlotOf(i);
            taken = c;
            takenIndex = j;
            takenCount = n;
            cards.RemoveAt(i);
            members.Remove(c);
            tracks.Remove(c);
            incoming.Remove(c);
            // İkinci bir boşluk tek indeksle anlatılamaz (aynı anda iki kart tutulmaz); o durumda yeniden numaralanır.
            if (gapIndex < 0) { gapIndex = j; gapCount = n; }
            else gapIndex = -1;

            bool wasOpen = open;
            open = false;
            ApplyLayout(true, Wave.Collect, members);
            if (wasOpen && cards.Count > 0) Sound?.Invoke(BoardSound.DeckCollect);
        }

        /// <summary>
        /// BOŞTA'ya kabul edilen kart: o anki pozundan (bırakıldığı yer) sıradaki yerine 0,32 s'de kayar, kalkışı iner. Kart henüz
        /// destede değilse bir sonraki <see cref="Layout"/>'ta kayar.
        /// </summary>
        public void AcceptDrop(BoardCardView c)
        {
            if (c == null) return;
            if (c == taken) { taken = null; takenIndex = -1; }
            Sound?.Invoke(BoardSound.DeckSlide);
            int i = IndexOf(c);
            if (i < 0) { incoming.Add(c); return; }
            incoming.Remove(c);
            Slide(c, PoseOf(SlotOf(i), SlotCount));
        }

        /// <summary>
        /// Alınan kartın geri dönüş yeri (model yereli): desteye alındığı sıradaki yeri (toplanmış destede). Bu kart alınan değilse
        /// false.
        /// </summary>
        public bool TryReturnPose(BoardCardView c, out Vector3 local)
        {
            local = default;
            if (c == null || c != taken || takenIndex < 0) return false;
            local = PoseOf(takenIndex, takenCount);
            return true;
        }

        int SlotCount => gapIndex >= 0 ? gapCount : cards.Count;
        int SlotOf(int i) => gapIndex >= 0 && i >= gapIndex ? i + 1 : i;
        Vector3 PoseOf(int j, int n) => open ? DeckMotion.OpenLocal(j, n) : DeckMotion.ClosedLocal(j, n);

        void Slide(BoardCardView c, Vector3 target)
        {
            float per = BoardLayout.DeckPerCardS;
            float now = Time.unscaledTime;
            c.MoveTo(target, 0, per, CardEase.EaseInOut);
            c.SetLift(0, 0, per);
            tracks[c] = new Track { Start = now, End = now + per };
        }

        void ApplyLayout(bool animate, Wave wave, HashSet<BoardCardView> known)
        {
            float now = Time.unscaledTime;
            float per = BoardLayout.DeckPerCardS;
            int n = SlotCount;
            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                if (c == null) continue;
                int j = SlotOf(i);
                Vector3 target = PoseOf(j, n);

                if (!animate)
                {
                    c.Place(target, 0);
                    tracks.Remove(c);
                    incoming.Remove(c);
                    continue;
                }
                if (incoming.Remove(c)) { Slide(c, target); continue; }

                Vector3 basePos = BasePose(c);
                if ((basePos - target).sqrMagnitude < Eps && Mathf.Abs(Mathf.DeltaAngle(c.TargetAngle, 0)) < 0.05f)
                {
                    // Zaten orada (ya da gecikmesini bekleyen hareket henüz başlamadı): bekleyen hareket iptal
                    if (c.Moving && (c.Target - target).sqrMagnitude >= Eps) c.Place(target, 0);
                    tracks.Remove(c);
                    continue;
                }
                if (c.Moving && (c.Target - target).sqrMagnitude < Eps) continue;   // zaten oraya gidiyor

                if (known != null && !known.Contains(c))
                {
                    // Desteye dışarıdan gelen (takasta cepten): 6 mm kalkışlı düz yol, takasla aynı anda
                    c.ArcTo(target, 0, per, SwapLiftMm);
                    tracks[c] = new Track { Start = now, End = now + per };
                    continue;
                }

                bool inFlight = c.Moving && tracks.TryGetValue(c, out var tr) && now >= tr.Start && now < tr.End;
                if (inFlight)
                {
                    float full = (DeckMotion.OpenLocal(j, n) - DeckMotion.ClosedLocal(j, n)).magnitude;
                    float seconds = DeckMotion.ReverseSeconds((basePos - target).magnitude, full);
                    c.MoveTo(target, 0, seconds, CardEase.EaseInOut);
                    tracks[c] = new Track { Start = now, End = now + seconds };
                    continue;
                }

                float delay;
                switch (wave)
                {
                    case Wave.Open: delay = DeckMotion.OpenDelay(j, n); break;
                    case Wave.Collect: delay = DeckMotion.CollectDelay(j, n); break;
                    default:
                        // Süren dalganın henüz başlamamış kartı sırasını korur
                        delay = tracks.TryGetValue(c, out var pending) && pending.Start > now ? pending.Start - now : 0f;
                        break;
                }
                c.MoveTo(target, 0, per, CardEase.EaseInOut, delay);
                tracks[c] = new Track { Start = now + delay, End = now + delay + per };
            }
        }

        /// <summary>Kartın kalkışsız pozu (kalkış −z'de; sarsıntı yok sayılır).</summary>
        static Vector3 BasePose(BoardCardView c)
        {
            var p = c.transform.localPosition;
            p.z += c.LiftMm * 0.001f;
            return p;
        }

        void OnDisable()
        {
            incoming.Clear();
            tracks.Clear();
        }
    }

    /// <summary>
    /// BOŞTA destesinin saf hesabı (testler doğrudan çağırır): kapalı ve açık pozlar, açılış/toplama gecikmeleri, yarıda tersine
    /// dönüşün süresi ve yumuşatılmış örnek. Eğri <see cref="CardEasing"/> (in-out), kartın kendi ara geçişiyle aynıdır.
    /// </summary>
    public static class DeckMotion
    {
        internal static float Num(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        /// <summary>Tersine dönüşün en kısa süresi (s).</summary>
        public static float MinReverseS => Num("motion.board.deckReverseMinS", 0.08f);

        /// <summary>Kapalı destede j. kartın (0 = üst) model yerelindeki pozu.</summary>
        public static Vector3 ClosedLocal(int j, int n) =>
            BoardLayout.MmToLocal(BoardLayout.DeckClosed(j), BoardLayout.CardZ(BoardLayout.DeckClosedLevel(j, Mathf.Max(1, n))));

        /// <summary>Açık elde j. kartın pozu (üst kart tepside, diğerleri sola).</summary>
        public static Vector3 OpenLocal(int j, int n) =>
            BoardLayout.MmToLocal(BoardLayout.HandOpen(j, Mathf.Max(1, n)), BoardLayout.CardZ(BoardLayout.HandLevel(j, Mathf.Max(1, n))));

        /// <summary>Açılışta j. kartın gecikmesi: üstten başlar, j·0,07 s.</summary>
        public static float OpenDelay(int j, int n) => BoardLayout.DeckStart(Mathf.Clamp(j, 0, Mathf.Max(0, n - 1)));

        /// <summary>Toplamada j. kartın gecikmesi: alttan (en soldaki) başlar, (n − 1 − j)·0,07 s.</summary>
        public static float CollectDelay(int j, int n) => BoardLayout.DeckStart(Mathf.Clamp(n - 1 - j, 0, Mathf.Max(0, n - 1)));

        /// <summary>Açılışın ya da toplamanın toplam süresi: 0,32 + (n − 1)·0,07 s (n = 10 → 0,95 s).</summary>
        public static float WaveSeconds(int n) => BoardLayout.DeckEnd(Mathf.Max(0, n - 1));

        /// <summary>
        /// Tersine dönüşün süresi: kalan yolun (<paramref name="remaining"/>) kartın tam yoluna (<paramref name="full"/>) oranı ×
        /// 0,32 s, en az 0,08 s. Böylece tepe hızı ilk hareketinkini aşmaz.
        /// </summary>
        public static float ReverseSeconds(float remaining, float full)
        {
            float per = BoardLayout.DeckPerCardS;
            float frac = full > 1e-6f ? Mathf.Clamp01(remaining / full) : 0f;
            return Mathf.Max(MinReverseS, per * frac);
        }

        /// <summary>Hareketin <paramref name="t"/> anındaki yumuşatılmış ilerlemesi (gecikme dahil; 0 → 1).</summary>
        public static float Progress(float delay, float seconds, float t)
        {
            if (t <= delay) return 0f;
            if (seconds <= 0f) return 1f;
            return CardEasing.Evaluate(CardEase.EaseInOut, (t - delay) / seconds);
        }

        /// <summary>Kartın <paramref name="t"/> anındaki yeri (from → to, gecikme ve süreyle).</summary>
        public static Vector3 Sample(Vector3 from, Vector3 to, float delay, float seconds, float t) =>
            Vector3.LerpUnclamped(from, to, Progress(delay, seconds, t));
    }
}
