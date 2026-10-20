Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
var b = new Corrigible.Ayna.Core.AynaBalance { PlannerSamples = 1, ScoreBattle = 24, ScoreArmyHp = 3, ScoreTier = 12 };
foreach (var bot in new[] { "Greedy", "Random", "Mind" })
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = bot == "Mind" ? 500 : 180, AynaBot = bot, OutputDirectory = "Docs/AYNA/denge/F1/strategy_verify", Balance = b }));
return string.Join("\n", results);
