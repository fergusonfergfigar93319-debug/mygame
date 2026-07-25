#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenguinRun.Editor
{
    /// <summary>Captures deterministic poses through the production avatar presenter.</summary>
    public static class ProjectPenguinPreviewCapture
    {
        // Intra-run pose gate (same-camera Idle vs action). Measured 2026-07-25 after
        // AnyState canTransitionToSelf=false + mid-clip re-snap:
        // run=4.34 lane=9.14 jump=6.25 slide=11.22 hurt=19.72 defeat=16.28 celebrate=10.01.
        // Threshold = half of the smallest measured action delta (4.34/2 ≈ 2.2).
        private const float MinPoseMeanAbsDiff = 2.2f;

        private enum Pose
        {
            Idle,
            Run,
            Lane,
            Jump,
            Slide,
            Hurt,
            Defeat,
            Celebrate,
        }

        [MenuItem("PenguinRun/Visuals/Capture Penguin Previews")]
        public static void CaptureAll()
        {
            var output = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName,
                "Artifacts", "PenguinPreviews");
            Directory.CreateDirectory(output);

            Capture(output, "front", Pose.Idle, new Vector3(0f, 1.35f, -4.2f));
            Capture(output, "back", Pose.Idle, new Vector3(0f, 1.45f, 4.2f));
            Capture(output, "side", Pose.Idle, new Vector3(4.2f, 1.4f, 0f));

            // Compare each action pose against Idle rendered from the SAME camera so
            // pixel diffs measure animation, not framing.
            var failures = new System.Collections.Generic.List<string>();
            AssertPose(failures, output, "run", Pose.Run, new Vector3(2.7f, 1.5f, -3.5f));
            AssertPose(failures, output, "lane", Pose.Lane, new Vector3(2.7f, 1.5f, -3.5f));
            AssertPose(failures, output, "jump", Pose.Jump, new Vector3(2.7f, 1.55f, -3.5f));
            AssertPose(failures, output, "slide", Pose.Slide, new Vector3(2.7f, 1.25f, -3.5f));
            AssertPose(failures, output, "hurt", Pose.Hurt, new Vector3(1.8f, 1.4f, -3.9f));
            AssertPose(failures, output, "defeat", Pose.Defeat, new Vector3(1.8f, 1.3f, -3.9f));
            AssertPose(failures, output, "celebrate", Pose.Celebrate, new Vector3(1.8f, 1.45f, -3.9f));

            if (failures.Count > 0)
            {
                var message = "[ProjectPenguinPreviewCapture] FAIL pose diff:\n" + string.Join("\n", failures);
                Debug.LogError(message);
                throw new UnityException(message);
            }

            Debug.Log($"[ProjectPenguinPreviewCapture] PASS: wrote 10 presenter previews to {output}");
        }

        public static void CaptureAllFromCommandLine()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                EditorApplication.Exit(1);
            }
        }

        private static Color32[] Capture(string outputDir, string name, Pose pose, Vector3 cameraPosition)
        {
            var sceneRoot = new GameObject("PenguinPreviewScene");
            RenderTexture renderTexture = null;
            Texture2D image = null;
            Material groundMaterial = null;
            try
            {
                var player = new GameObject("Penguin Character");
                player.transform.SetParent(sceneRoot.transform, false);
                var definition = Resources.Load<RunnerAvatarDefinition>(
                    "PenguinRun/Characters/Gugugaga/GugugagaAvatar");
                var character = PlayerAvatarPresenter.Create(player.transform, definition);
                player.transform.localScale = Vector3.one * 0.86f;

                DrivePose(character, player, pose);

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "PreviewGround";
                ground.transform.SetParent(sceneRoot.transform, false);
                ground.transform.position = new Vector3(0f, -0.02f, 0f);
                groundMaterial = new Material(Shader.Find("Standard"))
                    { color = new Color(0.18f, 0.31f, 0.42f) };
                ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

                CreateLight(sceneRoot.transform, "KeyLight", Quaternion.Euler(42f, -32f, 0f),
                    1.45f, new Color(1f, 0.92f, 0.82f));
                CreateLight(sceneRoot.transform, "RimLight", Quaternion.Euler(25f, 145f, 0f),
                    0.75f, new Color(0.45f, 0.72f, 1f));

                var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
                cameraObject.transform.SetParent(sceneRoot.transform, false);
                cameraObject.transform.position = cameraPosition;
                cameraObject.transform.LookAt(new Vector3(0f, 1f, 0f));
                var camera = cameraObject.GetComponent<Camera>();
                camera.fieldOfView = 34f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.035f, 0.06f, 0.1f);

                renderTexture = new RenderTexture(960, 720, 24, RenderTextureFormat.ARGB32)
                    { antiAliasing = 4 };
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image = new Texture2D(960, 720, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0);
                image.Apply();
                if (!string.IsNullOrEmpty(name))
                {
                    var bytes = image.EncodeToPNG();
                    if (bytes == null || bytes.Length < 1024)
                        throw new UnityException($"Avatar preview '{name}' was empty.");
                    File.WriteAllBytes(Path.Combine(outputDir, name + ".png"), bytes);
                }
                return image.GetPixels32();
            }
            finally
            {
                RenderTexture.active = null;
                if (image != null) Object.DestroyImmediate(image);
                if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
                Object.DestroyImmediate(sceneRoot);
                if (renderTexture != null) Object.DestroyImmediate(renderTexture);
            }
        }

        private static void DrivePose(IPlayerCharacterPresentation character, GameObject player, Pose pose)
        {
            var state = StateFor(pose);
            var animators = player.GetComponentsInChildren<Animator>(true);
            var stateName = StateNameFor(pose);

            // Push parameters first so transitions stay consistent with the forced state.
            for (var i = 0; i < 4; i++)
            {
                character.Tick(1f / 60f, state);
                foreach (var animator in animators) animator.Update(1f / 60f);
            }

            if (pose == Pose.Hurt) character.TriggerHurt();
            if (pose == Pose.Celebrate) character.TriggerCelebration();

            // Force mid-clip sampling so previews never freeze on the Idle→action blend.
            foreach (var animator in animators)
            {
                if (pose == Pose.Hurt)
                {
                    animator.Play("Hurt", 1, 0.35f);
                    animator.Update(0f);
                }
                else if (pose == Pose.Celebrate)
                {
                    animator.Play("Celebrate", 1, 0.35f);
                    animator.Update(0f);
                }
                else
                {
                    animator.Play(stateName, 0, MidNormalizedTime(pose));
                    animator.Update(0f);
                }
            }

            var settleFrames = pose is Pose.Hurt or Pose.Celebrate ? 8 : 24;
            for (var i = 0; i < settleFrames; i++)
            {
                character.Tick(1f / 60f, state);
                foreach (var animator in animators) animator.Update(1f / 60f);
            }

            // Re-snap after settle so exit-time transitions (Hurt/Celebrate) and looping
            // locomotion both render a stable mid-clip silhouette.
            foreach (var animator in animators)
            {
                if (pose == Pose.Hurt)
                {
                    animator.Play("Hurt", 1, 0.4f);
                    animator.Update(0f);
                }
                else if (pose == Pose.Celebrate)
                {
                    animator.Play("Celebrate", 1, 0.35f);
                    animator.Update(0f);
                }
                else if (pose != Pose.Idle)
                {
                    animator.Play(stateName, 0, MidNormalizedTime(pose));
                    animator.Update(0f);
                }
            }
        }

        private static string StateNameFor(Pose pose) => pose switch
        {
            Pose.Run => "Run",
            Pose.Lane => "Run",
            Pose.Jump => "Jump",
            Pose.Slide => "Slide",
            Pose.Defeat => "Defeat",
            _ => "Idle",
        };

        private static float MidNormalizedTime(Pose pose) => pose switch
        {
            Pose.Slide => 0.55f,
            Pose.Jump => 0.45f,
            Pose.Defeat => 0.7f,
            Pose.Run => 0.4f,
            Pose.Lane => 0.4f,
            _ => 0.2f,
        };

        private static void AssertPose(
            System.Collections.Generic.List<string> failures,
            string outputDir,
            string poseName,
            Pose pose,
            Vector3 cameraPosition)
        {
            var idle = Capture(outputDir, null, Pose.Idle, cameraPosition);
            var action = Capture(outputDir, poseName, pose, cameraPosition);
            if (idle == null || action == null || idle.Length != action.Length)
            {
                failures.Add($"{poseName}: pixel buffer mismatch");
                return;
            }

            double accum = 0;
            for (var i = 0; i < idle.Length; i++)
            {
                accum += System.Math.Abs(idle[i].r - action[i].r);
                accum += System.Math.Abs(idle[i].g - action[i].g);
                accum += System.Math.Abs(idle[i].b - action[i].b);
            }

            var mean = (float)(accum / (idle.Length * 3.0));
            Debug.Log($"[ProjectPenguinPreviewCapture] poseDiff {poseName}={mean:F2}");
            if (mean < MinPoseMeanAbsDiff)
                failures.Add($"{poseName}: meanAbsDiff={mean:F2} < {MinPoseMeanAbsDiff}");
        }

        private static RunnerAvatarState StateFor(Pose pose)
        {
            var running = pose is Pose.Run or Pose.Lane or Pose.Jump or Pose.Slide;
            var grounded = pose != Pose.Jump;
            var sliding = pose == Pose.Slide;
            var gameOver = pose == Pose.Defeat;
            var speed = running ? 0.72f : 0f;
            var laneLean = pose == Pose.Lane ? 0.9f : 0f;
            var verticalSpeed = pose == Pose.Jump ? 3.5f : 0f;
            return new RunnerAvatarState(running, grounded, sliding, gameOver,
                speed, laneLean, verticalSpeed, RunnerAvatarBuffFlags.None);
        }

        private static void CreateLight(Transform parent, string name, Quaternion rotation,
            float intensity, Color color)
        {
            var lightObject = new GameObject(name, typeof(Light));
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.rotation = rotation;
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.Soft;
        }
    }
}
#endif
