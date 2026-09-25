using NUnit.Framework;

namespace HeightIsTime.Tests
{
    public class TimeMathTests
    {
        const float Eps = 0.0001f;

        [Test]
        public void HeightToTime_AtGround_IsZero()
        {
            Assert.AreEqual(0f, TimeMath.HeightToTime(2f, 2f, 1f, 10f), Eps);
        }

        [Test]
        public void HeightToTime_ScalesWithSecondsPerMeter()
        {
            Assert.AreEqual(3f, TimeMath.HeightToTime(3f, 0f, 1f, 10f), Eps);
            Assert.AreEqual(6f, TimeMath.HeightToTime(3f, 0f, 2f, 10f), Eps);
        }

        [Test]
        public void HeightToTime_ClampsBelowGroundAndAboveMax()
        {
            Assert.AreEqual(0f, TimeMath.HeightToTime(-5f, 0f, 1f, 10f), Eps);
            Assert.AreEqual(10f, TimeMath.HeightToTime(50f, 0f, 1f, 10f), Eps);
        }

        [Test]
        public void Progress01_IsLinearInsideWindow_AndClampedOutside()
        {
            Assert.AreEqual(0f, TimeMath.Progress01(1f, 4f, 6f), Eps);
            Assert.AreEqual(0.5f, TimeMath.Progress01(5f, 4f, 6f), Eps);
            Assert.AreEqual(1f, TimeMath.Progress01(9f, 4f, 6f), Eps);
        }

        [Test]
        public void Progress01_ZeroLengthWindow_ActsAsStep()
        {
            Assert.AreEqual(0f, TimeMath.Progress01(4.99f, 5f, 5f), Eps);
            Assert.AreEqual(1f, TimeMath.Progress01(5f, 5f, 5f), Eps);
        }

        [Test]
        public void InWindow_IncludesEdges()
        {
            Assert.IsTrue(TimeMath.InWindow(6f, 6f, 10f));
            Assert.IsTrue(TimeMath.InWindow(10f, 6f, 10f));
            Assert.IsFalse(TimeMath.InWindow(5.99f, 6f, 10f));
        }

        [Test]
        public void InWindowWithHysteresis_StaysActiveJustOutsideWindow()
        {
            // Already active: a small dip below the window does not switch it off.
            Assert.IsTrue(TimeMath.InWindowWithHysteresis(5.9f, 6f, 10f, 0.2f, true));
            // Not active yet: it only turns on inside the real window.
            Assert.IsFalse(TimeMath.InWindowWithHysteresis(5.9f, 6f, 10f, 0.2f, false));
            // A big enough dip still switches it off.
            Assert.IsFalse(TimeMath.InWindowWithHysteresis(5.7f, 6f, 10f, 0.2f, true));
        }

        [Test]
        public void LiftTarget_IsNeutralWhenPlayerStandsOnIt()
        {
            // Room ground = player's standing height on the lift at its lowest point,
            // lift travel in meters == time window in seconds => standing still keeps it still.
            const float liftBottom = 0f, liftTop = 6f, standOffset = 1f;
            const float groundY = liftBottom + standOffset;
            for (float lift = 0f; lift <= 6f; lift += 0.5f)
            {
                float t = TimeMath.HeightToTime(lift + standOffset, groundY, 1f, 10f);
                float target = liftBottom + (liftTop - liftBottom) * TimeMath.Progress01(t, 0f, 6f);
                Assert.AreEqual(lift, target, Eps, $"lift at {lift}");
            }
        }
    }
}
