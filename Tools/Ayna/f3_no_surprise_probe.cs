// Yalnız tanı: aynı ilk 20 seri, Sürpriz ödülü ve ilk vuruşu kapalı karşılaştırma.
// Varsayılan kurallar veya canlı profil değişmez; kabul koşusu değildir.
return Corrigible.Ayna.EditorTools.AynaLearningBench.Start(new Corrigible.Ayna.EditorTools.LearningBenchOptions
{
    Series = 20, Bot = "Surprise", Tactical = true, Directory = "Docs/AYNA/denge/F3/ablation",
    Balance = new Corrigible.Ayna.Core.AynaBalance { SurpriseThreshold = 0 }
});
