using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HeightIsTime.EditorTools
{
    /// <summary>
    /// Builds the greybox level: 3 rooms, player, clock, camera and HUD. Running it again overwrites the scene,
    /// so after the team starts hand-editing, stop using it.
    ///
    /// Numbers to know (1 unit = 1 m): the player is 0.8 x 1 and jumps 2 m. The floor top is y = 0 everywhere,
    /// so the player's feet height equals world time (1 s per meter): standing on a 3 m ledge = time 3.
    /// </summary>
    public static class LevelScaffolder
    {
        const string ScenePath = "Assets/_Project/Scenes/HeightIsTime.unity";
        const string ArtFolder = "Assets/_Project/Art";
        const string SpritePath = ArtFolder + "/Square.png";
        const string MaterialPath = ArtFolder + "/GreyboxUnlit.mat";
        const string NoFrictionPath = ArtFolder + "/NoFriction.physicsMaterial2D";
        const string InputActionsPath = "Assets/Settings/InputSystem_Actions.inputactions";

        // Player centre when standing on the floor (y = 0) => time 0.
        const float GroundY = 0.5f;

        static readonly Color Ground = new Color(0.45f, 0.45f, 0.48f);
        static readonly Color WallColor = new Color(0.30f, 0.80f, 0.90f);
        static readonly Color BridgeColor = new Color(0.95f, 0.60f, 0.20f);
        static readonly Color GateColor = new Color(0.70f, 0.45f, 0.95f);
        static readonly Color ExitColor = new Color(0.30f, 0.85f, 0.40f);
        static readonly Color ClockColor = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color HandColor = new Color(1f, 0.75f, 0.3f, 0.5f);

        // Sorting: time lines and clocks behind everything, the moving wall behind the floor so it sinks "into" it.
        const int RulerOrder = -20, ClockOrder = -15, SinkOrder = -1, PlayerOrder = 10;

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

            RoomZone zone1 = BuildRoom1(level);
            BuildRoom2(level);
            ExitGoal exitGoal = BuildRoom3(level);
            Kill("OutOfBounds", new Vector2(-30f, -14f), new Vector2(60f, -12f), level);

            GameObject player = CreatePlayer(new Vector2(-4f, 0.6f));
            WorldClock clock = new GameObject("WorldClock").AddComponent<WorldClock>();
            Set(clock, "player", player.GetComponent<Rigidbody2D>());
            Set(clock, "startingRoom", zone1);

            Camera camera = CreateCamera(player.transform);
            CreateHud(camera, exitGoal, player);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelScaffolder] Built " + ScenePath);
        }

        // ---------- Room 1: Sinking Wall (teaches: jumping moves time) ----------
        // A wall taller than a jump. It sinks by exactly as much as you rise, so jumping makes it duck under you.
        static RoomZone BuildRoom1(Transform level)
        {
            Transform room = Group("Room1_SinkingWall", level);
            Box("Floor", new Vector2(-7f, -6f), new Vector2(8f, 0f), Ground, room);
            Box("LeftWall", new Vector2(-8f, -6f), new Vector2(-7f, 8f), Ground, room);
            RoomZone zone = Zone("Zone1", new Vector2(-7f, -6f), new Vector2(8f, 10f), maxTime: 4f,
                respawn: new Vector2(-4f, 1f), parent: room);

            GameObject wall = Box("Wall1", new Vector2(1.7f, 0f), new Vector2(2.3f, 2.6f), WallColor, room, SinkOrder);
            Vector2 top = wall.transform.position;
            AddMover(wall, top, top + Vector2.down * 4f, 0f, 4f, zone);

            Ruler(zone, room, -7f, 8f);
            Clock(new Vector2(-1f, 5f), 2f, zone, room);
            return zone;
        }

        // ---------- Room 2: Future Bridge (teaches: freeze HIGH, then go low) ----------
        // The bridge lies at ground level over a pit but only exists from time 4. Climb the stairs (time 5),
        // hold Shift to freeze, drop onto the bridge and run across. Without freezing it vanishes as you drop.
        static void BuildRoom2(Transform level)
        {
            Transform room = Group("Room2_FutureBridge", level);
            for (int step = 1; step <= 5; step++)
            {
                float right = step == 5 ? 14f : 8f + step;
                Box("Step" + step, new Vector2(7f + step, -6f), new Vector2(right, step), Ground, room);
            }
            Box("FarFloor", new Vector2(22f, -6f), new Vector2(30f, 0f), Ground, room);
            Kill("PitKillZone", new Vector2(14f, -4f), new Vector2(22f, -3f), room);
            RoomZone zone = Zone("Zone2", new Vector2(8f, -6f), new Vector2(30f, 10f), maxTime: 6f,
                respawn: new Vector2(8.5f, 1.6f), parent: room);

            Transform bridge = Group("FutureBridge", room);
            Box("Deck", new Vector2(14f, -0.3f), new Vector2(22f, 0f), BridgeColor, bridge);
            TimeToggle toggle = bridge.gameObject.AddComponent<TimeToggle>();
            Set(toggle, "timeMin", 4f);
            Set(toggle, "timeMax", 999f);
            Set(toggle, "room", zone);

            Ruler(zone, room, 8f, 30f);
            Clock(new Vector2(18f, 6.5f), 2f, zone, room);
        }

        // ---------- Room 3: Past Gate (teaches: freeze LOW, then go high) ----------
        // The gate on the ledge is only open in the past (time below 2), and you cannot jump over it. Freeze on
        // the floor at time 0, climb up while frozen and walk through to the exit.
        static ExitGoal BuildRoom3(Transform level)
        {
            Transform room = Group("Room3_PastGate", level);
            Box("Floor", new Vector2(30f, -6f), new Vector2(48f, 0f), Ground, room);
            Box("Step", new Vector2(36f, 0f), new Vector2(38f, 1.5f), Ground, room);
            Box("Ledge", new Vector2(38f, 0f), new Vector2(48f, 3f), Ground, room);
            Box("RightWall", new Vector2(48f, -6f), new Vector2(49f, 9f), Ground, room);
            RoomZone zone = Zone("Zone3", new Vector2(30f, -6f), new Vector2(48f, 10f), maxTime: 6f,
                respawn: new Vector2(32f, 1f), parent: room);

            GameObject gate = Box("Gate", new Vector2(42f, 3f), new Vector2(42.6f, 6.5f), GateColor, room);
            TimeToggle toggle = gate.AddComponent<TimeToggle>();
            Set(toggle, "timeMin", 2f);
            Set(toggle, "timeMax", 999f);
            Set(toggle, "room", zone);

            GameObject exit = Box("Exit", new Vector2(45f, 3f), new Vector2(46f, 4.5f), ExitColor, room);
            exit.GetComponent<BoxCollider2D>().isTrigger = true;

            Ruler(zone, room, 30f, 48f);
            Clock(new Vector2(40f, 8f), 2f, zone, room);
            return exit.AddComponent<ExitGoal>();
        }

        // ---------- Player, camera, HUD ----------

        static GameObject CreatePlayer(Vector2 position)
        {
            GameObject player = new GameObject("Player") { tag = "Player" };
            player.transform.position = position;
            player.transform.localScale = new Vector3(0.8f, 1f, 1f);
            SpriteRenderer sprite = player.AddComponent<SpriteRenderer>();
            sprite.sprite = square;
            sprite.sharedMaterial = material;
            sprite.sortingOrder = PlayerOrder;

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
            player.AddComponent<TimeFreeze>();
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

        static void CreateHud(Camera camera, ExitGoal exitGoal, GameObject player)
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

            // Freeze meter: thin cyan bar under the time bar.
            RectTransform meter = UiRect("FreezeMeter", canvasGo.transform, new Color(0f, 0f, 0f, 0.6f));
            meter.anchorMin = meter.anchorMax = new Vector2(0.5f, 1f);
            meter.pivot = new Vector2(0.5f, 1f);
            meter.sizeDelta = new Vector2(300f, 12f);
            meter.anchoredPosition = new Vector2(0f, -62f);
            RectTransform meterFill = UiRect("Fill", meter, new Color(0.55f, 0.9f, 1f));
            meterFill.anchorMin = Vector2.zero;
            meterFill.anchorMax = Vector2.one;
            meterFill.offsetMin = meterFill.offsetMax = Vector2.zero;

            TimeHud hud = canvasGo.AddComponent<TimeHud>();
            Set(hud, "fill", fill);
            Set(hud, "freezeFill", meterFill);
            Set(hud, "freeze", player.GetComponent<TimeFreeze>());
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

        // ---------- time visuals ----------

        // One line per second at the feet height for that second, across the room.
        static void Ruler(RoomZone zone, Transform room, float xMin, float xMax)
        {
            Transform rulerRoot = Group("TimeRuler", room);
            var lines = new List<Object>();
            for (int second = 0; second <= Mathf.FloorToInt(zone.MaxTime); second++)
            {
                float y = GroundY - 0.5f + second;
                GameObject line = Box("Line" + second, new Vector2(xMin, y - 0.02f), new Vector2(xMax, y + 0.02f),
                    new Color(1f, 1f, 1f, 0.05f), rulerRoot, RulerOrder, collider: false);
                lines.Add(line.GetComponent<SpriteRenderer>());
            }
            TimeRuler ruler = rulerRoot.gameObject.AddComponent<TimeRuler>();
            Set(ruler, "lines", lines.ToArray());
            Set(ruler, "room", zone);
        }

        // A diamond clock face with a hand that turns once per 10 s of world time.
        static void Clock(Vector2 centre, float radius, RoomZone zone, Transform room)
        {
            GameObject face = Box("ClockFace", centre - Vector2.one * radius * 0.7f, centre + Vector2.one * radius * 0.7f,
                ClockColor, room, ClockOrder, collider: false);
            face.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

            Transform pivot = Group("ClockHand", room);
            pivot.position = centre;
            Box("Hand", centre + new Vector2(-0.08f, 0f), centre + new Vector2(0.08f, radius * 0.9f), HandColor, pivot,
                ClockOrder + 1, collider: false);
            ClockHand hand = pivot.gameObject.AddComponent<ClockHand>();
            Set(hand, "room", zone);
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

        static GameObject Box(string name, Vector2 min, Vector2 max, Color color, Transform parent,
            int sortingOrder = 0, bool collider = true)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            go.transform.localScale = new Vector3(max.x - min.x, max.y - min.y, 1f);
            SpriteRenderer sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = square;
            sprite.sharedMaterial = material;
            sprite.color = color;
            sprite.sortingOrder = sortingOrder;
            if (collider) go.AddComponent<BoxCollider2D>();
            return go;
        }

        static void Ledge(string name, float xMin, float xMax, float top, Transform parent)
        {
            Box(name, new Vector2(xMin, top - 0.3f), new Vector2(xMax, top), Ground, parent);
        }

        static void Kill(string name, Vector2 min, Vector2 max, Transform parent)
        {
            GameObject go = Box(name, min, max, new Color(1f, 0f, 0f, 0.15f), parent);
            go.GetComponent<BoxCollider2D>().isTrigger = true;
            go.AddComponent<Hazard>();
        }

        static RoomZone Zone(string name, Vector2 min, Vector2 max, float maxTime, Vector2 respawn, Transform parent)
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

            Set(zone, "groundY", GroundY);
            Set(zone, "maxTime", maxTime);
            Set(zone, "respawnPoint", spawn);
            return zone;
        }

        // Time objects snap straight to their target (no speed cap): the world is a pure function of your height.
        static void AddMover(GameObject go, Vector2 a, Vector2 b, float tStart, float tEnd, RoomZone room)
        {
            Rigidbody2D body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            TimeMover mover = go.AddComponent<TimeMover>();
            Set(mover, "pointA", a);
            Set(mover, "pointB", b);
            Set(mover, "timeStart", tStart);
            Set(mover, "timeEnd", tEnd);
            Set(mover, "room", room);
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
                case Object[] array:
                    property.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        property.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                    break;
                default: Debug.LogError($"[LevelScaffolder] Unsupported value for '{field}'"); return;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
