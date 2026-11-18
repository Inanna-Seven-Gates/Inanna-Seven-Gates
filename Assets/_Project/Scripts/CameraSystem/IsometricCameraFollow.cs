using UnityEngine;
using Inanna.Player;

namespace Inanna.CameraSystem
{
    /// <summary>
    /// Câmera de ângulo fixo que segue a Inanna suavemente.
    /// - Ângulo definido por pitch/yaw/distância.
    /// - Look-ahead: a câmera se desloca um pouco em pro mouse.
    /// - Shake() para impacto de golpes/parry.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] PlayerAim aim;

        [Header("Ângulo fixo")]
        [Range(30f, 80f)] [SerializeField] float pitch = 50f;
        [SerializeField] float yaw = 45f;
        [SerializeField] float distance = 20f;

        [Header("Lente")]
        [SerializeField] bool applyLens = true;
        [Tooltip("Perspectiva com FOV baixo = look 'Hades'. Ortográfica = isométrico clássico.")]
        [SerializeField] bool orthographic = false;
        [SerializeField] float fieldOfView = 30f;
        [SerializeField] float orthographicSize = 7f;

        [Header("Seguimento")]
        [SerializeField] float followSmoothTime = 0.15f;
        [Tooltip("Ponto focado acima dos pés.")]
        [SerializeField] float focusHeight = 1f;

        [Header("Look-ahead (mira)")]
        [SerializeField] bool useAimLookAhead = true;
        [Range(0f, 0.5f)] [SerializeField] float lookAheadFactor = 0.25f;
        [SerializeField] float maxLookAhead = 3f;
        [SerializeField] float lookAheadSmoothTime = 0.25f;

        Camera _cam;
        Vector3 _focus, _focusVel, _lookAhead, _lookAheadVel;
        float _shakeAmp, _shakeTime, _shakeDur;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            ApplyLens();
        }

        void Start() => SnapToTarget();

        void ApplyLens()
        {
            if (!applyLens) return;
            _cam.orthographic = orthographic;
            if (orthographic) _cam.orthographicSize = orthographicSize;
            else _cam.fieldOfView = fieldOfView;
        }

        [ContextMenu("Snap to target")]
        public void SnapToTarget()
        {
            if (!target) return;
            _focus = target.position + Vector3.up * focusHeight;
            _focusVel = _lookAhead = _lookAheadVel = Vector3.zero;
            Apply(0f);
        }

        public void Shake(float amplitude = 0.15f, float duration = 0.15f)
        {
            _shakeAmp = amplitude; _shakeDur = duration; _shakeTime = duration;
        }

        void LateUpdate()
        {
            if (!target) return;

            Vector3 desiredLook = Vector3.zero;
            if (useAimLookAhead && aim)
            {
                Vector3 d = aim.AimPoint - target.position; d.y = 0f;
                desiredLook = Vector3.ClampMagnitude(d * lookAheadFactor, maxLookAhead);
            }
            _lookAhead = Vector3.SmoothDamp(_lookAhead, desiredLook, ref _lookAheadVel, lookAheadSmoothTime);

            Vector3 targetFocus = target.position + Vector3.up * focusHeight + _lookAhead;
            _focus = Vector3.SmoothDamp(_focus, targetFocus, ref _focusVel, followSmoothTime);

            Apply(Time.deltaTime);
        }

        void Apply(float dt)
        {
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pos = _focus + rot * Vector3.back * distance;

            if (_shakeTime > 0f)
            {
                _shakeTime -= dt;
                float k = Mathf.Clamp01(_shakeTime / Mathf.Max(_shakeDur, 0.0001f));
                Vector2 r = Random.insideUnitCircle * (_shakeAmp * k);
                pos += rot * new Vector3(r.x, r.y, 0f);
            }

            transform.SetPositionAndRotation(pos, rot);
        }

        void OnValidate()
        {
            if (_cam == null) _cam = GetComponent<Camera>();
            if (_cam != null && Application.isPlaying) ApplyLens();
        }
    }
}
