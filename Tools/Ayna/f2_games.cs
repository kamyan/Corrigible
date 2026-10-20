Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.RunDefault(500, "Docs/AYNA/denge/F2/world_auto"));
results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 180, Tactical = true, OutputDirectory = "Docs/AYNA/denge/F2/world_tactical" }));
foreach (var bot in new[] { "Random", "Greedy" })
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 90, AynaBot = bot, OutputDirectory = "Docs/AYNA/denge/F2/reference_bots" }));
return string.Join("\n", results);
