using UnityEngine;

namespace HeightIsTime
{
    /// <summary>A background clock hand: turns forward as you rise, backward as you fall.</summary>
    public class ClockHand : TimeObject
    {
        [SerializeField] float secondsPerTurn = 10f;

        protected override void ApplyTime(float time)
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, -360f * time / secondsPerTurn);
        }
    }
}
