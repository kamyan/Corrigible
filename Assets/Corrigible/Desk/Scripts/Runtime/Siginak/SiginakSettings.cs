using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Sığınak sunumunun ayarları (Docs/UI_Siginak PROMPT §2 “Güvenli geçiş”). <see cref="newPresentation"/> eski komuta
    /// odasıyla yeni sığınak odası arasında seçer; Aşama 10'dan beri varsayılan yeni görünümdür. Ölçek ve ışık
    /// eşleştirme sayıları tokenlarda olmayan uygulama değerleridir.
    /// </summary>
    [CreateAssetMenu(menuName = "Corrigible/Siginak Settings", fileName = "SiginakSettings")]
    public sealed class SiginakSettings : ScriptableObject
    {
        [Tooltip("Açıkken DeskGame yeni sığınak odasıyla açılır (eski oda, HUD ve alt çubuk gizlenir).")]
        public bool newPresentation = true;

        [Tooltip("Özgür bölge sayısına bağlı umut işaretleri (K1 notu; ayrı ve kapatılabilir).")]
        public bool hopeMarks = true;

        [Tooltip("K1 fiziksel kamerasının sensörü (mm): 16:9, dikey FOV ve lens kaydırması tokenlardan.")]
        public Vector2 sensorSizeMm = new Vector2(36, 20.25f);

        [Tooltip("Masadaki kâğıt haritanın genişliği (m); World Space haritanın ölçeği = genişlik / 1920.")]
        public float mapWidthMeters = 1.2f;

        [Tooltip("three.js yarım küre ışığını (yoğunluk 0,6, ACES ton eşleme) URP üç renkli ortam ışığına çeviren çarpan.")]
        public float ambientScale = 0.08f;

        [Tooltip("Ortam yansıması (skybox) şiddeti: metal yüzeyler karanlık sığınakta gökyüzünü yansıtmasın.")]
        public float reflectionIntensity = 0.15f;

        [Header("Işık şiddeti (three.js referansına göre ayarlandı; tokenlardaki urpStart başlangıçtır)")]
        public float bulbIntensity = 1.5f;
        public float deskLampIntensity = 3.5f;
        public float crtLeakIntensity = 0.3f;

        static SiginakSettings active;
        public static SiginakSettings Active
        {
            get { if (active == null) active = Resources.Load<SiginakSettings>("SiginakSettings"); return active; }
            set => active = value;
        }
    }
}
