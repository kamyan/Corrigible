using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk
{
    public enum DeskLang { Tr, En }

    /// <summary>
    /// <see cref="Loc"/> köprüsü: anahtar Türkçe kaynak metindir, tablo <c>Resources/Localization/strings.txt</c>.
    /// Oyun dili sahne yüklenmeden önce kurar; masa ayrıca dili sahneyi yeniden yüklemeden değiştirebilir (geliştirme F9).
    /// </summary>
    public sealed class DeskLoc
    {
        const string TablePath = "Localization/strings";

        public static readonly DeskLoc Instance = new DeskLoc();

        Dictionary<string, Dictionary<string, string>> tables;

        public DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;
        public event Action Changed;

        public string T(string source) => string.IsNullOrEmpty(source) ? source : Loc.T(source);

        /// <summary>
        /// Kaynak metnin verilen dildeki karşılığı (Türkçede kaynağın kendisi; çevirisi yoksa kaynak). Geçmişte başka dilde
        /// yazılmış bir metni tanımak için (ör. son raporda kırılma anları).
        /// </summary>
        public string In(string source, string lang)
        {
            if (lang == Loc.Turkish || string.IsNullOrEmpty(source)) return source;
            Load();
            return tables.TryGetValue(lang, out var table) && table.TryGetValue(source, out var value) && !string.IsNullOrEmpty(value) ? value : source;
        }

        void Load()
        {
            if (tables != null) return;
            var asset = Resources.Load<TextAsset>(TablePath);
            tables = asset != null ? Loc.ParseTable(asset.text) : new Dictionary<string, Dictionary<string, string>>();
        }

        public void SetLanguage(DeskLang lang)
        {
            if (lang == Lang && tables != null) return;
            Load();
            var code = lang == DeskLang.En ? Loc.English : Loc.Turkish;
            tables.TryGetValue(code, out var table);
            Loc.SetLanguage(code, table);
            Changed?.Invoke();
        }

        public void Toggle() => SetLanguage(Lang == DeskLang.Tr ? DeskLang.En : DeskLang.Tr);
    }
}
