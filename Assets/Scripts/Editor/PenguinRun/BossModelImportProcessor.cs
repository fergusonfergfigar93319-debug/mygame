using UnityEditor;
using UnityEngine;

namespace PenguinRun.EditorTools
{
    /// <summary>
    /// Bosses 目录 rigged FBX 导入规则：按 Legacy 动画导入（片段由 BossEncounter 程序化播放，
    /// 不引入 Animator 状态机，见 docs/adr/0002-cc0-rigged-boss-models.md）。
    /// </summary>
    internal sealed class BossModelImportProcessor : AssetPostprocessor
    {
        private const string BossFolder = "Assets/Resources/PenguinRun/Models/Bosses";

        private bool IsBossAsset => assetPath.Replace('\\', '/').StartsWith(BossFolder);

        private void OnPreprocessModel()
        {
            if (!IsBossAsset) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.globalScale = 1f;
            importer.isReadable = false;
        }

        private void OnPreprocessTexture()
        {
            if (!IsBossAsset) return;
            var importer = (TextureImporter)assetImporter;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
