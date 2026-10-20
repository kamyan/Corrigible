Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
var configs = new[] {
    new Corrigible.Ayna.Core.AynaBalance { PlannerSamples = 1 },
    new Corrigible.Ayna.Core.AynaBalance { PlannerSamples = 1, ScoreBattle = 24, ScoreArmyHp = 3, ScoreTier = 12 },
    new Corrigible.Ayna.Core.AynaBalance { PlannerSamples = 2, ScoreBattle = 24, ScoreArmyHp = 3, ScoreTier = 12 }
};
for (int i = 0; i < configs.Length; i++)
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 54, OutputDirectory = "Docs/AYNA/denge/F1/strategy_" + i, Balance = configs[i] }));
return string.Join("\n", results);
