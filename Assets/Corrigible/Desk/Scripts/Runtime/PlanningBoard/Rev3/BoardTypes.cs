using System;

namespace Corrigible.Desk
{
    /// <summary>Revizyon 3 kartlı panonun kart türleri (R16: kişi 80 × 120, eşya 80 × 50, hammadde 80 × 56, fiş 56 × 40, tarif 112 × 80 mm).</summary>
    public enum BoardCardKind { Person, Item, Material, Chip, Recipe }

    /// <summary>Eşya kartının yuvası; kimlik bandının deseni (kit düz, zırh taralı, yardımcı noktalı) bundan gelir.</summary>
    public enum ItemSlotKind { Kit, Armor, Aid }

    /// <summary>Kişi kartı şeridinin tonu (renk tek başına durum taşımaz; metin de yazar).</summary>
    public enum StripTone { Ink, Blue, Red, Mute }

    /// <summary>
    /// Panonun ses olayları; köprü <c>Sfx</c>'e çevirir (sunum Game assembly'sini görmez). Aşama 5 (R18, PLAN §1.13) sona ekler:
    /// defterin açılışı, sayfa çevirme, kapanış, tarif kartının defterden kalkışı ("şırp"), ürünün depoya kayışı ("şık").
    /// </summary>
    public enum BoardSound
    {
        Lift, Settle, Return, Deny, DeckSlide, DeckCollect, Tuck, Hurda, Padlock, Flap,
        BookOpen, PageTurn, BookClose, RecipeLift, ProductSlide,
    }

    /// <summary>
    /// Bırakma hedefinin türü. <see cref="BoardTargetKind.Book"/> (Aşama 5 · [B]): tarif kartının "deftere dön" hedefi; defterden
    /// alınan kopya için komutsuz dönüş, formdaki kart için iş emrinin iptali.
    /// </summary>
    public enum BoardTargetKind { None, Pocket, Bosta, Person, Store, Hurda, Order, Folder, Topic, Back, Deck, Empty, Book }

    /// <summary>
    /// Bırakma ya da tıklama hedefi. <see cref="Job"/>: cep için Core görevi (tek, emp, ana, craft, cook, train; Boşta "rest");
    /// <see cref="Id"/>: kişi kimliği (kişi kartı hedefi) ya da hammadde türü (stok kartı); <see cref="Row"/>: eğitim konusu satırı.
    /// </summary>
    public readonly struct BoardTarget : IEquatable<BoardTarget>
    {
        public readonly BoardTargetKind Kind;
        public readonly string Job;
        public readonly string Id;
        public readonly int Row;

        public BoardTarget(BoardTargetKind kind, string job = null, string id = null, int row = -1)
        {
            Kind = kind; Job = job; Id = id; Row = row;
        }

        public static readonly BoardTarget None = new BoardTarget(BoardTargetKind.None);
        public static BoardTarget Pocket(string job) => new BoardTarget(BoardTargetKind.Pocket, job);
        public static BoardTarget Bosta() => new BoardTarget(BoardTargetKind.Bosta, "rest");
        public static BoardTarget Person(string id) => new BoardTarget(BoardTargetKind.Person, null, id);
        public static BoardTarget Store() => new BoardTarget(BoardTargetKind.Store);
        public static BoardTarget Hurda() => new BoardTarget(BoardTargetKind.Hurda);
        public static BoardTarget Order() => new BoardTarget(BoardTargetKind.Order);
        public static BoardTarget Topic(int row) => new BoardTarget(BoardTargetKind.Topic, null, null, row);
        /// <summary>Tarif kartının deftere dönüşü (Aşama 5 · [B]).</summary>
        public static BoardTarget Book() => new BoardTarget(BoardTargetKind.Book);

        public bool IsNone => Kind == BoardTargetKind.None;

        public bool Equals(BoardTarget o) => Kind == o.Kind && Job == o.Job && Id == o.Id && Row == o.Row;
        public override bool Equals(object obj) => obj is BoardTarget t && Equals(t);
        public override int GetHashCode() => ((int)Kind * 397) ^ (Job?.GetHashCode() ?? 0) ^ (Id?.GetHashCode() ?? 0) ^ Row;
        public override string ToString() => Kind + (Job != null ? ":" + Job : "") + (Id != null ? "#" + Id : "") + (Row >= 0 ? "@" + Row : "");
    }

    /// <summary>Bırakma önizlemesi (üzerine gelme etiketi): başlık, gövde, uyarı (amber çerçeve).</summary>
    public readonly struct DropPreview
    {
        public readonly string Title, Body;
        public readonly bool Warning, Valid;

        public DropPreview(string title, string body, bool warning, bool valid)
        {
            Title = title; Body = body; Warning = warning; Valid = valid;
        }

        public static readonly DropPreview Invalid = new DropPreview(null, null, false, false);
    }
}
