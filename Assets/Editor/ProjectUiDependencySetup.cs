#if UNITY_EDITOR
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures the third-party UI packages required by the runtime UI are initialized
/// in a fresh checkout and in unattended build environments.
/// </summary>
public static class ProjectUiDependencySetup
{
    private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    /// <summary>
    /// Unity batch entry point:
    /// Unity -batchmode -executeMethod ProjectUiDependencySetup.Apply -quit
    /// </summary>
    public static void Apply()
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null)
        {
            FinishSetup();
            ExitBatchMode(0);
            return;
        }

        AssetDatabase.importPackageCompleted += OnImportPackageCompleted;
        AssetDatabase.importPackageFailed += OnImportPackageFailed;
        ImportTmpEssentials();
    }

    public static void Verify()
    {
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
        if (settings == null)
            throw new InvalidOperationException($"TMP Essentials are missing: {TmpSettingsPath}");

        Debug.Log($"[ProjectUiDependencySetup] TMP Settings verified at {TmpSettingsPath}.");
    }

    private static void ImportTmpEssentials()
    {
        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
        if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            throw new InvalidOperationException("Unable to resolve the installed com.unity.ugui package path.");

        var essentialsPackage = Path.Combine(
            packageInfo.resolvedPath,
            "Package Resources",
            "TMP Essential Resources.unitypackage");

        if (!File.Exists(essentialsPackage))
            throw new FileNotFoundException("TMP Essential Resources package was not found.", essentialsPackage);

        Debug.Log($"[ProjectUiDependencySetup] Importing TMP Essential Resources from {essentialsPackage}.");
        AssetDatabase.ImportPackage(essentialsPackage, interactive: false);
    }

    private static void OnImportPackageCompleted(string packageName)
    {
        if (!packageName.Contains("TMP Essential Resources", StringComparison.OrdinalIgnoreCase))
            return;

        UnsubscribeImportCallbacks();
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        FinishSetup();
        ExitBatchMode(0);
    }

    private static void OnImportPackageFailed(string packageName, string errorMessage)
    {
        UnsubscribeImportCallbacks();
        Debug.LogError($"[ProjectUiDependencySetup] Failed to import {packageName}: {errorMessage}");
        ExitBatchMode(1);
    }

    private static void FinishSetup()
    {
        DotweenBatchSetup.Apply();
        Verify();
        AssetDatabase.SaveAssets();
        Debug.Log("[ProjectUiDependencySetup] TMP Essentials and DOTween setup are ready.");
    }

    private static void UnsubscribeImportCallbacks()
    {
        AssetDatabase.importPackageCompleted -= OnImportPackageCompleted;
        AssetDatabase.importPackageFailed -= OnImportPackageFailed;
    }

    private static void ExitBatchMode(int exitCode)
    {
        if (Application.isBatchMode)
            EditorApplication.Exit(exitCode);
    }
}
#endif
