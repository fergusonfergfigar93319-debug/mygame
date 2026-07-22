using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenguinRun
{
    [CreateAssetMenu(menuName = "PenguinRun/Avatar/Accessory Socket Set", fileName = "RunnerAccessorySocketSet")]
    public sealed class RunnerAccessorySocketSet : ScriptableObject
    {
        [Serializable]
        public sealed class Socket
        {
            [SerializeField] private string id;
            [SerializeField] private string relativePath;

            public string Id => id;
            public string RelativePath => relativePath;
        }

        [SerializeField] private List<Socket> sockets = new();

        public IReadOnlyList<Socket> Sockets => sockets;
    }
}
