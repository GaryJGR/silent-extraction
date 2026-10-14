using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SilentExtraction.Editor
{
    /// <summary>Reproducible greybox authoring tool. Generates a separate scene; keeps template assets.</summary>
    public static class LevelBuilder
    {
        const string Root = "Assets/SilentExtraction/Generated";
        public const string ScenePath = Root + "/Facility.unity";
        static Material floor, wall, edge, cyan, mint, amber, dark;
        static Transform environment;

        [MenuItem("Silent Extraction/1. Create Playable Level")]
        public static void CreateLevel()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play mode before generating the level."); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            floor = MaterialAsset("Floor", new Color(.10f, .15f, .21f));
            wall = MaterialAsset("Cover", new Color(.17f, .23f, .29f));
            edge = MaterialAsset("Edge", new Color(.21f, .34f, .43f));
            dark = MaterialAsset("Dark", new Color(.04f, .07f, .1f));
            cyan = MaterialAsset("Data", new Color(.22f, .75f, 1), true);
            mint = MaterialAsset("Player", new Color(.34f, .96f, .74f), true);
            amber = MaterialAsset("Guard", new Color(1, .65f, .21f), true);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.44f, .52f, .65f);
            RenderSettings.fog = false;
            var light = new GameObject("Key light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(48, -28, 0);
            light.intensity = 1.5f;
            light.shadows = LightShadows.Soft;
            environment = new GameObject("Facility geometry").transform;
            Box("Facility floor", new Vector3(0, -.2f, 0), new Vector3(32, .4f, 38), floor, true);
            Box("West boundary", new Vector3(-16, 1, 0), new Vector3(.5f, 2, 38), wall, true);
            Box("East boundary", new Vector3(16, 1, 0), new Vector3(.5f, 2, 38), wall, true);
            Box("North boundary", new Vector3(0, 1, 19), new Vector3(32, 2, .5f), wall, true);
            Box("South boundary", new Vector3(0, .4f, -19), new Vector3(32, .8f, .5f), wall, true);
            // Staggered cover supports both a short exposed route and a longer covered route.
            Cover("South-west stack", -7, -10, 6, 2.5f);
            Cover("South-east stack", 7, -11, 5, 3);
            Cover("West partition", -8, -2, 2, 8);
            Cover("Central partition", 0, 1, 6, 2);
            Cover("East partition", 9, 1, 2, 7);
            Cover("North-west stack", -7, 10, 5, 3);
            Cover("North-center partition", 1, 11, 2, 6);
            Cover("Data terminal cover", 10, 10, 4, 1.5f);
            // Thin surface markings have no collider, so they never affect navigation.
            for (int z = -16; z <= 16; z += 2)
                Box("Lane dash", new Vector3(4.1f, .025f, z), new Vector3(.08f, .03f, .7f), edge, false);
            for (int x = -14; x <= 14; x += 2)
                Box("Floor seam", new Vector3(x, .006f, 0), new Vector3(.025f, .01f, 37), dark, false);
            var extract = Box("Extraction pad", new Vector3(-12, .04f, -15), new Vector3(3.4f, .08f, 3.4f), mint, false);
            Box("Extraction inset", new Vector3(-12, .09f, -15), new Vector3(2.9f, .025f, 2.9f), dark, false);
            TextSign("EXTRACT", new Vector3(-12, .13f, -17.4f), mint.color, 1.7f);
            var pedestal = Box("Core pedestal", new Vector3(11, .3f, 14), new Vector3(1.6f, .6f, 1.6f), dark, true);
            var core = Box("Data core", new Vector3(11, 1.15f, 14), new Vector3(.7f, .7f, .7f), cyan, false);
            core.transform.rotation = Quaternion.Euler(0, 45, 15);
            TextSign("DATA / 01", new Vector3(11, .13f, 16.5f), cyan.color, 1.6f);
            TextSign("SECTOR A", new Vector3(-10, .03f, -5), new Color(.34f, .48f, .58f), 2.3f);
            TextSign("SECTOR B", new Vector3(5, .03f, 7), new Color(.34f, .48f, .58f), 2.3f);

            var surface = environment.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << 8;
            Physics.SyncTransforms();
            surface.BuildNavMesh();
            // Save the generated mesh so a clean checkout and standalone build use identical paths.
            string meshPath = AssetDatabase.GenerateUniqueAssetPath(Root + "/FacilityNavMesh.asset");
            AssetDatabase.CreateAsset(surface.navMeshData, meshPath);

            var playerObject = new GameObject("Player");
            playerObject.transform.position = new Vector3(-12, .05f, -15);
            var controller = playerObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = .34f;
            controller.center = Vector3.up * .9f;
            controller.stepOffset = .25f;
            var player = playerObject.AddComponent<PlayerMotor>();
            player.visual = CharacterVisual(playerObject.transform, mint, false);

            var guardObject = new GameObject("Guard - Sentinel 01");
            guardObject.transform.position = new Vector3(-3, .03f, -6);
            var agent = guardObject.AddComponent<NavMeshAgent>();
            agent.radius = .38f;
            agent.height = 1.8f;
            agent.speed = 2.1f;
            agent.angularSpeed = 180;
            agent.acceleration = 12;
            agent.stoppingDistance = .3f;
            var guard = guardObject.AddComponent<GuardBrain>();
            guard.player = player;
            Transform guardVisual = CharacterVisual(guardObject.transform, amber, true);
            guard.visor = guardVisual.Find("Visor").GetComponent<Renderer>();
            var route = new GameObject("Patrol route").transform;
            Vector3[] points = { new Vector3(5, 0, -6), new Vector3(5, 0, 6), new Vector3(-4, 0, 6), new Vector3(-4, 0, -6) };
            guard.patrolPoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Transform waypoint = new GameObject("Waypoint " + (i + 1)).transform;
                waypoint.SetParent(route);
                waypoint.position = points[i];
                guard.patrolPoints[i] = waypoint;
            }
            var cone = new GameObject("Guard vision projection").AddComponent<VisionCone>();
            cone.guard = guard;
            var camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f, .04f, .065f);
            camera.orthographic = true;
            camera.orthographicSize = 12.5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100;
            camera.gameObject.AddComponent<AudioListener>();
            var follow = camera.gameObject.AddComponent<FollowCamera>();
            follow.target = player.transform;
            camera.transform.position = player.transform.position + follow.offset;
            camera.transform.rotation = Quaternion.Euler(56, 0, 0);
            var mission = new GameObject("Mission").AddComponent<MissionController>();
            mission.player = player;
            mission.guard = guard;
            mission.dataCore = core.transform;
            mission.extraction = extract.transform;
            mission.gameObject.AddComponent<MissionHUD>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "Gary Jiang";
            PlayerSettings.productName = "Silent Extraction";
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = mission.gameObject;
            SceneView.lastActiveSceneView?.Frame(new Bounds(Vector3.zero, new Vector3(32, 4, 38)), false);
            Debug.Log("SILENT_EXTRACTION: Level generated and navigation baked. Press Play.");
        }

        static Material MaterialAsset(string name, Color color, bool emissive = false)
        {
            string path = Root + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Smoothness", .25f);
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * .5f);
            }
            return material;
        }
        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, bool solid)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(environment);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (solid) obj.layer = 8;
            else Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        static void Cover(string name, float x, float z, float width, float depth)
        {
            Box(name, new Vector3(x, 1.05f, z), new Vector3(width, 2.1f, depth), wall, true);
            Box(name + " top trim", new Vector3(x, 2.12f, z), new Vector3(width + .04f, .07f, depth + .04f), edge, false);
        }
        static Transform CharacterVisual(Transform parent, Material accent, bool guard)
        {
            Transform root = new GameObject("Visual").transform;
            root.SetParent(parent, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(root, false);
            body.transform.localPosition = Vector3.up * .85f;
            body.transform.localScale = new Vector3(.65f, .75f, .65f);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = guard ? wall : dark;
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.name = "Visor";
            visor.transform.SetParent(root, false);
            visor.transform.localPosition = new Vector3(0, 1.35f, .28f);
            visor.transform.localScale = new Vector3(.52f, .16f, .15f);
            Object.DestroyImmediate(visor.GetComponent<Collider>());
            visor.GetComponent<Renderer>().sharedMaterial = accent;
            var backpack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backpack.name = "Pack";
            backpack.transform.SetParent(root, false);
            backpack.transform.localPosition = new Vector3(0, .8f, -.28f);
            backpack.transform.localScale = new Vector3(.44f, .6f, .28f);
            Object.DestroyImmediate(backpack.GetComponent<Collider>());
            backpack.GetComponent<Renderer>().sharedMaterial = accent;
            return root;
        }
        static void TextSign(string text, Vector3 position, Color color, float width)
        {
            var obj = new GameObject(text);
            obj.transform.SetParent(environment);
            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(90, 0, 0);
            var mesh = obj.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.fontSize = 64;
            mesh.characterSize = .15f * width;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = color;
        }

        [MenuItem("Silent Extraction/2. Validate Level")]
        public static void ValidateLevel()
        {
            var mission = Object.FindAnyObjectByType<MissionController>();
            if (mission == null) throw new System.Exception("Open Facility scene before validation.");
            Physics.SyncTransforms();
            Vector3[] destinations = { mission.player.transform.position, new Vector3(11, 0, 12.5f), mission.extraction.position };
            var path = new NavMeshPath();
            foreach (var waypoint in mission.guard.patrolPoints)
            {
                if (!NavMesh.SamplePosition(waypoint.position, out NavMeshHit hit, 1, NavMesh.AllAreas))
                    throw new System.Exception("Patrol point off navmesh: " + waypoint.name);
                if (!NavMesh.CalculatePath(mission.guard.transform.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    throw new System.Exception("Patrol route unreachable: " + waypoint.name);
            }
            for (int i = 1; i < destinations.Length; i++)
            {
                NavMesh.SamplePosition(destinations[i - 1], out NavMeshHit a, 2, NavMesh.AllAreas);
                NavMesh.SamplePosition(destinations[i], out NavMeshHit b, 2, NavMesh.AllAreas);
                if (!NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                    throw new System.Exception("Objective route is unreachable.");
            }
            Debug.Log("SILENT_EXTRACTION: VALIDATION PASS - all patrol points and objective routes are reachable.");
        }

        [MenuItem("Silent Extraction/3. Build macOS Demo")]
        public static void BuildMac()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play mode before building."); return; }
            if (!File.Exists(ScenePath)) throw new System.Exception("Generate the level first.");
            var cone = Object.FindAnyObjectByType<VisionCone>();
            var materialPath = "Assets/SilentExtraction/Generated/Vision.mat";
            var vision = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (vision == null)
            {
                vision = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                vision.SetFloat("_Surface", 1);
                vision.SetFloat("_SrcBlend", 5);
                vision.SetFloat("_DstBlend", 10);
                vision.SetFloat("_ZWrite", 0);
                vision.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                vision.renderQueue = 3000;
                AssetDatabase.CreateAsset(vision, materialPath);
            }
            cone.baseMaterial = vision;
            EditorUtility.SetDirty(cone);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Builds");
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/SilentExtraction.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });
            Debug.Log("SILENT_EXTRACTION: BUILD " + result.summary.result + " / errors: " + result.summary.totalErrors);
        }
    }
}
