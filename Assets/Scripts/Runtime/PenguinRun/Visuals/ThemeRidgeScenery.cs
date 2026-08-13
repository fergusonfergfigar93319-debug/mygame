using UnityEngine;

namespace PenguinRun
{
    /// <summary>
    /// 远山剪影构建（从 PenguinRunnerGame 拆出）：低模山脊 + 雪冠双层网格。
    /// 配色由调用方按主题传入（雪松暮紫 / 海洋礁蓝 / 天空云白……），注册回布景系统由调用方负责。
    /// </summary>
    internal static class ThemeRidgeScenery
    {
        public static GameObject CreateRidgeGroup(float x, float z, float height, Color bodyColor, Color capColor)
        {
            var group = new GameObject("Distant Ice Ridge");
            group.transform.position = new Vector3(x, 0f, z);
            var ridge = new GameObject("Low Poly Ridge", typeof(MeshFilter), typeof(MeshRenderer));
            ridge.transform.SetParent(group.transform, false);
            ridge.GetComponent<MeshFilter>().sharedMesh = CreateRidgeSilhouetteMesh(height, x < 0f);
            ridge.GetComponent<MeshRenderer>().sharedMaterial = RunnerVisuals.CreateMaterial(bodyColor);
            var cap = new GameObject("Ridge Snow Cap", typeof(MeshFilter), typeof(MeshRenderer));
            cap.transform.SetParent(group.transform, false);
            cap.transform.localPosition = new Vector3(0f, height * 0.06f, -0.04f);
            cap.GetComponent<MeshFilter>().sharedMesh = CreateRidgeSilhouetteMesh(height * 0.62f, x < 0f);
            cap.GetComponent<MeshRenderer>().sharedMaterial = RunnerVisuals.CreateMaterial(capColor);
            return group;
        }

        private static Mesh CreateRidgeSilhouetteMesh(float height, bool facesRight)
        {
            var width = height * 1.15f;
            var peakX = facesRight ? width * 0.16f : -width * 0.16f;
            var mesh = new Mesh { name = "Distant Ridge Silhouette" };
            mesh.vertices = new[]
            {
                new Vector3(-width, 0f, 0f), new Vector3(-width * 0.45f, height * 0.52f, 0f),
                new Vector3(peakX, height, 0f), new Vector3(width * 0.5f, height * 0.34f, 0f), new Vector3(width, 0f, 0f),
                new Vector3(-width, 0f, 0.35f), new Vector3(-width * 0.45f, height * 0.52f, 0.35f),
                new Vector3(peakX, height, 0.35f), new Vector3(width * 0.5f, height * 0.34f, 0.35f), new Vector3(width, 0f, 0.35f),
            };
            mesh.triangles = new[] { 0, 1, 6, 0, 6, 5, 1, 2, 7, 1, 7, 6, 2, 3, 8, 2, 8, 7, 3, 4, 9, 3, 9, 8 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
