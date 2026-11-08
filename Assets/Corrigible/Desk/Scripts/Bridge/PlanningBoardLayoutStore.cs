using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Corrigible.Core;
using Corrigible.Game;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Yerleşim dosyasının bir girdisi. Sürüm 2'de iş emri fişleri: <see cref="Key"/> <c>"production:&lt;tür&gt;:&lt;k&gt;"</c>,
    /// <see cref="Position"/> pano mm (sol üstten, y aşağı), <see cref="Angle"/> derece. Sürüm 3'te (Aşama 5) ayrıca formdaki tarif
    /// kartları: <c>"recipe:&lt;iş emri kimliği&gt;"</c>, aynı birimlerle. Sürüm 1'in <c>"production:&lt;tür&gt;"</c> girdileri tuval
    /// birimindeydi; okunurken atılır.
    /// </summary>
    [Serializable]
    public sealed class PlanningBoardPlacement
    {
        public string Key, OwnerId;
        public Vector2 Position;
        public float Angle;
    }

    /// <summary>
    /// Yalnız sunum yerleşimi (<c>planning_board_layout.json</c>); komut geçmişini, rastgeleliği ve görev kararlarını değiştirmez.
    /// Sürüm 2 (Revizyon 3 · Aşama 3): fiş yerleri pano mm ve açıyla, kimse eğitimde değilken halkalı konu
    /// (<see cref="TrainingTopic"/>). Sürüm 3 (Aşama 5 · R18, PLAN §1.15): defterin son açılımı (<see cref="BookSpread"/>), tarif
    /// kartlarının yerleri (<see cref="Entries"/>'te <c>recipe:</c> girdileri) ve yeni ürünün durumu (<see cref="NewItem"/>). Sürüm
    /// alanı olmayan dosya sürüm 1'dir; sürüm 2 dosyası olduğu gibi okunur (yeni alanlar varsayılanla).
    /// </summary>
    [Serializable]
    public sealed class PlanningBoardLayout
    {
        /// <summary>Dosya biçimi; 0 ya da 1 = eski (tuval birimli) dosya. <see cref="PlanningBoardLayoutStore.Save"/> güncel sürümü yazar.</summary>
        public int Version;
        public uint Seed;
        /// <summary>Kimse EĞİTİM cebinde değilken halkalı konu (Core konu kimliği); boşsa "tek".</summary>
        public string TrainingTopic;
        public List<PlanningBoardPlacement> Entries = new List<PlanningBoardPlacement>();
        /// <summary>Sürüm 3: defterin son açılımı (0–2).</summary>
        public int BookSpread;
        /// <summary>Sürüm 3: yeni ürünün sunum durumu; <c>Id</c> boşsa yok (JsonUtility boş sınıfı da yazar).</summary>
        public NewItemState NewItem;
    }

    /// <summary>
    /// <c>planning_board_layout.json</c> deposu: tohum denetimi, atomik yazma, sürüm 1 → 3 ve 2 → 3 geçişi ve
    /// <see cref="BoardUiState"/> ile karşılıklı çeviri. Dosya oyun kaydına girmez; okumak ve yazmak RNG çekmez.
    /// </summary>
    public static class PlanningBoardLayoutStore
    {
        public const string FileName = "planning_board_layout.json";
        /// <summary>Güncel dosya biçimi (pano mm, fiş açısı, eğitim konusu; sürüm 3: defter açılımı, tarif kartı yerleri, yeni ürün).</summary>
        public const int CurrentVersion = 3;
        /// <summary>Defterin açılım sayısı (altı tarif, açılım başına iki; <c>cards.recipe.spreads</c>).</summary>
        public const int BookSpreads = 3;
        const string ProductionPrefix = "production:", RecipePrefix = "recipe:";
        public static string FilePath => Path.Combine(SaveStore.StorageDirectory, FileName);

        /// <summary>
        /// Bu harekâtın yerleşimini okur; dosya yoksa, tohum tutmazsa ya da okunamazsa null. Sürüm 1 dosyasında tohum denetimi
        /// korunur, tuval birimli <c>production:*</c> girdileri ve konu atılır. Sürüm 2 dosyasının fiş yerleri ve konusu korunur
        /// (yalnız sürüm alanı yükselir; yeni alanlar varsayılanla gelir). Sonuç bellekte güncel sürümdedir.
        /// </summary>
        public static PlanningBoardLayout Load(GameRecord record)
        {
            if (record == null) return null;
            try
            {
                if (!File.Exists(FilePath)) return null;
                var layout = JsonUtility.FromJson<PlanningBoardLayout>(File.ReadAllText(FilePath));
                if (layout == null || layout.Seed != record.seed || layout.Entries == null) return null;
                layout.Entries.RemoveAll(e => e == null);
                // Sürüm 1 temizliği yalnız sürüm 1'e (eleştiri 1: "< CurrentVersion" sürüm 2'nin fiş yerlerini de silerdi)
                if (layout.Version < 2)
                {
                    layout.Entries.RemoveAll(e => e.Key != null && e.Key.StartsWith(ProductionPrefix, StringComparison.Ordinal));
                    layout.TrainingTopic = null;
                }
                if (layout.Version < CurrentVersion)
                {
                    layout.Entries.RemoveAll(e => e.Key != null && e.Key.StartsWith(RecipePrefix, StringComparison.Ordinal));
                    layout.BookSpread = 0;
                    layout.NewItem = null;
                    layout.Version = CurrentVersion;
                }
                return layout;
            }
            catch (Exception e) { Debug.LogWarning("Pano yerleşimi okunamadı: " + e.Message); return null; }
        }

        /// <summary>Yerleşimi bu harekâtın tohumuyla ve güncel sürümle yazar (önce <c>.writing</c>, sonra yer değiştirme).</summary>
        public static void Save(GameRecord record, PlanningBoardLayout layout)
        {
            if (record == null || layout == null) return;
            layout.Seed = record.seed;
            layout.Version = CurrentVersion;
            try
            {
                Directory.CreateDirectory(SaveStore.StorageDirectory);
                string pending = FilePath + ".writing";
                File.WriteAllText(pending, JsonUtility.ToJson(layout));
                if (File.Exists(FilePath)) File.Replace(pending, FilePath, null);
                else File.Move(pending, FilePath);
            }
            catch (Exception e) { Debug.LogWarning("Pano yerleşimi yazılamadı: " + e.Message); }
        }

        /// <summary>
        /// Okunan yerleşimi sunum durumuna aktarır: fiş yerleri (<c>"production:&lt;tür&gt;:&lt;k&gt;"</c> → mm.x, mm.y, açı), eğitim
        /// konusu (geçersizse "tek"), defter açılımı (0–2'ye kırpılır), tarif kartı yerleri (<c>"recipe:&lt;iş emri&gt;"</c>) ve yeni ürün
        /// (<c>Id</c> boşsa yok). Seçim sıfırlanır. <paramref name="layout"/> null ise yalnız sıfırlar.
        /// </summary>
        public static void Apply(PlanningBoardLayout layout, BoardUiState ui)
        {
            if (ui == null) return;
            ui.Reset();
            if (layout == null) return;
            if (IsSubject(layout.TrainingTopic)) ui.TrainingTopic = layout.TrainingTopic;
            ui.BookSpread = Math.Max(0, Math.Min(BookSpreads - 1, layout.BookSpread));
            var item = layout.NewItem;
            if (item != null && !string.IsNullOrEmpty(item.Id))
                ui.NewItem = new NewItemState { Day = item.Day, Id = item.Id, Slid = item.Slid, Touched = item.Touched };
            if (layout.Entries == null) return;
            foreach (var e in layout.Entries)
            {
                if (e == null) continue;
                if (TryParseChipKey(e.Key, out var kind, out var index))
                    ui.ChipPoses[BoardUiState.ChipPoseKey(kind, index)] = new Vector3(e.Position.x, e.Position.y, e.Angle);
                else if (TryParseRecipeKey(e.Key, out var orderId))
                    ui.RecipePoses[orderId] = new Vector3(e.Position.x, e.Position.y, e.Angle);
            }
        }

        /// <summary>
        /// Sunum durumunu yerleşime yazar: girdiler fiş yerlerinden, sonra tarif kartı yerlerinden yeniden kurulur (ikisi de anahtar
        /// sırasıyla; sürüm 1'den kalan okunmayan girdiler düşer); eğitim konusu, defter açılımı ve yeni ürün (yoksa boş) saklanır.
        /// Dosyaya yazmaz (<see cref="Save"/>).
        /// </summary>
        public static void Capture(BoardUiState ui, PlanningBoardLayout layout)
        {
            if (ui == null || layout == null) return;
            if (layout.Entries == null) layout.Entries = new List<PlanningBoardPlacement>();
            layout.Entries.Clear();
            layout.Version = CurrentVersion;
            layout.TrainingTopic = IsSubject(ui.TrainingTopic) ? ui.TrainingTopic : null;
            layout.BookSpread = Math.Max(0, Math.Min(BookSpreads - 1, ui.BookSpread));
            var item = ui.NewItem;
            layout.NewItem = item != null && !string.IsNullOrEmpty(item.Id)
                ? new NewItemState { Day = item.Day, Id = item.Id, Slid = item.Slid, Touched = item.Touched } : null;
            if (ui.ChipPoses != null)
            {
                var keys = new List<string>(ui.ChipPoses.Keys);
                keys.Sort(StringComparer.Ordinal);
                foreach (var key in keys)
                {
                    if (!TryParseChipKey(key, out _, out _)) continue;
                    var pose = ui.ChipPoses[key];
                    layout.Entries.Add(new PlanningBoardPlacement { Key = key, Position = new Vector2(pose.x, pose.y), Angle = pose.z });
                }
            }
            var orders = new List<string>(ui.RecipePoses.Keys);
            orders.Sort(StringComparer.Ordinal);
            foreach (var orderId in orders)
            {
                if (string.IsNullOrEmpty(orderId)) continue;
                var pose = ui.RecipePoses[orderId];
                layout.Entries.Add(new PlanningBoardPlacement { Key = RecipeKey(orderId), Position = new Vector2(pose.x, pose.y), Angle = pose.z });
            }
        }

        /// <summary>Sürüm 3 tarif kartı anahtarı: <c>"recipe:is-3"</c>.</summary>
        public static string RecipeKey(string orderId) => RecipePrefix + orderId;

        /// <summary>Sürüm 3 tarif kartı anahtarı <c>"recipe:&lt;iş emri&gt;"</c> mı (kimlik boş değil).</summary>
        public static bool TryParseRecipeKey(string key, out string orderId)
        {
            orderId = null;
            if (string.IsNullOrEmpty(key) || !key.StartsWith(RecipePrefix, StringComparison.Ordinal) || key.Length <= RecipePrefix.Length) return false;
            orderId = key.Substring(RecipePrefix.Length);
            return true;
        }

        /// <summary>Sürüm 2 fiş anahtarı <c>"production:&lt;tür&gt;:&lt;k&gt;"</c> mı (tür bilinen hammadde, k ≥ 0).</summary>
        public static bool TryParseChipKey(string key, out string kind, out int index)
        {
            kind = null; index = -1;
            if (string.IsNullOrEmpty(key) || !key.StartsWith(ProductionPrefix, StringComparison.Ordinal)) return false;
            int colon = key.LastIndexOf(':');
            if (colon <= ProductionPrefix.Length) return false;
            string k = key.Substring(ProductionPrefix.Length, colon - ProductionPrefix.Length);
            if (Array.IndexOf(PlanningBoardData.MaterialKinds, k) < 0) return false;
            if (!int.TryParse(key.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var i)) return false;
            kind = k; index = i;
            return true;
        }

        static bool IsSubject(string subject) => subject != null && Array.IndexOf(PlanningBoardData.Subjects, subject) >= 0;
    }
}
