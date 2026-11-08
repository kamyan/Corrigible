using UnityEditor;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>
    /// <c>Art/Rev3/Models</c> altındaki OBJ'lerin içe aktarma ayarı (modeller/README.md › OBJ kuralı; Aşama 0'da
    /// <c>_AxisTest</c> ile doğrulandı): ölçek 1, birim dönüşümü ve eksen dönüşümü yok, normaller dosyadan. MTL malzemeleri
    /// yalnız adları için içe aktarılır; prefab'lar <see cref="Rev3MaterialLibrary"/> malzemelerini kullanır.
    /// </summary>
    sealed class Rev3ModelPostprocessor : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Rev3ModelImporter.ModelsDir + "/")) return;
            var mi = (ModelImporter)assetImporter;
            mi.globalScale = 1;
            mi.useFileScale = false;
            mi.bakeAxisConversion = false;
            mi.importNormals = ModelImporterNormals.Import;
            mi.importTangents = ModelImporterTangents.CalculateMikk;
            mi.meshCompression = ModelImporterMeshCompression.Off;
            mi.isReadable = false;
            mi.generateSecondaryUV = false;
            mi.importBlendShapes = false;
            mi.importVisibility = false;
            mi.importCameras = false;
            mi.importLights = false;
            mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }
    }
}
