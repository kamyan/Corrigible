Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 180, Tactical = true, OutputDirectory = "Docs/AYNA/denge/F2/final_tactical" }));
foreach (var bot in new[] { "Random", "Greedy" })
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 90, AynaBot = bot, OutputDirectory = "Docs/AYNA/denge/F2/final_bots" }));
return string.Join("\n", results);
