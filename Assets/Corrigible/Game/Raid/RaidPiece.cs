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
        [Tooltip("Havada süzülme genliği (dron).")]
        [SerializeField] float hover;

        public int X, Y;
        public string Kind;
        public Transform Body => body != null ? body : transform;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        MaterialPropertyBlock mpb;
        Color[] baseColors, emissions;
        float flashUntil, flashLength = 1;
        Color flashColor;
        bool spent;
        Vector3 bodyRest;
        float phase;

        void Awake()
        {
            mpb = new MaterialPropertyBlock();
            baseColors = new Color[tinted.Length];
            emissions = new Color[tinted.Length];
            for (int i = 0; i < tinted.Length; i++)
            {
                var m = tinted[i].sharedMaterial;
                baseColors[i] = m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : Color.white;
                emissions[i] = m.HasProperty(EmissionColor) ? m.GetColor(EmissionColor) : Color.black;
            }
            bodyRest = Body.localPosition;
            phase = Random.value * 10f;
        }

        public void SetHp(int value, int max)
        {
            if (hp == null) return;
            hp.text = new string('■', Mathf.Max(0, value)) + "<color=#ffffff40>" + new string('■', Mathf.Max(0, max - value)) + "</color>";
        }

        public void SetBadges(string orderText, string flagText, Color flagColor)
        {
            if (order != null) { order.gameObject.SetActive(orderText != null); if (orderText != null) order.text = orderText; }
            if (flag != null) { flag.gameObject.SetActive(flagText != null); if (flagText != null) { flag.text = flagText; flag.color = flagColor; } }
        }

        public void SetSelected(bool on) { if (selectRing != null && selectRing.activeSelf != on) selectRing.SetActive(on); }

        public void SetSpent(bool on) { if (spent != on) { spent = on; Apply(); } }

        public void Flash(Color color, float seconds = 0.35f)
        {
            flashColor = color; flashLength = seconds; flashUntil = Time.time + seconds;
        }

        void Update()
        {
            if (hover > 0) Body.localPosition = bodyRest + Vector3.up * (Mathf.Sin((Time.time + phase) * 2.2f) * hover);
            if (flashUntil > 0) { Apply(); if (Time.time > flashUntil) flashUntil = 0; }
        }

        void Apply()
        {
            float f = flashUntil > 0 ? Mathf.Clamp01((flashUntil - Time.time) / flashLength) : 0;
            for (int i = 0; i < tinted.Length; i++)
            {
                if (tinted[i] == null) continue;
                tinted[i].GetPropertyBlock(mpb);
                mpb.SetColor(BaseColor, spent ? baseColors[i] * 0.45f : baseColors[i]);
                mpb.SetColor(EmissionColor, Color.Lerp(spent ? emissions[i] * 0.3f : emissions[i], flashColor * 3f, f));
                tinted[i].SetPropertyBlock(mpb);
            }
        }
    }
}
