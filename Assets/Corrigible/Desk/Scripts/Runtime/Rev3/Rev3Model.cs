using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// Revizyon 3 model prefab'ının kökü (içe aktarıcı kurar). Parçaları, yazı yuvalarını, çapaları ve ışık noktalarını
    /// manifestteki adlarıyla tutar; görünümler kendi referanslarını sahnede [SerializeField] ile bağlar, bu liste editör
    /// araçları, testler ve tek seferlik bağlama script'leri içindir. Manifestin üçgen sayısı ve sınır kutusu doğrulama için
    /// saklanır (sıfır poz, model uzayı).
    /// </summary>
    public sealed class Rev3Model : MonoBehaviour
    {
        [SerializeField] string modelName, family;
        [SerializeField] int manifestTris;
        [SerializeField] Bounds manifestBounds;
        [SerializeField] PartDriver[] parts = new PartDriver[0];
        [SerializeField] Rev3TextSlot[] slots = new Rev3TextSlot[0];
        [SerializeField] Transform[] anchors = new Transform[0];
        [SerializeField] Light[] lights = new Light[0];

        public string ModelName => modelName;
        public string Family => family;
        public int ManifestTris => manifestTris;
        public Bounds ManifestBounds => manifestBounds;
        public PartDriver[] Parts => parts;
        public Rev3TextSlot[] Slots => slots;
        public Transform[] Anchors => anchors;
        public Light[] Lights => lights;

        public void Configure(string name, string familyName, int tris, Bounds bounds, PartDriver[] partList, Rev3TextSlot[] slotList,
            Transform[] anchorList, Light[] lightList)
        {
            modelName = name; family = familyName; manifestTris = tris; manifestBounds = bounds;
            parts = partList; slots = slotList; anchors = anchorList; lights = lightList;
        }

        public PartDriver Part(string name) { foreach (var p in parts) if (p != null && p.PartName == name) return p; return null; }
        public Rev3TextSlot Slot(string name) { foreach (var s in slots) if (s != null && s.SlotName == name) return s; return null; }
        public Transform Anchor(string name) { foreach (var a in anchors) if (a != null && a.name == name) return a; return null; }
        public Light LightPoint(string name) { foreach (var l in lights) if (l != null && l.name == name) return l; return null; }

        /// <summary>Bütün parçalar dinlenme pozuna (dönüş/kayma <c>rest</c>, swap 0, görünür).</summary>
        public void ResetPose()
        {
            foreach (var p in parts) if (p != null) { p.SetVisible(true); p.ResetToRest(); }
        }

        /// <summary>Bütün dönen ve kayan parçalar 0'a (manifestin sınır kutusu bu pozda ölçülür).</summary>
        public void ZeroPose()
        {
            foreach (var p in parts) if (p != null && p.Kind != PartKind.Swap) p.Set(0);
        }
    }
}
