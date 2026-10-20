Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var configs = new[] {
    new Corrigible.Ayna.Core.AynaBalance { AutoWinnerDamage = 2, AutoLoserDamage = 4 },
    new Corrigible.Ayna.Core.AynaBalance { AutoWinnerDamage = 2, AutoLoserDamage = 4, AutoRewardRatio = 1 },
    new Corrigible.Ayna.Core.AynaBalance { FortressExpandLimit = 3 },
    new Corrigible.Ayna.Core.AynaBalance { FortressExpandLimit = 3, AutoWinnerDamage = 2, AutoLoserDamage = 4, AutoRewardRatio = 1 }
};
configs[2].AutoUtility[(int)Corrigible.Ayna.Core.UnitKind.Decoy] = 3.5;
configs[3].AutoUtility[(int)Corrigible.Ayna.Core.UnitKind.Decoy] = 3.5;
var results = new System.Collections.Generic.List<string>();
for (int i = 0; i < configs.Length; i++)
    results.Add(Corrigible.Ayna.EditorTools.AynaSelfPlay.Run(new Corrigible.Ayna.EditorTools.SelfPlayOptions { Games = 54, OutputDirectory = "Docs/AYNA/denge/F1/combat_" + i, Balance = configs[i] }));
return string.Join("\n", results);
