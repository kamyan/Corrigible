Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
var configs = new[] {
    new Corrigible.Ayna.Core.AynaBalance(),
    new Corrigible.Ayna.Core.AynaBalance { ScoreTier = 8, ScoreAwareness = .5, SwarmAwarenessWeight = .75 },
    new Corrigible.Ayna.Core.AynaBalance { ScoreTier = 8, ScoreAwareness = .5, SwarmAwarenessWeight = .75, IncomePerServer = .65, CoreIncome = 1.3, ExpandCost = 3 }
};
for (int i = 0; i < configs.Length; i++)
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 54, OutputDirectory = "Docs/AYNA/denge/F1/tuning_" + i, Balance = configs[i] }));
return string.Join("\n", results);
