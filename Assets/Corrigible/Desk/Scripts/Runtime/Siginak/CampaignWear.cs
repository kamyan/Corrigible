using System;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Harekât izleri (Revizyon 2 · S1; ayrı ve kapatılabilir): vardiyalar geçtikçe oda yıpranır. Eşikli nesneler (umut
    /// tenekesinin yaprakları V.001'de 3, V.010'da 7, V.025'te room.js'in büyük bitkisi: 11 yaprak + sap; V.016'dan teleks
    /// kuyruğu masadan sarkar, ikinci kupa ve teneke; V.025'te afişin yıpranan köşesine ek bant), masadaki yırtık teleks
    /// kâğıdı yığını ve EVRAK tepsisinin kalınlığı (⌊vardiya ÷ 3⌋ kat, en çok 10). Pano
    /// kartları, maket odaları ve DEPO iz değil, oyunun verisidir. Değeri bağlayıcı (SiginakDesk) yazar.
    /// Revizyon 3: tepsi modelinin yığını gövdede, geç yığını (<see cref="trayLateStack"/>) bir aşama nesnesidir; o görünürken
    /// tepsinin en üst kâğıdı (<see cref="traySheet"/>, EvrakTray'in A5'i) yığının üstüne çıkar.
    /// </summary>
    public sealed class CampaignWear : MonoBehaviour
    {
        [Serializable]
        public struct Stage
        {
            [Tooltip("Bu vardiyadan itibaren görünür.")]
            public int fromShift;
            [Tooltip("Bu vardiyada (ve sonra) gizlenir; 0 = hep görünür. Büyüyen nesnenin önceki hâli için.")]
            public int untilShift;
            public GameObject[] objects;
        }

        [SerializeField] Stage[] stages = Array.Empty<Stage>();
        [Tooltip("Masadaki yırtık teleks kâğıtları, alttan üste (kat başına bir kâğıt).")]
        [SerializeField] GameObject[] tornSheets = Array.Empty<GameObject>();
        [Tooltip("EVRAK tepsisindeki yığın (Cube): kalınlığı kat sayısıyla büyür; üst kâğıt yığının üstüne oturur.")]
        [SerializeField] Transform trayPapers, trayTopSheet;
        [SerializeField, Min(0)] float trayFloor = 0.006f, trayBase = 0.006f, trayPerLayer = 0.0028f;
        [SerializeField, Min(1)] int shiftsPerLayer = 3;
        [SerializeField, Min(0)] int maxLayers = 10;
        [Tooltip("Revizyon 3: tepsi modelinin geç yığını (bir aşamanın nesnesi). Boşsa kâğıt yüksekliği değişmez.")]
        [SerializeField] GameObject trayLateStack;
        [Tooltip("Revizyon 3: tepsinin en üst kâğıdı; yerel y alçak yığında traySheetLow, geç yığın görünürken traySheetHigh.")]
        [SerializeField] Transform traySheet;
        [SerializeField] float traySheetLow = 0.0285f, traySheetHigh = 0.0582f;

        int shown = -1;
        bool shownEnabled;

        /// <summary>Kat sayısı: ⌊vardiya ÷ 3⌋, en çok 10.</summary>
        public int Layers(int shift) => Mathf.Clamp(shift / Mathf.Max(1, shiftsPerLayer), 0, maxLayers);

        /// <summary><paramref name="enabled"/> kapalıysa (ayar) oda ilk vardiyadaki gibi görünür.</summary>
        public void Show(int shift, bool enabled)
        {
            if (shift == shown && enabled == shownEnabled) return;
            shown = shift; shownEnabled = enabled;
            int s = enabled ? shift : 0;
            foreach (var st in stages)
                foreach (var go in st.objects)
                    if (go != null) go.SetActive(s >= st.fromShift && (st.untilShift <= 0 || s < st.untilShift));
            int layers = Layers(s);
            for (int i = 0; i < tornSheets.Length; i++)
                if (tornSheets[i] != null) tornSheets[i].SetActive(i < layers);
            if (trayPapers != null)
            {
                float h = trayBase + layers * trayPerLayer;
                var p = trayPapers.localPosition;
                trayPapers.localScale = new Vector3(trayPapers.localScale.x, h, trayPapers.localScale.z);
                trayPapers.localPosition = new Vector3(p.x, trayFloor + h / 2, p.z);
                if (trayTopSheet != null)
                {
                    var t = trayTopSheet.localPosition;
                    trayTopSheet.localPosition = new Vector3(t.x, trayFloor + h + trayTopSheet.localScale.y / 2, t.z);
                }
            }
            if (traySheet != null && trayLateStack != null)
            {
                var p = traySheet.localPosition;
                traySheet.localPosition = new Vector3(p.x, trayLateStack.activeSelf ? traySheetHigh : traySheetLow, p.z);
            }
        }
    }
}
