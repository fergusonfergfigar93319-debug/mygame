#if UNITY_EDITOR
using UnityEditor;

namespace PenguinRun.Editor
{
    internal sealed class RunnerAvatarModelImportProcessor : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/PenguinRun/Characters/Gugugaga/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;

            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.importAnimation = assetPath.EndsWith(
                "gugugaga_avatar.fbx",
                System.StringComparison.OrdinalIgnoreCase);
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
}
#endif
