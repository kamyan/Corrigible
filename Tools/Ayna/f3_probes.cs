var oldProfile = Corrigible.Ayna.Core.AynaJson.ReadProfile("{\"Version\":1,\"Generation\":4,\"Games\":4,\"Architectures\":[1,2,1],\"OrderCounts\":[7,0,0,0,0,0,0,0,0,0,0,0,0],\"RegionCounts\":[0,0,0,0,0,0,0,0,0,0,0,0]}");
var item = new Corrigible.Ayna.Core.Prediction { Kind = Corrigible.Ayna.Core.OrderKind.Expand, Region = "in", Probability = .72 };
var oldLanguage = Corrigible.Core.Loc.Lang;
var tableField = typeof(Corrigible.Core.Loc).GetField("table", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
var oldTable = (System.Collections.Generic.Dictionary<string,string>)tableField.GetValue(null);
string tr, en;
try
{
    Corrigible.Core.Loc.SetLanguage("tr"); tr = Corrigible.Ayna.Mind.ForecastText.Format(item);
    var tables = Corrigible.Core.Loc.ParseTable(System.IO.File.ReadAllText("Assets/Corrigible/Resources/Localization/strings.txt"));
    Corrigible.Core.Loc.SetLanguage("en", tables["en"]); en = Corrigible.Ayna.Mind.ForecastText.Format(item);
}
finally { Corrigible.Core.Loc.SetLanguage(oldLanguage, oldTable); tableField.SetValue(null, oldTable); }
return new { oldProfile.Version, oldProfile.Generation, OrderCount = oldProfile.OrderCounts[0], Targets = oldProfile.Targets.Length, Context = oldProfile.CycleTargets.Length, Turkish = tr, English = en, LanguageRestored = Corrigible.Core.Loc.Lang == oldLanguage };
