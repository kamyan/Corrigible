using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kartlı panonun geometrisiz işaretleri (Revizyon 3 · R15b): pano düzeyindeki World Space tuval <c>Pano işaretleri</c>
    /// üstünde (1 birim = 1 mm, z = pano yüzü − 0,0026, GraphicRaycaster yok). Amber hedef çerçevesi (yalnız üzerinde durulan
    /// geçerli hedef; dikdörtgen her yandan 4 mm büyür), sürükleme boyunca geçersiz ceplerin sönük örtüsü, tebeşir yeniden
    /// yazımının silinti izi ve klavye odağı (aynı amber çerçevenin ayrı bir kopyası). Dikdörtgenler pano mm'sidir (sol üst
    /// köşeden, y aşağı); yerleri <see cref="BoardLayout.MmToLocal"/> ile modelden hesaplanır, böylece tuvalin pivotu ne olursa
    /// olsun doğru yere düşer (model kökü bağlı değilse tuvalin sol üst köşesi iç alanın sol üst köşesi sayılır).
    /// Görüntüler yalnız değişince açılıp kapanır; her karede tuval yeniden kurulmaz.
    /// </summary>
    public sealed class BoardMarks : MonoBehaviour
    {
        [Tooltip("İşaret tuvalinin kökü (1 birim = 1 mm). Boşsa bu nesne.")]
        [SerializeField] RectTransform root;
        [Tooltip("PlanningBoard model kökü (pano mm → dünya). Boşsa tuvalin sol üstü iç alanın sol üstü sayılır.")]
        [SerializeField] Transform modelRoot;
        [Tooltip("Amber hedef çerçevesi şablonu (frame_target, 9 dilim, hale dahil).")]
        [SerializeField] Image glowPrefab;
        [Tooltip("Klavye odağı şablonu; boşsa hedef çerçevesi kullanılır.")]
        [SerializeField] Image focusPrefab;
        [Tooltip("Geçersiz cep örtüsü şablonu (dim, rgba(20,24,18,0,35)); boşsa düz renk kare.")]
        [SerializeField] Image dimPrefab;
        [Tooltip("Tebeşir silintisi (smudge); rengi korunur, saydamlığı 0 → 0,35 → 0 oynar.")]
        [SerializeField] Image smudge;

        Image glow, focus;
        readonly List<Image> dims = new List<Image>();
        int dimShown;
        float smudgeStart = -1, smudgeSeconds;

        RectTransform Root => root != null ? root : transform as RectTransform;

        static float Num(string key, float fallback)
        {
            var t = SiginakTheme.Active;
            return t != null ? t.NumberOf(key, fallback) : fallback;
        }

        static float PadMm => Num("board.glowPadMm", 4);
        static float SmudgePeak => Num("board.smudgeAlpha", 0.35f);

        /// <summary>
        /// Silintinin model yereli z'si: başlık arduvazı yüzün 3 mm önünde (ön yüzü ≈ −0,017), tebeşir yazısı (<c>TXT_Header</c>)
        /// −0,0172'de. İşaret tuvali yüzün 2,6 mm önünde (−0,0166) olduğundan silinti tuval düzleminde kalsa arduvazın arkasında
        /// görünmezdi; yazının 0,4 mm önüne konur.
        /// </summary>
        static float SmudgeZ => Num("board.layoutMm.header.smudgeZ", -0.0176f);

        static Color DimColor
        {
            get
            {
                var t = SiginakTheme.Active;
                return t != null && t.TryGet("board.dim", out var c) ? c : new Color(20 / 255f, 24 / 255f, 18 / 255f, 0.35f);
            }
        }

        /// <summary>Tek seferlik sahne script'i ve testler bağlar.</summary>
        public void Configure(RectTransform canvasRoot, Transform model, Image glowTemplate, Image focusTemplate, Image dimTemplate, Image smudgeImage)
        {
            root = canvasRoot; modelRoot = model; glowPrefab = glowTemplate; focusPrefab = focusTemplate; dimPrefab = dimTemplate; smudge = smudgeImage;
        }

        /// <summary>Şu an çerçeve gösteriliyor mu (testler).</summary>
        public bool GlowShown => glow != null && glow.enabled;
        public bool FocusShown => focus != null && focus.enabled;
        public int DimCount => dimShown;

        void Awake()
        {
            HideTemplate(glowPrefab);
            HideTemplate(focusPrefab);
            HideTemplate(dimPrefab);
            if (smudge != null) { smudge.raycastTarget = false; smudge.enabled = false; }
        }

        static void HideTemplate(Image template)
        {
            if (template != null && template.gameObject.scene.IsValid() && template.gameObject.activeSelf) template.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- hedef ve odak

        /// <summary>Amber çerçeve, dikdörtgen her yandan 4 mm büyür (yalnız üzerinde durulan geçerli hedef).</summary>
        public void ShowGlow(Rect boardMm)
        {
            if (glow == null) glow = Make(glowPrefab, "Hedef çerçevesi");
            if (glow != null) Put(glow, Pad(boardMm));
        }

        public void ClearGlow() { if (glow != null && glow.enabled) glow.enabled = false; }

        /// <summary>Klavye odağı: hedef çerçevesinin ayrı bir kopyası (sürükleme çerçevesiyle aynı anda durabilir).</summary>
        public void ShowFocus(Rect boardMm)
        {
            if (focus == null) focus = Make(focusPrefab != null ? focusPrefab : glowPrefab, "Klavye odağı");
            if (focus != null) Put(focus, Pad(boardMm));
        }

        public void ClearFocus() { if (focus != null && focus.enabled) focus.enabled = false; }

        static Rect Pad(Rect r)
        {
            float p = PadMm;
            return new Rect(r.x - p, r.y - p, r.width + 2 * p, r.height + 2 * p);
        }

        // ---------------------------------------------------------------- sönük örtü

        /// <summary>Geçersiz hedeflerin örtüsü (sürükleme boyunca); öncekilerin yerini alır.</summary>
        public void Dim(IEnumerable<Rect> boardMm)
        {
            int n = 0;
            if (boardMm != null)
                foreach (var r in boardMm)
                {
                    if (r.width <= 0 || r.height <= 0) continue;
                    if (n == dims.Count)
                    {
                        var img = dimPrefab != null ? Make(dimPrefab, "Sönük örtü") : MakePlain("Sönük örtü", DimColor);
                        if (img == null) break;
                        dims.Add(img);
                    }
                    Put(dims[n], r);
                    n++;
                }
            for (int i = n; i < dims.Count; i++) if (dims[i] != null && dims[i].enabled) dims[i].enabled = false;
            dimShown = n;
        }

        public void ClearDim()
        {
            for (int i = 0; i < dims.Count; i++) if (dims[i] != null && dims[i].enabled) dims[i].enabled = false;
            dimShown = 0;
        }

        // ---------------------------------------------------------------- tebeşir silintisi

        /// <summary>Değişen alt dizginin üstünde silinti: saydamlık 0 → tepe (0,35) → 0, <paramref name="seconds"/> içinde.</summary>
        public void Smudge(Rect boardMm, float seconds)
        {
            if (smudge == null) return;
            Put(smudge, boardMm, SmudgeZ);
            smudgeStart = Time.unscaledTime;
            smudgeSeconds = Mathf.Max(0.01f, seconds);
            SetAlpha(smudge, 0);
        }

        void Update()
        {
            if (smudgeStart < 0 || smudge == null) return;
            float t = (Time.unscaledTime - smudgeStart) / smudgeSeconds;
            if (t >= 1)
            {
                smudgeStart = -1;
                smudge.enabled = false;
                return;
            }
            SetAlpha(smudge, SmudgePeak * (1 - Mathf.Abs(2 * t - 1)));
        }

        static void SetAlpha(Graphic g, float a)
        {
            var c = g.color;
            if (Mathf.Approximately(c.a, a)) return;
            c.a = a;
            g.color = c;
        }

        void OnDisable()
        {
            ClearGlow();
            ClearFocus();
            ClearDim();
            if (smudge != null) smudge.enabled = false;
            smudgeStart = -1;
        }

        // ---------------------------------------------------------------- yerleştirme

        Image Make(Image template, string objectName)
        {
            var area = Root;
            if (template == null || area == null) return null;
            var img = Instantiate(template, area, false);
            img.gameObject.name = objectName;
            img.raycastTarget = false;
            img.enabled = false;
            if (!img.gameObject.activeSelf) img.gameObject.SetActive(true);
            return img;
        }

        Image MakePlain(string objectName, Color color)
        {
            var area = Root;
            if (area == null) return null;
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.layer = area.gameObject.layer;
            go.transform.SetParent(area, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.color = color;
            img.enabled = false;
            return img;
        }

        /// <summary>
        /// Görüntüyü pano mm dikdörtgenine oturtur ve açar. <paramref name="modelZ"/> verilirse (NaN değilse) görüntü tuval
        /// düzleminde değil o model yereli z'de durur (silinti başlığın önünde).
        /// </summary>
        void Put(Graphic g, Rect mm, float modelZ = float.NaN)
        {
            var area = Root;
            if (g == null || area == null) return;
            var r = g.rectTransform;
            if (modelRoot != null)
            {
                bool lifted = !float.IsNaN(modelZ);
                Vector3 world = modelRoot.TransformPoint(BoardLayout.MmToLocal(mm.center, lifted ? modelZ : BoardLayout.FaceZ));
                Vector3 local = area.InverseTransformPoint(world);
                float k = area.InverseTransformVector(modelRoot.TransformVector(new Vector3(0.001f, 0, 0))).magnitude;
                if (k <= 1e-6f) k = 1;
                r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = mm.size * k;
                r.localPosition = new Vector3(local.x, local.y, lifted ? local.z : 0);
            }
            else
            {
                r.anchorMin = r.anchorMax = new Vector2(0, 1);
                r.pivot = new Vector2(0.5f, 0.5f);
                r.sizeDelta = mm.size;
                r.anchoredPosition3D = new Vector3(mm.center.x, -mm.center.y, 0);
            }
            r.localRotation = Quaternion.identity;
            r.localScale = Vector3.one;
            if (!g.enabled) g.enabled = true;
        }
    }
}
