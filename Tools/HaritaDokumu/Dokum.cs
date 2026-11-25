using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Corrigible.Core;

// Üretilmiş harita dökümü (aşama C0; Tools/HaritaDokumu/README.md). Gerçek Corrigible.Core ile, Unity olmadan Mono'da koşar.
// Kullanım: dokum.exe <tohumBaş> <tohumSon> <çıktıKlasörü> → seed_XX.svg (+ sheet.html); PNG'ye çeviri dokum.sh'te.
// Çizim: hücreler bölge rengiyle (deniz açık), kıyı koyu, bölge sınırı ince, kablo bağlantıları kesik, karargâh yıldızı,
// her bölgenin en iç hücresinde ad ve paket kimliği, sağda bağlantı listesi.
public static class Program
{
    static readonly string[] Palette =
    {
        "#e6b8a2", "#c9d6a3", "#a8c5da", "#e8d48b", "#c7b3d9", "#9fd1c2",
        "#f0c38e", "#b9c9e8", "#d9a9b9", "#bcd99b", "#d8c4a4", "#a9d3e0",
    };

    static string F(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);
    static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    public static void Main(string[] args)
    {
        uint s0 = uint.Parse(args[0]), s1 = uint.Parse(args[1]);
        string dir = args[2];
        Directory.CreateDirectory(dir);
        for (uint seed = s0; seed <= s1; seed++)
        {
            var m = WorldMap.Generate(seed);
            File.WriteAllText(Path.Combine(dir, "seed_" + seed.ToString("00", CultureInfo.InvariantCulture) + ".svg"), Svg(m));
            Console.WriteLine("tohum " + seed + " deneme " + m.Attempt + " kıta " + m.Continents + " kara " + m.Cells.Count(c => c.Land)
                + " bağlantı " + m.Links.Count + " kablo " + m.Links.Count(l => l.Cable) + " " + m.Fingerprint);
        }
    }

    static string Svg(WorldMap m)
    {
        const int side = 420;
        var sb = new StringBuilder();
        sb.Append("<svg xmlns='http://www.w3.org/2000/svg' width='").Append(1920 + side).Append("' height='1080' viewBox='0 0 ")
            .Append(1920 + side).Append(" 1080'>");
        sb.Append("<rect width='100%' height='100%' fill='#f4efe4'/><rect width='1920' height='1080' fill='#cfdde3'/>");
        var colors = m.Regions.Select((r, i) => (r.Id, Palette[i])).ToDictionary(x => x.Id, x => x.Item2);
        foreach (var c in m.Cells)
        {
            if (!c.Land) continue;
            sb.Append("<polygon points='");
            for (int k = 0; k < 6; k++) sb.Append(F(c.X[k])).Append(',').Append(F(c.Y[k])).Append(' ');
            sb.Append("' fill='").Append(colors[c.Region]).Append("' stroke='").Append(colors[c.Region]).Append("' stroke-width='0.8'/>");
        }
        foreach (var e in m.Borders)
            sb.Append("<line x1='").Append(F(e.X1)).Append("' y1='").Append(F(e.Y1)).Append("' x2='").Append(F(e.X2)).Append("' y2='").Append(F(e.Y2))
                .Append("' stroke='#5a4a3a' stroke-width='3' stroke-linecap='round'/>");
        foreach (var e in m.Coast)
            sb.Append("<line x1='").Append(F(e.X1)).Append("' y1='").Append(F(e.Y1)).Append("' x2='").Append(F(e.X2)).Append("' y2='").Append(F(e.Y2))
                .Append("' stroke='#1f2a33' stroke-width='5' stroke-linecap='round'/>");
        foreach (var l in m.Links.Where(l => l.Cable))
            sb.Append("<line x1='").Append(F(l.X1)).Append("' y1='").Append(F(l.Y1)).Append("' x2='").Append(F(l.X2)).Append("' y2='").Append(F(l.Y2))
                .Append("' stroke='#b0302a' stroke-width='4' stroke-dasharray='14 10'/>");
        foreach (var r in m.Regions)
        {
            var c = m.Cells[r.Inner[0]];
            string fill = m.Thick(r.Inner[0]) ? "#1f2a33" : "#b0302a";
            sb.Append("<text x='").Append(F(c.CX)).Append("' y='").Append(F(c.CY - 4)).Append("' font-family='Helvetica' font-size='24' font-weight='bold' text-anchor='middle' fill='")
                .Append(fill).Append("'>").Append(Esc(r.SourceName)).Append("</text>");
            sb.Append("<text x='").Append(F(c.CX)).Append("' y='").Append(F(c.CY + 22)).Append("' font-family='Helvetica' font-size='19' text-anchor='middle' fill='#3a3a3a'>")
                .Append(r.Id).Append(" · ").Append(new[] { "KG", "erken", "orta", "geç" }[r.Stage]).Append(" · d").Append(r.Distance).Append(" · ")
                .Append(r.Cells.Count).Append("h</text>");
        }
        var hq = m.Cells[m.HqCell];
        sb.Append("<circle cx='").Append(F(hq.CX)).Append("' cy='").Append(F(hq.CY - 42)).Append("' r='15' fill='#b0302a' stroke='#fff' stroke-width='3'/>");
        sb.Append("<text x='").Append(F(hq.CX)).Append("' y='").Append(F(hq.CY - 36)).Append("' font-family='Helvetica' font-size='16' font-weight='bold' text-anchor='middle' fill='#fff'>S</text>");

        int y = 40;
        void Line(string t, int size = 22, string color = "#222")
        {
            sb.Append("<text x='1940' y='").Append(y).Append("' font-family='Menlo,monospace' font-size='").Append(size).Append("' fill='").Append(color).Append("'>")
                .Append(Esc(t)).Append("</text>");
            y += size + 6;
        }
        Line("TOHUM " + m.Seed + (m.Fallback ? " (YEDEK)" : " · deneme " + m.Attempt), 26);
        Line(m.Fingerprint, 18, "#666");
        Line("kıta " + m.Continents + " · kara " + m.Cells.Count(c => c.Land) + " · bağlantı " + m.Links.Count, 20);
        y += 6;
        foreach (var l in m.Links)
            Line((l.Cable ? "~ " : "  ") + l.A.PadRight(4) + "– " + l.B.PadRight(4) + (l.Cable ? "kablo" : l.SharedEdges + " kenar"), 20,
                l.Cable ? "#b0302a" : "#222");
        y += 6;
        foreach (var r in m.Regions) Line(r.Id.PadRight(4) + " der " + r.Neighbors.Count + " · " + string.Join(",", r.Neighbors), 17, "#444");
        sb.Append("</svg>");
        return sb.ToString();
    }
}
