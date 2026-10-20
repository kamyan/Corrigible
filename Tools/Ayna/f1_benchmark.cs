Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.RunDefault(500, "Docs/AYNA/denge/F1/final"));
foreach (var bot in new[] { "Random", "Greedy" })
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 180, AynaBot = bot, OutputDirectory = "Docs/AYNA/denge/F1/final" }));
return string.Join("\n", results);
