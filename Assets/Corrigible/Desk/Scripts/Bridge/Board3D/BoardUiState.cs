using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Corrigible.Desk.Bridge
{
    /// <summary>
    /// Kartlı panonun sunuma ait durumu (Revizyon 3 · R15): seçili kart, kimse eğitimde değilken kalemle seçilen konu, iş emri
    /// fişlerinin serbest yerleri ve (Aşama 5) defterin açılımı, formdaki tarif kartlarının yerleri ve yeni ürünün durumu. Kayda (<c>GameRecord</c>) hiç girmez; sunucu <c>planning_board_layout.json</c>'a yazar. Simülasyonu
    /// değiştirmez, RNG çekmez.
    /// </summary>
    public sealed class BoardUiState
    {
        /// <summary>Seçili kartın kimliği (kişi <c>sakin-N</c>/<c>resident-N</c> ya da eşya <c>esya-N</c>); seçim yoksa null.</summary>
        public string SelectedId;

        /// <summary>Kimse EĞİTİM cebinde değilken halkalı konu (Core konu kimliği: tek emp ana craft cook care gather).</summary>
        public string TrainingTopic = "tek";

        /// <summary>
        /// İş emri fişlerinin serbest yerleri: anahtar <c>"production:&lt;tür&gt;:&lt;k&gt;"</c>, değer (pano mm x, pano mm y, açı derece).
        /// k, o türün fiş sırasıdır (<c>chip:&lt;tür&gt;:&lt;k&gt;</c> ile aynı).
        /// </summary>
        public Dictionary<string, Vector3> ChipPoses = new();

        // ------------------------------------------------------------------ Aşama 5 (PLAN §1.15) · W0 sözleşmesi, W4 doldurur

        /// <summary>Defterin son açılımı (0–2); defter açılınca buradan başlar.</summary>
        public int BookSpread;

        /// <summary>[B] Formdaki tarif kartlarının serbest yerleri: iş emri kimliği (is-N) → (pano mm x, pano mm y, açı derece).</summary>
        public readonly Dictionary<string, Vector3> RecipePoses = new Dictionary<string, Vector3>();

        /// <summary>Yeni ürünün sunum durumu (kayış oynadı mı, karta dokunuldu mu); yoksa null.</summary>
        public NewItemState NewItem;

        const string ChipPrefix = "chip:", PosePrefix = "production:";

        /// <summary>Fiş kimliği: <c>"chip:metal:0"</c>.</summary>
        public static string ChipId(string kind, int index) => ChipPrefix + kind + ":" + index.ToString(CultureInfo.InvariantCulture);

        /// <summary>Fişin yerleşim anahtarı: <c>"production:metal:0"</c>.</summary>
        public static string ChipPoseKey(string kind, int index) => PosePrefix + kind + ":" + index.ToString(CultureInfo.InvariantCulture);

        /// <summary><c>"chip:&lt;tür&gt;:&lt;k&gt;"</c> kimliğini çözer; biçim tutmazsa false.</summary>
        public static bool TryParseChipId(string chipId, out string kind, out int index)
        {
            kind = null; index = -1;
            if (string.IsNullOrEmpty(chipId) || !chipId.StartsWith(ChipPrefix, System.StringComparison.Ordinal)) return false;
            int colon = chipId.LastIndexOf(':');
            if (colon <= ChipPrefix.Length) return false;
            if (!int.TryParse(chipId.Substring(colon + 1), NumberStyles.None, CultureInfo.InvariantCulture, out index)) { index = -1; return false; }
            kind = chipId.Substring(ChipPrefix.Length, colon - ChipPrefix.Length);
            return kind.Length > 0;
        }

        /// <summary>Fişin kayıtlı yeri; yoksa null (görünüm kendiliğinden yeri seçer).</summary>
        public Vector3? ChipPose(string chipId)
        {
            if (ChipPoses == null || !TryParseChipId(chipId, out var kind, out var index)) return null;
            return ChipPoses.TryGetValue(ChipPoseKey(kind, index), out var pose) ? pose : (Vector3?)null;
        }

        /// <summary>Fişin bırakıldığı yeri saklar (pano mm ve açı).</summary>
        public void SetChipPose(string chipId, Vector3 pose)
        {
            if (!TryParseChipId(chipId, out var kind, out var index)) return;
            if (ChipPoses == null) ChipPoses = new Dictionary<string, Vector3>();
            ChipPoses[ChipPoseKey(kind, index)] = pose;
        }

        /// <summary>
        /// Bir fiş depoya geri alındı (<c>board_stage n − 1</c> başarılı): o fişin yeri silinir, aynı türün sonraki fişleri birer sıra
        /// kayar; böylece kalan fişler yerinde durur.
        /// </summary>
        public void RemoveChip(string kind, int index, int countBefore)
        {
            if (ChipPoses == null || string.IsNullOrEmpty(kind) || index < 0) return;
            ChipPoses.Remove(ChipPoseKey(kind, index));
            for (int k = index + 1; k < countBefore; k++)
            {
                string from = ChipPoseKey(kind, k);
                if (!ChipPoses.TryGetValue(from, out var pose)) continue;
                ChipPoses.Remove(from);
                ChipPoses[ChipPoseKey(kind, k - 1)] = pose;
            }
        }

        /// <summary>[B] Formdaki tarif kartının kayıtlı yeri (pano mm x, y, açı); yoksa null (kart TARİF kutusunun çapasına oturur).</summary>
        public Vector3? RecipePose(string orderId) =>
            !string.IsNullOrEmpty(orderId) && RecipePoses.TryGetValue(orderId, out var pose) ? pose : (Vector3?)null;

        /// <summary>[B] Tarif kartının bırakıldığı yeri saklar (iş emri kimliği → pano mm ve açı).</summary>
        public void SetRecipePose(string orderId, Vector3 pose)
        {
            if (!string.IsNullOrEmpty(orderId)) RecipePoses[orderId] = pose;
        }

        /// <summary>[B] Emir iptal edildi ya da tamamlandı: kartın yeri silinir. Silindiyse true.</summary>
        public bool RemoveRecipePose(string orderId) => !string.IsNullOrEmpty(orderId) && RecipePoses.Remove(orderId);

        /// <summary>Yeni oyun ya da başka kayıt: seçim, konu, fiş yerleri ve Aşama 5 alanları (açılım, tarif yerleri, yeni ürün) sıfırlanır.</summary>
        public void Reset()
        {
            SelectedId = null;
            TrainingTopic = "tek";
            if (ChipPoses == null) ChipPoses = new Dictionary<string, Vector3>();
            else ChipPoses.Clear();
            BookSpread = 0;
            RecipePoses.Clear();
            NewItem = null;
        }
    }

    /// <summary>
    /// Yeni ürünün sunum durumu (Aşama 5 · R18d; PLAN §1.11, §1.15): hangi gün, hangi eşya (esya-N; gün başına bir kez sabitlenir),
    /// kayış oynadı mı, karta dokunuldu mu (YENİ etiketi düştü). Yerleşim dosyasında (v3 <c>newItem</c>) saklanır; kayda girmez.
    /// </summary>
    [System.Serializable]
    public sealed class NewItemState
    {
        public int Day;
        public string Id;
        public bool Slid, Touched;
    }
}
