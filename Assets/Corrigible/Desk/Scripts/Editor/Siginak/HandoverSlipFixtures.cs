using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Corrigible.Desk.Bridge;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// Devir fişinin (R19c) sunum fikstürleri (Revizyon 3 · Aşama 4, Karar D8 karma "B" kolu): tasarımın iki örnek fişinin
    /// (<c>tasarim/kagit_kaynak/devir_fisi_v001_*.html</c>, <c>devir_fisi_v031_*.html</c>; görüntüleri
    /// <c>threejs/r10/papers_new/devir_fisi_*@2x.png</c>) dilden bağımsız verisi ve en kötü gerçekçi fiş (E13). Core'a dokunmaz:
    /// veri elle kurulur, simülasyondan okunmaz (V.031'e gerçek komutlarla birebir ulaşmak UNVERIFIED; D8).
    /// <list type="bullet">
    /// <item><see cref="V001"/>, <see cref="V031"/>, <see cref="Worst"/>: veri (punto taraması ve görüntüler aynı veriyi kullanır).</item>
    /// <item><see cref="Present"/> (Play): verilen fişi odadaki canlı fişin ve tepsideki kopyanın yüzüne yazar, isteğe bağlı masanın
    /// geç görünüşü (<c>CampaignWear.Show</c>); bir sonraki <c>game.Changed</c> ya da dil değişimi gerçek fişe döner.</item>
    /// <item><see cref="RenderFlat"/>: fiş yüzünün düz, dik çizimi (960 × 1440, tasarım PNG'siyle aynı boy) ve referansla yan yana
    /// kopyası (<c>A4_R19c_*</c> görüntüleri için). Önizleme sahnesinde kurar, açık sahneye dokunmaz.</item>
    /// </list>
    /// Kişi adları çevrilmez (R19c "isimler Türkçe"); sözcükleri <see cref="HandoverSlipFormat"/> o anki dilde yazar.
    /// </summary>
    public static class HandoverSlipFixtures
    {
        /// <summary>Fiş görünümünün prefab'ı (W3 kurucusu).</summary>
        public const string PrefabPath = DeskSpriteManifest.Root + "/Prefabs/Siginak/HandoverSlipView.prefab";
        /// <summary>Tasarımın fiş görüntüleri (@2x, 960 × 1440).</summary>
        public const string ReferenceDir = "Docs/UI_Siginak_Rev3/threejs/r10/papers_new";
        /// <summary>Düz çizimlerin varsayılan klasörü (entegratör görüntüleri <c>screens_unity_rev3/</c>'e taşır).</summary>
        public const string OutDir = "Temp";

        /// <summary>Fikstür adları: <c>V001</c>, <c>V031</c> (R19c) ve <c>worst</c> (E13).</summary>
        public static readonly string[] Names = { "V001", "V031", "worst" };

        /// <summary>Adı verilen fikstür (büyük/küçük harf duyarsız); bilinmiyorsa null.</summary>
        public static HandoverSlipFacts Named(string name)
        {
            switch ((name ?? "").Trim().ToLowerInvariant())
            {
                case "v001": return V001();
                case "v031": return V031();
                case "worst": return Worst();
                default: return null;
            }
        }

        /// <summary>Bütün fikstürler (ad, veri) sırayla.</summary>
        public static IEnumerable<(string name, HandoverSlipFacts facts)> All()
        {
            foreach (var n in Names) yield return (n, Named(n));
        }

        static SlipFieldRow Field(string role, string name, params string[] items) =>
            new SlipFieldRow { role = role, name = name, items = items.ToList() };

        static SlipShelterRow Shelter(string name, string job, string subject = null, SlipPersonState state = SlipPersonState.None, int shifts = 0) =>
            new SlipShelterRow { name = name, job = job, subject = subject, state = state, stateShifts = shifts };

        /// <summary>
        /// R19c V.001 (ilk vardiya; <c>devir_fisi_v001_tr.html</c>, C §7.3): 8/10 kişi, erzak 24/60 → 21,9 (−2,1); T Cem kit · zırh,
        /// E İpek kit · zırh, A Zeynep kit; Suna ÜRETİM, Deniz MUTFAK, Selin EĞİTİM · TEKNİSYEN, Eren ve Nermin BOŞTA; EĞİTİM Selin ·
        /// Teknisyen +2 (2→4); ÜRETİM iş emri yok · çıktı yok; KAPI açık başvuru yok; uyarı yok.
        /// </summary>
        public static HandoverSlipFacts V001()
        {
            var f = new HandoverSlipFacts
            {
                receiptId = "vardiya-0", shift = 1, copy = 1, alive = 8, capacity = 10,
                supplies = 24.0, suppliesCapacity = 60, suppliesAfter = 21.9,
                training = new SlipTraining { active = true, name = "Selin", subject = "tek", xpBefore = 2, xpAfter = 4, tierBefore = 1, tierAfter = 1 },
                production = new SlipProduction { kind = SlipProductionKind.None },
                openApplications = 0,
            };
            f.field.Add(Field("tek", "Cem", "kit_tek", "armor"));
            f.field.Add(Field("emp", "İpek", "kit_emp", "armor"));
            f.field.Add(Field("ana", "Zeynep", "kit_ana"));
            f.shelter.Add(Shelter("Suna", "craft"));
            f.shelter.Add(Shelter("Deniz", "cook"));
            f.shelter.Add(Shelter("Selin", "train", "tek"));
            f.shelter.Add(Shelter("Eren", "rest"));
            f.shelter.Add(Shelter("Nermin", "rest"));
            return f;
        }

        /// <summary>
        /// R19c V.031 (geç harekât; <c>devir_fisi_v031_tr.html</c>, PNG): 9/10 kişi, erzak 17/60 → 15,6 (−1,4); T Deniz kit · zırh,
        /// E Cem kit · zırh · EMP hücresi, A Zeynep kit · bakım kiti; Rauf ÜRETİM, Nermin MUTFAK, Suna EĞİTİM · EMP, Selin BOŞTA ·
        /// yaralı, İpek BOŞTA · yatıyor 2, Aylin BOŞTA · kısıtlı 2; EĞİTİM Suna · EMP +2 → EĞİTİMLİ; ÜRETİM Zırh plakası 0/1 → 1/1 ·
        /// Rauf; KAPI 1 açık başvuru; uyarılar KAPI ve DOSYA (Rauf · inceleme fişi açık).
        /// </summary>
        public static HandoverSlipFacts V031()
        {
            var f = new HandoverSlipFacts
            {
                receiptId = "vardiya-30", shift = 31, copy = 1, alive = 9, capacity = 10,
                supplies = 17.0, suppliesCapacity = 60, suppliesAfter = 15.6,
                training = new SlipTraining { active = true, name = "Suna", subject = "emp", xpBefore = 0, xpAfter = 2, tierBefore = 0, tierAfter = 1 },
                production = new SlipProduction { kind = SlipProductionKind.Progress, itemType = "armor", maker = "Rauf", done = 0, after = 1, turns = 1 },
                openApplications = 1,
            };
            f.field.Add(Field("tek", "Deniz", "kit_tek", "armor"));
            f.field.Add(Field("emp", "Cem", "kit_emp", "armor", "emp_cell"));
            f.field.Add(Field("ana", "Zeynep", "kit_ana", "medkit"));
            f.shelter.Add(Shelter("Rauf", "craft"));
            f.shelter.Add(Shelter("Nermin", "cook"));
            f.shelter.Add(Shelter("Suna", "train", "emp"));
            f.shelter.Add(Shelter("Selin", "rest", null, SlipPersonState.Hurt));
            f.shelter.Add(Shelter("İpek", "rest", null, SlipPersonState.Down, 2));
            f.shelter.Add(Shelter("Aylin", "rest", null, SlipPersonState.Restricted, 2));
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.Door });
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.FileReview, name = "Rauf" });
            return f;
        }

        /// <summary>
        /// En kötü gerçekçi fiş (E13; W3'ün taşma testindeki veriyle aynı): 10 canlı (3 saha + 7 sığınak), en uzun adlar (aday havuzu
        /// + sayı eki), her görev × olası durum (Core: yatan yalnız dinlenir, kısıtlı gözetmensiz yalnız dinlenir, yaralı her işe girer),
        /// eğitim + üretim + kapı, KAPI uyarısı ve üç adlı birleşik DOSYA kutusu. Punto taraması fişi bununla da ölçer.
        /// </summary>
        public static HandoverSlipFacts Worst()
        {
            var f = new HandoverSlipFacts
            {
                receiptId = "vardiya-118", shift = 119, copy = 1, alive = 10, capacity = 10,
                supplies = 59.9, suppliesCapacity = 60, suppliesAfter = 60.0,
                training = new SlipTraining { active = true, name = "Levent 31", subject = "craft", xpBefore = 1, xpAfter = 3, tierBefore = 0, tierAfter = 1 },
                production = new SlipProduction { kind = SlipProductionKind.Progress, itemType = "armor", maker = "Nermin 18", done = 0, after = 1, turns = 1 },
                openApplications = 1,
            };
            f.field.Add(Field("tek", "Levent 30", "kit_tek", "armor", "emp_cell"));
            f.field.Add(Field("emp", "Yalcin 12", "kit_emp", "armor", "medkit"));
            f.field.Add(Field("ana", "Feride 21", "kit_ana", "armor", "emp_cell"));
            f.shelter.Add(Shelter("Nermin 18", "craft", null, SlipPersonState.Hurt));
            f.shelter.Add(Shelter("Zeynep 27", "cook", null, SlipPersonState.Hurt));
            f.shelter.Add(Shelter("Levent 31", "train", "craft", SlipPersonState.Hurt));
            f.shelter.Add(Shelter("Gülizar", "rest", null, SlipPersonState.Down, 12));
            f.shelter.Add(Shelter("Mehmet 19", "rest", null, SlipPersonState.Restricted, 12));
            f.shelter.Add(Shelter("Kerem 22", "rest", null, SlipPersonState.RestrictionEnded));
            f.shelter.Add(Shelter("Orhan 25", "rest", null, SlipPersonState.Hurt));
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.Door });
            f.warnings.Add(new SlipWarning { kind = SlipWarningKind.FileReview, name = "Mehmet 19, Kerem 22, Gülizar" });
            return f;
        }

        /// <summary>Tasarımın fiş görüntüsü (yalnız V001 ve V031; öteki için null).</summary>
        public static string ReferencePng(string name, bool english)
        {
            string n = (name ?? "").Trim().ToLowerInvariant();
            if (n != "v001" && n != "v031") return null;
            return ReferenceDir + "/devir_fisi_" + n + "_" + (english ? "en" : "tr") + "@2x.png";
        }

        // ================================================================== Play: canlı fişe sunum

        static readonly FieldInfo CloseMachine = typeof(SiginakShiftClose).GetField("machine", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo CloseTray = typeof(SiginakShiftClose).GetField("tray", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo PaperFace = typeof(ShiftReceiptPaper).GetField("face", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo TrayFiled = typeof(EvrakTray).GetField("filedSlip", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Play içinde (yalnız sunum): <paramref name="name"/> fikstürünü odadaki fişin yüzüne (basılı fiş dilde ya da elde, ya da
        /// okunan arşiv fişi) ve tepsideki kopyaya yazar; <paramref name="wear"/> ise masanın geç görünüşü fişin vardiyasıyla
        /// (<c>CampaignWear.Show(shift, true)</c>; ör. V031 → kupa, kâğıt yığını). Simülasyon değişmez, komut yazılmaz; bir sonraki
        /// <c>game.Changed</c> ya da dil değişimi gerçek fişi geri getirir. Fişi görmek için önce basıp ele al (A4_R19c_* görüntüleri).
        /// </summary>
        public static string Present(string name, bool wear = true)
        {
            if (!Application.isPlaying) return "Play gerekli";
            var facts = Named(name);
            if (facts == null) return "Bilinmeyen fiş fikstürü: " + name + " (" + string.Join(", ", Names) + ")";
            var close = Object.FindFirstObjectByType<SiginakShiftClose>(FindObjectsInactive.Include);
            if (close == null) return "Sahnede SiginakShiftClose yok (W6 sahne adımı)";
            int bound = 0;
            var machine = CloseMachine?.GetValue(close) as ShiftReceiptMachine;
            var paper = machine != null ? machine.Paper : null;
            if (paper != null && PaperFace?.GetValue(paper) is HandoverSlipFace face && face != null) { face.Bind(facts); bound++; }
            var tray = CloseTray?.GetValue(close) as EvrakTray;
            if (tray != null && TrayFiled?.GetValue(tray) is HandoverSlipFace filed && filed != null && filed.gameObject.activeInHierarchy) { filed.Bind(facts); bound++; }
            if (wear)
            {
                var campaign = Object.FindFirstObjectByType<CampaignWear>(FindObjectsInactive.Include);
                if (campaign != null) campaign.Show(facts.shift, true);
            }
            string where = paper == null ? "fiş yok" : paper.State.ToString();
            return $"{name}: {bound} yüz yazıldı (fiş {where}); vardiya {facts.shift:000}, dil {Corrigible.Core.Loc.Lang}";
        }

        // ================================================================== düz çizim (960 × 1440)

        /// <summary>
        /// Fiş yüzünün düz, dik çizimi: prefab önizleme sahnesinde verilen fikstürle bağlanır, fişin önünden dik (ortografik) kamerayla
        /// <paramref name="width"/> × 1,5·<paramref name="width"/> çizilir (varsayılan 960 × 1440 = tasarım PNG'sinin @2x boyu). Işık:
        /// önizleme sahnesine eklenen tek yönlü beyaz ışık (oda ışığı değil; renk karşılaştırması için değil, yerleşim için). Referans
        /// görüntü varsa yanına konup <c>…_yan.png</c> da yazılır (sol Unity, sağ tasarım). Dil <c>Loc</c> ile geçici değişir.
        /// Işıklı uGUI yazısının önizleme sahnesinde çizimi UNVERIFIED (W3 sapma 6); yazılar görünmüyorsa kare odada alınmalı.
        /// </summary>
        public static string RenderFlat(string name, bool english, string path = null, int width = 960)
        {
            var facts = Named(name);
            if (facts == null) return "Bilinmeyen fiş fikstürü: " + name + " (" + string.Join(", ", Names) + ")";
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) return "Fiş prefab'ı yok: " + PrefabPath + " (Aşama 4'ün tek seferlik kurulumu; script silindi)";
            width = Mathf.Clamp(width, 120, 4096);
            int height = Mathf.RoundToInt(width * 1.5f);
            string tag = english ? "EN" : "TR";
            path ??= Path.Combine(OutDir, "A4_R19c_" + name.ToUpperInvariant() + "_" + tag + "_flat.png");

            string previous = Corrigible.Core.Loc.Lang;
            var preview = EditorSceneManager.NewPreviewScene();
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            var active = RenderTexture.active;
            Texture2D tex = null, reference = null, pair = null;
            try
            {
                SiginakTextScan.UseLanguage(english ? Corrigible.Core.Loc.English : Corrigible.Core.Loc.Turkish);
                var go = Object.Instantiate(prefab);
                foreach (var tr in go.GetComponentsInChildren<Transform>(true)) tr.gameObject.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(go, preview);
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (!go.activeSelf) go.SetActive(true);
                var face = go.GetComponent<HandoverSlipFace>();
                if (face == null) return "Prefab'ın kökünde HandoverSlipFace yok: " + PrefabPath;
                face.Bind(facts);
                Canvas.ForceUpdateCanvases();

                // Yüz −z'de: kamera önde (−z), +z'ye bakar; üst +y
                var camGo = new GameObject("Slip flat camera") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(camGo, preview);
                var cam = camGo.AddComponent<Camera>();
                cam.enabled = false;
                cam.scene = preview;
                cam.orthographic = true;
                cam.orthographicSize = 0.075f;
                cam.aspect = width / (float)height;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.11f, 0.10f, 1);
                camGo.transform.SetPositionAndRotation(new Vector3(0, 0, -0.5f), Quaternion.LookRotation(Vector3.forward, Vector3.up));

                var lightGo = new GameObject("Slip flat light") { hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(lightGo, preview);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1.2f;
                light.shadows = LightShadows.None;
                lightGo.transform.rotation = Quaternion.LookRotation(new Vector3(0.15f, -0.2f, 1f).normalized, Vector3.up);

                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                string result = path + (face.Overflowing ? " · TAŞMA" : "") + $" · içerik altı {face.ContentBottomMm:0.0} mm / alt blok {face.FooterTopMm:0.0} mm";

                string refPath = ReferencePng(name, english);
                if (refPath != null && File.Exists(refPath))
                {
                    reference = new Texture2D(2, 2, TextureFormat.RGB24, false);
                    if (reference.LoadImage(File.ReadAllBytes(refPath)))
                    {
                        pair = SideBySide(tex, reference);
                        string pairPath = Path.Combine(dir ?? "", Path.GetFileNameWithoutExtension(path).Replace("_flat", "") + "_yan.png");
                        File.WriteAllBytes(pairPath, pair.EncodeToPNG());
                        result += " · yan yana " + pairPath;
                    }
                }
                return result;
            }
            finally
            {
                RenderTexture.active = active;
                RenderTexture.ReleaseTemporary(rt);
                if (tex != null) Object.DestroyImmediate(tex);
                if (reference != null) Object.DestroyImmediate(reference);
                if (pair != null) Object.DestroyImmediate(pair);
                EditorSceneManager.ClosePreviewScene(preview);
                SiginakTextScan.UseLanguage(previous);
            }
        }

        /// <summary>Bütün R19c düz çizimleri (V001, V031 × TR, EN).</summary>
        public static string RenderAllFlat(string dir = OutDir)
        {
            var lines = new List<string>();
            foreach (var n in new[] { "V001", "V031" })
                foreach (bool en in new[] { false, true })
                    lines.Add(RenderFlat(n, en, Path.Combine(dir, "A4_R19c_" + n + "_" + (en ? "EN" : "TR") + "_flat.png")));
            return string.Join("\n", lines);
        }

        /// <summary>İki görüntü yan yana (sol <paramref name="left"/>, sağ <paramref name="right"/> sol yüksekliğine en yakın komşuyla ölçeklenir).</summary>
        static Texture2D SideBySide(Texture2D left, Texture2D right)
        {
            int h = left.height;
            int rw = Mathf.Max(1, Mathf.RoundToInt(right.width * (h / (float)right.height)));
            var pair = new Texture2D(left.width + rw, h, TextureFormat.RGB24, false);
            pair.SetPixels(0, 0, left.width, h, left.GetPixels());
            var scaled = new Color[rw * h];
            for (int y = 0; y < h; y++)
            {
                int sy = Mathf.Min(right.height - 1, Mathf.FloorToInt(y * right.height / (float)h));
                for (int x = 0; x < rw; x++)
                {
                    int sx = Mathf.Min(right.width - 1, Mathf.FloorToInt(x * right.width / (float)rw));
                    scaled[y * rw + x] = right.GetPixel(sx, sy);
                }
            }
            pair.SetPixels(left.width, 0, rw, h, scaled);
            pair.Apply();
            return pair;
        }
    }
}
