using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    [CreateAssetMenu(menuName = "PenguinRun/Avatar/Appearance Set", fileName = "RunnerAppearanceSet")]
    public sealed class RunnerAppearanceSet : ScriptableObject
    {
        [Serializable]
        public sealed class MaterialOverride
        {
            [SerializeField] private string rendererPath;
            [SerializeField] private Material material;

            public string RendererPath => rendererPath;
            public Material Material => material;
        }

        [SerializeField] private List<MaterialOverride> overrides = new();

        public IReadOnlyList<MaterialOverride> Overrides => overrides;
    }
}
