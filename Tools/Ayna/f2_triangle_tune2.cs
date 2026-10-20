Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var output = new System.Collections.Generic.List<object>();
var kinds = new[] { new[] { Corrigible.Ayna.Core.UnitKind.Guardian, Corrigible.Ayna.Core.UnitKind.Spider, Corrigible.Ayna.Core.UnitKind.Guardian }, new[] { Corrigible.Ayna.Core.UnitKind.Scout, Corrigible.Ayna.Core.UnitKind.Sniper, Corrigible.Ayna.Core.UnitKind.Emp }, new[] { Corrigible.Ayna.Core.UnitKind.Intrusion, Corrigible.Ayna.Core.UnitKind.Relay, Corrigible.Ayna.Core.UnitKind.Decoy } };
for (int config = 0; config < 4; config++)
{
    var b = new Corrigible.Ayna.Core.AynaBalance();
    b.UnitMove[0] = b.UnitMove[2] = 3;
    b.UnitHp[6] = b.UnitHp[7] = b.UnitHp[8] = b.UnitHp[9] = 3;
    if (config == 0) b.SniperDamage = 1;
    if (config == 1) b.UnitHp[1] = 1;
    if (config == 2) { b.ClassBonus = 2; b.UnitHp[1] = 1; }
    if (config == 3) { b.ClassBonus = 2; b.SniperDamage = 1; b.UnitHp[1] = 1; }
    var rates = new double[3]; var games = new System.Collections.Generic.List<object>();
    for (int edge = 0; edge < 3; edge++) for (int i = 0; i < 16; i++)
    {
        var setup = new Corrigible.Ayna.Core.BoardSetup { Seed = (uint)(2000000 + i / 2), Template = i / 4 % 4, Transform = i / 2 % 8, IntentSide = i % 2 == 0 ? Corrigible.Ayna.Core.Side.Ayna : Corrigible.Ayna.Core.Side.Player };
        for (int side = 0; side < 2; side++) for (int unit = 0; unit < 3; unit++) { var k = kinds[(edge + side) % 3][unit]; setup.Fighters.Add(new Corrigible.Ayna.Core.BoardFighter { Id = unit * 2 + side + 1, Kind = k, Hp = b.UnitHp[(int)k], Side = (Corrigible.Ayna.Core.Side)side }); }
        var g = Corrigible.Ayna.EditorTools.AynaBoardBench.Duel(setup, b); rates[edge] += g.Score / 16; games.Add(g);
    }
    output.Add(new { config, Balance = b, rates, games });
}
System.IO.Directory.CreateDirectory("Docs/AYNA/denge/F2/triangle_tune");
System.IO.File.WriteAllText("Docs/AYNA/denge/F2/triangle_tune/results2.json", Corrigible.Ayna.Core.AynaJson.Write(output));
return "Docs/AYNA/denge/F2/triangle_tune/results2.json";
