using System;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// BOŞTA tepsisi (K4): görevsiz kişiler ahşap piyon olarak kıskaçlı panonun önünde durur; plakada “BOŞTA n”. Tepsiye
    /// sığmayan kişi sayısı plakada okunur (piyonlar en çok tepsi genişliği kadar sıkışır). − PERS.'te piyon odadan
    /// kavisle tepsiye zıplar.
    /// </summary>
    public sealed class IdleTray : MonoBehaviour
    {
        [SerializeField] Transform[] pawns = Array.Empty<Transform>();
        [SerializeField] TMP_Text plate;
        [Tooltip("Piyonların tepsi yerelindeki taban yüksekliği ve en büyük aralık (m).")]
        [SerializeField] float baseY = 0.018f, spacing = 0.06f, width = 0.22f;

        int shown = -1;
        float hopAt = -1;
        int hopIndex;
        Vector3 hopFrom;

        public void Configure(Transform[] pawnFigures, TMP_Text plateText, float baseHeight, float maxSpacing, float span)
        {
            pawns = pawnFigures; plate = plateText; baseY = baseHeight; spacing = maxSpacing; width = span;
        }

        Vector3 Rest(int i, int count)
        {
            float step = count > 1 ? Mathf.Min(spacing, width / (count - 1)) : 0;
            return new Vector3(-step * (count - 1) / 2 + i * step, baseY, 0);
        }

        public void Bind(int idle)
        {
            int visible = Mathf.Min(idle, pawns.Length);
            for (int i = 0; i < pawns.Length; i++)
            {
                bool on = i < visible;
                if (pawns[i].gameObject.activeSelf != on) pawns[i].gameObject.SetActive(on);
                if (!(hopAt >= 0 && i == hopIndex)) pawns[i].localPosition = Rest(i, visible);
            }
            shown = idle;
            plate.text = Corrigible.Core.Loc.F("BOŞTA {0}", idle);
        }

        /// <summary>Son görünen piyonun dünya konumu (+ PERS.: odaya buradan zıplar).</summary>
        public Vector3 LastPawnWorld()
        {
            int visible = Mathf.Clamp(Mathf.Min(shown, pawns.Length), 1, pawns.Length);
            return transform.TransformPoint(Rest(visible - 1, visible));
        }

        /// <summary>− PERS.: yeni gelen (sondaki) piyon odadan kavisle tepsiye zıplar (0,30 s).</summary>
        public void HopIn(Vector3 fromWorld)
        {
            int visible = Mathf.Min(shown, pawns.Length);
            if (visible <= 0) return;
            hopIndex = visible - 1;
            hopFrom = transform.InverseTransformPoint(fromWorld);
            hopAt = Time.unscaledTime;
        }

        void Update()
        {
            if (hopAt < 0) return;
            float p = Mathf.Clamp01((Time.unscaledTime - hopAt) / 0.3f);
            int visible = Mathf.Min(shown, pawns.Length);
            pawns[hopIndex].localPosition = Vector3.Lerp(hopFrom, Rest(hopIndex, visible), p) + Vector3.up * (0.06f * Mathf.Sin(p * Mathf.PI));
            if (p >= 1) hopAt = -1;
        }
    }
}
