using UnityEngine;

namespace Inanna.Player
{
    /// <summary>
    /// Movimentação e rotação da Inanna.
    /// - Movimento relativo à câmera.
    /// - Rotação: segue a direção do movimento; vira para a mira ao atacar / segurar o arremesso.
    /// - Hooks para outros sistemas (combate, dash, perda dos "me's"): SpeedMultiplier, MovementLocked,
    ///   RotationLocked, FaceDirectionThisFrame(), SetDashVelocity()/EndDash().
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerAim aim;
        [SerializeField] Camera cam;

        [Header("Movimento")]
        [SerializeField] float moveSpeed = 6f;
        [Tooltip("Aceleração/desaceleração em unidades/s².")]
        [SerializeField] float acceleration = 60f;
        [SerializeField] float gravity = -25f;

        [Header("Rotação")]
        [Tooltip("Graus por segundo.")]
        [SerializeField] float turnSpeed = 1080f;
        [Tooltip("Por quanto tempo a Inanna olha para a mira depois de apertar o ataque.")]
        [SerializeField] float faceAimAfterAttack = 0.3f;
        [Tooltip("Sempre olhando para o mouse.")]
        [SerializeField] bool alwaysFaceAim = false;

        // ---- Hooks públicos ----
        public float SpeedMultiplier { get; set; } = 1f;
        public bool MovementLocked { get; set; }
        public bool RotationLocked { get; set; }
        public Vector3 Velocity => _horizontalVel;
        /// <summary>Direção de movimento desejada.</summary>
        public Vector3 MoveDirection { get; private set; }
        public bool IsDashing => _dashActive;

        CharacterController _cc;
        Vector3 _horizontalVel;
        float _verticalVel;
        float _faceAimUntil;
        Vector3 _facingOverride;
        bool _hasFacingOverride;
        Vector3 _dashVel;
        bool _dashActive;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (!input) input = GetComponent<PlayerInputReader>();
            if (!aim) aim = GetComponent<PlayerAim>();
            if (!cam) cam = Camera.main;
        }

        /// <summary>Força a Inanna a olhar para 'dir' apenas neste frame.</summary>
        public void FaceDirectionThisFrame(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            _facingOverride = dir.normalized;
            _hasFacingOverride = true;
        }

        public void FaceAimFor(float seconds) => _faceAimUntil = Mathf.Max(_faceAimUntil, Time.time + seconds);

        /// <summary>Assume o controle da velocidade horizontal. Chame todo frame enquanto durar.</summary>
        public void SetDashVelocity(Vector3 v)
        {
            v.y = 0f;
            _dashVel = v;
            _dashActive = true;
        }

        public void EndDash()
        {
            if (!_dashActive) return;
            _dashActive = false;
            _horizontalVel = Vector3.ClampMagnitude(_dashVel, moveSpeed); // saída suave
            _dashVel = Vector3.zero;
        }

        void Update()
        {
            if (input.AttackPressed) FaceAimFor(faceAimAfterAttack);
            HandleMovement();
        }

        void LateUpdate() => HandleRotation();

        void HandleMovement()
        {
            if (_cc.isGrounded && _verticalVel < 0f) _verticalVel = -2f;
            else _verticalVel += gravity * Time.deltaTime;

            if (_dashActive)
            {
                _horizontalVel = _dashVel;
                MoveDirection = _dashVel.sqrMagnitude > 0.001f ? _dashVel.normalized : Vector3.zero;
                _cc.Move((_dashVel + Vector3.up * _verticalVel) * Time.deltaTime);
                return;
            }

            Vector2 raw = MovementLocked ? Vector2.zero : input.Move;

            // Eixos da câmera projetados no chão: W = para cima na tela, D = para a direita na tela.
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.transform.right; right.y = 0f; right.Normalize();

            Vector3 dir = fwd * raw.y + right * raw.x;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            MoveDirection = dir;

            Vector3 targetVel = dir * (moveSpeed * SpeedMultiplier);
            _horizontalVel = Vector3.MoveTowards(_horizontalVel, targetVel, acceleration * Time.deltaTime);

            _cc.Move((_horizontalVel + Vector3.up * _verticalVel) * Time.deltaTime);
        }

        void HandleRotation()
        {
            Vector3 face = Vector3.zero;

            if (_hasFacingOverride) face = _facingOverride;
            else if (alwaysFaceAim || input.ThrowHeld || Time.time < _faceAimUntil) face = aim.AimDirection;
            else if (_horizontalVel.sqrMagnitude > 0.05f) face = _horizontalVel;

            _hasFacingOverride = false;

            if (RotationLocked) return;
            face.y = 0f;
            if (face.sqrMagnitude < 0.0001f) return;

            Quaternion target = Quaternion.LookRotation(face.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }
    }
}
