using Corrigible.Core;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Son şalterin gerekçe plakası (KOS aşama 6): pirinç plakanın iki satırı ve üzerine gelme satırı. İlk satır şalterin adı
    /// ("SON ŞALTER" / "LAST SWITCH"; oyunun adı olan "SON ŞALTER" anahtarından ayrı, bu yüzden DeskLocText değil burada yazılır).
    /// İkinci satır hazırken çekilirse hangi çeşidin geleceğini ("çekilirse: ÇEKİLME" / "çekilirse: TAHLİYE"), kapak kalkıkken
    /// "ÇEKİLME · kolu indir" / "TAHLİYE · kolu indir", kol inikken "ÇEKİLME · damgala" / "TAHLİYE · damgala",
    /// kilitliyken kısa nedeni ("mühürlü · 5. vardiya", "mühürlü · rehber") yazar. Tahliyede mürekkep kırmızı
    /// (<c>lastSwitch.plate.line2InkEvac</c>), değilse <c>lastSwitch.plate.line2Ink</c>. Kilit ve çeşit metinleri köprüden o anki
    /// dilde gelir (köprü her karede yeniden verir); plakanın kendi kısa metinleri burada çevrilir. Simülasyonu görmez.
    /// </summary>
    public sealed class LastSwitchPlate : MonoBehaviour
    {
        [Tooltip("Birinci satır: şalterin adı (o anki dilde, büyük harf).")]
        [SerializeField] TMP_Text line1;
        [SerializeField] TMP_Text line2;
        [SerializeField] DeskTarget target;

        /// <summary>Plakada o an yazan metin.</summary>
        public string Text { get; private set; } = string.Empty;
        /// <summary>Üzerine gelme satırı.</summary>
        public string Line { get; private set; } = string.Empty;
        /// <summary>Son gösterilen durum.</summary>
        public ShiftPhase Phase { get; private set; } = ShiftPhase.Ready;
        public TMP_Text Line1 => line1;
        public TMP_Text Line2 => line2;
        public DeskTarget Target => target;

        /// <summary>Tek seferlik sahne script'i ve testler bağlar.</summary>
        public void Configure(TMP_Text line, DeskTarget clickTarget, TMP_Text name = null)
        {
            line2 = line;
            target = clickTarget;
            if (name != null) line1 = name;
        }

        /// <summary>Şalterin adı, o anki dilde ("SON ŞALTER" / "LAST SWITCH").</summary>
        public static string Name => DeskText.Upper(Loc.T("son şalter"));


        /// <param name="phase">Şalterin durumu.</param>
        /// <param name="lockPlate">Kilitliyken kısa neden; kilit yoksa null.</param>
        /// <param name="lockLine">Kilitliyken uzun neden (üzerine gelme satırı).</param>
        /// <param name="variantPlate">Çekilirse gelecek çeşit (kısa).</param>
        /// <param name="variantLine">Çekilirse gelecek çeşit (uzun; üzerine gelme satırı).</param>
        /// <param name="evac">Çekilirse tahliye: mürekkep kırmızı.</param>
        public void Show(ShiftPhase phase, string lockPlate, string lockLine, string variantPlate, string variantLine, bool evac)
        {
            Phase = phase;
            string text, line;
            switch (phase)
            {
                case ShiftPhase.Locked:
                    text = lockPlate ?? string.Empty;
                    line = lockLine ?? text;
                    break;
                case ShiftPhase.CoverUp:
                    // Kolu indireceği an da çeşit plakada okunur (teleks ve kenar şeridi KLS'de kadraj dışında)
                    text = evac ? Loc.T("TAHLİYE · kolu indir") : Loc.T("ÇEKİLME · kolu indir");
                    line = variantLine;
                    break;
                case ShiftPhase.Waiting:
                    text = evac ? Loc.T("TAHLİYE · damgala") : Loc.T("ÇEKİLME · damgala");
                    line = variantLine;
                    break;
                case ShiftPhase.Rejecting:
                    text = Loc.T("çekilemedi");
                    line = lockLine ?? variantLine;
                    break;
                default:
                    text = variantPlate ?? string.Empty;
                    line = variantLine;
                    break;
            }
            Text = text;
            Line = line ?? string.Empty;
            if (line1 != null)
            {
                string name = Name;
                if (line1.text != name) line1.text = name;
            }
            if (line2 != null)
            {
                if (line2.text != text) line2.text = text;
                var theme = SiginakTheme.Active;
                if (theme != null)
                    line2.color = theme.ColorOf(evac && phase != ShiftPhase.Locked ? "lastSwitch.plate.line2InkEvac" : "lastSwitch.plate.line2Ink");
            }
            if (target != null) target.LineOverride = Line;
        }
    }
}
