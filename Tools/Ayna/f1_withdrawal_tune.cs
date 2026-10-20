Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var results = new System.Collections.Generic.List<string>();
var configs = new[] {
    new Corrigible.Ayna.Core.AynaBalance { PlannerCandidates = 8 },
    new Corrigible.Ayna.Core.AynaBalance { PlannerCandidates = 8, ScoreBattle = 6, ScoreArmyHp = 3, ScoreTier = 8 },
    new Corrigible.Ayna.Core.AynaBalance { PlannerCandidates = 8, ScoreBattle = 10, ScoreArmyHp = 3, ScoreTier = 8 }
};
for (int i = 0; i < configs.Length; i++)
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 54, OutputDirectory = "Docs/AYNA/denge/F1/withdrawal_" + i, Balance = configs[i] }));
return string.Join("\n", results);
