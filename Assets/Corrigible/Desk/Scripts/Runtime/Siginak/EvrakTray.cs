using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Masadaki EVRAK tepsisi (K6 notu › çıkış, K7): son 5 vardiya raporu ve en altta emir belgesi (açılışta BAŞLA ya da
    /// KAYITTAN DEVAM ile dosyalanır). En üstte son raporun A5 teleks kâğıdı (rapor yoksa emir belgesi) durur; okuma
    /// görünümü açıkken kâğıt tepside değildir, dosyalanınca geri gelir. Raporlar kayda girmez: bağlayıcı yan dosyaya yazar.
    /// <para>
    /// Revizyon 3 · R19 (PLAN §1.5, §1.7, Karar D7-A): sürüklenen devir fişi tepsinin üstündeyken amber hedef ışığı yanar
    /// (<c>shiftClose.evrakHighlight</c>: tür, renk, şiddet × <c>targetIntensityScale</c>, menzil, gölgesiz; kaydedilmiş hâlde ve
    /// <c>Awake</c>'te kapalı, poz ışığı tarifinde yok). Fiş üstteki kâğıdın (<c>sheet</c>; <c>CampaignWear</c> yığınla birlikte
    /// kaldırır) yüksekliğine iner (<see cref="SlipDropPose"/>); dosyalanan son fiş tepside üst kâğıt olarak durur
    /// (<see cref="ShowFiled"/>), elde okunurken kopya gizlenir (<see cref="SetFiledHidden"/>).
    /// </para>
    /// </summary>
    public sealed class EvrakTray : MonoBehaviour
    {
        [SerializeField, Min(1)] int keep = 5;
        [Tooltip("Son raporun masadaki A5 kâğıdı.")]
        [SerializeField] GameObject sheet;
        [SerializeField] TMP_Text sheetTitle;
        [SerializeField] Collider receiptDropArea;
        [Header("Revizyon 3 · R19 devir fişi")]
        [Tooltip("EVRAK hedef ışığı (Point; yalnız sürüklenen fiş tepsinin üstündeyken yanar).")]
        [SerializeField] Light targetLight;
        [Tooltip("Token şiddetinin URP çarpanı (bölge parlaklığıyla kalibre).")]
        [SerializeField] float targetIntensityScale = 1f;
        [Tooltip("Sürüklenen fişin EVRAK üstündeki pozu (Slip over).")]
        [SerializeField] Transform slipOver;
        [Tooltip("Dosyalanan son fişin tepsideki kopyası (çarpıştırıcısız).")]
        [SerializeField] HandoverSlipFace filedSlip;
        [Tooltip("Fişin bırakma pozu: tepsi yereli ofset (m; y üst kâğıdın üstünden) ve dönüş (three.js dress_room_switch.js:60).")]
        [SerializeField] Vector3 slipDropOffset = new Vector3(0.006f, 0.0015f, -0.004f), slipDropEuler = new Vector3(90, -0.3f, 0);

        readonly List<ShiftReportInput> reports = new List<ShiftReportInput>();
        bool sheetOut, briefing, briefingOut, filedHidden;

        public IReadOnlyList<ShiftReportInput> Reports => reports;
        /// <summary>Emir belgesi tepside (açılış bitti).</summary>
        public bool HasBriefing => briefing;
        /// <summary>Okuma görünümü dosyalanırken kâğıdın kaydığı nokta.</summary>
        public Vector3 DropPoint => sheet != null ? sheet.transform.position : transform.position;
        public event Action Changed;

        /// <summary>R19: hedef ışığı yanık (fiş EVRAK'ın üstünde).</summary>
        public bool ReceiptTargetLit { get; private set; }
        /// <summary>R19: sürüklenen fişin EVRAK üstündeki pozu.</summary>
        public Pose SlipOverPose => slipOver != null ? new Pose(slipOver.position, slipOver.rotation) : new Pose(transform.position, transform.rotation);
        /// <summary>
        /// R19: fişin tepsiye indiği poz. Yükseklik üstteki kâğıdı izler (<c>sheet</c>'in tepsi yereli y'si + 1,5 mm; erken yığın
        /// 0,0285, geç yığın 0,0582), dönüş tepsininki · (90, −0,3, 0): yüz yukarı, üst kenar oyuncudan uzakta.
        /// </summary>
        public Pose SlipDropPose
        {
            get
            {
                float sheetY = sheet != null ? transform.InverseTransformPoint(sheet.transform.position).y : 0;
                var local = slipDropOffset + Vector3.up * sheetY;
                return new Pose(transform.TransformPoint(local), transform.rotation * Quaternion.Euler(slipDropEuler));
            }
        }
        /// <summary>R19: tepside duran son dosyalanmış fişin verisi (yoksa null).</summary>
        public HandoverSlipFacts LastFiled { get; private set; }
        /// <summary>R19: tepsideki kopya, arşiv fişi elde okunduğu için gizli.</summary>
        public bool FiledHidden => filedHidden;
        public Light TargetLight => targetLight;

        /// <summary>
        /// R19: hedef ışığını yakar / söndürür. Yanarken tür, renk, şiddet (× <c>targetIntensityScale</c>), menzil ve gölge
        /// <c>shiftClose.evrakHighlight.*</c> token'larından yazılır; token yoksa ışığın kendi değeri kalır.
        /// </summary>
        public void SetReceiptTarget(bool on)
        {
            ReceiptTargetLit = on;
            if (targetLight == null) return;
            if (on) ApplyHighlightTokens(targetLight, targetIntensityScale);
            if (targetLight.enabled != on) targetLight.enabled = on;
        }

        /// <summary>Hedef ışığının token değerleri (saf yardımcı; testler doğrudan çağırır).</summary>
        public static void ApplyHighlightTokens(Light light, float intensityScale)
        {
            if (light == null) return;
            var theme = SiginakTheme.Active;
            if (theme == null) return;
            if (Enum.TryParse(theme.Text("shiftClose.evrakHighlight.type") ?? "", out LightType type) && light.type != type) light.type = type;
            if (theme.TryGet("shiftClose.evrakHighlight.color", out var color)) light.color = color;
            if (theme.TryNumber("shiftClose.evrakHighlight.intensity", out var intensity)) light.intensity = intensity * intensityScale;
            if (theme.TryNumber("shiftClose.evrakHighlight.range", out var range)) light.range = range;
            if (theme.TryNumber("shiftClose.evrakHighlight.shadows", out var shadows))
                light.shadows = shadows > 0.5f ? LightShadows.Soft : LightShadows.None;
        }

        /// <summary>
        /// R19: dosyalanan son fiş tepside üst kâğıt olarak görünür (kopya bırakma pozuna, yüzü bu veriyle); null kopyayı gizler.
        /// </summary>
        public void ShowFiled(HandoverSlipFacts facts)
        {
            LastFiled = facts;
            if (filedSlip != null && facts != null)
            {
                PlaceFiled();
                filedSlip.Bind(facts);
            }
            ApplyFiled();
        }

        /// <summary>Kopya bırakma pozuna (üst kâğıdın o anki yüksekliği).</summary>
        void PlaceFiled()
        {
            var pose = SlipDropPose;
            var t = filedSlip.transform;
            if (t.position != pose.position || t.rotation != pose.rotation) t.SetPositionAndRotation(pose.position, pose.rotation);
        }

        // Üst kâğıdın yüksekliğini CampaignWear yazar (kayıttan devamda GameLoaded'dan sonraki Changed'de, devirde yığın büyüyünce):
        // görünen kopya o yüksekliği izler, yığına gömülmez ya da havada kalmaz.
        void LateUpdate()
        {
            if (filedSlip != null && filedSlip.gameObject.activeSelf) PlaceFiled();
        }

        /// <summary>R19: arşivdeki fiş elde okunurken tepsideki kopya gizlenir.</summary>
        public void SetFiledHidden(bool hidden)
        {
            filedHidden = hidden;
            ApplyFiled();
        }

        void ApplyFiled()
        {
            if (filedSlip == null) return;
            bool show = LastFiled != null && !filedHidden;
            if (filedSlip.gameObject.activeSelf != show) filedSlip.gameObject.SetActive(show);
        }

        public void Configure(GameObject topSheet, TMP_Text title) { sheet = topSheet; sheetTitle = title; }
        public void ConfigureReceiptDrop(Collider area) => receiptDropArea = area;
        /// <summary>R19 bağları (tek seferlik sahne script'i ve testler).</summary>
        public void ConfigureSlip(Light highlight, Transform over, HandoverSlipFace filed)
        { targetLight = highlight; slipOver = over; filedSlip = filed; }
        public bool ContainsReceiptDrop(Vector2 screen, Camera camera)
            => receiptDropArea != null && camera != null && receiptDropArea.Raycast(camera.ScreenPointToRay(screen), out _, camera.farClipPlane);

        void Awake()
        {
            Apply();
            SetReceiptTarget(false);
        }

        void OnDisable() => SetReceiptTarget(false);

        /// <summary>Yeni rapor (en sona); en eskisi düşer.</summary>
        public void Add(ShiftReportInput report)
        {
            if (report == null) return;
            reports.Add(report);
            if (reports.Count > keep) reports.RemoveRange(0, reports.Count - keep);
            Apply();
            Changed?.Invoke();
        }

        /// <summary>Kayıttan devam: raporları animasyonsuz geri koyar.</summary>
        public void Restore(IEnumerable<ShiftReportInput> saved)
        {
            reports.Clear();
            if (saved != null) reports.AddRange(saved);
            if (reports.Count > keep) reports.RemoveRange(0, reports.Count - keep);
            sheetOut = false;
            Apply();
        }

        public void Clear() => Restore(null);

        /// <summary>Son rapor okuma görünümünde (tepside değil).</summary>
        public void SetSheetOut(bool taken)
        {
            sheetOut = taken;
            Apply();
        }

        /// <summary>Emir belgesi tepsiye dosyalandı (yeni oyunda açılışa kadar yok).</summary>
        public void SetBriefing(bool filed)
        {
            briefing = filed;
            briefingOut = false;
            Apply();
        }

        /// <summary>Emir belgesi okuma görünümünde (H); rapor yoksa üstteki kâğıt odur.</summary>
        public void SetBriefingOut(bool taken)
        {
            briefingOut = taken;
            Apply();
        }

        /// <summary>Dil değişince kâğıttaki başlık.</summary>
        public void Relabel() => Apply();

        void Apply()
        {
            bool report = reports.Count > 0;
            bool show = report ? !sheetOut : briefing && !briefingOut;
            if (sheet != null && sheet.activeSelf != show) sheet.SetActive(show);
            if (show && sheetTitle != null)
                sheetTitle.text = report ? Corrigible.Core.Loc.F("VARDİYA RAPORU · {0:000}", reports[reports.Count - 1].before.shift)
                    : Corrigible.Core.Loc.T("HAREKÂT EMRİ");
        }
    }
}
