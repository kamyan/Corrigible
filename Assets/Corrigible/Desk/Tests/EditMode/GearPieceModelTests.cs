using System.Collections.Generic;
using System.Linq;
using Corrigible.Game.Raid;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Tesis ekipmanının parçaları (aşama 9; tasarım §5.4, yol E): yaylı taret, yürüyen yem, makaralı vinç, köpek, tel örgü, kalas duvar,
    /// hurda ve karo süsleri (kapalı/kalkık kepenk, sandık nişi kapalı/açık, tutuş krikosu) var olan mesh asset'lerinden ve Rev3 malzeme
    /// kütüphanesinden kurulmuş prefab'lar. Sözleşme: kütüphane alanları dolu; kök ölçeksiz ve dönüşsüz; birim ve engelde gövde dolu,
    /// görünen her renderer parlamaya katılır, çarpıştırıcı var; süslerde RaidPiece ve çarpıştırıcı yok (iç içe sandık hariç, tahta
    /// kaldırır); mesh'ler asset (çalışma anında geometri üretilmez); her alt meshin malzemesi var; kırmızı ve ışıma yok; üçgen bütçesi
    /// (engel ve süs ≤ 400, birim ≤ 1500); Unit/Object dalları kendi alanını döndürür, yedek ölçek uygulanmaz. Aşama 11: saha rölesi
    /// (<c>relay</c>, nesne bütçesi) aynı sözleşmeyle.
    /// </summary>
    public sealed class GearPieceModelTests
    {
        const string LibraryPath = "Assets/Corrigible/Settings/RaidPieceLibrary.asset";
        static RaidPieceLibrary Lib => AssetDatabase.LoadAssetAtPath<RaidPieceLibrary>(LibraryPath);

        [Test]
        public void NewSignatureMachines_UseSixDistinctPrefabParts()
        {
            var names = new[] { "alev", "muhur", "tampon", "montaj", "kirag", "capa" };
            foreach (var name in names)
            {
                Assert.IsTrue(Lib.HasOwnPiece(name), name);
                Common(name, Lib.Unit(name).gameObject, 1500, teamSide: false);
                Assert.IsNotNull(Lib.Unit(name).GetComponent<BoxCollider>(), name);
            }
            CollectionAssert.AllItemsAreUnique(names.Select(Lib.Unit));
        }

        static bool IsRed(Color c) { Color.RGBToHSV(c, out float h, out float s, out _); return (h < 0.04f || h > 0.96f) && s > 0.5f; }

        /// <summary>Parçanın kendi mesh'leri (iç içe Rev3 sandığı hariç: o kendi paketinin testinde).</summary>
        static IEnumerable<MeshFilter> OwnMeshes(GameObject go) =>
            go.GetComponentsInChildren<MeshFilter>(true).Where(f => PrefabUtility.GetNearestPrefabInstanceRoot(f.gameObject) == null);

        static int Triangles(IEnumerable<MeshFilter> filters) => filters.Sum(f => f.sharedMesh.triangles.Length / 3);

        static void Common(string name, GameObject go, int budget, bool teamSide = true)
        {
            Assert.IsNotNull(go, name + " atanmış");
            Assert.AreEqual(Vector3.one, go.transform.localScale, name + " kök ölçeksiz");
            Assert.AreEqual(Quaternion.identity, go.transform.localRotation, name + " kök dönüşsüz");
            var own = OwnMeshes(go).ToList();
            Assert.IsNotEmpty(own, name + " parçaları");
            foreach (var f in own)
            {
                Assert.IsNotNull(f.sharedMesh, name + "/" + f.name + " mesh");
                Assert.IsTrue(AssetDatabase.Contains(f.sharedMesh), name + "/" + f.name + ": mesh asset (çalışma anında üretilmez)");
                var r = f.GetComponent<MeshRenderer>();
                Assert.AreEqual(f.sharedMesh.subMeshCount, r.sharedMaterials.Length, name + "/" + f.name + ": her alt meshin malzemesi");
                foreach (var m in r.sharedMaterials)
                {
                    Assert.IsNotNull(m, name + "/" + f.name + " malzeme");
                    Assert.IsTrue(AssetDatabase.GetAssetPath(m).StartsWith("Assets/Corrigible/Desk/Art/Rev3/Materials/"), name + "/" + f.name + ": Rev3 malzeme kütüphanesinden");
                    if (teamSide) Assert.IsFalse(IsRed(m.GetColor("_BaseColor")), name + " kırmızı yok (ekip tarafı): " + m.name);
                    var e = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
                    Assert.IsTrue(e.r + e.g + e.b < 1e-4f, name + " ışıma yok: " + m.name);
                }
            }
            Assert.LessOrEqual(Triangles(own), budget, name + " üçgen bütçesi");
        }

        [Test]
        public void UnitsAndBarriers_OwnPieces_Contract()
        {
            var lib = Lib;
            var pieces = new (string kind, RaidPiece piece, bool unit)[]
            {
                ("taret", lib.turret, true), ("yem", lib.decoy, true), ("vinc", lib.winch, true), ("kopek", lib.dog, true),
                ("tel", lib.wire, false), ("kalas", lib.plank, false), ("hurda", lib.scrap, false),
                // Tesis aşama 11: savunmanın saha rölesi (nesne; ekip tarafı, kırmızı yok)
                ("role", lib.relay, false),
            };
            foreach (var (kind, piece, unit) in pieces)
            {
                Assert.IsNotNull(piece, kind + " parçası atanmış");
                Common(kind, piece.gameObject, unit ? 1500 : 400);
                Assert.IsTrue(lib.HasOwnPiece(kind), kind);
                Assert.AreEqual(Vector3.one, lib.FallbackScale(kind), kind + ": yedek ölçek uygulanmaz");
                Assert.AreSame(piece, unit ? lib.Unit(kind) : lib.Object(kind), kind + ": dal kendi alanını döndürür");
                var so = new SerializedObject(piece);
                var body = so.FindProperty("body").objectReferenceValue as Transform;
                Assert.IsNotNull(body, kind + " gövdesi");
                var tinted = Enumerable.Range(0, so.FindProperty("tinted").arraySize)
                    .Select(i => so.FindProperty("tinted").GetArrayElementAtIndex(i).objectReferenceValue as Renderer).ToList();
                Assert.IsFalse(tinted.Contains(null), kind + " boyananlarda boş yok");
                foreach (var r in body.GetComponentsInChildren<Renderer>().Where(r => r.GetComponent<TMP_Text>() == null))
                    CollectionAssert.Contains(tinted, r, kind + "/" + r.name + " parlamaya katılır");
                Assert.IsNotNull(piece.GetComponent<Collider>(), kind + ": tıklama için çarpıştırıcı");
                Assert.IsNull(so.FindProperty("hp").objectReferenceValue, kind + " can yazısı ortak ekran havuzunda");
            }
            // Ayırt edilebilirlik: kalas atışı keser (yüksek), tel ve hurda alçak; köpek piyondan alçak; birimler ve engeller ayrı parçalar
            float H(RaidPiece p) { var i = Object.Instantiate(p); try { return i.VisualHeight; } finally { Object.DestroyImmediate(i.gameObject); } }
            Assert.Greater(H(lib.plank), H(lib.wire), "kalas telden yüksek");
            Assert.Greater(H(lib.plank), H(lib.scrap), "kalas hurdadan yüksek");
            Assert.Less(H(lib.dog), H(lib.technician), "köpek piyondan alçak");
            Assert.AreEqual(8, pieces.Select(p => p.piece).Distinct().Count(), "her tür kendi parçası");
            Assert.Greater(H(lib.relay), H(lib.scrap), "röle (anten direği) hurdadan yüksek");
            CollectionAssert.DoesNotContain(new Object[] { lib.drone, lib.civilian, lib.technician, lib.cooling }, lib.Unit("taret"), "dron, sivil ya da yedek değil");
        }

        [Test]
        public void Decor_NoPieceNoCollider_Contract()
        {
            var lib = Lib;
            foreach (var (name, go) in new[] { ("shutter", lib.shutter), ("shutterOpen", lib.shutterOpen), ("crateNiche", lib.crateNiche), ("crateNicheOpen", lib.crateNicheOpen), ("holdJack", lib.holdJack) })
            {
                Common(name, go, 400);
                Assert.IsNull(go.GetComponent<RaidPiece>(), name + ": süs parça değil (seçilmez, etiketsiz)");
                foreach (var f in OwnMeshes(go)) Assert.IsNull(f.GetComponent<Collider>(), name + "/" + f.name + ": süs çarpıştırıcısız");
            }
            // Sandık nişi iç içe ikmal sandığını taşır (kapalı: kapaklı sandık; açık: boş tepsi)
            Assert.IsNotNull(lib.crateNiche.GetComponentInChildren<RaidPiece>(true), "kapalı nişte sandık");
            Assert.AreNotSame(lib.crateNiche, lib.crateNicheOpen);
        }

        /// <summary>Yedek kalıbı: parça atanmamışsa aygıt ve köpek Teknisyen piyonu ölçekli (dron değil), engel kırık dolap enkazından
        /// kurulur (sivil sunucu değil). Kütüphane kopyasında alanlar boşaltılır; asset değişmez.</summary>
        [Test]
        public void Fallbacks_TechnicianScaled_NotDrone()
        {
            var lib = Object.Instantiate(Lib);
            try
            {
                lib.turret = lib.decoy = lib.winch = lib.dog = null;
                lib.wire = lib.plank = lib.scrap = null;
                foreach (var kind in new[] { "taret", "yem", "vinc", "kopek" })
                {
                    Assert.IsFalse(lib.HasOwnPiece(kind), kind);
                    Assert.AreSame(lib.technician, lib.Unit(kind), kind + ": yedek ekip piyonu");
                    Assert.AreEqual(lib.gearUnitFallbackScale, lib.FallbackScale(kind), kind);
                }
                foreach (var kind in new[] { "tel", "kalas", "hurda" })
                {
                    Assert.IsFalse(lib.HasOwnPiece(kind), kind);
                    Assert.AreEqual(lib.barrierFallbackScale, lib.FallbackScale(kind), kind);
                }
                Assert.AreSame(lib.drone, lib.Unit("dron"), "eski türler aynı");
                Assert.AreSame(lib.civilian, lib.Object("civ"));
                Assert.AreEqual(Vector3.one, lib.FallbackScale("civ"));
            }
            finally { Object.DestroyImmediate(lib); }
        }
    }
}
