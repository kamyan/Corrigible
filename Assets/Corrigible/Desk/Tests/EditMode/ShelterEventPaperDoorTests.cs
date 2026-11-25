using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.Tests
{
    /// <summary>
    /// Kapı belgeleri (Docs/KAPI-BELGELERI-VE-YZ-MEKANLARI-TASARIMI.md §A): olay tutanağının ve sunucu ödülünün ortak görünümü
    /// (<see cref="ShelterEventPaperView"/>) tek başına. Dışarıdan gelen kâğıt kapının yanındaki belge çekmecesinin tablasına iner
    /// (<see cref="ShelterEventPaperView.Where.Door"/>; testte tabla "Kapı panosu" adlı boş nesnedir), çekmece açıkken tıklanınca ele gelir,
    /// elden bırakılınca tablaya döner; karar verilince tablada şalteri bekler (<see cref="ShelterEventPaperView.Where.Outbox"/>) ve
    /// gönderilince gizlenir. Tabladayken kâğıt tablanın çocuğudur (tepsinin etiketi karışmaz), gizlenince yeniden tepsinin çocuğu olur.
    /// Kayıttan devam tablada ve animasyonsuz; kapı çapası yoksa kâğıt tepsiye gelir ve bırakılınca tepsiye iner. Yüz ve tuval bağlanmaz (görünüm ikisi de yokken çalışır); örnekler test sonunda silinir,
    /// açık sahneye dokunulmaz.
    /// </summary>
    public sealed class ShelterEventPaperDoorTests
    {
        const float Eps = 1e-4f;
        const string DoorTitle = "KAPI · OLAY TUTANAĞI";
        const string DoorLine = "Kâğıdı panodan al.";

        readonly List<Object> spawned = new List<Object>();

        Transform tray, rest, board, door;
        ShelterEventPaperView view;
        BoxCollider hit;
        MeshRenderer quad;
        Camera cam;

        [TearDown]
        public void TearDown()
        {
            for (int i = spawned.Count - 1; i >= 0; i--) if (spawned[i] != null) Object.DestroyImmediate(spawned[i]);
            spawned.Clear();
        }

        GameObject Spawn(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            else spawned.Add(go);
            return go;
        }

        /// <summary>Masa düzeni: EVRAK tepsisi ve dinlenme çapası, tepsinin içindeki kâğıt (kaydedilmiş sahnedeki gibi), duvardaki kapı
        /// panosu ve çapası, el pozunun kamerası. <paramref name="withDoor"/> değilse kapı çapası bağlanmaz.</summary>
        void Build(bool withDoor = true)
        {
            tray = Spawn("Paper tray").transform;
            tray.SetPositionAndRotation(new Vector3(0.4f, 0.78f, 0.2f), Quaternion.Euler(90f, 0f, 0f));
            rest = Spawn("Rest", tray).transform;
            rest.localPosition = new Vector3(0f, 0f, -0.01f);

            board = Spawn("Kapı panosu").transform;
            board.SetPositionAndRotation(new Vector3(2.19f, 1.5f, -0.52f), Quaternion.Euler(0f, 90f, 0f));
            door = Spawn("Olay tutanağı kapıda", board).transform;
            door.localPosition = new Vector3(0f, 0.05f, -0.01f);

            cam = Spawn("Kamera").AddComponent<Camera>();
            cam.transform.SetPositionAndRotation(new Vector3(0f, 1.3f, -0.6f), Quaternion.Euler(30f, 0f, 0f));

            var paper = Spawn("Olay tutanağı", tray);
            hit = paper.AddComponent<BoxCollider>();
            hit.size = new Vector3(0.15f, 0.21f, 0.002f);
            hit.enabled = false;
            quad = paper.AddComponent<MeshRenderer>();
            view = paper.AddComponent<ShelterEventPaperView>();
            view.Configure(null, hit, quad, null, rest, cam, null, withDoor ? door : null);
            view.DoorTitle = DoorTitle;
            view.DoorLine = DoorLine;
        }

        static ShelterEventPaperData Data() => new ShelterEventPaperData
        {
            Key = "hirsizlik|3", Kind = "hirsizlik", Shift = "V.004", Title = "HIRSIZLIK", Body = "Depodan erzak eksildi.",
            Confirm = "önce bir satır seç (1, 2)", HoverLine = "Karar bekliyor",
        };

        static void Near(Pose expected, Transform t, string message)
        {
            Assert.Less(Vector3.Distance(expected.position, t.position), Eps, message + ": konum");
            Assert.Less(Quaternion.Angle(expected.rotation, t.rotation), 0.05f, message + ": dönüş");
        }

        /// <summary>Kapıya iniş: Arriving sırasında AtDoor, sonra panoda (Door) DoorPose'ta; panonun çocuğu, çarpıştırıcı açık, masa kilitli değil.</summary>
        [Test]
        public void Bind_AtDoor_Animated_ArrivesOnTheDoorBoard()
        {
            Build();
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, view.State);
            view.Bind(Data(), animate: true, atDoor: true);
            Assert.AreEqual(ShelterEventPaperView.Where.Arriving, view.State, "panoya iniyor");
            Assert.IsTrue(view.AtDoor, "inerken de kapıda sayılır");
            Assert.IsFalse(view.InTray, "tepsiye inmiyor");
            Assert.IsTrue(view.Moving);
            Assert.IsTrue(view.Visible);
            Assert.AreSame(board, view.transform.parent, "kapıya inen kâğıt panonun çocuğu");

            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, view.State, "kâğıt kapı panosunda");
            Assert.IsFalse(view.Moving);
            Assert.IsTrue(view.AtDoor);
            Assert.IsFalse(view.InTray);
            Assert.IsFalse(view.InHand);
            Assert.IsFalse(view.Busy, "panodaki kâğıt masayı kilitlemez");
            Near(view.DoorPose, view.transform, "DoorPose");
            Near(new Pose(door.position, door.rotation), view.transform, "kapı çapası");
            Assert.AreSame(board, view.transform.parent, "panoda kâğıt panonun çocuğu (tepsinin etiketi karışmaz)");
            Assert.IsTrue(hit.enabled, "panoda çarpıştırıcı açık");
            Assert.IsTrue(quad.enabled, "kâğıt görünür");
        }

        [Test]
        public void ClickAt_OnTheDoor_RaisesClicked()
        {
            Build();
            view.Bind(Data(), animate: false, atDoor: true);
            int clicks = 0, rows = 0, putBacks = 0;
            view.Clicked += () => clicks++;
            view.RowClicked += _ => rows++;
            view.PutBackClicked += () => putBacks++;
            view.ClickAt(new Vector2(75f, 100f));
            Assert.AreEqual(1, clicks, "panodaki kâğıda tık köprüye gider");
            Assert.AreEqual(0, rows);
            Assert.AreEqual(0, putBacks);

            view.Interactable = false;
            view.ClickAt(new Vector2(75f, 100f));
            Assert.AreEqual(1, clicks, "etkileşim kapalıyken tık alınmaz");
        }

        /// <summary>Çekmeceden ele, elden çekmeceye: kapıdan alınan kâğıt bırakılınca (Esc) çekmecenin tablasına döner (DoorPose) ve
        /// tablanın çocuğu kalır; EVRAK tepsisine inmez.</summary>
        [Test]
        public void ToHand_FromDoor_ThenPutBack_ReturnsToTheDrawer()
        {
            Build();
            var sounds = new List<ShelterEventSound>();
            view.Sound += sounds.Add;
            view.Bind(Data(), animate: true, atDoor: true);
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, view.State);

            Assert.IsTrue(view.ToHand(), "tabladaki kâğıt ele gelir");
            Assert.AreEqual(ShelterEventPaperView.Where.ToHand, view.State);
            Assert.IsTrue(view.InHand && view.Busy);
            Assert.IsFalse(view.AtDoor);
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Hand, view.State, "kâğıt elde");
            Near(view.HandTarget, view.transform, "el pozu");
            Assert.IsTrue(hit.enabled, "elde çarpıştırıcı açık");

            Assert.IsTrue(view.PutBack(), "elden bırakılır");
            Assert.AreEqual(ShelterEventPaperView.Where.ToTray, view.State);
            Assert.AreSame(board, view.transform.parent, "tablaya dönerken tablanın çocuğu");
            Assert.IsTrue(view.AtDoor, "çekmeceye dönüyor");
            Assert.IsFalse(view.InTray, "EVRAK tepsisine inmez");
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, view.State, "kâğıt yeniden çekmecede");
            Near(view.DoorPose, view.transform, "DoorPose");
            Assert.IsTrue(view.AtDoor);
            Assert.IsFalse(view.InTray);
            CollectionAssert.AreEqual(new[] { ShelterEventSound.ToHand, ShelterEventSound.ToTray }, sounds,
                "görünüm inişte ses yaymaz (iniş sesi köprüde); ele alış ve geri koyuş");

            int clicks = 0;
            view.Clicked += () => clicks++;
            view.ClickAt(new Vector2(75f, 100f));
            Assert.AreEqual(1, clicks, "açık çekmecedeki kâğıda tık");
            Assert.IsTrue(view.ToHand(), "çekmeceden yeniden ele");
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Hand, view.State);
        }

        /// <summary>Çekmece kapalıyken (<see cref="ShelterEventPaperView.DoorOpen"/> false) tabladaki kâğıdın çarpıştırıcısı kapalıdır ve
        /// tık alınmaz; çekmece açılınca ikisi de döner.</summary>
        [Test]
        public void DrawerClosed_PaperNotClickable()
        {
            Build();
            view.Bind(Data(), animate: false, atDoor: true);
            int clicks = 0;
            view.Clicked += () => clicks++;
            view.DoorOpen = false;
            view.Advance(0f);
            Assert.IsFalse(hit.enabled, "kapalı çekmecede çarpıştırıcı kapalı");
            view.ClickAt(new Vector2(75f, 100f));
            Assert.AreEqual(0, clicks, "kapalı çekmecedeki kâğıda tık alınmaz");
            view.DoorOpen = true;
            view.Advance(0f);
            Assert.IsTrue(hit.enabled);
            view.ClickAt(new Vector2(75f, 100f));
            Assert.AreEqual(1, clicks);
        }

        /// <summary>Karar: iz basılan kâğıt tablaya konur (Filing → Outbox; AtDoor değil, InOutbox), masa kilidi kalkar, tık alınmaz;
        /// <see cref="ShelterEventPaperView.Send"/> onu gizler ve tepsiye bağlar. Çapası olmayan kâğıt eskisi gibi tepsiye inip gizlenir.</summary>
        [Test]
        public void FileToDrawer_WaitsForTheSwitch_ThenSendHides()
        {
            Build();
            view.Bind(Data(), animate: false, atDoor: true);
            view.ToHand(); view.Advance(5f);
            view.Stamp(new[] { "KARARA BAĞLANDI", "Affet" });
            Assert.AreEqual(ShelterEventPaperView.Where.Stamping, view.State);
            view.FileToDrawer();
            Assert.AreEqual(ShelterEventPaperView.Where.Filing, view.State);
            Assert.IsTrue(view.InOutbox, "tablaya iniyor");
            Assert.AreSame(board, view.transform.parent, "tablanın çocuğu");
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Outbox, view.State, "karar tablada şalteri bekliyor");
            Assert.IsTrue(view.InOutbox);
            Assert.IsFalse(view.AtDoor, "okunacak belge sayılmaz");
            Assert.IsFalse(view.Busy, "masa kilitli değil");
            Assert.IsTrue(view.Visible);
            Assert.IsFalse(hit.enabled, "gönderilecek kâğıt tıklanmaz");
            Near(view.DoorPose, view.transform, "DoorPose");
            Assert.IsFalse(view.ToHand(), "karara bağlı kâğıt ele gelmez");

            Assert.IsTrue(view.Send(), "şalter");
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, view.State);
            Assert.IsFalse(view.InOutbox);
            Assert.AreSame(tray, view.transform.parent, "gizlenince tepsiye bağlanır");
            Assert.IsFalse(view.Send(), "ikinci kez gönderilmez");

            // Çapasız: FileToDrawer eski dosyalamadır
            TearDown();
            Build(withDoor: false);
            view.Bind(Data(), animate: false);
            view.ToHand(); view.Advance(5f);
            view.Stamp(new[] { "KARARA BAĞLANDI", "Affet" });
            view.FileToDrawer();
            Assert.AreEqual(ShelterEventPaperView.Where.Filing, view.State);
            Assert.IsFalse(view.InOutbox);
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, view.State, "çapasızda tepsiye inip gizlenir");
        }

        /// <summary>Kayıttan devam: kâğıt animasyonsuz panoda (tepsiye bırakılmış olsa da: yer sunum durumudur, kayda girmez).</summary>
        [Test]
        public void Restore_AtDoor_NoMotion()
        {
            Build();
            view.Bind(Data(), animate: true, atDoor: true);
            view.Advance(5f);
            view.ToHand();
            view.Advance(5f);
            view.PutBack();
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, view.State, "önce çekmeceye bırakıldı");

            view.Restore(Data(), true);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, view.State, "kayıttan devamda panoda");
            Assert.IsFalse(view.Moving, "animasyon yok");
            Assert.IsTrue(view.AtDoor);
            Assert.IsFalse(view.InTray);
            Near(view.DoorPose, view.transform, "DoorPose");
            Assert.AreSame(board, view.transform.parent);
            Assert.IsTrue(hit.enabled);

            view.Restore(null, true);
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, view.State, "null gizler");
            Assert.AreSame(tray, view.transform.parent, "gizlenince tepsiye bağlanır");
        }

        /// <summary>Kapı çapası bağlı değilse atDoor tepsiye düşer (eski düzen): Tray, RestPose, tepsinin çocuğu.</summary>
        [Test]
        public void NoDoorAnchor_AtDoorFallsBackToTheTray()
        {
            Build(withDoor: false);
            Assert.IsNull(view.DoorAnchor);
            Near(view.DoorPose, rest, "çapasız DoorPose tepsi pozudur");
            view.Bind(Data(), animate: true, atDoor: true);
            Assert.IsTrue(view.InTray, "tepsiye iniyor");
            Assert.IsFalse(view.AtDoor);
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Tray, view.State);
            Near(view.RestPose, view.transform, "RestPose");
            Assert.AreSame(tray, view.transform.parent);

            view.Restore(Data(), true);
            Assert.AreEqual(ShelterEventPaperView.Where.Tray, view.State, "çapasız kayıttan devam da tepside");
        }

        [Test]
        public void HideNow_FromTheDoor_RestoresTheTrayParent()
        {
            Build();
            view.Bind(Data(), animate: false, atDoor: true);
            Assert.AreSame(board, view.transform.parent);
            view.HideNow();
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, view.State);
            Assert.AreSame(tray, view.transform.parent, "gizlenen kâğıt tepsinin çocuğu");
            Assert.IsFalse(view.Visible);
            Assert.IsFalse(quad.enabled, "Quad kapandı");
            Assert.IsFalse(hit.enabled, "çarpıştırıcı kapandı");
            Assert.IsNull(view.Data);

            // Bind(null) da gizler ve tepsiye bağlar
            view.Bind(Data(), animate: true, atDoor: true);
            Assert.AreSame(board, view.transform.parent);
            view.Bind(null, false);
            Assert.AreEqual(ShelterEventPaperView.Where.Hidden, view.State);
            Assert.AreSame(tray, view.transform.parent);
        }

        /// <summary>Panoda üzerine gelme etiketi köprünün yazdığı başlık ve satır; tepside null (etiketi tepsinin hedefi verir).</summary>
        [Test]
        public void Hover_AtDoor_DoorTitleAndLine_NullOnTheTray()
        {
            Build();
            view.Bind(Data(), animate: true, atDoor: true);
            Assert.IsNull(view.HoverTitle, "inerken etiket yok");
            view.Advance(5f);
            Assert.AreEqual(DoorTitle, view.HoverTitle, "panoda DoorTitle");
            Assert.AreEqual(DoorLine, view.Line, "panoda DoorLine");

            view.ToHand();
            view.Advance(5f);
            Assert.IsNull(view.HoverTitle, "elde satır üzerinde değilken etiket yok");
            Assert.IsNull(view.Line);

            view.PutBack();
            view.Advance(5f);
            Assert.AreEqual(ShelterEventPaperView.Where.Door, view.State, "bırakılan kâğıt çekmeceye döner");
            Assert.AreEqual(DoorTitle, view.HoverTitle, "çekmecede yine DoorTitle");

            // Çapasız düzende tepside etiketi tepsinin hedefi verir
            TearDown();
            Build(withDoor: false);
            view.Bind(Data(), animate: false);
            Assert.AreEqual(ShelterEventPaperView.Where.Tray, view.State);
            Assert.IsNull(view.HoverTitle, "tepside etiketi tepsinin hedefi verir");
            Assert.IsNull(view.Line);
        }
    }
}
