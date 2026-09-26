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
        static readonly Color DangerColor = new Color(0.90f, 0.25f, 0.25f);
        static readonly Color ExitColor = new Color(0.30f, 0.85f, 0.40f);
        static readonly Color ClockColor = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color HandColor = new Color(1f, 0.75f, 0.3f, 0.5f);

        // Sorting: time lines and clocks behind everything, moving walls behind the floor so they sink "into" it.
        const int RulerOrder = -20, ClockOrder = -15, SinkOrder = -1, RockOrder = 1, PlayerOrder = 10;

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
            Kill("OutOfBounds", new Vector2(-30f, -14f), new Vector2(70f, -12f), level);

            GameObject player = CreatePlayer(new Vector2(-4f, 0.6f));
            WorldClock clock = new GameObject("WorldClock").AddComponent<WorldClock>();
            Set(clock, "player", player.GetComponent<Rigidbody2D>());
            Set(clock, "startingRoom", zone1);

            Camera camera = CreateCamera(player.transform);
            CreateHud(camera, exitGoal);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[LevelScaffolder] Built " + ScenePath);
        }

        // ---------- Room 1: Sinking Walls ----------
        // Walls taller than a jump. Each one sinks by exactly as much as you rise, so jumping makes it duck under
        // you. The red crusher does the opposite: it drops when you jump, so walk under it without jumping.
        static RoomZone BuildRoom1(Transform level)
        {
            Transform room = Group("Room1_SinkingWalls", level);
            Box("Floor", new Vector2(-7f, -6f), new Vector2(15f, 0f), Ground, room);
            Box("LeftWall", new Vector2(-8f, -6f), new Vector2(-7f, 7f), Ground, room);
            Box("Ceiling", new Vector2(-8f, 6f), new Vector2(15f, 7f), Ground, room);
            RoomZone zone = Zone("Zone1", new Vector2(-7f, -6f), new Vector2(15f, 7f), maxTime: 4f,
                respawn: new Vector2(-4f, 1f), parent: room);

            SinkingWall("Wall1", 2f, 2.6f, zone, room);
            SinkingWall("Wall2", 7f, 3.2f, zone, room);

            // Crusher hangs from the ceiling with a 1.3 m gap below it: enough to walk under, not to jump under.
            GameObject crusher = Box("Crusher", new Vector2(11.25f, 1.3f), new Vector2(12.75f, 6f), DangerColor,
                room, SinkOrder);
            crusher.GetComponent<BoxCollider2D>().isTrigger = true;
            crusher.AddComponent<Hazard>();
            Vector2 top = crusher.transform.position;
            AddMover(crusher, top, top + Vector2.down * 4f, 0f, 4f, zone);

            Ruler(zone, room, -7f, 15f);
            Clock(new Vector2(4.5f, 3.5f), 2.2f, zone, room);
            return zone;
        }

        static void SinkingWall(string name, float x, float height, RoomZone zone, Transform room)
        {
            GameObject wall = Box(name, new Vector2(x - 0.3f, 0f), new Vector2(x + 0.3f, height), WallColor, room,
                SinkOrder);
            Vector2 top = wall.transform.position;
            AddMover(wall, top, top + Vector2.down * 4f, 0f, 4f, zone);
        }

        // ---------- Room 2: Growing Bridge ----------
        // A staircase of 1 m steps. Each step you climb adds one bridge piece over the pit (piece i needs time
        // i - 0.5), so by the top step (time 6) the bridge is complete. Fall in and it vanishes.
        static void BuildRoom2(Transform level)
        {
            Transform room = Group("Room2_GrowingBridge", level);
            for (int step = 1; step <= 6; step++)
            {
                float right = step == 6 ? 23f : 15f + step;
                Box("Step" + step, new Vector2(15f + step - 1, -6f), new Vector2(right, step), Ground, room);
            }
            Box("EndLedge", new Vector2(35f, -6f), new Vector2(38f, 6f), Ground, room);
            Kill("PitKillZone", new Vector2(23f, -4f), new Vector2(35f, -3f), room);
            RoomZone zone = Zone("Zone2", new Vector2(15f, -6f), new Vector2(39.5f, 12f), maxTime: 8f,
                respawn: new Vector2(15.5f, 1.7f), parent: room);

            Transform bridge = Group("GrowingBridge", room);
            for (int piece = 1; piece <= 6; piece++)
            {
                float left = 23f + (piece - 1) * 2f;
                GameObject part = Box("Piece" + piece, new Vector2(left, 5.7f), new Vector2(left + 2f, 6f),
                    BridgeColor, bridge);
                TimeToggle toggle = part.AddComponent<TimeToggle>();
                Set(toggle, "timeMin", piece - 0.5f);
                Set(toggle, "timeMax", 999f);
                Set(toggle, "room", zone);
            }

            Ruler(zone, room, 15f, 39.5f);
            Clock(new Vector2(29f, 9.5f), 2.2f, zone, room);
        }

        // ---------- Room 3: Mirror Rock ----------
        // The rock mirrors you: climb 1 m and it drops 1 m (rock centre = 10.5 - time), so you would meet at 5 m.
        // The main shaft (right) has the obvious ledges and is a trap between 3 and 7 m. The side passage (left)
        // is walled off from the rock between 3 and 7 m, so climb that while the rock passes, then cross over.
        static ExitGoal BuildRoom3(Transform level)
        {
            Transform room = Group("Room3_MirrorRock", level);
            Box("Floor", new Vector2(38f, -6f), new Vector2(49f, 0f), Ground, room);
            Box("LeftWall", new Vector2(39f, 1.8f), new Vector2(40f, 14f), Ground, room);
            Box("RightWall", new Vector2(46f, -6f), new Vector2(49f, 9.8f), Ground, room);
            Box("UpperRightWall", new Vector2(49f, 9.8f), new Vector2(50f, 14f), Ground, room);
            Box("Ceiling", new Vector2(39f, 13f), new Vector2(50f, 14f), Ground, room);
            Box("Divider", new Vector2(42.8f, 3f), new Vector2(43.2f, 7f), Ground, room);

            // Side passage (safe).
            Ledge("Side1", 40f, 41.2f, 1.4f, room);
            Ledge("Side2", 41.6f, 42.8f, 2.8f, room);
            Ledge("Side3", 40f, 41.2f, 4.2f, room);
            Ledge("Side4", 41.6f, 42.8f, 5.6f, room);
            Ledge("Side5", 40f, 41.2f, 7.0f, room);
            // Main shaft (trap between 3 and 7 m).
            Ledge("Main1", 44.5f, 46f, 1.4f, room);
            Ledge("Main2", 43.2f, 44.6f, 2.8f, room);
            Ledge("Main3", 44.6f, 46f, 4.2f, room);
            Ledge("Main4", 43.2f, 44.6f, 5.6f, room);
            // Above the divider, back in the main shaft.
            Ledge("Upper", 43.2f, 44.8f, 8.4f, room);

            RoomZone zone = Zone("Zone3", new Vector2(39.5f, -6f), new Vector2(50f, 14f), maxTime: 10f,
                respawn: new Vector2(42.2f, 1f), parent: room);

            GameObject rock = Box("Rock", new Vector2(43.3f, 10f), new Vector2(45.9f, 11f), DangerColor, room,
                RockOrder);
            rock.GetComponent<BoxCollider2D>().isTrigger = true;
            rock.AddComponent<Hazard>();
            AddMover(rock, new Vector2(44.6f, 10.5f), new Vector2(44.6f, 0.5f), 0f, 10f, zone);
            // Ghost: where the rock ends up at time 10.
            Ghost("RockGhost", new Vector2(43.3f, 0f), new Vector2(45.9f, 1f), DangerColor, room);

            GameObject exit = Box("Exit", new Vector2(47f, 9.8f), new Vector2(48f, 11.3f), ExitColor, room);
            exit.GetComponent<BoxCollider2D>().isTrigger = true;

            Ruler(zone, room, 39.5f, 50f);
            Clock(new Vector2(53f, 7f), 2.5f, zone, room);
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

        static void Ghost(string name, Vector2 min, Vector2 max, Color color, Transform parent)
        {
            color.a = 0.12f;
            Box(name, min, max, color, parent, RulerOrder + 1, collider: false);
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
