using System;
using System.Collections.Generic;
using Corrigible.Core;
using UnityEngine;

namespace Corrigible.Desk
{
    public enum DeskLang { Tr, En }

    /// <summary>Masanın metin kaynağı. Varsayılan uygulama projenin <see cref="Loc"/> sistemine bağlanır (PROMPT §6.7).</summary>
    public interface IDeskLocalizer
    {
        DeskLang Lang { get; }
        string T(string source);
        event Action Changed;
    }

    /// <summary>
    /// <see cref="Loc"/> köprüsü: anahtar Türkçe kaynak metindir, tablo <c>Resources/Localization/strings.txt</c>.
    /// Oyun dili sahne yüklenmeden önce kurar; masa ayrıca dili sahneyi yeniden yüklemeden değiştirebilir (geliştirme F9).
    /// </summary>
    public sealed class DeskLoc : IDeskLocalizer
    {
        const string TablePath = "Localization/strings";

        public static readonly DeskLoc Instance = new DeskLoc();

        Dictionary<string, Dictionary<string, string>> tables;

        public DeskLang Lang => Loc.Lang == Loc.English ? DeskLang.En : DeskLang.Tr;
        public event Action Changed;

        public string T(string source) => string.IsNullOrEmpty(source) ? source : Loc.T(source);

        public void SetLanguage(DeskLang lang)
        {
            if (lang == Lang && tables != null) return;
            if (tables == null)
            {
                var asset = Resources.Load<TextAsset>(TablePath);
                tables = asset != null ? Loc.ParseTable(asset.text) : new Dictionary<string, Dictionary<string, string>>();
            }
            var code = lang == DeskLang.En ? Loc.English : Loc.Turkish;
            tables.TryGetValue(code, out var table);
            Loc.SetLanguage(code, table);
            Changed?.Invoke();
        }

        public void Toggle() => SetLanguage(Lang == DeskLang.Tr ? DeskLang.En : DeskLang.Tr);
    }
}
