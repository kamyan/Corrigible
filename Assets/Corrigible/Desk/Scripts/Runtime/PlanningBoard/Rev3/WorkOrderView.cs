using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Corrigible.Desk
{
    /// <summary>
    /// ÜRETİM · İŞ EMRİ (R15a, R15e, R18c; PROMPT §5): boş basılı formun dinamik yazıları (numara, durum satırı, Aşama 5'te DURDU
    /// damgası), TARİF kutusundaki tarif kartı (<see cref="LayoutRecipe"/>) ve hazırlanan hammadde fişleri. Fiş kayıtlı pozunda (pano mm + serbest açı, sunum dosyasında) ya da kendiliğinden yerinde
    /// (<see cref="BoardLayout.ChipSpot"/>) durur; merkezi girdi alanına (<see cref="BoardLayout.Inputs"/>) kırpılır, iç
    /// konumlar oturtulmaz (eleştiri #14). Yeni fiş stok kartından 0,1 s'de çıkar. Sabit form yazıları ("İŞ EMRİ · ÜRETİM",
    /// "TARİF", "KAPANIŞTA: …") sahnede DeskLocText ile durur.
    /// </summary>
    public sealed class WorkOrderView : MonoBehaviour
    {
        [Tooltip("Formun numara satırı (\"No 001\"; sunucu biçimler).")]
        [SerializeField] TMP_Text numberText;
        [Tooltip("Formun tarif / durum satırı (Kalam, mavi; boş olabilir).")]
        [SerializeField] TMP_Text recipeText;
        [Tooltip("Aşama 5: DURDU damgası (CP 700, kırmızı, −6°; boşsa damga yok).")]
        [SerializeField] TMP_Text stampText;
        [Tooltip("Aşama 10 (D-F, R18c): girdi alanının üstündeki ipucu; yalnız hammadde bekleyen taslakta yazılı.")]
        [SerializeField] TMP_Text dropHintText;
        [Tooltip("Aşama 5: TARİF kutusunun çapası (Order_Recipe); tarif kartı buraya oturur.")]
        [SerializeField] Transform recipeHome;

        /// <summary>Bu görünümün daha önce dizdiği fiş kartları (yeniden numaralanan fiş yeni sayılmaz).</summary>
        readonly HashSet<BoardCardView> laidOut = new HashSet<BoardCardView>();

        /// <summary>
        /// Fişleri dizer. Listede null öğe (sürükleme denetleyicisinin tuttuğu fiş) atlanır ama sırası korunur, böylece
        /// öteki fişlerin kendiliğinden yeri kaymaz. <paramref name="savedChipPose"/>: fiş kimliği → (mm.x, mm.y, açı) ya da null.
        /// </summary>
        public void Layout(IReadOnlyList<BoardCardView> chips, Func<string, Vector3?> savedChipPose, bool animate)
        {
            if (chips == null) return;
            float settle = BoardViewUtil.Num("motion.board.itemTuckS", 0.15f);
            float chipOut = BoardViewUtil.Num("motion.board.chipOutS", 0.1f);
            for (int i = 0; i < chips.Count; i++)
            {
                var c = chips[i];
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                string id = c.Id;
                Vector2 mm = BoardLayout.ChipSpot(i, out float angle, out float level);
                Vector3? saved = savedChipPose != null ? savedChipPose(id) : null;
                if (saved.HasValue) { mm = new Vector2(saved.Value.x, saved.Value.y); angle = saved.Value.z; }
                mm = ClampToInputs(mm);
                var target = BoardLayout.MmToLocal(mm, BoardLayout.CardZ(level));

                // Yeni fiş = havuzdan yeni çıkan (gizliydi) ya da hiç dizilmemiş kart. Kimliği değişen görünür kart (tür içi yeniden
                // numaralama, BoardView3D) yerinden süzülür, stok kartından yeniden çıkmaz.
                bool isNew = !c.gameObject.activeSelf || !laidOut.Contains(c);
                laidOut.Add(c);
                c.SetMagnetVisible(false);
                if (isNew && animate && TrySource(id, level, out var from))
                {
                    c.Place(from, 0);
                    c.SetVisible(true);
                    c.MoveTo(target, angle, chipOut, CardEase.EaseOut);
                    continue;
                }
                c.SetVisible(true);
                BoardViewUtil.Pose(c, target, angle, animate && !isNew, settle, CardEase.EaseOut);
            }
        }

        /// <summary>Fişin merkezini girdi alanına kırpar (kenardaki fiş içeri itilir, içtekine dokunulmaz).</summary>
        public static Vector2 ClampToInputs(Vector2 mm)
        {
            var r = BoardLayout.Inputs;
            return new Vector2(Mathf.Clamp(mm.x, r.xMin, r.xMax), Mathf.Clamp(mm.y, r.yMin, r.yMax));
        }

        /// <summary>Fişin çıktığı stok kartının merkezi ("chip:&lt;tür&gt;:&lt;i&gt;" kimliğinden).</summary>
        static bool TrySource(string id, float level, out Vector3 local)
        {
            local = default;
            if (!BoardViewUtil.TryParseChip(id, out var kind, out _)) return false;
            int k = Array.IndexOf(BoardLayout.MaterialKinds, kind);
            if (k < 0) return false;
            local = BoardLayout.MmToLocal(BoardLayout.Material(k), BoardLayout.CardZ(level + 1f));
            return true;
        }

        /// <summary>Formun numarası ("No 001") ve tarif / durum satırı; sunucu yerelleştirip biçimler.</summary>
        public void SetSheet(string number, string recipeLine)
        {
            BoardViewUtil.SetText(numberText, number);
            BoardViewUtil.SetText(recipeText, recipeLine);
        }

        // ------------------------------------------------------------------ Aşama 5 (R18c; PLAN §1.7)

        /// <summary>Tarif kartının seviyesi (<see cref="BoardLayout.RecipeLevel"/>; sürükleme denetleyicisiyle tek kaynak).</summary>
        public const float RecipeLevel = BoardLayout.RecipeLevel;

        /// <summary>Bu görünümün daha önce yerleştirdiği tarif kartları (gizliden gelen kart anında konur, süzülmez).</summary>
        readonly HashSet<BoardCardView> recipeLaid = new HashSet<BoardCardView>();

        /// <summary>DURDU damgası (sahnede bağlıysa).</summary>
        public TMP_Text StampText => stampText;
        public TMP_Text NumberText => numberText;
        public TMP_Text StatusText => recipeText;
        /// <summary>Girdi alanı ipucu (sahnede bağlıysa).</summary>
        public TMP_Text DropHintText => dropHintText;
        /// <summary>Damganın kenar çerçevesi (isteğe bağlı; damgayla birlikte görünür).</summary>
        public GameObject StampFrame => stampFrame;

        [Tooltip("Aşama 5: DURDU damgasının kenar çerçevesi (isteğe bağlı nesne; damga yazısıyla birlikte açılır).")]
        [SerializeField] GameObject stampFrame;

        /// <summary>
        /// Formun bütün dinamik yazıları: numara, durum satırı (mavi kalem; sahnedeki renk) ve damga. Damga yalnız sözcük doluyken
        /// görünür ("DURDU" / "STOPPED"; kurucu yalnız <see cref="OrderState.Stopped"/>'ta verir), rengi temanın <c>stamp.stopped</c>'ı
        /// (#9A1E14): durum renk + sözcük + damga ile okunur, yalnız renkle değil.
        /// </summary>
        public void SetSheet(OrderData d)
        {
            if (d == null) return;
            SetSheet(d.Number, d.Status);
            BoardViewUtil.SetText(dropHintText, d.DropHint);
            bool stamped = !string.IsNullOrEmpty(d.Stamp);
            var ink = BoardViewUtil.Col("stamp.stopped", new Color32(0x9A, 0x1E, 0x14, 0xFF));
            if (stampText != null)
            {
                BoardViewUtil.SetText(stampText, stamped ? d.Stamp : "");
                if (stamped && stampText.color != ink) stampText.color = ink;
            }
            if (stampFrame != null)
            {
                if (stampFrame.activeSelf != stamped) stampFrame.SetActive(stamped);
                // çerçeve damgayla aynı mürekkep (temadan; sahnede renk tutulmaz)
                if (stamped && stampFrame.TryGetComponent<UnityEngine.UI.Graphic>(out var g) && g.color != ink) g.color = ink;
            }
        }

        /// <summary>Damga şu anda görünüyor mu (sözcük dolu).</summary>
        public bool Stamped => stampText != null && !string.IsNullOrEmpty(stampText.text);

        /// <summary>
        /// Tarif kartını TARİF kutusuna yerleştirir: kayıtlı poz (pano mm.x, mm.y, açı; [B] oyuncunun bıraktığı serbest yer) ya da
        /// kutunun çapası (<see cref="recipeHome"/>, yoksa <see cref="BoardLayout.Recipe"/> (514, 328)). Kartın merkezi iş emri
        /// bölgesine (<see cref="BoardLayout.Order"/>, kartın yarısı kadar içeride) kırpılır. Mıknatıs panoda görünür. Gizliden gelen
        /// kart anında konur; görünen kart <c>motion.board.itemTuckS</c> ile süzülür.
        /// </summary>
        public void LayoutRecipe(BoardCardView card, Vector3? savedPose, bool animate)
        {
            if (card == null) return;
            Vector2 mm;
            float angle = 0f;
            if (savedPose.HasValue) { mm = new Vector2(savedPose.Value.x, savedPose.Value.y); angle = savedPose.Value.z; }
            else mm = RecipeHomeMm(card.transform.parent);
            mm = ClampToOrder(mm);
            var target = BoardLayout.MmToLocal(mm, BoardLayout.CardZ(RecipeLevel));
            bool isNew = !card.gameObject.activeSelf || !recipeLaid.Contains(card);
            recipeLaid.Add(card);
            card.SetMagnetVisible(true);
            card.SetVisible(true);
            BoardViewUtil.Pose(card, target, angle, animate && !isNew, BoardViewUtil.Num("motion.board.itemTuckS", 0.15f), CardEase.EaseOut);
        }

        /// <summary>
        /// Tarif kartının merkezini iş emri bölgesine kırpar (kart bölgeden taşmaz; içteki poza dokunulmaz). Kural
        /// <see cref="BoardLayout.ClampRecipe"/>'tedir: sürükleme denetleyicisinin bırakma pozu ile aynı.
        /// </summary>
        public static Vector2 ClampToOrder(Vector2 mm) => BoardLayout.ClampRecipe(mm);

        /// <summary>
        /// TARİF kutusunun pano mm'si: çapa bağlıysa onun dünya konumu <paramref name="space"/>'in (kartların ebeveyni; model yereli)
        /// yereline çevrilir; değilse token (514, 328).
        /// </summary>
        public Vector2 RecipeHomeMm(Transform space)
        {
            if (recipeHome == null) return BoardLayout.Recipe;
            var frame = space != null ? space : transform.parent;
            var local = frame != null ? frame.InverseTransformPoint(recipeHome.position) : recipeHome.position;
            return BoardLayout.LocalToMm(local);
        }

        /// <summary>TARİF kutusu çapasının model yereli konumu (kart seviyesinin z'siyle; çapa yoksa tokenın (514, 328) mm'si).</summary>
        public Vector3 RecipeHomeLocal => BoardLayout.MmToLocal(RecipeHomeMm(null), BoardLayout.CardZ(RecipeLevel));
    }
}
