namespace Corrigible.Desk
{
    /// <summary>
    /// Bölge özelliği ve makine türü işaretleri (Revizyon 2 · 1.2 eki): oyundaki kimlik → sprite adı (<see cref="DeskSprites"/>
    /// kaydında, <c>DeskSprites.Apply(img, ad)</c>). Orta sütun <c>raid_tokens.json</c>'daki anahtardır (paketin önerdiği ad);
    /// test, JSON'un aynı sprite'ı verdiğini denetler. Kare çerçeveli sürüm (fişin SAHA satırı, işaretler kutusu) "_frame"
    /// ekiyle. Zorluk kademesinin getirdikleri ("kopya", "guclu") bölge özelliği değildir, işaretleri yok.
    /// </summary>
    public static class SiginakGlyphs
    {
        public readonly struct Row
        {
            public readonly string Id, TokenKey, Sprite;
            public Row(string id, string tokenKey, string sprite) { Id = id; TokenKey = tokenKey; Sprite = sprite; }
        }

        /// <summary><c>Baskin.RegionTraits</c> → <c>regionTraits.&lt;anahtar&gt;.glyph</c></summary>
        public static readonly Row[] Traits =
        {
            new Row("guvenli", "safeEntry", "glyph_safe"),
            new Row("dar", "narrowCorridor", "glyph_corridor"),
            new Row("kisa", "shortWatch", "glyph_short"),
            new Row("takviye", "reinforcement", "glyph_reinforce"),
            new Row("acik", "openGround", "glyph_open"),
            new Row("sis", "sandstorm", "glyph_sand"),
            new Row("sogutma", "coolingNet", "glyph_cooling"),
            new Row("kalabalik", "crowdedCity", "glyph_crowd"),
            new Row("zirhli", "armoredCabinet", "glyph_armored"),
            new Row("sebeke", "powerGrid", "glyph_grid"),
            new Row("fabrika", "copyFactory", "glyph_factory"),
            new Row("devriye", "fastPatrol", "glyph_patrol"),
        };

        /// <summary><c>Baskin.UnitTypes</c> (makineler) → <c>machineTypes.glyph.&lt;anahtar&gt;</c></summary>
        public static readonly Row[] Machines =
        {
            new Row("dron", "drone", "glyph_drone"),
            new Row("zdron", "armorDrone", "glyph_armordrone"),
            new Row("bakim", "maintenanceRobot", "glyph_maint"),
            new Row("kilit", "locker", "glyph_locker"),
            new Row("havan", "mortar", "glyph_mortar"),
            new Row("kalkan", "shieldDrone", "glyph_shield"),
            new Row("bomba", "bomber", "glyph_bomber"),
            new Row("cogalt", "replicator", "glyph_replicator"),
        };

        /// <summary>Bölge özelliğinin işareti; bilinmeyen kimlikte null (yerinde yalnız yazı durur).</summary>
        public static string Trait(string id, bool framed = false) => Find(Traits, id, framed);

        /// <summary>Makine türünün işareti; bilinmeyen kimlikte null.</summary>
        public static string Machine(string id, bool framed = false) => Find(Machines, id, framed);

        static string Find(Row[] rows, string id, bool framed)
        {
            foreach (var r in rows) if (r.Id == id) return framed ? r.Sprite + "_frame" : r.Sprite;
            return null;
        }
    }
}
