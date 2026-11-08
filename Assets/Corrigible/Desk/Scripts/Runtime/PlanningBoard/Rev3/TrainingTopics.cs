using System;
using Corrigible.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// EĞİTİM cebinin konu listesi (R15a; Soru 1: yedi satır, 16 mm aralık): yedi satır yazısı (içe aktarılan TXT_Topics
    /// gizli), kırmızı yağlı kalem halkası (Image Filled Radial360, satır değişince 0,2 s'de yeniden çizilir) ve satır
    /// çarpıştırıcıları. Satırlar bu bileşenin çocuklarıdır; tıklama ve üzerine gelme buraya kabarır, satır çarpışan nesneden
    /// bulunur. Eğitimdeki kişinin usta olduğu konu soluk yazılır ve etiketinde "usta" der (renk tek başına durum taşımaz).
    /// </summary>
    public sealed class TrainingTopics : MonoBehaviour, IHoverSource, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        const int Rows = 7;

        [Tooltip("Yedi satır yazısı (üstten alta: tek emp ana craft cook care gather).")]
        [SerializeField] TMP_Text[] rows = new TMP_Text[Rows];
        [Tooltip("Kalem halkası (ring_pencil); 1 birim = 1 mm dünya tuvalinde, pano yüzünün önünde.")]
        [SerializeField] Image ring;
        [Tooltip("Satır çarpıştırıcıları (bu nesnenin çocukları); boyutları BoardLayout.TopicRow'dan kurulur.")]
        [SerializeField] BoxCollider[] rowHits = new BoxCollider[Rows];
        [Tooltip("Pano modelinin kökü (model yereli = BoardLayout'un metre çerçevesi).")]
        [SerializeField] Transform frame;
        [SerializeField] HoverTag hoverTag;

        readonly string[] shownLabels = new string[Rows];
        readonly bool[] shownMastered = new bool[Rows];
        Color[] baseColours;
        int ringRow = -2;
        float ringStart;
        bool ringFilling, hitsFitted, interactive = true, hovered;
        int hoverRow = -1;
        PointerEventData hoverEvent;

        /// <summary>Satır tıklandı (0–6); sunucu eğitimdeki kişiye <c>TrainResident</c> der ya da seçimi saklar.</summary>
        public event Action<int> RowClicked;

        public HoverTag Tag { get => hoverTag; set => hoverTag = value; }
        public int RingRow => ringRow;

        public string HoverTitle => hoverRow >= 0 && hoverRow < Rows ? shownLabels[hoverRow] : null;
        public string Line => hoverRow >= 0 && hoverRow < Rows && shownMastered[hoverRow] ? Loc.T("usta") : null;

        void Awake()
        {
            CaptureColours();
            FitHits();
            if (ring != null)
            {
                ring.type = Image.Type.Filled;
                ring.fillMethod = Image.FillMethod.Radial360;
                ring.raycastTarget = false;
            }
        }

        void CaptureColours()
        {
            if (baseColours != null || rows == null) return;
            baseColours = new Color[rows.Length];
            for (int i = 0; i < rows.Length; i++) baseColours[i] = rows[i] != null ? rows[i].color : Color.white;
        }

        /// <summary>Satır çarpıştırıcılarını BoardLayout.TopicRow dikdörtgenlerine oturtur (nesneleri taşımadan: center ve size).</summary>
        void FitHits()
        {
            if (hitsFitted || frame == null || rowHits == null) return;
            hitsFitted = true;
            for (int i = 0; i < rowHits.Length && i < Rows; i++)
            {
                var hit = rowHits[i];
                if (hit == null) continue;
                var r = BoardLayout.TopicRow(i);
                Vector3 world = frame.TransformPoint(BoardLayout.MmToLocal(r.center, BoardLayout.FaceZ - 0.0015f));
                var t = hit.transform;
                hit.center = t.InverseTransformPoint(world);
                Vector3 s = t.lossyScale, fs = frame.lossyScale;
                hit.size = new Vector3(SafeDiv(r.width / 1000f * fs.x, s.x), SafeDiv((r.height - 1f) / 1000f * fs.y, s.y), SafeDiv(0.003f * fs.z, s.z));
            }
        }

        static float SafeDiv(float a, float b) => Mathf.Abs(b) < 1e-9f ? a : Mathf.Abs(a / b);

        /// <summary>
        /// Satırları yazar. <paramref name="mastered"/>: eğitimdeki kişi o konuda usta (soluk). <paramref name="ringRow"/>:
        /// halkalı satır (−1 = halka yok); değişince ve <paramref name="animate"/> ise halka 0,2 s'de yeniden çizilir.
        /// </summary>
        public void SetRows(string[] labels, bool[] mastered, int ringRow, bool animate)
        {
            CaptureColours();
            float dim = BoardViewUtil.Num("board.topics.masteredAlpha", 0.4f);
            for (int i = 0; i < Rows; i++)
            {
                string label = labels != null && i < labels.Length ? labels[i] ?? "" : "";
                bool m = mastered != null && i < mastered.Length && mastered[i];
                shownLabels[i] = label;
                shownMastered[i] = m;
                if (rows == null || i >= rows.Length || rows[i] == null) continue;
                BoardViewUtil.SetText(rows[i], label);
                var c = baseColours[i];
                var want = new Color(c.r, c.g, c.b, c.a * (m ? dim : 1f));
                if (rows[i].color != want) rows[i].color = want;
            }
            SetRing(ringRow, animate);
        }

        void SetRing(int row, bool animate)
        {
            if (ring == null) { ringRow = row; return; }
            if (row == ringRow) return;
            bool hadRing = ringRow >= 0;
            ringRow = row;
            if (row < 0 || row >= Rows) { ring.enabled = false; ringFilling = false; return; }
            PlaceRing(row);
            ring.enabled = true;
            if (animate && hadRing) { ring.fillAmount = 0f; ringStart = Time.unscaledTime; ringFilling = true; }
            else { ring.fillAmount = 1f; ringFilling = false; }
        }

        void PlaceRing(int row)
        {
            var rt = ring.rectTransform;
            var r = BoardLayout.TopicRow(row);
            float w = BoardViewUtil.Num("board.layoutMm.topics.ringMm[0]", 74f);
            float h = BoardViewUtil.Num("board.layoutMm.topics.ringMm[1]", 15f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (frame != null)
            {
                rt.position = frame.TransformPoint(BoardLayout.MmToLocal(r.center, BoardLayout.FaceZ - 0.0006f));
                var parent = rt.parent;
                float perMm = parent != null && Mathf.Abs(parent.lossyScale.x) > 1e-9f ? frame.lossyScale.x * 0.001f / parent.lossyScale.x : 1f;
                rt.sizeDelta = new Vector2(w * perMm, h * perMm);
            }
            else rt.sizeDelta = new Vector2(w, h);
        }

        /// <summary>K4'e varınca açık; başka pozlarda satır çarpıştırıcıları kapalı.</summary>
        public void SetInteractive(bool on)
        {
            interactive = on;
            if (rowHits != null) for (int i = 0; i < rowHits.Length; i++) if (rowHits[i] != null) rowHits[i].enabled = on;
            if (!on) EndHover();
        }

        static bool Blocked => DeskTarget.Blocked != null && DeskTarget.Blocked();

        int RowOf(GameObject go)
        {
            if (go == null || rowHits == null) return -1;
            for (int i = 0; i < rowHits.Length; i++) if (rowHits[i] != null && rowHits[i].gameObject == go) return i;
            return -1;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!interactive || Blocked) return;
            hovered = true;
            hoverEvent = eventData;
            UpdateHover(RowOf(eventData.pointerCurrentRaycast.gameObject));
        }

        public void OnPointerExit(PointerEventData eventData) => EndHover();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || eventData.dragging || !interactive || Blocked) return;
            int press = RowOf(eventData.pointerPressRaycast.gameObject);
            int now = RowOf(eventData.pointerCurrentRaycast.gameObject);
            if (press < 0 || press != now) return;
            RowClicked?.Invoke(press);
        }

        void UpdateHover(int row)
        {
            if (row == hoverRow) return;
            if (hoverTag != null && hoverRow >= 0) hoverTag.Hide(this);
            hoverRow = row;
            if (hoverTag != null && row >= 0) hoverTag.Show(this);
        }

        void EndHover()
        {
            hovered = false;
            hoverEvent = null;
            UpdateHover(-1);
        }

        void Update()
        {
            if (hovered)
            {
                if (!interactive || Blocked || hoverEvent == null) EndHover();
                else UpdateHover(RowOf(hoverEvent.pointerCurrentRaycast.gameObject));
            }
            if (!ringFilling || ring == null) return;
            float seconds = BoardViewUtil.Num("motion.board.ringS", 0.2f);
            float t = seconds <= 0 ? 1f : Mathf.Clamp01((Time.unscaledTime - ringStart) / seconds);
            ring.fillAmount = t;
            if (t >= 1f) ringFilling = false;
        }

        void OnDisable() => EndHover();
    }
}
