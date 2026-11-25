using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masadaki teleks kâğıdı (K1 notu): başlık “TELEKS · KYGA-07” ve son satırlar; en yeni satır makineye en yakın olan.
    /// Yeni satır harf harf basılır (motion.telexCharS; ya da satır başına verilen toplam süre, R19b2 1,2 s). Kod CP 700, durum CP 700 (yapılamadıysa kırmızı), ileti CP 400.
    /// Revizyon 2 (S1): son ileti makinenin önüne asılı ön kâğıtta büyük yazılır (K1'den okunur); yeni iletide eski ön kâğıt
    /// koparılır ve düşer.
    /// Revizyon 3: makineden sarkan kâğıdın dört yuvası (<see cref="frontCode"/>, durum, ileti, önceki satır) tek ön yazının
    /// yerini alır.
    /// Teleks daktilosu (kullanıcının modeli): rulo daktilonun arkasındadır ve satır basılırken döner (<see cref="roll"/>, harf başına
    /// <c>motion.telexRollDegPerChar</c>).
    /// <para>
    /// Tek kâğıt akışı (teleks ve devir kâğıdı birleşti): silindirde her an en çok bir kâğıt durur. Yeni satırda o anki teleks kâğıdı
    /// (en az bir harfi basılmışsa) düşer (<see cref="torn"/>: aynı TelexPaper'ın kopyası, <see cref="PlatenFeed.DropPose(float, SiginakTheme)"/>),
    /// yeni kâğıt (<see cref="sheet"/>) aynı karede silindirin içine geri alınır ve satır satır yazılarak yükselir: o an basılan satırın
    /// taban çizgisi yazım çizgisinde (<see cref="printLine"/>) durur, satır değişince <c>motion.telexFeed.slideS</c>'de ease-out kayar,
    /// satır bitince dinlenme pozuna çıkar (<see cref="Settled"/>). Devir fişi silindiri tuttuğunda (<see cref="PlatenBusy"/>, köprü
    /// yazar; her kare okunur) o anki kâğıt düşer, yeni satırlar kâğıda basılmaz (yalnız kaydedilir; okunur sayılmadığı için ekran
    /// şeridine düşer). <see cref="Typing"/> basım sürerken true (devir fişi beklerken okunur: önce teleks satırı biter, sonra fiş basılır).
    /// </para>
    /// </summary>
    public sealed class Teleprinter : MonoBehaviour
    {
        [SerializeField] TMP_Text paper;
        [SerializeField] Renderer paperRenderer;
        [SerializeField, Min(1)] int visibleLines = 5;
        [SerializeField] string header = "TELEKS · KYGA-07";
        [Tooltip("Ön kâğıt: son ileti (kod, durum, ileti) ve altında bir önceki satır.")]
        [SerializeField] TMP_Text front;
        [Tooltip("Revizyon 3 kâğıt yuvaları (TelexPaper TXT_Code/Status/Message/Prev). Durum yuvası doluysa ön yazı yerine bunlar yazılır.")]
        [SerializeField] TMP_Text frontCode, frontStatus, frontMessage, frontPrev;
        [Tooltip("Düşen kâğıdın kopyası (kapalı durur). Tek kâğıt akışında TelexPaper'ın ikinci örneğini taşır.")]
        [SerializeField] Transform torn;
        [Tooltip("Revizyon 2 kopyasının birleşik yazısı (kopyanın yuvaları yoksa).")]
        [SerializeField] TMP_Text tornText;
        [SerializeField, Min(0.05f)] float tearSeconds = 0.4f;
        [Tooltip("Daktilonun arkasındaki kâğıt rulosu (kendi x ekseninde döner). Boşsa dönen rulo yok.")]
        [SerializeField] Transform roll;
        [Header("Tek kâğıt akışı (silindir)")]
        [Tooltip("Teleks kâğıdının kökü (TelexPaper): yeni satırda silindirin içine geri alınır ve satır satır yükselir; boşsa kâğıt kıpırdamaz.")]
        [SerializeField] Transform sheet;
        [Tooltip("Kâğıdın TELEKS hedef kutusu (Paper target): kâğıt silindirde yokken kapalı.")]
        [SerializeField] Collider sheetTarget;
        [Tooltip("Yazım çizgisi: silindirin üstü, kâğıt düzleminde; yukarı ekseni kâğıdın çıkış yönü.")]
        [SerializeField] Transform printLine;
        [Tooltip("Düşen kopyanın dört yuvası (Torn sheet/TelexPaper); doluysa yuva metinleri bire bir kopyalanır.")]
        [SerializeField] TMP_Text tornCode, tornStatus, tornMessage, tornPrev;

        readonly List<TelexLine> shown = new List<TelexLine>();
        float typedSince, typeSeconds = -1, tornAt = -100;
        int typingLength, rolledChars, renderedChars;
        bool typing;
        Quaternion rollRest;
        bool rollRestSet;
        Vector3 tornPos;
        Quaternion tornRot;
        Pose tornRest;
        bool tornRestSet;
        string fitStatus, tornStatusKey, frontComposite = "";
        int fitPercent = StatusPercent, tornPercent = StatusPercent;

        // silindir
        bool sheetPresent = true, restSet, busySeen;
        Pose sheetRest;
        Vector3 retractAxis = Vector3.up, restCenterLocal, restStatusLocal;
        float retract, retractFrom, retractTo, stepAt;
        int instantUntilFrame = -10;
        readonly List<int> rowStarts = new List<int>();
        readonly List<float> rowRetract = new List<float>();

        /// <summary>Ön kâğıttaki durum satırının boyu (ileti %100; S1: durum 66 / ileti 50 doku px).</summary>
        const int StatusPercent = 132;
        /// <summary>Revizyon 3 durum yuvası kendi puntosunda (büyük harf 15 mm); sığmazsa ileti boyuna (10 mm) kadar küçülür.</summary>
        const int SlotStatusFloor = 67;
        /// <summary>TMP'nin <c>maxVisibleCharacters</c> varsayılanı (bütün harfler görünür).</summary>
        const int AllCharacters = 99999;

        bool Slots => frontStatus != null;
        bool HasFront => front != null || Slots;

        public void Configure(TMP_Text text, Renderer renderer, string headerSource) { paper = text; paperRenderer = renderer; header = headerSource; }

        /// <summary>Kâğıt rulosu (tek seferlik sahne script'i ve testler).</summary>
        public void ConfigureRoll(Transform paperRoll) { roll = paperRoll; rollRestSet = false; }

        /// <summary>En yeni satır harf harf basılıyor.</summary>
        public bool Typing => typing;

        /// <summary>Rulonun basılan harflerle döndüğü toplam açı (derece; yalnız sunum).</summary>
        public float RollDegrees { get; private set; }

        /// <summary>
        /// Silindir devir fişinde mi (köprü yazar: fiş besleniyor, dilde, elde, sürükleniyor ya da EVRAK'tan dönüyor; boş: hiç). Her
        /// kare okunur: false → true geçişinde o anki teleks kâğıdı düşer; doluyken yeni satırlar kâğıda basılmaz.
        /// </summary>
        public Func<bool> PlatenBusy { get; set; }

        /// <summary>Silindirde teleks kâğıdı var (görünür).</summary>
        public bool SheetPresent => sheetPresent;

        /// <summary>Basım bitti ve kâğıt dinlenme pozunda (ya da silindirde kâğıt yok): son satır kayışı da bitti.</summary>
        public bool Settled => !typing && (sheet == null || !sheetPresent || (retract <= 1e-6f && retractTo <= 1e-6f));

        /// <summary>Kâğıdın dinlenme pozundan silindirin içine geri alındığı mesafe (m; yalnız sunum).</summary>
        public float SheetRetractM => retract;

        /// <summary>Düşen kopya hâlâ düşüyor.</summary>
        public bool Dropping => torn != null && tornAt >= 0 && torn.gameObject.activeSelf;

        /// <summary>Son ölçülen satırların geri alma mesafeleri (m; yukarıdan aşağı: kod, durum, ileti satırları; testler).</summary>
        public IReadOnlyList<float> RowRetracts => rowRetract;
        /// <summary>Son ölçülen satırların basım sırasındaki ilk harfi (<see cref="RowRetracts"/> ile aynı sırada).</summary>
        public IReadOnlyList<int> RowStarts => rowStarts;

        bool Busy => sheet != null && PlatenBusy != null && PlatenBusy();
        Transform Frame => sheet != null && sheet.parent != null ? sheet.parent : transform;
        static float SlideS => SiginakTheme.Active != null ? SiginakTheme.Active.NumberOf("motion.telexFeed.slideS", 0.06f) : 0.06f;

        void Awake() => EnsureRest();

        /// <summary>Kâğıdın dinlenme pozu ve okunurluk ölçüsünün noktaları kaydedilmiş pozdan bir kez alınır (EditMode testleri Awake'siz).</summary>
        void EnsureRest()
        {
            if (restSet || sheet == null) return;
            restSet = true;
            var frame = Frame;
            sheetRest = new Pose(sheet.localPosition, sheet.localRotation);
            var up = printLine != null ? frame.InverseTransformDirection(printLine.up) : sheet.localRotation * Vector3.up;
            retractAxis = up.sqrMagnitude > 1e-8f ? up.normalized : Vector3.up;
            restCenterLocal = paperRenderer != null ? frame.InverseTransformPoint(paperRenderer.bounds.center) : sheet.localPosition;
            var status = Slots ? frontStatus : front;
            restStatusLocal = status != null ? frame.InverseTransformPoint(status.transform.position) : restCenterLocal;
        }

        void EnsureTornRest()
        {
            if (tornRestSet || torn == null) return;
            tornRestSet = true;
            tornRest = new Pose(torn.localPosition, torn.localRotation);
        }

        /// <summary>
        /// Son satırları gösterir; <paramref name="animateNewest"/> ise en yeni satır harf harf basılır. <paramref name="lineSeconds"/>
        /// &gt; 0: satırın tamamı o sürede (harf başına süre satır uzunluğuna bölünür; TR ve EN aynı sürede biter), değilse harf
        /// başına <c>motion.telexCharS</c>. Tek kâğıt akışında (Play): o anki kâğıt düşer, yenisi alttan yazılarak çıkar; silindir
        /// fişteyken satır yalnız kaydedilir. Animasyonsuz çağrı (kayıttan devam, temizleme) kâğıdı anında dinlenme pozunda gösterir
        /// (liste boşsa silindir boş) ve bir sonraki kareye dek fişin silindir değişimini de anında uygular (düşüş yok).
        /// </summary>
        public void Show(IReadOnlyList<TelexLine> lines, bool animateNewest, float lineSeconds = -1)
        {
            EnsureRest();
            bool play = Application.isPlaying;
            // Revizyon 2/3 ön kâğıdı aynı satırın yeniden gösterilişinde koparılmaz; tek kâğıt akışında her yeni satır (TelexFeed.Post her
            // çağrıda bir satır ekler; aynı metinli başlık satırı da) yeni kâğıda basılır, o anki kâğıt düşer
            bool newer = lines.Count > 0 && (shown.Count == 0 || !Same(lines[lines.Count - 1], shown[0]));
            bool busy = Busy;
            if (sheet != null)
            {
                if (!animateNewest) instantUntilFrame = Time.frameCount + 1;
                SyncBusy(busy);
            }
            if (sheet == null)
            {
                // Revizyon 2/3 yolu (kâğıt kıpırdamaz): eski ön kâğıt koparılır, kopyası düşer
                if (animateNewest && play && torn != null && HasFront && shown.Count > 0 && newer) Drop();
            }
            else if (animateNewest && play && !busy && sheetPresent && renderedChars > 0 && sheet.gameObject.activeSelf)
                Drop();

            shown.Clear();
            for (int i = lines.Count - 1; i >= 0 && shown.Count < visibleLines; i--) shown.Add(lines[i]);
            typedSince = Time.unscaledTime;
            typeSeconds = lineSeconds;
            typingLength = shown.Count > 0 ? Plain(shown[0]).Length : 0;
            rolledChars = 0;

            if (sheet != null && busy)
            {
                // Silindirde fiş: satır kâğıda basılmaz (yuvalar yazılır, kâğıt gizli kalır)
                typing = false;
                Render(typingLength);
                SetSheet(false);
                return;
            }
            typing = animateNewest && shown.Count > 0 && play;
            if (sheet == null)
            {
                Render(typing ? 0 : typingLength);
                return;
            }
            if (typing)
            {
                // Yeni kâğıt: tam metinle satırlar ölçülür, kâğıt ilk satırı yazım çizgisinde olacak kadar geri alınır
                SetSheet(true);
                Render(typingLength);
                MeasureRows();
                retract = retractFrom = retractTo = PlatenFeed.RowRetract(0, typingLength, rowStarts, rowRetract);
                stepAt = Time.unscaledTime;
                Place();
                Render(0);
                return;
            }
            retract = retractFrom = retractTo = 0;
            Place();
            // Play'de boş liste silindiri boşaltır; Edit modda kâğıt görünür kalır (kaydedilmiş sahne kirlenmez)
            SetSheet(!play || shown.Count > 0);
            Render(typingLength);
            if (shown.Count > 0) MeasureRows();
        }

        static bool Same(TelexLine a, TelexLine b) => a.code == b.code && a.status == b.status && a.message == b.message;

        void Update()
        {
            Tear();
            if (sheet != null && Application.isPlaying) ApplyBusy();
            if (typing)
            {
                int n = TypedChars(Time.unscaledTime - typedSince, typingLength, typeSeconds, SiginakTheme.Active.Number("motion.telexCharS", 0.03f));
                Render(n);
                Roll(n);
                float target = PlatenFeed.RowRetract(n, typingLength, rowStarts, rowRetract);
                if (n >= typingLength) typing = false;
                StepTo(target);
            }
            Slide();
        }

        /// <summary>
        /// Fişin silindir durumunu hemen uygular (Update her kare yapar). <paramref name="instant"/>: kayıttan devam ve yeni oyun gibi
        /// anında geçiş (bir sonraki kareye dek düşüş yok; fiş silindirde değilse kâğıt son satırla dinlenme pozunda). Köprü
        /// (<c>SiginakShiftClose</c>) fişi geri yükledikten sonra çağırır: <c>GameLoaded</c> dinleyicilerinin sırası ya da geri yüklemenin
        /// bir kare gecikmesi sonucu değiştirmez.
        /// </summary>
        public void SyncPlaten(bool instant)
        {
            EnsureRest();
            if (sheet == null) return;
            if (instant) instantUntilFrame = Mathf.Max(instantUntilFrame, Time.frameCount + 1);
            ApplyBusy();
        }

        void ApplyBusy()
        {
            bool busy = Busy;
            SyncBusy(busy);
            if (busy && sheetPresent) { typing = false; SetSheet(false); }
        }

        /// <summary>
        /// Fişin silindiri tutup bırakması (çekme yöntemi): tutmaya geçişte o anki kâğıt düşer, ama animasyonsuz <see cref="Show"/>'dan
        /// sonraki kareye dek (kayıttan devam, yeni oyun) anında gizlenir; bırakmada kâğıt yalnız o pencerede geri gelir (yoksa silindir
        /// boş kalır, sonraki satır yeni kâğıda basılır).
        /// </summary>
        void SyncBusy(bool busy)
        {
            if (busy == busySeen) return;
            busySeen = busy;
            bool instant = Time.frameCount <= instantUntilFrame;
            if (busy) Eject(!instant);
            else if (instant && shown.Count > 0)
            {
                retract = retractFrom = retractTo = 0;
                Place();
                SetSheet(true);
                Render(typingLength);
            }
        }

        /// <summary>
        /// O anki teleks kâğıdını silindirden çıkarır: <paramref name="animate"/> ve en az bir harfi basılmışsa kopyası düşer; kâğıt
        /// gizlenir, basım kesilir (silindir boş). Fişin beslemesi başlarken köprünün çekme kenarı çağırır; testler doğrudan.
        /// </summary>
        public void Eject(bool animate)
        {
            EnsureRest();
            if (sheet == null) return;
            if (animate && Application.isPlaying && sheetPresent && renderedChars > 0 && sheet.gameObject.activeSelf) Drop();
            typing = false;
            retract = retractFrom = retractTo = 0;
            Place();
            SetSheet(false);
        }

        void SetSheet(bool on)
        {
            if (sheet == null) return;
            sheetPresent = on;
            if (sheet.gameObject.activeSelf != on) sheet.gameObject.SetActive(on);
            if (sheetTarget != null && sheetTarget.enabled != on) sheetTarget.enabled = on;
        }

        void StepTo(float target)
        {
            if (Mathf.Abs(target - retractTo) < 1e-6f) return;
            // Önceki kayışın şu anki değerinden (son yerleştirilen değerden değil): uzun karelerde (satır başına bir kare) her kare
            // yeni kayış sıfırdan başlayıp kâğıt yerinde saymasın
            retractFrom = PlatenFeed.Ease(retractFrom, retractTo, Time.unscaledTime - stepAt, SlideS);
            retractTo = target;
            stepAt = Time.unscaledTime;
        }

        void Slide()
        {
            if (sheet == null || !restSet) return;
            float r = PlatenFeed.Ease(retractFrom, retractTo, Time.unscaledTime - stepAt, SlideS);
            if (r == retract) return;
            retract = r;
            Place();
        }

        void Place()
        {
            if (sheet == null || !restSet) return;
            var p = sheetRest.position - retractAxis * retract;
            if (sheet.localPosition != p) sheet.localPosition = p;
            if (sheet.localRotation != sheetRest.rotation) sheet.localRotation = sheetRest.rotation;
        }

        /// <summary>
        /// Basılan satırın satırları (kod, durum, ileti satırları) ve her birinin geri alma mesafesi: satırın taban çizgisinin dinlenme
        /// pozundaki yeri yazım çizgisine kâğıdın yukarı ekseni boyunca uzaklığı (yazılar tam metinle yazılmışken ölçülür).
        /// </summary>
        void MeasureRows()
        {
            rowStarts.Clear();
            rowRetract.Clear();
            if (sheet == null || printLine == null || shown.Count == 0 || !Slots) return;
            var l = shown[0];
            string code = l.code ?? "", st = l.status ?? "", msg = l.message ?? "";
            int statusStart = code.Length + 2, messageStart = statusStart + st.Length + 2;
            var frame = Frame;
            var line = frame.InverseTransformPoint(printLine.position);
            float Retract(TMP_Text t, float baseline)
            {
                var local = frame.InverseTransformPoint(t.transform.TransformPoint(new Vector3(0, baseline, 0))) + retractAxis * retract;
                return Vector3.Dot(local - line, retractAxis);
            }
            float First(TMP_Text t)
            {
                t.ForceMeshUpdate(true, true);
                var info = t.textInfo;
                return Retract(t, info.lineCount > 0 ? info.lineInfo[0].baseline : 0);
            }
            void Add(int start, float r)
            {
                if (rowRetract.Count > 0) r = Mathf.Min(r, rowRetract[rowRetract.Count - 1]);
                rowStarts.Add(start);
                rowRetract.Add(Mathf.Max(0, r));
            }
            if (frontCode != null) Add(0, First(frontCode));
            Add(statusStart, First(frontStatus));
            if (frontMessage != null && msg.Length > 0)
            {
                frontMessage.ForceMeshUpdate(true, true);
                var info = frontMessage.textInfo;
                for (int i = 0; i < info.lineCount; i++)
                    Add(messageStart + info.lineInfo[i].firstCharacterIndex, Retract(frontMessage, info.lineInfo[i].baseline));
            }
        }

        /// <summary>Rulo basılan her harfte <c>motion.telexRollDegPerChar</c> döner (kâğıt silindire çekilir).</summary>
        void Roll(int typed)
        {
            if (roll == null || typed <= rolledChars) return;
            RollBy((typed - rolledChars) * (SiginakTheme.Active != null ? SiginakTheme.Active.Number("motion.telexRollDegPerChar", 2f) : 2f));
            rolledChars = typed;
        }

        /// <summary>Ruloyu <paramref name="degrees"/> döndürür (devir fişi beslenirken köprü çağırır; yalnız sunum).</summary>
        public void RollBy(float degrees)
        {
            if (roll == null || degrees == 0) return;
            if (!rollRestSet) { rollRest = roll.localRotation; rollRestSet = true; }
            RollDegrees = Mathf.Repeat(RollDegrees + degrees, 360f);
            roll.localRotation = rollRest * Quaternion.Euler(-RollDegrees, 0, 0);
        }

        /// <summary>
        /// Basılan satırın <paramref name="elapsed"/> saniyede görünen harf sayısı (saf). <paramref name="lineSeconds"/> &gt; 0 ise
        /// <paramref name="length"/> harf o sürede eşit aralıkla basılır; değilse harf başına <paramref name="charSeconds"/> (en az
        /// 5 ms).
        /// </summary>
        public static int TypedChars(float elapsed, int length, float lineSeconds, float charSeconds)
        {
            if (length <= 0) return 0;
            if (elapsed <= 0) return 0;
            if (lineSeconds > 0) return Mathf.Min(length, Mathf.FloorToInt(elapsed / lineSeconds * length + 1e-4f));
            return Mathf.Min(length, Mathf.FloorToInt(elapsed / Mathf.Max(0.005f, charSeconds)));
        }

        /// <summary>
        /// Kâğıdın orta noktası kameranın görüş alanında mı. Tek kâğıt akışında kâğıdın dinlenme pozuna göre ölçülür (yeni kâğıt
        /// silindirden çıkarken de aynı sonuç); silindir fişteyken teleks kâğıdı görünmez.
        /// </summary>
        public bool IsVisibleFrom(Camera view)
        {
            if (paperRenderer == null) return false;
            EnsureRest();
            if (Busy) return false;
            var centre = sheet != null ? Frame.TransformPoint(restCenterLocal) : paperRenderer.bounds.center;
            var p = view.WorldToViewportPoint(centre);
            return p.z > 0 && p.x > 0.02f && p.x < 0.98f && p.y > 0.02f && p.y < 0.98f;
        }

        /// <summary>
        /// Kâğıt görüş alanında ve satırları ekranda en az <paramref name="minPixels"/> px (1080p'ye göre) mi. Değilse satır
        /// ekranın altındaki şeritte de basılır (Aşama 10 punto taraması: masadaki kâğıt hiçbir pozda 14 px'e ulaşmıyor).
        /// Ön kâğıt varsa (Revizyon 2) onun, kâğıt yuvaları varsa (Revizyon 3) durum yuvasının satırı ölçülür (tek kâğıt akışında
        /// dinlenme pozunda). Silindir fişteyken okunmaz.
        /// </summary>
        public bool IsReadableFrom(Camera view, float minPixels)
        {
            var text = Slots ? frontStatus : front != null ? front : paper;
            if (text == null || !IsVisibleFrom(view)) return false;
            // TMP 3D: punto 10 = 1 m em; ön kâğıdın durum satırı %132, yuvada %100 (uzun durumda biraz küçülür)
            float em = text.fontSize * (HasFront ? fitPercent / 100f : 1) * 0.1f * Mathf.Abs(text.transform.lossyScale.y);
            var at = sheet != null && text != paper ? Frame.TransformPoint(restStatusLocal) : text.transform.position;
            var a = view.WorldToScreenPoint(at);
            var b = view.WorldToScreenPoint(at + text.transform.up * em);
            return Vector2.Distance(a, b) * 1080f / Mathf.Max(1, view.pixelHeight) >= minPixels;
        }

        static string Plain(TelexLine l) => l.code + "  " + l.status + "  " + l.message;

        /// <summary>
        /// Düşüş başlar: kopya o anki kâğıdın o anki pozunda (yarı çıkmış olabilir) ve aynı yazılarla açılır. Süren önceki düşüş anında
        /// biter (tek kopya).
        /// </summary>
        void Drop()
        {
            if (torn == null) return;
            EnsureTornRest();
            if (tornAt >= 0) { torn.gameObject.SetActive(false); tornAt = -100; }
            if (tornStatus != null && frontStatus != null)
            {
                if (tornCode != null) tornCode.text = frontCode != null ? frontCode.text : "";
                tornStatus.text = frontStatus.text;
                if (tornMessage != null)
                {
                    tornMessage.text = frontMessage != null ? frontMessage.text : "";
                    tornMessage.maxVisibleCharacters = frontMessage != null ? frontMessage.maxVisibleCharacters : AllCharacters;
                }
                if (tornPrev != null) tornPrev.text = frontPrev != null ? frontPrev.text : "";
            }
            else if (tornText != null) tornText.text = frontComposite;
            tornPos = tornRest.position - (sheet != null ? retractAxis * retract : Vector3.zero);
            tornRot = tornRest.rotation;
            torn.localPosition = tornPos;
            torn.localRotation = tornRot;
            tornAt = Time.unscaledTime;
            torn.gameObject.SetActive(true);
        }

        /// <summary>
        /// Düşen kâğıt (S1 notu › hareket: 0,4 s): teleks daktilosunda silindirden yukarı çekilir, arkaya atılıp daktilonun arkasına
        /// düşer ve kaybolur (<see cref="PlatenFeed.DropPose(float, SiginakTheme)"/>; token <c>motion.telexTear.*</c>). Eski teleksin
        /// ağzından öne devrilip masaya düşüş daktiloda kapağın ve klavyenin içinden geçiyordu.
        /// </summary>
        void Tear()
        {
            if (torn == null || tornAt < 0) return;
            float t = (Time.unscaledTime - tornAt) / tearSeconds;
            if (t >= 1)
            {
                torn.gameObject.SetActive(false);
                torn.localPosition = tornRest.position; torn.localRotation = tornRest.rotation;
                tornAt = -100;
                return;
            }
            var pose = PlatenFeed.DropPose(t, SiginakTheme.Active);
            torn.localPosition = tornPos + pose.position;
            torn.localRotation = tornRot * pose.rotation;
        }

        /// <summary>Düşen kâğıdın yolu (<see cref="PlatenFeed.DropPose(float, SiginakTheme)"/>; eski adı).</summary>
        public static Pose TearPose(float t, SiginakTheme theme) => PlatenFeed.DropPose(t, theme);

        /// <summary>Düşen kâğıdın yolu (<see cref="PlatenFeed.DropPose(float, Vector3, float, Vector3)"/>; eski adı).</summary>
        public static Pose TearPose(float t, Vector3 moveM, float fallM, Vector3 euler) => PlatenFeed.DropPose(t, moveM, fallM, euler);

        /// <summary>Ön kâğıt: kod (soluk), durum (büyük, yapılamadıysa kırmızı), ileti; altında önceki satır küçük.</summary>
        void RenderFront(int newestChars)
        {
            if (!HasFront) return;
            if (shown.Count == 0)
            {
                frontComposite = "";
                if (front != null) front.text = "";
                if (Slots) foreach (var slot in new[] { frontCode, frontStatus, frontMessage, frontPrev }) if (slot != null) slot.text = "";
                return;
            }
            var t = SiginakTheme.Active;
            var l = shown[0];
            string ink3 = ColorUtility.ToHtmlStringRGB(t.ColorOf("ink3")), status = ColorUtility.ToHtmlStringRGB(t.ColorOf(l.kind == TelexKind.Failed ? "status.threat" : "ink"));
            // Basılan satır harf harf: önce kod, sonra durum, sonra ileti dolar
            string code = l.code ?? "", st = l.status ?? "", msg = l.message ?? "";
            int n = newestChars;
            string Take(ref int left, string s) { var r = s.Substring(0, Mathf.Clamp(left, 0, s.Length)); left -= s.Length + 2; return r; }
            string codeText = Take(ref n, code), statusText = Take(ref n, st), messageText = Take(ref n, msg);
            string prevText = shown.Count > 1 && newestChars >= Plain(l).Length ? shown[1].code + "  " + shown[1].status : "";
            int fit = StatusFit(st);
            if (Slots)
            {
                if (frontCode != null) frontCode.text = "<b><color=#" + ink3 + ">" + codeText + "</color></b>";
                frontStatus.text = "<size=" + fit + "%><b><color=#" + status + ">" + statusText + "</color></b></size>";
                if (frontMessage != null)
                {
                    // Tam metin yazılır, harfler maxVisibleCharacters ile açılır: satır sarması basım boyunca tam metninki (MeasureRows'un
                    // satır başlarıyla aynı); yarım kelime üst satırda basılıp sonra aşağı atlamaz
                    frontMessage.text = msg;
                    frontMessage.maxVisibleCharacters = messageText.Length >= msg.Length ? AllCharacters : messageText.Length;
                }
                if (frontPrev != null) frontPrev.text = prevText.Length > 0 ? "<color=#" + ink3 + "><b>" + prevText + "</b></color>" : "";
            }
            // Birleşik metin: Revizyon 2 ön kâğıdı ve yuvasız kopya
            var sb = new StringBuilder();
            sb.Append("<b><color=#").Append(ink3).Append(">").Append(codeText).Append("</color></b>\n");
            sb.Append("<size=").Append(Slots ? TornFit(st) : fit).Append("%><b><color=#").Append(status).Append(">").Append(statusText).Append("</color></b></size>\n");
            sb.Append(messageText);
            if (prevText.Length > 0)
                sb.Append("\n<size=68%><color=#").Append(ink3).Append("><b>").Append(prevText).Append("</b></color></size>");
            frontComposite = sb.ToString();
            if (front != null) front.text = frontComposite;
        }

        /// <summary>
        /// Durum satırı tek satırda kalsın (EN “KNOCK AT THE DOOR”, TR “KYGA-07 HAT AÇIK”): sığmazsa %132'den (yuvada %100'den)
        /// küçülür, ileti boyunun altına inmez. Satır başına bir kez ölçülür.
        /// </summary>
        int StatusFit(string status)
        {
            if (status == fitStatus) return fitPercent;
            fitStatus = status;
            var text = Slots ? frontStatus : front;
            int full = Slots ? 100 : StatusPercent, floor = Slots ? SlotStatusFloor : 100;
            fitPercent = full;
            var r = text.rectTransform.rect;
            float width = r.width - text.margin.x - text.margin.z;
            float need = text.GetPreferredValues("<size=" + full + "%><b>" + status + "</b></size>", 100000, 0).x;
            if (width > 0 && need > width) fitPercent = Mathf.Max(floor, Mathf.FloorToInt(full * width / need));
            return fitPercent;
        }

        /// <summary>Kâğıt yuvaları kipinde yuvasız kopyanın durum satırı: kendi genişliğine göre %132'den küçülür (en az %100).</summary>
        int TornFit(string status)
        {
            if (status == tornStatusKey) return tornPercent;
            tornStatusKey = status;
            tornPercent = StatusPercent;
            if (tornText == null) return tornPercent;
            var r = tornText.rectTransform.rect;
            float width = r.width - tornText.margin.x - tornText.margin.z;
            float need = tornText.GetPreferredValues("<size=" + StatusPercent + "%><b>" + status + "</b></size>", 100000, 0).x;
            if (width > 0 && need > width) tornPercent = Mathf.Max(100, Mathf.FloorToInt(StatusPercent * width / need));
            return tornPercent;
        }

        void Render(int newestChars)
        {
            renderedChars = shown.Count > 0 ? Mathf.Clamp(newestChars, 0, typingLength) : 0;
            RenderFront(newestChars);
            if (paper == null) return;
            var t = SiginakTheme.Active;
            string ink3 = ColorUtility.ToHtmlStringRGB(t.ColorOf("ink3"));
            var sb = new StringBuilder();
            sb.Append("<b>").Append(Corrigible.Core.Loc.T(header)).Append("</b>\n");
            for (int i = 0; i < shown.Count; i++)
            {
                var l = shown[i];
                if (i == 0 && newestChars < Plain(l).Length)
                {
                    sb.Append("\n").Append(Plain(l).Substring(0, newestChars));
                    continue;
                }
                string statusColor = ColorUtility.ToHtmlStringRGB(t.ColorOf(l.kind == TelexKind.Failed ? "status.threat" : "ink"));
                sb.Append("\n<color=#").Append(ink3).Append(">").Append(l.code).Append("</color>  <b><color=#").Append(statusColor).Append(">")
                    .Append(l.status).Append("</color></b>\n").Append(l.message);
            }
            paper.text = sb.ToString();
        }
    }
}
