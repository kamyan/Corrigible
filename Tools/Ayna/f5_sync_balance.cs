Corrigible.Ayna.EditorTools.AynaSelfPlay.AssertEditorSafe();
var asset=UnityEditor.AssetDatabase.LoadAssetAtPath<Corrigible.Ayna.Game.AynaBalanceConfig>("Assets/Corrigible/Ayna/Settings/AynaBalance.asset");
var expected=new Corrigible.Ayna.Core.AynaBalance { TacticalPlanning=true };
var so=new UnityEditor.SerializedObject(asset);var root=so.FindProperty("values");
foreach(var field in typeof(Corrigible.Ayna.Core.AynaBalance).GetFields())
{
    var p=root.FindPropertyRelative(field.Name);var v=field.GetValue(expected);
    if(v is double d)p.doubleValue=d;else if(v is int i)p.intValue=i;else if(v is bool b)p.boolValue=b;
    else if(v is double[] da){p.arraySize=da.Length;for(int n=0;n<da.Length;n++)p.GetArrayElementAtIndex(n).doubleValue=da[n];}
    else if(v is int[] ia){p.arraySize=ia.Length;for(int n=0;n<ia.Length;n++)p.GetArrayElementAtIndex(n).intValue=ia[n];}
}
so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.EditorUtility.SetDirty(asset);UnityEditor.AssetDatabase.SaveAssetIfDirty(asset);
return new { Match=Corrigible.Ayna.Core.AynaJson.Write(expected)==Corrigible.Ayna.Core.AynaJson.Write(asset.Snapshot()),Balance=asset.Snapshot() };
