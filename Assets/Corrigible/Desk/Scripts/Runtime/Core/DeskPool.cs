using System.Collections.Generic;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Dinamik listeler için şablon havuzu (araştırma satırları, teleks öğeleri, kaynak satırları, harita düğümleri).
    /// Şablon prefab'da kapalı durur; öğeler yalnızca liste büyüyünce yaratılır, fazlası gizlenir (veri değişince çalışır).
    /// </summary>
    public sealed class DeskPool<T> where T : Component
    {
        readonly T template;
        readonly Transform parent;
        readonly List<T> items = new List<T>();

        public DeskPool(T template, Transform parent = null)
        {
            this.template = template;
            this.parent = parent != null ? parent : template.transform.parent;
            DeskUi.SetActive(template, false);
        }

        public int Count => items.Count;

        public T Get(int i)
        {
            while (items.Count <= i)
            {
                var c = Object.Instantiate(template, parent);
                c.name = template.name + " " + items.Count;
                DeskUi.MarkPreview(c.gameObject);   // edit mod önizlemesinde kaydedilmez
                items.Add(c);
            }
            DeskUi.SetActive(items[i], true);
            return items[i];
        }

        /// <summary>İlk <paramref name="count"/> öğe dışındakileri gizler.</summary>
        public void Trim(int count)
        {
            for (int i = count; i < items.Count; i++) DeskUi.SetActive(items[i], false);
        }
    }
}
