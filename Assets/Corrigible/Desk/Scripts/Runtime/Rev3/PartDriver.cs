using UnityEngine;

namespace Corrigible.Desk
{
    public enum PartKind { Rotate, Slide, Swap }
    public enum PartAxis { X, Y, Z }

    /// <summary>
    /// Revizyon 3 modelinin hareketli parçası (Docs/UI_Siginak_Rev3/modeller/README.md). Parçanın köşeleri pivotuna göre
    /// yazılmıştır; nesne pivotta durur (<c>localPosition = pivotLocal</c>, ebeveyn parçaya göre) ve kendi yerel ekseninde
    /// döner (derece) ya da kayar (metre). <see cref="PartKind.Swap"/> parça yalnız malzeme ya da görünürlük değiştirir.
    /// Değerler manifestten içe aktarıcıda yazılır; mantık sahibi görünümdedir, bu bileşen yalnız pozu uygular.
    /// </summary>
    public sealed class PartDriver : MonoBehaviour
    {
        [SerializeField] string partName;
        [SerializeField] PartKind kind;
        [SerializeField] PartAxis axis;
        [Tooltip("Aralık: dönüşte derece, kaymada metre (manifest “range”).")]
        [SerializeField] float rangeFrom, rangeTo;
        [SerializeField] float rest;
        [Tooltip("Pivotun ebeveyn parçaya göre yeri (manifest “pivotLocal”).")]
        [SerializeField] Vector3 pivotLocal;
        [Tooltip("Swap durumlarında değişen malzeme yuvası; durum 0 modelin kendi malzemesidir.")]
        [SerializeField] Renderer swapRenderer;
        [SerializeField] int swapSlot;
        [SerializeField] Material[] swapMaterials = new Material[0];

        float value, target, speed;
        int swapState;

        public string PartName => partName;
        public PartKind Kind => kind;
        public PartAxis Axis => axis;
        public float RangeFrom => rangeFrom;
        public float RangeTo => rangeTo;
        public float Rest => rest;
        public Vector3 PivotLocal => pivotLocal;
        public float Value => value;
        public int SwapState => swapState;
        public int SwapCount => swapMaterials.Length;
        public bool Moving => speed > 0;

        public void Configure(string name, PartKind k, PartAxis a, float from, float to, float restValue, Vector3 pivot,
            Renderer swapTarget = null, int slot = 0, Material[] swaps = null)
        {
            partName = name; kind = k; axis = a; rangeFrom = from; rangeTo = to; rest = restValue; pivotLocal = pivot;
            swapRenderer = swapTarget; swapSlot = slot; swapMaterials = swaps ?? new Material[0];
            ResetToRest();
        }

        Vector3 AxisVector => axis == PartAxis.X ? Vector3.right : axis == PartAxis.Y ? Vector3.up : Vector3.forward;

        /// <summary>Dinlenme pozu: dönüş/kayma <c>rest</c>, malzeme durumu 0, görünür.</summary>
        public void ResetToRest()
        {
            speed = 0;
            if (kind != PartKind.Swap) Set(rest);
            else { SetSwap(0); }
        }

        /// <summary>Değeri hemen uygular (derece ya da metre; aralık dışı değer aralığa kırpılır).</summary>
        public void Set(float v)
        {
            if (kind == PartKind.Swap) return;
            value = target = Mathf.Clamp(v, Mathf.Min(rangeFrom, rangeTo), Mathf.Max(rangeFrom, rangeTo));
            speed = 0;
            Apply();
        }

        /// <summary>0 → aralığın başı, 1 → sonu.</summary>
        public void SetNormalized(float t) => Set(Mathf.Lerp(rangeFrom, rangeTo, t));

        /// <summary>Değere <paramref name="seconds"/> içinde sabit hızla gider (süreler token <c>motion.*</c>).</summary>
        public void AnimateTo(float v, float seconds)
        {
            if (kind == PartKind.Swap) return;
            target = Mathf.Clamp(v, Mathf.Min(rangeFrom, rangeTo), Mathf.Max(rangeFrom, rangeTo));
            if (seconds <= 0 || Mathf.Approximately(target, value)) { Set(target); return; }
            speed = Mathf.Abs(target - value) / seconds;
        }

        /// <summary>Swap durumu: 0 modelin malzemesi, 1… sonraki malzemeler (manifest notundaki sırayla).</summary>
        public void SetSwap(int state)
        {
            if (swapMaterials.Length == 0 || swapRenderer == null) { swapState = 0; return; }
            swapState = Mathf.Clamp(state, 0, swapMaterials.Length - 1);
            var mats = swapRenderer.sharedMaterials;
            if (swapSlot < 0 || swapSlot >= mats.Length) return;
            if (mats[swapSlot] == swapMaterials[swapState]) return;
            mats[swapSlot] = swapMaterials[swapState];
            swapRenderer.sharedMaterials = mats;
        }

        /// <summary>Parçayı (çocuk yazı yuvalarıyla birlikte) gösterir ya da gizler.</summary>
        public void SetVisible(bool on) { if (gameObject.activeSelf != on) gameObject.SetActive(on); }

        void Update()
        {
            if (speed <= 0) return;
            value = Mathf.MoveTowards(value, target, speed * Time.unscaledDeltaTime);
            if (Mathf.Approximately(value, target)) speed = 0;
            Apply();
        }

        void Apply()
        {
            if (kind == PartKind.Rotate) { transform.localPosition = pivotLocal; transform.localRotation = Quaternion.AngleAxis(value, AxisVector); }
            else if (kind == PartKind.Slide) { transform.localRotation = Quaternion.identity; transform.localPosition = pivotLocal + AxisVector * value; }
        }
    }
}
