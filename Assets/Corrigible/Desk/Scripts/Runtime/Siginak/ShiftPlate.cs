using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Vardiya şalterinin pirinç plakasının ikinci satırı (Revizyon 3 · R19a, <c>TXT_Plate2</c>; PLAN §1.4, §1.9). Metin ve renk
    /// şalterin durumundan gelir: hazır “kapağı kaldır”, kapak kalkık “kolu indir”, fiş bekliyor “fişi EVRAK’a koy” (kırmızı,
    /// <c>shiftClose.plate.line2InkWaiting</c>), kilitli kısa neden (ör. “baskın sürüyor”), ret “fiş basılamadı”; öteki durumlar
    /// <c>shiftClose.plate.line2Ink</c>. R19 kipinde üzerine gelme etiketinin satırını (<see cref="DeskTarget.LineOverride"/>)
    /// yalnız bu bileşen yazar: kilitliyken uzun neden, değilse plakanın metni. Dil değişince (<c>DeskLoc.Changed</c>) yeniden yazar.
    /// <para>
    /// Kilit nedenleri köprüden o anki dilde gelir (<see cref="ShiftSwitch.SetDisabled(bool, string, string)"/>); dil değişince
    /// köprünün sonraki kilit denetimi yeni dildeki metni getirir. Plakanın kendi metinleri burada çevrilir. Edit modda
    /// <c>OnEnable</c> çalışmadığı için dil aboneliği ilk <see cref="Show"/>'da da kurulur.
    /// </para>
    /// </summary>
    public sealed class ShiftPlate : MonoBehaviour
    {
        [SerializeField] TMP_Text line2;
        [SerializeField] DeskTarget target;

        ShiftPhase phase = ShiftPhase.Ready;
        string lockPlate, lockLine, shownLang;
        bool shown, subscribed;

        /// <summary>Plakada o an yazan metin (o anki dilde).</summary>
        public string Text { get; private set; } = string.Empty;
        /// <summary>Son gösterilen durum.</summary>
        public ShiftPhase Phase => phase;
        /// <summary>Plakanın ikinci satırı (sahne bağı; testler).</summary>
        public TMP_Text Line2 => line2;
        /// <summary>Üzerine gelme satırı yazılan hedef (sahne bağı; testler).</summary>
        public DeskTarget Target => target;

        /// <summary>Tek seferlik sahne script'i ve testler bağlar.</summary>
        public void Configure(TMP_Text line, DeskTarget clickTarget)
        {
            line2 = line;
            target = clickTarget;
            shown = false;
        }

        void OnEnable() => Subscribe();

        void OnDisable()
        {
            if (!subscribed) return;
            DeskLoc.Instance.Changed -= Relabel;
            subscribed = false;
        }

        void Subscribe()
        {
            if (subscribed) return;
            subscribed = true;
            DeskLoc.Instance.Changed += Relabel;
        }

        /// <summary>
        /// Plakayı duruma göre yazar. <paramref name="lockPlate"/> kilitliyken plakanın kısa nedeni, <paramref name="lockLine"/>
        /// üzerine gelme satırının uzun nedeni (kaynak değil, gösterilecek metin). Girdiler ve dil aynıysa bir şey yapmaz (şalter her
        /// karede çağırabilir).
        /// </summary>
        public void Show(ShiftPhase phase, string lockPlate, string lockLine)
        {
            Subscribe();
            if (shown && this.phase == phase && this.lockPlate == lockPlate && this.lockLine == lockLine && shownLang == Loc.Lang) return;
            this.phase = phase;
            this.lockPlate = lockPlate;
            this.lockLine = lockLine;
            Write();
        }

        /// <summary>Son durumu o anki dilde yeniden yazar (<c>DeskLoc.Changed</c>; henüz gösterilmediyse bir şey yapmaz).</summary>
        public void Relabel()
        {
            if (this == null)
            {
                // Edit modda yok edilen bileşenin OnDisable'ı çalışmaz: abonelik kendini temizler.
                DeskLoc.Instance.Changed -= Relabel;
                return;
            }
            if (shown) Write();
        }

        void Write()
        {
            shown = true;
            shownLang = Loc.Lang;
            Text = TextFor(phase, lockPlate);
            if (line2 != null)
            {
                line2.text = Text;
                line2.color = InkFor(phase);
            }
            if (target != null)
                target.LineOverride = phase == ShiftPhase.Locked
                    ? (!string.IsNullOrEmpty(lockLine) ? lockLine : NullIfEmpty(lockPlate))
                    : NullIfEmpty(Text);
        }

        /// <summary>Durumun plaka metni (o anki dilde). Kilitliyken köprünün verdiği kısa neden.</summary>
        public static string TextFor(ShiftPhase phase, string lockPlate)
        {
            switch (phase)
            {
                case ShiftPhase.CoverUp: return Loc.T("kolu indir");
                case ShiftPhase.Waiting: return Loc.T("fişi EVRAK’a koy");
                case ShiftPhase.Locked: return lockPlate ?? string.Empty;
                case ShiftPhase.Rejecting: return Loc.T("fiş basılamadı");
                default: return Loc.T("kapağı kaldır");
            }
        }

        /// <summary>Durumun mürekkebi: fiş bekliyor kırmızı, öteki durumlar koyu kahve (token; yedekler token değerleri).</summary>
        public static Color InkFor(ShiftPhase phase) => phase == ShiftPhase.Waiting
            ? Col("shiftClose.plate.line2InkWaiting", new Color32(0x7A, 0x18, 0x10, 0xFF))
            : Col("shiftClose.plate.line2Ink", new Color32(0x4A, 0x3A, 0x22, 0xFF));

        static Color Col(string key, Color fallback)
        {
            var t = SiginakTheme.Active;
            return t != null && t.TryGet(key, out var c) ? c : fallback;
        }

        static string NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;
    }
}
