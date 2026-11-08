using TMPro;
using UnityEngine;

namespace Corrigible.Game.Raid
{
    /// <summary>
    /// Baskın tahtasındaki 3D parça (dolap ya da birim): can pipleri, sıra/şüphe rozetleri, seçim halkası, vuruş parlaması.
    /// Görsel kısmı <see cref="body"/> altındadır; tahta konumu kökün <c>transform.position</c>'ıdır.
    /// </summary>
    public sealed class RaidPiece : MonoBehaviour
    {
        [Tooltip("Canlandırılan gövde (sallanma, hamle, düşme). Boşsa kök kullanılır.")]
        [SerializeField] Transform body;
        [Tooltip("Parlama ve soluklaşma uygulanan renderer'lar.")]
        [SerializeField] Renderer[] tinted = new Renderer[0];
        [SerializeField] TMP_Text hp;
        [SerializeField] TMP_Text order;
        [SerializeField] TMP_Text flag;
        [SerializeField] GameObject selectRing;
        [Tooltip("Uzman piyonu: tur sonunda harf Y'de 180° arkasını döner.")]
        [SerializeField] bool standee;
        [SerializeField] TMP_Text stateLabel;
        [SerializeField] GameObject damageMark;
        [Tooltip("Tur bitince açılan işaret (piyonun altlığındaki ✓ etiketi); düşünce kapanır.")]
        [SerializeField] GameObject spentMark;
        [Tooltip("Zırh plakası (piyonun altlığında); RaidBoard3D birimin zırhına göre açar.")]
        [SerializeField] GameObject plateMark;
        [SerializeField] Color spentTint = new Color(0.46f, 0.42f, 0.35f);
        [Tooltip("Tur bitince boyanan renderer'ların spentTint'e karışma oranı (1 = tam renk).")]
        [Range(0, 1)] [SerializeField] float spentBlend = 0.35f;
        [Tooltip("Düşünce gövdenin yerel dönüşü (derece).")]
        [SerializeField] Vector3 downEuler = new Vector3(85, 0, 0);
        [Tooltip("Düşünce gövdenin yükselmesi (devrilen harf altlığın kenarına yaslanır).")]
        [SerializeField] float downLift;
        [Tooltip("Havada süzülme genliği (dron).")]
        [SerializeField] float hover;
        [Header("Revizyon 3: harf ve düşme ayrı menteşelerde")]
        [SerializeField] Transform letterPivot, fallPivot, aimingArm, aimingBase;
        [SerializeField] float turnSeconds = .2f;
        [SerializeField] bool preserveEmission;

        public int X, Y;
        public string Kind;
        public Transform Body => body != null ? body : transform;
        public Transform FallBody => fallPivot != null ? fallPivot : Body;
        public Quaternion DownRotation => (fallPivot != null ? fallRotation : bodyRotation) * Quaternion.Euler(downEuler);

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock mpb;
        Color[][] baseColors;
        Color[][] baseEmission;
        float flashUntil, flashLength = 1;
        Color flashColor;
        bool spent, down;
        Vector3 bodyRest;
        Quaternion bodyRotation;
        Vector3 bodyScale;
        float phase;
        Quaternion poseFrom, poseTo;
        float poseStarted;
        bool flipping;
        Quaternion letterRotation, fallRotation;
        Transform flipBody;

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            baseColors = new Color[tinted.Length][];
            baseEmission = new Color[tinted.Length][];
            for (int i = 0; i < tinted.Length; i++)
            {
                if (tinted[i] == null || tinted[i].sharedMaterial == null) continue;
                var materials = tinted[i].sharedMaterials;
                baseColors[i] = new Color[materials.Length];
                baseEmission[i] = new Color[materials.Length];
                for (int j = 0; j < materials.Length; j++)
                {
                    baseColors[i][j] = materials[j] != null && materials[j].HasProperty(BaseColor) ? materials[j].GetColor(BaseColor) : Color.white;
                    baseEmission[i][j] = materials[j] != null && materials[j].HasProperty(EmissionColor) ? materials[j].GetColor(EmissionColor) : Color.black;
                }
            }
            bodyRest = Body.localPosition;
            bodyRotation = Body.localRotation;
            bodyScale = Body.localScale;
            if (letterPivot != null) letterRotation = letterPivot.localRotation;
            if (fallPivot != null) fallRotation = fallPivot.localRotation;
            phase = Random.value * 10f;
        }

        public void SetHp(int value, int max)
        {
            if (hp != null) hp.text = Corrigible.Core.Loc.F("{0}/{1}", Mathf.Max(0, value), max);
            if (damageMark != null) damageMark.SetActive(value > 0 && value < max);
        }

