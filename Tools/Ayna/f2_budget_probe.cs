Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
foreach (var bot in new[] { "Mind", "Greedy", "Random" })
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = bot == "Mind" ? 54 : 90, AynaBot = bot, Balance = new Corrigible.Ayna.Core.AynaBalance { PlannerCandidates = 8 }, OutputDirectory = "Docs/AYNA/denge/F2/budget_probe" }));
return string.Join("\n", results);
