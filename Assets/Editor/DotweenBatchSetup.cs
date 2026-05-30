#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Headless DOTween install finalizer. Run via:
/// Unity -batchmode -executeMethod DotweenBatchSetup.Apply -quit
/// </summary>
public static class DotweenBatchSetup
{
    const string Define = "DOTWEEN";

    static readonly BuildTargetGroup[] TargetGroups =
    {
        BuildTargetGroup.Standalone,
        BuildTargetGroup.Android,
        BuildTargetGroup.iOS,
        BuildTargetGroup.WebGL,
    };

    public static void Apply()
    {
        EnsureDefineOnAllTargets();
        RefreshDotweenModuleDefines();
        AssetDatabase.SaveAssets();
        Debug.Log("[DotweenBatchSetup] DOTween batch setup complete.");
    }

    static void EnsureDefineOnAllTargets()
    {
        foreach (var group in TargetGroups)
        {
            if (group == BuildTargetGroup.Unknown)
                continue;

            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group)
                .Split(';')
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();

            if (defines.Contains(Define))
                continue;

            defines.Add(Define);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
            Debug.Log($"[DotweenBatchSetup] Added {Define} for {group}.");
        }
    }

    static void RefreshDotweenModuleDefines()
    {
        var editorAsm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "DOTweenEditor");
        if (editorAsm == null)
        {
            Debug.LogWarning("[DotweenBatchSetup] DOTweenEditor assembly not loaded; skip module refresh.");
            return;
        }

        var definesType = editorAsm.GetType("DG.DOTweenEditor.DOTweenDefines");
        if (definesType == null)
        {
            Debug.LogWarning("[DotweenBatchSetup] DOTweenDefines type not found.");
            return;
        }

        var refreshAll = definesType.GetMethod("RefreshAll", BindingFlags.Public | BindingFlags.Static);
        if (refreshAll == null)
        {
            Debug.LogWarning("[DotweenBatchSetup] DOTweenDefines.RefreshAll not found.");
            return;
        }

        refreshAll.Invoke(null, null);
        Debug.Log("[DotweenBatchSetup] DOTweenDefines.RefreshAll applied.");
    }
}
#endif