        public void SetBadges(string orderText, string flagText, Color flagColor)
        {
            if (order != null) { order.gameObject.SetActive(orderText != null); if (orderText != null) order.text = orderText; }
            if (flag != null) { flag.gameObject.SetActive(flagText != null); if (flagText != null) { flag.text = flagText; flag.color = flagColor; } }
        }

        public void SetSelected(bool on) { if (selectRing != null && selectRing.activeSelf != on) selectRing.SetActive(on); }

        public void SetPlate(bool on) { if (plateMark != null && plateMark.activeSelf != on) plateMark.SetActive(on); }

        /// <summary>Kaynak kolunun modeldeki −Z burnunu hedefe yöneltir; yalnız görünümü değiştirir.</summary>
        public bool AimAt(Vector3 target)
        {
            if (aimingArm == null) return false;
            var direction = target - aimingArm.position; direction.y=0;
            if (direction.sqrMagnitude > .0001f)
            {
                aimingArm.rotation = Quaternion.LookRotation(-direction,Vector3.up);
                if(aimingBase)aimingBase.rotation=aimingArm.rotation;
            }
            return true;
        }

        public void SetSpent(bool on)
        {
            if (spent == on) return;
            spent = on; ApplyPose(standee && !down && Application.isPlaying); Apply();
        }

        public void SetDown(bool on) { if (down == on) return; down = on; ApplyPose(); Apply(); }

        /// <summary>Kâğıt etiketler tahtanın havuzunda çizilir; eski prefab yazıları üst üste gelmesin.</summary>
        public void SetWorldLabelsVisible(bool on)
        {
            if (hp != null) hp.gameObject.SetActive(on);
            if (order != null) order.gameObject.SetActive(on);
            if (flag != null) flag.gameObject.SetActive(on);
        }

        public float VisualHeight
        {
            get
            {
                float top = 0.4f;
                foreach (var r in Body.GetComponentsInChildren<Renderer>())
                    if (r.GetComponent<TMP_SubMesh>() == null && r.GetComponent<TMP_Text>() == null) top = Mathf.Max(top, r.bounds.max.y - transform.position.y);
                return Mathf.Clamp(top, 0.25f, 1.6f);
            }
        }

        void ApplyPose(bool animate = false)
        {
            Body.localScale = bodyScale;
            Body.localPosition = bodyRest + (down ? Vector3.up * downLift : Vector3.zero);
            if (spentMark != null) spentMark.SetActive(spent && !down);
            flipBody = letterPivot != null ? letterPivot : Body;
            if (fallPivot != null) fallPivot.localRotation = down ? DownRotation : fallRotation;
            var rest = letterPivot != null ? letterRotation : bodyRotation;
            var rotation = fallPivot == null && down ? DownRotation : rest * (standee && spent && !down ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
            flipping = animate;
            if (animate) { poseFrom = flipBody.localRotation; poseTo = rotation; poseStarted = Time.time; }
            else flipBody.localRotation = rotation;
            if (stateLabel != null)
            {
                stateLabel.gameObject.SetActive(down || spent);
                stateLabel.text = down ? Corrigible.Core.Loc.T("DÜŞTÜ") : Corrigible.Core.Loc.T("TUR BİTTİ");
            }
        }

        public void Flash(Color color, float seconds = 0.35f)
        {
            flashColor = color; flashLength = seconds; flashUntil = Time.time + seconds;
        }

        void Update()
        {
            if (flipping)
            {
                float t = Mathf.Clamp01((Time.time - poseStarted) / Mathf.Max(.01f,turnSeconds));
                flipBody.localRotation = Quaternion.Slerp(poseFrom, poseTo, t * t * (3 - 2 * t));
                if (t >= 1) flipping = false;
            }
            if (hover > 0 && !down) Body.localPosition = bodyRest + Vector3.up * (Mathf.Sin((Time.time + phase) * 2.2f) * hover);
            if (flashUntil > 0) { Apply(); if (Time.time > flashUntil) flashUntil = 0; }
        }

        void Apply()
        {
            float f = flashUntil > 0 ? Mathf.Clamp01((flashUntil - Time.time) / flashLength) : 0;
            for (int i = 0; i < tinted.Length; i++)
            {
                if (tinted[i] == null || baseColors[i] == null) continue;
                // Çok malzemeli parçalarda (dolabın üç yüzü) her malzemenin boyası ayrı kalır.
                for (int j = 0; j < baseColors[i].Length; j++)
                {
                    tinted[i].GetPropertyBlock(mpb, j);
                    var rest = spent && !down ? Color.Lerp(baseColors[i][j], spentTint, spentBlend) : baseColors[i][j];
                    mpb.SetColor(BaseColor, Color.Lerp(rest, flashColor, f * 0.65f));
                    mpb.SetColor(EmissionColor, preserveEmission ? baseEmission[i][j] : Color.black);
                    tinted[i].SetPropertyBlock(mpb, j);
                }
            }
        }
    }
}
