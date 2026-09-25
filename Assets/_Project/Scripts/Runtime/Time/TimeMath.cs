using UnityEngine;

namespace HeightIsTime
{
    /// <summary>Pure math behind "height is time". No Unity objects, so it is unit-tested.</summary>
    public static class TimeMath
    {
        /// <summary>World time for a player at height <paramref name="y"/> in a room.</summary>
        public static float HeightToTime(float y, float groundY, float secondsPerMeter, float maxTime)
        {
            return Mathf.Clamp((y - groundY) * secondsPerMeter, 0f, maxTime);
        }

        /// <summary>0 before <paramref name="tStart"/>, 1 after <paramref name="tEnd"/>, linear in between.</summary>
        public static float Progress01(float t, float tStart, float tEnd)
        {
            if (tEnd <= tStart) return t >= tStart ? 1f : 0f;
            return Mathf.Clamp01((t - tStart) / (tEnd - tStart));
        }

        public static bool InWindow(float t, float min, float max)
        {
            return t >= min && t <= max;
        }

        /// <summary>
        /// Like <see cref="InWindow"/>, but once active it tolerates <paramref name="margin"/> seconds outside the
        /// window. Stops things like the bridge flickering when the player bobs at the threshold height.
        /// </summary>
        public static bool InWindowWithHysteresis(float t, float min, float max, float margin, bool wasActive)
        {
            return wasActive ? InWindow(t, min - margin, max + margin) : InWindow(t, min, max);
        }
    }
}
