using UnityEngine;

namespace HiddenBull.UrpStyle
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HiddenBull/HiddenBull Style/Liquid/HB Style Liquid Floater")]
    public sealed class HBStyleLiquidFloater : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How far below the surface this object's pivot rides, in metres. Negative values lift it " +
                 "above the liquid.")]
        float m_Draft;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("How much the object leans with the slope of the waves. 0 keeps it upright, 1 follows the " +
                 "surface fully.")]
        float m_Tilt = 0.5f;

        [SerializeField]
        [Tooltip("The footprint of the object on the liquid, in metres, along its own X and Z. The liquid is " +
                 "read at its four corners, so a large object rides over small waves instead of following " +
                 "each one.")]
        Vector2 m_Size = Vector2.one;

        [SerializeField]
        [Tooltip("Where the middle of the footprint sits relative to the pivot, in metres, along the object's " +
                 "own X and Z. Use it when the pivot is not in the middle of the hull.")]
        Vector2 m_Offset;

        Quaternion m_Lean = Quaternion.identity;

        public float draft
        {
            get => m_Draft;
            set => m_Draft = value;
        }

        public float tilt
        {
            get => m_Tilt;
            set => m_Tilt = Mathf.Clamp01(value);
        }

        public Vector2 size
        {
            get => m_Size;
            set => m_Size = Vector2.Max(value, Vector2.zero);
        }

        public Vector2 offset
        {
            get => m_Offset;
            set => m_Offset = value;
        }

        public bool afloat { get; private set; }

        void OnEnable()
        {
            m_Lean = Quaternion.identity;
        }

        void OnDisable()
        {
            transform.rotation = Quaternion.Inverse(m_Lean) * transform.rotation;
            m_Lean = Quaternion.identity;
        }

        void LateUpdate()
        {
            var level = Quaternion.Inverse(m_Lean) * transform.rotation;
            var forward = Vector3.ProjectOnPlane(level * Vector3.forward, Vector3.up);

            forward = forward.sqrMagnitude < 1e-6f ? Vector3.forward : forward.normalized;

            var right = Vector3.Cross(Vector3.up, forward);
            var position = transform.position;
            var middle = position + right * m_Offset.x + forward * m_Offset.y;
            var along = forward * (m_Size.y * 0.5f);
            var across = right * (m_Size.x * 0.5f);

            afloat = HBStyleLiquid.TryGetUncutHeight(middle + along + across, out var frontRight)
                   & HBStyleLiquid.TryGetUncutHeight(middle + along - across, out var frontLeft)
                   & HBStyleLiquid.TryGetUncutHeight(middle - along + across, out var backRight)
                   & HBStyleLiquid.TryGetUncutHeight(middle - along - across, out var backLeft);

            if (!afloat)
                return;

            var pitch = (frontRight + frontLeft - backRight - backLeft) * 0.5f / Mathf.Max(m_Size.y, 1e-3f) * m_Tilt;
            var roll = (frontRight + backRight - frontLeft - backLeft) * 0.5f / Mathf.Max(m_Size.x, 1e-3f) * m_Tilt;
            var up = Vector3.Cross(forward + Vector3.up * pitch, right + Vector3.up * roll).normalized;

            position.y = (frontRight + frontLeft + backRight + backLeft) * 0.25f
                       - pitch * m_Offset.y - roll * m_Offset.x - m_Draft;

            m_Lean = Quaternion.FromToRotation(Vector3.up, up);

            transform.SetPositionAndRotation(position, m_Lean * Quaternion.LookRotation(forward, Vector3.up));
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 0.7f, 0.95f, 0.8f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(m_Offset.x, 0f, m_Offset.y), new Vector3(m_Size.x, 0f, m_Size.y));
        }
    }
}
