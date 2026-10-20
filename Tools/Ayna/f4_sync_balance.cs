Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<Corrigible.Ayna.Game.AynaBalanceConfig>("Assets/Corrigible/Ayna/Settings/AynaBalance.asset");
var so=new UnityEditor.SerializedObject(asset);var values=so.FindProperty("values");
var defaults=new Corrigible.Ayna.Core.AynaBalance();
values.FindPropertyRelative("SurprisePolicy").intValue=defaults.SurprisePolicy;
foreach(var name in new[]{"SurpriseEvidence","SurpriseThreshold","ScoreApproach","ApproachHorizon"})values.FindPropertyRelative(name).doubleValue=(double)typeof(Corrigible.Ayna.Core.AynaBalance).GetField(name).GetValue(defaults);
so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.SetDirty(asset);UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
return asset.Snapshot();
