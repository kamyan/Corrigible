using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// JSON token dosyasından düzleştirilmiş tema: renkler ve sayılar JSON yoluyla (“status.threat”, “desk.bar.widths.trust”)
    /// saklanır. İçeriği editördeki içe aktarıcı doldurur; bileşenler sabit hex ya da ölçü yazmaz.
    /// </summary>
    public abstract class TokenTheme : ScriptableObject
    {
        [Serializable] public struct NamedColor { public string key; public Color color; }
        [Serializable] public struct NamedFloat { public string key; public float value; }

        public List<NamedColor> colors = new List<NamedColor>();
        public List<NamedFloat> numbers = new List<NamedFloat>();

        Dictionary<string, Color> colorMap;
        Dictionary<string, float> numberMap;

        protected virtual void OnEnable() => Invalidate();
        protected virtual void OnValidate() => Invalidate();

        /// <summary>İçe aktarmadan sonra arama tablolarını yeniden kurdurur.</summary>
        public virtual void Invalidate() { colorMap = null; numberMap = null; }

        void Index()
        {
            colorMap = new Dictionary<string, Color>(colors.Count);
            foreach (var c in colors) colorMap[c.key] = c.color;
            numberMap = new Dictionary<string, float>(numbers.Count);
            foreach (var n in numbers) numberMap[n.key] = n.value;
        }

        public bool TryGet(string key, out Color color)
        {
            if (colorMap == null) Index();
            return colorMap.TryGetValue(key, out color);
        }

        /// <summary>Anahtarın rengi; yoksa eflatun (eksik anahtar hemen görünsün).</summary>
        public Color Get(string key) => TryGet(key, out var c) ? c : Color.magenta;

        public bool TryNumber(string key, out float value)
        {
            if (numberMap == null) Index();
            return numberMap.TryGetValue(key, out value);
        }

        public float Number(string key, float fallback = 0) => TryNumber(key, out var v) ? v : fallback;
    }
}
