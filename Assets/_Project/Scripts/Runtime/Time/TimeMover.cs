using UnityEngine;

namespace HeightIsTime
{
    /// <summary>
    /// Moves from <see cref="pointA"/> to <see cref="pointB"/> as time goes from <see cref="timeStart"/> to
    /// <see cref="timeEnd"/>. Used by the lift and the falling rock.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class TimeMover : TimeObject
    {
        [SerializeField] Vector2 pointA;
        [SerializeField] Vector2 pointB;
        [SerializeField] float timeStart;
        [SerializeField] float timeEnd = 10f;
        [SerializeField] AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("Top speed in m/s. 0 = snap straight to the target (rock). The lift uses a cap so it glides.")]
        [SerializeField] float maxSpeed;

        [Tooltip("Ignore target changes smaller than this (m). Keeps the lift from creeping.")]
        [SerializeField] float deadZone;

        Rigidbody2D body;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        protected override void ApplyTime(float time)
        {
            Vector2 target = TargetAt(time);
            Vector2 current = body.position;
            if ((target - current).magnitude <= deadZone) return;

            if (maxSpeed > 0f)
                target = Vector2.MoveTowards(current, target, maxSpeed * Time.fixedDeltaTime);
            body.MovePosition(target);
        }

        public Vector2 TargetAt(float time)
        {
            float progress = curve.Evaluate(TimeMath.Progress01(time, timeStart, timeEnd));
            return Vector2.LerpUnclamped(pointA, pointB, progress);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pointA, pointB);
            Gizmos.DrawWireSphere(pointA, 0.15f);
            Gizmos.DrawWireSphere(pointB, 0.15f);
        }
    }
}
