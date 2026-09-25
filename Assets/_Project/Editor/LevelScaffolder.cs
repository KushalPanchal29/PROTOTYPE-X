using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HeightIsTime.EditorTools
{
    /// <summary>
    /// Builds the first greybox version of the level: 3 rooms, player, clock, camera and HUD.
    /// Run it once. After that, edit the scene by hand; running it again overwrites the scene.
    ///
    /// Numbers to know (1 unit = 1 m): the player is 0.8 x 1 and jumps 2 m. Each room's ground line is where
    /// the player's centre is when standing at time 0, so standing 3 m above it = time 3.
    /// </summary>
    public static class LevelScaffolder
    {
        const string ScenePath = "Assets/_Project/Scenes/HeightIsTime.unity";
        const string ArtFolder = "Assets/_Project/Art";
        const string SpritePath = ArtFolder + "/Square.png";
        const string MaterialPath = ArtFolder + "/GreyboxUnlit.mat";
        const string NoFrictionPath = ArtFolder + "/NoFriction.physicsMaterial2D";
        const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        static readonly Color Ground = new Color(0.45f, 0.45f, 0.48f);
        static readonly Color LiftColor = new Color(0.30f, 0.80f, 0.90f);
        static readonly Color BridgeColor = new Color(0.95f, 0.60f, 0.20f);
        static readonly Color RockColor = new Color(0.90f, 0.25f, 0.25f);
        static readonly Color ExitColor = new Color(0.30f, 0.85f, 0.40f);

        static Sprite square;
        static Material material;

        [MenuItem("Prototype/Scaffold Greybox Level")]
        static void ScaffoldFromMenu()
        {
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Scaffold level",
                    "This overwrites " + ScenePath + " and any hand edits in it. Continue?", "Overwrite", "Cancel"))
                return;
            Scaffold();
        }

        /// <summary>Entry point for batch mode: Unity -batchmode -quit -executeMethod ...ScaffoldBatch</summary>
        public static void ScaffoldBatch() => Scaffold();

        static void Scaffold()
        {
            CreateArtAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Transform level = new GameObject("Level").transform;

            // ---------- Room 1: The Lift (ride it up by jumping) ----------
            Transform room1 = Group("Room1_Lift", level);
            Box("Floor", new Vector2(-5f, -1f), new Vector2(3f, 0f), Ground, room1);
            Box("LeftWall", new Vector2(-6f, -1f), new Vector2(-5f, 12f), Ground, room1);
            // The tall block right of the lift is Room 1's wall and Room 2's starting ledge (top at 6.5).
            Box("StartLedge", new Vector2(3f, -1f), new Vector2(8f, 6.5f), Ground, level);

            // Lift: 2 x 0.5, top rests at 0.5. Standing on it the player's centre is 1.0 -> room ground line.
            // It travels 6 m over time 0..6, so standing still on it never moves it (see TimeMathTests).
            GameObject lift = Box("Lift", new Vector2(1f, 0f), new Vector2(3f, 0.5f), LiftColor, room1);
            RoomZone zone1 = Zone("Zone1", new Vector2(-5f, -1f), new Vector2(3.2f, 12f), groundY: 1f, maxTime: 6f,
                respawn: new Vector2(-2f, 1f), parent: room1);
            AddMover(lift, new Vector2(2f, 0.25f), new Vector2(2f, 6.25f), 0f, 6f,
                maxSpeed: 4f, deadZone: 0.05f, room: zone1);

            // ---------- Room 2: The Future Bridge (only solid from time 6) ----------
            Transform room2 = Group("Room2_Bridge", level);
            Transform bridge = Group("FutureBridge", room2);
            Box("BridgeLeft", new Vector2(8f, 5.7f), new Vector2(13f, 6f), BridgeColor, bridge);
            Box("BridgeRight", new Vector2(14.5f, 5.7f), new Vector2(19.5f, 6f), BridgeColor, bridge);
            Box("EndLedge", new Vector2(19.5f, -1f), new Vector2(22f, 6f), Ground, room2);
            Kill("PitKillZone", new Vector2(8f, -4f), new Vector2(19.5f, -3f), room2);
            // Ground line 0: standing on the bridge (centre 6.5) = time 6.5, which leaves 0.5 s of margin.
            RoomZone zone2 = Zone("Zone2", new Vector2(3.2f, -4f), new Vector2(23.8f, 14f), groundY: 0f, maxTime: 10f,
                respawn: new Vector2(5.5f, 7.2f), parent: room2);
            TimeToggle toggle = bridge.gameObject.AddComponent<TimeToggle>();
            Set(toggle, "timeMin", 6f);
            Set(toggle, "timeMax", 999f);
            Set(toggle, "room", zone2);

            // ---------- Room 3: The Rock Shaft ----------
            // Left column (x 24..26.5) is safe up to the 4.4 m ledge. The rock falls down the right column
            // (x 26.6..29) between time 4.6 and 5.8. Jump up from the left ledge: the rock drops past while you
            // rise, then drift right onto the ledges above it. Fall back below 5.8 m and it flies back up at you.
            Transform room3 = Group("Room3_Shaft", level);
            Box("Floor", new Vector2(22f, -1f), new Vector2(29f, 0f), Ground, room3);
            Box("LeftWall", new Vector2(23f, 1.8f), new Vector2(24f, 18f), Ground, room3);
            Box("RightWall", new Vector2(29f, -1f), new Vector2(32f, 10f), Ground, room3);
            Box("UpperRightWall", new Vector2(32f, 10f), new Vector2(33f, 18f), Ground, room3);
            Box("Ceiling", new Vector2(23f, 17f), new Vector2(33f, 18f), Ground, room3);
            Box("Ledge1", new Vector2(27f, 1.1f), new Vector2(29f, 1.4f), Ground, room3);
            Box("Ledge2", new Vector2(24.5f, 2.6f), new Vector2(26f, 2.9f), Ground, room3);
            Box("SafeLedge", new Vector2(24f, 4.1f), new Vector2(26.4f, 4.4f), Ground, room3);
            Box("Ledge4", new Vector2(26.6f, 5.6f), new Vector2(28f, 5.9f), Ground, room3);
            Box("Ledge5", new Vector2(27.5f, 7f), new Vector2(29f, 7.3f), Ground, room3);
            Box("Ledge6", new Vector2(26.6f, 8.4f), new Vector2(27.8f, 8.7f), Ground, room3);
            RoomZone zone3 = Zone("Zone3", new Vector2(23.8f, -1f), new Vector2(33f, 18f), groundY: 0.5f, maxTime: 10f,
                respawn: new Vector2(25f, 1f), parent: room3);

            GameObject rock = Box("Rock", new Vector2(26.65f, 12f), new Vector2(28.95f, 13f), RockColor, room3);
            rock.GetComponent<BoxCollider2D>().isTrigger = true;
            rock.AddComponent<Hazard>();
            AddMover(rock, new Vector2(27.8f, 12.5f), new Vector2(27.8f, 0.5f), 4.6f, 5.8f, maxSpeed: 0f,
                deadZone: 0f, room: zone3);

            GameObject exit = Box("Exit", new Vector2(30f, 10f), new Vector2(31f, 11.5f), ExitColor, room3);
            exit.GetComponent<BoxCollider2D>().isTrigger = true;
            ExitGoal exitGoal = exit.AddComponent<ExitGoal>();

            Kill("OutOfBounds", new Vector2(-20f, -12f), new Vector2(50f, -10f), level);

            // ---------- Player, clock, camera, HUD ----------
            GameObject player = CreatePlayer(new Vector2(-2f, 0.6f));
            GameObject clockGo = new GameObject("WorldClock");
            WorldClock clock = clockGo.AddComponent<WorldClock>();
            Set(clock, "player", player.GetComponent<Rigidbody2D>());
            Set(clock, "startingRoom", zone1);

            Camera camera = CreateCamera(player.transform);
            CreateHud(camera, exitGoal);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelScaffolder] Built " + ScenePath);
        }

        static GameObject CreatePlayer(Vector2 position)
        {
            GameObject player = new GameObject("Player") { tag = "Player" };
            player.transform.position = position;
            player.transform.localScale = new Vector3(0.8f, 1f, 1f);
            SpriteRenderer sprite = player.AddComponent<SpriteRenderer>();
            sprite.sprite = square;
            sprite.sharedMaterial = material;
            sprite.sortingOrder = 10;

            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D box = player.AddComponent<BoxCollider2D>();
            box.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

            PlayerController2D controller = player.AddComponent<PlayerController2D>();
            Set(controller, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath));
            Set(controller, "jumpHeight", 2f);
            player.AddComponent<PlayerRespawn>();
            return player;
        }

        static Camera CreateCamera(Transform target)
        {
            GameObject go = new GameObject("Main Camera") { tag = "MainCamera" };
            go.transform.position = new Vector3(target.position.x, target.position.y + 1f, -10f);
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            go.AddComponent<AudioListener>();
            CameraFollow2D follow = go.AddComponent<CameraFollow2D>();
            Set(follow, "target", target);
            return camera;
        }

        static void CreateHud(Camera camera, ExitGoal exitGoal)
        {
            GameObject canvasGo = new GameObject("HUD");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            // Time bar: top centre. The fill's right edge follows world time.
            RectTransform bar = UiRect("TimeBar", canvasGo.transform, new Color(0f, 0f, 0f, 0.6f));
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(600f, 24f);
            bar.anchoredPosition = new Vector2(0f, -30f);
            RectTransform fill = UiRect("Fill", bar, new Color(1f, 0.75f, 0.3f));
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;

            TimeHud hud = canvasGo.AddComponent<TimeHud>();
            Set(hud, "fill", fill);
            Set(hud, "targetCamera", camera);

            // Win panel: shapes only (no fonts, per the no-external-assets rule).
            RectTransform win = UiRect("WinPanel", canvasGo.transform, new Color(0.1f, 0.5f, 0.2f, 0.75f));
            win.anchorMin = Vector2.zero;
            win.anchorMax = Vector2.one;
            win.offsetMin = win.offsetMax = Vector2.zero;
            RectTransform badge = UiRect("Badge", win, ExitColor);
            badge.sizeDelta = new Vector2(220f, 220f);
            badge.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Set(exitGoal, "winPanel", win.gameObject);
        }

        // ---------- helpers ----------

        static void CreateArtAssets()
        {
            Directory.CreateDirectory(ArtFolder);
            if (!File.Exists(SpritePath))
            {
                Texture2D texture = new Texture2D(4, 4);
                Color[] pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels);
                File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SpritePath);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 4f;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }
            square = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

            material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default")
                                ?? Shader.Find("Sprites/Default");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath) == null)
                AssetDatabase.CreateAsset(new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f },
                    NoFrictionPath);
        }

        static Transform Group(string name, Transform parent)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        static GameObject Box(string name, Vector2 min, Vector2 max, Color color, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            go.transform.localScale = new Vector3(max.x - min.x, max.y - min.y, 1f);
            SpriteRenderer sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = square;
            sprite.sharedMaterial = material;
            sprite.color = color;
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        static void Kill(string name, Vector2 min, Vector2 max, Transform parent)
        {
            GameObject go = Box(name, min, max, new Color(1f, 0f, 0f, 0.15f), parent);
            go.GetComponent<BoxCollider2D>().isTrigger = true;
            go.AddComponent<Hazard>();
        }

        static RoomZone Zone(string name, Vector2 min, Vector2 max, float groundY, float maxTime, Vector2 respawn,
            Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = max - min;
            RoomZone zone = go.AddComponent<RoomZone>();

            Transform spawn = new GameObject("Respawn").transform;
            spawn.SetParent(go.transform, false);
            spawn.position = respawn;

            Set(zone, "groundY", groundY);
            Set(zone, "maxTime", maxTime);
            Set(zone, "respawnPoint", spawn);
            return zone;
        }

        static TimeMover AddMover(GameObject go, Vector2 a, Vector2 b, float tStart, float tEnd, float maxSpeed,
            float deadZone, RoomZone room)
        {
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            TimeMover mover = go.AddComponent<TimeMover>();
            Set(mover, "pointA", a);
            Set(mover, "pointB", b);
            Set(mover, "timeStart", tStart);
            Set(mover, "timeEnd", tEnd);
            Set(mover, "maxSpeed", maxSpeed);
            Set(mover, "deadZone", deadZone);
            Set(mover, "room", room);
            return mover;
        }

        static RectTransform UiRect(string name, Transform parent, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        static void Set(Object target, string field, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[LevelScaffolder] {target.GetType().Name} has no field '{field}'");
                return;
            }
            switch (value)
            {
                case float f: property.floatValue = f; break;
                case Vector2 v: property.vector2Value = v; break;
                case Object o: property.objectReferenceValue = o; break;
                default: Debug.LogError($"[LevelScaffolder] Unsupported value for '{field}'"); return;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
