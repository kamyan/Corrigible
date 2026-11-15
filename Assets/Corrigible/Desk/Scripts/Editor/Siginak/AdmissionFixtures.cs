using System.Collections.Generic;
using System.Linq;
using Corrigible.Core;
using Corrigible.Desk.Bridge;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Kapıdaki aday grubunun (KOS aşama 2) sunum fikstürleri (Revizyon 3 · Aşama 6 kalıbı, <see cref="ProductionFixtures"/> gibi). Core'a
    /// dokunmaz, komut yazmaz: istasyonun anlık görüntüsü ayrı bir fikstür simülasyonundan (tohum 79, oyunun denge asset'i, panonun açılış
    /// komutları, gerçek kapanışlar) kurulur ve görünümlere bağlanır.
    /// <list type="bullet">
    /// <item><see cref="Present"/> (Play): adı verilen fikstürü istasyona çizer (<c>AdmissionStation.Bind(s, false)</c>, damgalar da) ve
    /// kamerayı K8A'ya oturtur. Bir sonraki <c>game.Changed</c>, dil değişimi ya da <see cref="Restore"/> gerçek görüntüye döner. Görüntü
    /// aynı çağrıda <see cref="PresentAndCapture"/> ile alınır (tek kare, <c>CorrigibleDebug.Capture</c>).</item>
    /// <item><see cref="ScanCases"/>: punto taramasının (<see cref="SiginakTextScan.ScanAdmissionPapers"/>) kâğıt verileri.</item>
    /// </list>
    /// Eski tek adaylı kabulün fikstürleri (belge, talimat şeridi, bulgu fişi, talep formu, Rauf/Aylin, kişi dosyasındaki eski iz satırları)
    /// ve düz kâğıt çizimi aşama 7'de söküldü. Görüntüler kayıt yalıtımıyla alınmalı (SessionState <c>Corrigible.TestStorage</c>, GELISTIRME).
    /// </summary>
    public static class AdmissionFixtures
    {
        /// <summary>Kâğıt prefab'larının klasörü.</summary>
        public const string PrefabDir = DeskSpriteManifest.Root + "/Prefabs/Rev3Admission";
        /// <summary>Aday kartı ve grup fişi (KOS aşama 2).</summary>
        public const string CardPrefab = PrefabDir + "/CandidateCard.prefab";
        public const string GroupSlipPrefab = PrefabDir + "/CandidateGroupSlip.prefab";
        public const string OutDir = "Temp";

        /// <summary>
        /// Fikstür adları: <c>group</c> (ilk grup: kartlar ve kapı fişi), <c>group_full</c> (kapasite 8: YER YOK listesi, bir kişi
        /// işaretli), <c>group_stamped</c> (ilk kartta KABUL izi), <c>group_en</c> (group İngilizce).
        /// </summary>
        public static readonly string[] Names = { "group", "group_full", "group_stamped", "group_en" };

        // ================================================================== veri (Core komutları; ayrı simülasyon)

        /// <summary>
        /// Oyunun denge asset'iyle tohum <paramref name="seed"/> ve panonun açılış komutları (<c>board_auto</c>, emekli görevler BOŞTA'ya;
        /// <c>PlanningBoardPresenter.RetireJobs</c> ile aynı): Boşta Cem, İpek, Zeynep.
        /// </summary>
        public static Simulation NewSimulation(uint seed = 79)
        {
            var sim = ProductionFixtures.NewSimulation(seed);
            foreach (var p in sim.Board.People.Where(p => p.Alive && (p.Job == "care" || p.Job == "gather" || p.Job == "watch" || p.Job == "contact")).ToArray())
                sim.AssignResident(p.Id, "rest");
            return sim;
        }

        /// <summary>Fikstürü <paramref name="day"/>. vardiyaya gerçek kapanışlarla (<c>PrintShift</c> + <c>FileShift</c>) getirir.</summary>
        public static void CloseTo(Simulation sim, int day)
        {
            for (int guard = 0; sim.S.Day < day && guard < 64; guard++)
            {
                if (!sim.PrintShift().Ok) break;
                if (!sim.FileShift(sim.Board.ReceiptId).Ok) break;
            }
        }

        /// <summary>
        /// İlk aday grubu kapıda (KOS aşama 2): tohum <paramref name="seed"/>, oyunun dengesi, grup gelene dek gerçek kapanışlar (en çok 8).
        /// <paramref name="capacity"/> &gt; 0 ise sığınak kapasitesi (8 = kurucularla dolu: YER YOK). Ölçülen: tohum 79 → V.005'te 2 aday,
        /// tohum 3 → V.003'te 3 aday.
        /// </summary>
        public static Simulation FirstGroup(uint seed = 79, int capacity = 0)
        {
            var sim = NewSimulation(seed);
            if (capacity > 0) sim.B.boardResidentCapacity = capacity;
            for (int i = 0; i < 8 && sim.Admission.Group.Count == 0; i++) CloseTo(sim, sim.S.Day + 1);
            return sim;
        }

        /// <summary>Dolu sığınakta ilk grup (kapasite 8; yalnız görüntü ve tarama için).</summary>
        public static Simulation FullGroup(uint seed = 79) => FirstGroup(seed, 8);

        /// <summary>
        /// Aday grubu fikstürünün anlık görüntüsü o anki dilde: <c>group</c> / <c>group_en</c>, <c>group_full</c> (panodaki son kişi
        /// yollanacak olarak işaretli), <c>group_stamped</c> (ilk kartta KABUL izi). Bilinmeyen ad null.
        /// </summary>
        public static AdmissionSnapshot GroupSnapshot(string name)
        {
            string key = (name ?? "").Trim().ToLowerInvariant();
            switch (key)
            {
                case "group":
                case "group_en":
                    return AdmissionSnapshotBuilder.Build(FirstGroup(), new AdmissionUi());
                case "group_full":
                {
                    var sim = FullGroup();
                    var ui = new AdmissionUi { ReplaceForGroup = AdmissionSnapshotBuilder.GroupKey(sim), ReplaceId = sim.Board.People.Last(p => p.Alive).Id };
                    return AdmissionSnapshotBuilder.Build(sim, ui);
                }
                case "group_stamped":
                {
                    var sim = FirstGroup();
                    var s = AdmissionSnapshotBuilder.Build(sim, new AdmissionUi());
                    if (s.Group != null && s.Group.Cards.Count > 0) AdmissionSnapshotBuilder.StampCard(s.Group, s.Group.Cards[0].Id, sim.B);
                    return s;
                }
            }
            return null;
        }

        /// <summary>Punto taramasının kâğıt verileri (o anki dilde): tohum 79'un ilk grubu, dolu sığınak ve tohum 3'ün üç kişilik grubu.</summary>
        public static List<(string name, AdmissionSnapshot snapshot)> ScanCases() => new List<(string, AdmissionSnapshot)>
        {
            ("Grup", GroupSnapshot("group")),
            ("Grup_dolu", GroupSnapshot("group_full")),
            ("Grup_3", AdmissionSnapshotBuilder.Build(FirstGroup(3), new AdmissionUi())),
        };

        // ================================================================== Play: istasyona sunum

        static T Ref<T>(Object owner, string field) where T : Object
        {
            if (owner == null) return null;
            var p = new SerializedObject(owner).FindProperty(field);
            return p != null && p.propertyType == SerializedPropertyType.ObjectReference ? p.objectReferenceValue as T : null;
        }

        static List<StampDecision> Stamps(SiginakAdmission admission)
        {
            var list = new List<StampDecision>();
            var p = new SerializedObject(admission).FindProperty("stamps");
            if (p == null || !p.isArray) return list;
            for (int i = 0; i < p.arraySize; i++)
                if (p.GetArrayElementAtIndex(i).objectReferenceValue is StampDecision s && s != null) list.Add(s);
            return list;
        }

        /// <summary>
        /// Play içinde (yalnız sunum): <paramref name="name"/> fikstürünü istasyona ve damgalara bağlar, kamerayı K8A'ya oturtur.
        /// <c>group_en</c> masa dilini İngilizceye çevirir. Komut yazılmaz, simülasyon değişmez. Bir sonraki <c>Changed</c> gerçek görüntüye
        /// döner.
        /// </summary>
        public static string Present(string name)
        {
            if (!Application.isPlaying) return "Play gerekli";
            string key = (name ?? "").Trim().ToLowerInvariant();
            if (!Names.Contains(key)) return "Bilinmeyen fikstür: " + name + " (" + string.Join(", ", Names) + ")";
            var admission = Object.FindFirstObjectByType<SiginakAdmission>(FindObjectsInactive.Include);
            if (admission == null) return "Sahnede SiginakAdmission yok (W6)";
            var station = Ref<AdmissionStation>(admission, "station");
            var rig = Ref<SiginakCameraRig>(admission, "rig");
            if (station == null || rig == null) return "SiginakAdmission bağları eksik (station, rig; W6)";
            if (key == "group_en" && DeskLoc.Instance.Lang != DeskLang.En) DeskLoc.Instance.SetLanguage(DeskLang.En);

            var s = GroupSnapshot(key);
            if (s == null) return "Fikstür kurulamadı: " + key;
            // Önce poz (varış işleyicisi gerçek görüntüyü yeniden çizebilir), sonra fikstür
            rig.Snap(SiginakPose.Admission);
            station.Bind(s, false);
            foreach (var st in Stamps(admission))
            {
                var d = s.Stamps[(int)st.Choice];
                if (d != null) st.Bind(d);
                st.ReturnToRack(false);
            }
            if (s.Group == null) return key + ": grup yok · poz " + rig.Current;
            return $"{key}: grup {s.Group.Key} · {s.Group.Cards.Count} kart · {(s.Group.Full ? "dolu, işaretli " + (s.Group.ReplaceId ?? "yok") : "yer var")} · poz {rig.Current} · dil {Loc.Lang}";
        }

        /// <summary>Fikstürü sunar ve aynı çağrıda görüntüyü alır.</summary>
        public static string PresentAndCapture(string name, string path = null)
        {
            string result = Present(name);
            path ??= System.IO.Path.Combine(OutDir, "A6_fikstur_" + (name ?? "x").Trim().ToLowerInvariant() + ".png");
            return result + " · " + Corrigible.EditorTools.CorrigibleDebug.Capture(path);
        }

        /// <summary>Gerçek görüntüye döner (akışın çizimi; komut yazmaz).</summary>
        public static string Restore()
        {
            if (!Application.isPlaying) return "Play gerekli";
            var admission = Object.FindFirstObjectByType<SiginakAdmission>(FindObjectsInactive.Include);
            if (admission == null) return "Sahnede SiginakAdmission yok";
            admission.Restore();
            var presenter = Object.FindFirstObjectByType<PlanningBoardPresenter>(FindObjectsInactive.Include);
            if (presenter != null) presenter.Refresh();
            return "kabul ve pano gerçek görüntüde";
        }
    }
}
