using System;
using System.Collections;
using System.IO;
using System.Linq;
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

        static Transform Find(string name) => GameObject.Find(name).transform;
        static float Top(string name) => GameObject.Find(name).GetComponent<BoxCollider2D>().bounds.max.y;
        static float Now => WorldClock.Instance.CurrentTime;

        // ---------- Room 1: Sinking Walls ----------

        [UnityTest]
        public IEnumerator Room1_JumpingSinksTheWallAndCarriesYouOverIt()
        {
            yield return LoadLevel();
            yield return Teleport(-1f, 0.6f);
            Assert.Greater(Top("Wall1"), 2.5f, "wall starts taller than a jump");

            Press(keyboard.dKey);
            while (player.position.x < -0.3f) yield return new WaitForFixedUpdate();
            Press(keyboard.spaceKey);
            float lowestTop = float.MaxValue;
            bool shot = false;
            for (float t = 0f; t < 1.2f; t += Time.fixedDeltaTime)
            {
                lowestTop = Mathf.Min(lowestTop, Top("Wall1"));
                if (!shot && Now > 1.8f) { Screenshot("room1-wall-sinks-mid-jump"); shot = true; }
                if (t > 0.45f) Release(keyboard.spaceKey);
                yield return new WaitForFixedUpdate();
            }
            Release(keyboard.dKey);

            Assert.Less(lowestTop, 1f, "wall sank while the player was in the air");
            Assert.Greater(player.position.x, 3f, "player got over wall 1");
            Assert.AreEqual(0, deaths);
            yield return Wait(0.5f);
            Assert.Greater(Top("Wall1"), 2.5f, "wall rose back after landing");
        }

        [UnityTest]
        public IEnumerator Room1_WalkingUnderTheCrusherIsSafe()
        {
            yield return LoadLevel();
            yield return Teleport(9.5f, 0.6f);
            Press(keyboard.dKey);
            yield return Wait(1.2f);
            Release(keyboard.dKey);
            Assert.Greater(player.position.x, 13.5f);
            Assert.AreEqual(0, deaths);
        }

        [UnityTest]
        public IEnumerator Room1_JumpingUnderTheCrusherKills()
        {
            yield return LoadLevel();
            yield return Teleport(12f, 0.6f);
            yield return Jump();
            yield return Wait(0.3f);
            Assert.GreaterOrEqual(deaths, 1);
        }

        // ---------- Room 2: Growing Bridge ----------

        [UnityTest]
        public IEnumerator Room2_EachStepAddsOneBridgePiece()
        {
            yield return LoadLevel();
            for (int step = 1; step <= 6; step++)
            {
                float x = step == 6 ? 21f : 15f + step - 0.5f;
                yield return Teleport(x, step + 0.6f);
                yield return Wait(0.2f);
                int solid = Enumerable.Range(1, 6)
                    .Count(i => GameObject.Find("Piece" + i).GetComponent<BoxCollider2D>().enabled);
                Assert.AreEqual(step, solid, $"on step {step} (time {Now:0.0})");
                if (step == 3) Screenshot("room2-bridge-half-built");
            }
            Screenshot("room2-bridge-complete");
        }

        [UnityTest]
        public IEnumerator Room2_CompleteBridgeCanBeWalkedAcross()
        {
            yield return LoadLevel();
            yield return Teleport(15.5f, 1.6f); // enter room 2
            yield return Teleport(22f, 6.6f);
            Press(keyboard.dKey);
            yield return Wait(2.5f);
            Release(keyboard.dKey);
            Assert.Greater(player.position.x, 35f, "reached the end ledge");
            Assert.AreEqual(0, deaths);
        }

        // ---------- Room 3: Mirror Rock ----------

        [UnityTest]
        public IEnumerator Room3_RockMirrorsYourHeight()
        {
            yield return LoadLevel();
            yield return Teleport(42.2f, 1f); // enter room 3
            float[] ledges = { 1.4f, 4.2f, 7.0f };
            float[] xs = { 40.6f, 40.6f, 40.6f };
            for (int i = 0; i < ledges.Length; i++)
            {
                yield return Teleport(xs[i], ledges[i] + 0.6f);
                yield return Wait(0.2f);
                Assert.AreEqual(10.5f - ledges[i], Find("Rock").position.y, 0.15f, $"on ledge {ledges[i]}");
                if (i == 1) Screenshot("room3-rock-mirrors-you");
            }
            Assert.AreEqual(0, deaths);
        }

        [UnityTest]
        public IEnumerator Room3_SidePassageIsSafeWhileTheRockPasses()
        {
            yield return LoadLevel();
            yield return Teleport(42.2f, 1f);
            yield return Teleport(42.2f, 6.2f); // Side4, time 5.6: rock is right beside you
            yield return Wait(1f);
            Screenshot("room3-side-passage-rock-beside-you");
            Assert.AreEqual(0, deaths);
        }

        [UnityTest]
        public IEnumerator Room3_JumpingInTheMainShaftAtMeetingHeightKills()
        {
            yield return LoadLevel();
            yield return Teleport(42.2f, 1f);
            yield return Teleport(45.3f, 4.8f); // Main3, time 4.2
            Assert.AreEqual(0, deaths, "standing on Main3 is still safe");
            yield return Jump();
            yield return Wait(0.3f);
            Assert.GreaterOrEqual(deaths, 1);
        }

        [UnityTest]
        public IEnumerator Room3_JumpFromUpperLedgeReachesTheExit()
        {
            yield return LoadLevel();
            yield return Teleport(42.2f, 1f);
            yield return Teleport(44.2f, 9.0f); // Upper ledge, time 8.4
            Press(keyboard.dKey);
            yield return Jump();
            yield return Wait(1f);
            Release(keyboard.dKey);
            Assert.IsTrue(GameObject.Find("HUD").transform.Find("WinPanel").gameObject.activeSelf, "win panel");
            Assert.AreEqual(0, deaths);
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
