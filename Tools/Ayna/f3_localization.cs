var path = "Assets/Corrigible/Resources/Localization/strings.txt";
var original = System.IO.File.ReadAllBytes(path);
byte[] generated = null;
string message = null;
try
{
    message = Corrigible.EditorTools.LocalizationTool.UpdateTable();
    generated = System.IO.File.ReadAllBytes(path);
}
finally
{
    // Güncelleme eski satırları yeniden sıralar; bu faz yalnız eklenen AYNA satırlarını taşır.
    // Arada başka bir yazma olduysa ona dokunma.
    if (generated != null && System.Linq.Enumerable.SequenceEqual(System.IO.File.ReadAllBytes(path), generated))
    {
        System.IO.File.WriteAllBytes(path, original);
        UnityEditor.AssetDatabase.ImportAsset(path);
    }
}
return new { Message = message, OriginalLayoutPreserved = System.Linq.Enumerable.SequenceEqual(original, System.IO.File.ReadAllBytes(path)), Missing = Corrigible.EditorTools.LocalizationTool.Validate().Count };
