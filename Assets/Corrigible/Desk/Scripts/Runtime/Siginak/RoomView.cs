using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Corrigible.Desk
{
    /// <summary>
    /// Kesit maketinde bir oda (K4 notu, K8 › oda durumları): çalışan oda aydınlık (ampul + nokta ışık), enerjisiz ve
    /// hasarlı oda karanlık (duvar #5A554B, sönük ampul); hasarlıda moloz ve düşmüş kiriş. Kapasite = pirinç disk yuva,
    /// dolu yuva = piyon. Pirinç çerçevede dizin kartı (ad, satır, damga); seçiliyken oda ağzına 8 px sarı + 2 px mürekkep,
    /// kartta iç sarı çerçeve. Durumu bağlayıcı verir; burada yalnız görünüm ve kısa hareketler vardır.
    /// </summary>
    public sealed class RoomView : MonoBehaviour
    {
        [SerializeField] string roomId;
        [Header("Oda")]
        [SerializeField] Renderer wall, floor, bulb;
        [SerializeField] Material wallOn, wallOff, floorOn, floorOff, bulbOn, bulbOff;
        [SerializeField] Light lamp;
        [SerializeField] float lampIntensity = 0.15f;
        [SerializeField] GameObject damage;
        [Header("Piyonlar")]
        [Tooltip("Yuva ve piyonların x merkezi, taban y'si ve z'si (maket yereli).")]
        [SerializeField] Vector3 slotOrigin;
        [SerializeField] Transform[] slots = Array.Empty<Transform>();
        [SerializeField] Transform[] pawns = Array.Empty<Transform>();
        [Header("Kart")]
        [SerializeField] Image cardPaper, cardSelect;
        [SerializeField] TMP_Text cardName, cardLine;
        [SerializeField] RectTransform stamp;
        [SerializeField] Image stampFrame;
        [SerializeField] TMP_Text stampText;
        [SerializeField] GameObject openingFrame;

        ShelterRoomData data;
        bool selectionShown;
        int shownWorkers = -1;
        float lightOnAt = -1;
        readonly float[] hopAt = new float[8];
        readonly Vector3[] hopFrom = new Vector3[8];

        public string RoomId => roomId;
        public ShelterRoomData Data => data;
        /// <summary>K4 pozunda oda ışıkları güçlenir (tokens › roomBulb preview: ×2,4); bağlayıcı yazar.</summary>
        public float LightScale { get; set; } = 1;

        public void Configure(string id, Renderer wallRenderer, Renderer floorRenderer, Renderer bulbRenderer, Material wallLit, Material wallDark,
            Material floorLit, Material floorDark, Material bulbLit, Material bulbDark, Light pointLight, GameObject damageGroup, Vector3 origin,
            Transform[] slotDiscs, Transform[] pawnFigures, Image paper, Image select, TMP_Text nameText, TMP_Text lineText, RectTransform stampRoot,
            Image stampEdge, TMP_Text stampWord, GameObject opening)
        {
            roomId = id; wall = wallRenderer; floor = floorRenderer; bulb = bulbRenderer; wallOn = wallLit; wallOff = wallDark; floorOn = floorLit;
            floorOff = floorDark; bulbOn = bulbLit; bulbOff = bulbDark; lamp = pointLight; lampIntensity = pointLight != null ? pointLight.intensity : 0;
            damage = damageGroup; slotOrigin = origin; slots = slotDiscs; pawns = pawnFigures; cardPaper = paper; cardSelect = select;
            cardName = nameText; cardLine = lineText; stamp = stampRoot; stampFrame = stampEdge; stampText = stampWord; openingFrame = opening;
        }

        void Awake() { for (int i = 0; i < hopAt.Length; i++) hopAt[i] = -1; }

        public void Bind(ShelterRoomData d)
        {
            data = d;
            var t = SiginakTheme.Active;
            bool lit = d.Condition == RoomCondition.Working;
            if (wall != null) wall.sharedMaterial = lit ? wallOn : wallOff;
            if (floor != null) floor.sharedMaterial = lit ? floorOn : floorOff;
            if (bulb != null) bulb.sharedMaterial = lit ? bulbOn : bulbOff;
            if (lamp != null) lamp.enabled = lit;
            if (damage != null && damage.activeSelf != (d.Condition == RoomCondition.Damaged)) damage.SetActive(d.Condition == RoomCondition.Damaged);

            // Yuvalar kapasite kadar, aralık 0,26 m'ye yayılır (room.js › slot/peg)
            int cap = Mathf.Min(d.Capacity, slots.Length);
            for (int i = 0; i < slots.Length; i++)
            {
                bool on = i < cap;
                if (slots[i].gameObject.activeSelf != on) slots[i].gameObject.SetActive(on);
                float x = cap > 1 ? -0.13f + i * (0.26f / (cap - 1)) : 0;
                var p = slotOrigin + new Vector3(x, 0, 0);
                slots[i].localPosition = p + new Vector3(0, 0.001f, 0);
                bool staffed = on && i < d.Workers;
                if (pawns[i].gameObject.activeSelf != staffed) pawns[i].gameObject.SetActive(staffed);
                if (hopAt[i] < 0) pawns[i].localPosition = p + new Vector3(0, 0.002f, 0);
            }
            shownWorkers = d.Workers;

            // Kart: seçiliyken #FFFDF5 + iç sarı; damga: HASARLI · ENERJİSİZ · EMİR UYGULANDI (yeni, bir vardiya)
            cardName.text = d.Name;
            cardLine.text = d.Line;
            ApplySelection();
            string key = d.Condition == RoomCondition.Damaged ? "stamp.damaged" : d.Condition == RoomCondition.NoPower ? "stamp.noPower" : d.Fresh ? "stamp.orderDone" : null;
            stamp.gameObject.SetActive(key != null);
            if (key != null)
            {
                var c = t.ColorOf(key);
                stampFrame.color = c;
                stampText.color = c;
                stampText.text = d.Condition == RoomCondition.Damaged ? Corrigible.Core.Loc.T("HASARLI")
                    : d.Condition == RoomCondition.NoPower ? Corrigible.Core.Loc.T("ENERJİSİZ") : Corrigible.Core.Loc.T("EMİR UYGULANDI");
            }
        }

        /// <summary>
        /// Seçim (açıklıktaki ve karttaki sarı çerçeve, açık kart kâğıdı) yalnız raf pozunda (K4) görünür; K1'de maket bütün
        /// olarak tek hedeftir. Bağlayıcı poza göre kurar.
        /// </summary>
        public bool SelectionShown
        {
            get => selectionShown;
            set
            {
                if (selectionShown == value) return;
                selectionShown = value;
                if (data != null) ApplySelection();
            }
        }

        void ApplySelection()
        {
            bool on = data.Selected && selectionShown;
            cardPaper.color = SiginakTheme.Active.ColorOf(on ? "ui.frameButtonHover" : "index");
            cardSelect.enabled = on;
            if (openingFrame.activeSelf != on) openingFrame.SetActive(on);
        }

        /// <summary>Yuvadaki piyonun dünya konumu (− PERS.: tepsiye bu noktadan zıplar).</summary>
        public Vector3 PawnWorld(int index) => pawns[Mathf.Clamp(index, 0, pawns.Length - 1)].position;

        /// <summary>+ PERS.: piyon tepsiden kavisle yuvasına zıplar (0,30 s).</summary>
        public void HopIn(int index, Vector3 fromWorld)
        {
            if (index < 0 || index >= pawns.Length) return;
            hopFrom[index] = transform.InverseTransformPoint(fromWorld);
            hopAt[index] = Time.unscaledTime;
        }

        /// <summary>ONAR / GELİŞTİR: ışık 0,2 s'de yanar, bir kez titrer.</summary>
        public void LightUp() => lightOnAt = Time.unscaledTime;

        void Update()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < pawns.Length && i < hopAt.Length; i++)
            {
                if (hopAt[i] < 0) continue;
                float p = Mathf.Clamp01((now - hopAt[i]) / 0.3f);
                int cap = data != null ? Mathf.Min(data.Capacity, slots.Length) : slots.Length;
                float x = cap > 1 ? -0.13f + i * (0.26f / (cap - 1)) : 0;
                var rest = slotOrigin + new Vector3(x, 0.002f, 0);
                pawns[i].localPosition = Vector3.Lerp(hopFrom[i], rest, p) + Vector3.up * (0.06f * Mathf.Sin(p * Mathf.PI));
                if (p >= 1) hopAt[i] = -1;
            }
            if (lamp == null || !lamp.enabled) return;
            float k = 1;
            if (lightOnAt >= 0)
            {
                float e = now - lightOnAt;
                k = e < 0.2f ? e / 0.2f : e < 0.28f ? 1 : e < 0.34f ? 0.35f : 1;
                if (e >= 0.4f) lightOnAt = -1;
            }
            lamp.intensity = lampIntensity * LightScale * k;
        }
    }
}
