using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HeightIsTime.Tests
{
    /// <summary>
    /// Plays the real level with simulated keys. Positions come from LevelScaffolder (floor top is y = 0, so a
    /// player standing on a ledge of height h has world time h). Set HIT_SCREENSHOT_DIR to save screenshots.
    /// </summary>
    public class LevelPlayTests : InputTestFixture
    {
        Keyboard keyboard;
        Rigidbody2D player;
        int deaths;

        IEnumerator LoadLevel()
        {
            keyboard = InputSystem.AddDevice<Keyboard>();
            SceneManager.LoadScene("HeightIsTime");
            yield return null;
            yield return new WaitForFixedUpdate();
            player = GameObject.Find("Player").GetComponent<Rigidbody2D>();
            deaths = 0;
            player.GetComponent<PlayerRespawn>().Respawned += () => deaths++;
        }

        IEnumerator Teleport(float x, float y)
        {
            player.linearVelocity = Vector2.zero;
            player.position = new Vector2(x, y);
            player.transform.position = new Vector3(x, y, 0f);
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
        }

        static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return new WaitForFixedUpdate();
        }

        IEnumerator Jump(float holdSeconds = 0.45f)
        {
            Press(keyboard.spaceKey);
            yield return Wait(holdSeconds);
            Release(keyboard.spaceKey);
        }

        // Holds right and jumps each time the player reaches one of the given x positions.
        IEnumerator RunRightJumpingAt(float timeout, params float[] jumpXs)
        {
            Press(keyboard.dKey);
            int next = 0;
            float end = Time.time + timeout;
            while (Time.time < end)
            {
                if (next < jumpXs.Length && player.position.x >= jumpXs[next])
                {
                    next++;
                    yield return Jump(0.35f);
                }
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.dKey);
        }

        static float Top(string name) => GameObject.Find(name).GetComponent<BoxCollider2D>().bounds.max.y;
        static bool Solid(string name) => GameObject.Find(name).GetComponent<BoxCollider2D>().enabled;
        static WorldClock Clock => WorldClock.Instance;
        bool Won => GameObject.Find("HUD").transform.Find("WinPanel").gameObject.activeSelf;

        // ---------- Room 1: Sinking Wall ----------

        [UnityTest]
        public IEnumerator Room1_JumpingSinksTheWallAndCarriesYouOverIt()
        {
            yield return LoadLevel();
            yield return Teleport(-1f, 0.6f);
            Assert.Greater(Top("Wall1"), 2.5f, "wall starts taller than a jump");

            Press(keyboard.dKey);
            while (player.position.x < -0.6f) yield return new WaitForFixedUpdate();
            Press(keyboard.spaceKey);
            float lowestTop = float.MaxValue;
            bool shot = false;
            for (float t = 0f; t < 1.2f; t += Time.fixedDeltaTime)
            {
                lowestTop = Mathf.Min(lowestTop, Top("Wall1"));
                if (!shot && Clock.CurrentTime > 1.8f) { Screenshot("room1-wall-sinks-mid-jump"); shot = true; }
                if (t > 0.45f) Release(keyboard.spaceKey);
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.dKey);

            Assert.Less(lowestTop, 1f, "wall sank while the player was in the air");
            Assert.Greater(player.position.x, 3f, "player got over the wall");
            Assert.AreEqual(0, deaths);
            yield return Wait(0.5f);
            Assert.Greater(Top("Wall1"), 2.5f, "wall rose back after landing");
        }

        [UnityTest]
        public IEnumerator Room1_WallCannotBeJumpedWhileFrozenAtGround()
        {
            yield return LoadLevel();
            yield return Teleport(-1f, 0.6f);
            Press(keyboard.leftShiftKey); // freeze at time 0: the wall stays full height
            yield return RunRightJumpingAt(1.5f, -0.6f);
            Release(keyboard.leftShiftKey);
            Assert.Less(player.position.x, 1.7f, "blocked by the wall");
        }

        // ---------- Room 2: Future Bridge ----------

        [UnityTest]
        public IEnumerator Room2_BridgeOnlyExistsFromTime4()
        {
            yield return LoadLevel();
            yield return Teleport(8.5f, 1.6f); // step 1
            Assert.IsFalse(Solid("Deck"), "time 1: no bridge");
            yield return Teleport(11.5f, 4.6f); // step 4
            Assert.IsTrue(Solid("Deck"), "time 4: bridge");
            Screenshot("room2-bridge-appears-from-the-stairs");
        }

        [UnityTest]
        public IEnumerator Room2_DroppingWithoutFreezeLosesTheBridge()
        {
            yield return LoadLevel();
            yield return Teleport(8.5f, 1.6f);
            yield return Teleport(13f, 5.6f); // top step, time 5
            Press(keyboard.dKey);
            yield return Wait(2f);
            Release(keyboard.dKey);
            Assert.GreaterOrEqual(deaths, 1, "fell through the vanished bridge");
        }

        [UnityTest]
        public IEnumerator Room2_FreezingHighLetsYouRunAcrossTheLowBridge()
        {
            yield return LoadLevel();
            yield return Teleport(8.5f, 1.6f);
            yield return Teleport(13f, 5.6f);
            Press(keyboard.leftShiftKey);
            yield return Wait(0.1f);
            Assert.IsTrue(Clock.IsFrozen);
            Press(keyboard.dKey);
            bool shot = false;
            float timeout = Time.time + 4f;
            while (player.position.x < 23f && deaths == 0 && Time.time < timeout)
            {
                if (!shot && player.position.x > 17f) { Screenshot("room2-frozen-running-on-bridge"); shot = true; }
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.dKey);
            Release(keyboard.leftShiftKey);
            Assert.AreEqual(0, deaths);
            Assert.Greater(player.position.x, 22.5f, "reached the far floor");
            yield return Wait(0.2f);
            Assert.IsFalse(Clock.IsFrozen);
            Assert.IsFalse(Solid("Deck"), "bridge gone again after unfreezing at ground level");
        }

        // ---------- Room 3: Past Gate ----------

        [UnityTest]
        public IEnumerator Room3_GateBlocksYouOnTheLedgeWithoutFreeze()
        {
            yield return LoadLevel();
            yield return Teleport(32f, 1f);
            yield return Teleport(39f, 3.6f); // on the ledge, time 3
            Press(keyboard.dKey);
            yield return Wait(1.5f);
            Release(keyboard.dKey);
            Assert.IsTrue(Solid("Gate"));
            Assert.Less(player.position.x, 41.7f, "blocked by the gate");
            Screenshot("room3-gate-closed-up-high");
        }

        [UnityTest]
        public IEnumerator Room3_FreezeLowThenClimbReachesTheExit()
        {
            yield return LoadLevel();
            yield return Teleport(32f, 1f);
            yield return Teleport(33f, 0.6f);
            Press(keyboard.leftShiftKey); // freeze at time 0: gate open
            yield return RunRightJumpingAt(3.5f, 35.2f, 37.2f);
            Release(keyboard.leftShiftKey);
            Assert.AreEqual(0, deaths);
            Assert.IsTrue(Won, "reached the exit through the open gate");
        }

        // ---------- Freeze meter ----------

        [UnityTest]
        public IEnumerator Freeze_RunsOutAndNeedsARelease()
        {
            yield return LoadLevel();
            TimeFreeze freeze = player.GetComponent<TimeFreeze>();
            Press(keyboard.leftShiftKey);
            yield return Wait(0.5f);
            Assert.IsTrue(Clock.IsFrozen);
            yield return Wait(4f);
            Assert.IsFalse(Clock.IsFrozen, "meter ran out");
            yield return Wait(1f);
            Assert.IsFalse(Clock.IsFrozen, "still held: stays unfrozen until released");
            Release(keyboard.leftShiftKey);
            yield return Wait(1f);
            Assert.Greater(freeze.Meter01, 0.1f, "meter refills");
        }

        // ---------- screenshots ----------

        void Screenshot(string name)
        {
            string dir = Environment.GetEnvironmentVariable("HIT_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(dir)) return;
            Directory.CreateDirectory(dir);

            Camera camera = Camera.main;
            Vector3 p = player.position;
            camera.transform.position = new Vector3(p.x, p.y + 1f, camera.transform.position.z);

            RenderTexture target = new RenderTexture(1280, 720, 24);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            camera.targetTexture = null;
            RenderTexture.active = previous;
            File.WriteAllBytes(Path.Combine(dir, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
            target.Release();
        }
    }
}
