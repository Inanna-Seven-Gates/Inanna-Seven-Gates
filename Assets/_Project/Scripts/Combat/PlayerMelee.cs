using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inanna.CameraSystem;
using Inanna.Player;

namespace Inanna.Combat
{
    /// <summary>
    /// Ataque corpo a corpo do gihar (botão esquerdo).
    /// - Só ataca se houver pelo menos 1 gihar na mão (GiharLoadout.CanMelee).
    /// - Ignora o clique enquanto o jogador está mirando o arremesso.
    /// - A direção do golpe é travada na mira no início do swing.
    /// </summary>
    public class PlayerMelee : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerAim aim;
        [SerializeField] GiharLoadout loadout;
        [SerializeField] GiharThrow thrower;
        [SerializeField] IsometricCameraFollow cameraFollow;

        [Header("Alvos")]
        [SerializeField] LayerMask targetMask;

        [Header("Golpe")]
        [SerializeField] float range = 1.2f;
        [Range(30f, 360f)] [SerializeField] float arcAngle = 120f;
        [SerializeField] float damage = 10f;
        [SerializeField] float knockback = 6f;

        [Header("Tempos (segundos)")]
        [SerializeField] float windup = 0.08f;
        [SerializeField] float active = 0.12f;
        [SerializeField] float recovery = 0.18f;
        [SerializeField] float inputBuffer = 0.2f;
        [Tooltip("Multiplicador de velocidade enquanto ataca.")]
        [Range(0f, 1f)] [SerializeField] float moveSpeedDuringAttack = 0.35f;

        [Header("Feedback")]
        [SerializeField] float hitStopDuration = 0.05f;
        [SerializeField] float shakeAmplitude = 0.08f;
        [SerializeField] float shakeDuration = 0.1f;
        [SerializeField] Material swingMaterial;
        [Tooltip("Altura do leque em relação ao pivô do jogador.")]
        [SerializeField] float swingVisualYOffset = -0.9f;

        public bool IsAttacking => _state != State.Idle;

        enum State { Idle, Windup, Active, Recovery }
        State _state = State.Idle;
        float _stateEnd;
        float _bufferedUntil = -1f;
        Vector3 _swingDir = Vector3.forward;
        bool _hitStopRunning, _shookThisSwing;

        readonly HashSet<IDamageable> _hitThisSwing = new();
        readonly Collider[] _buffer = new Collider[32];
        GameObject _visual;

        void Awake()
        {
            if (!input) input = GetComponent<PlayerInputReader>();
            if (!motor) motor = GetComponent<PlayerMotor>();
            if (!aim) aim = GetComponent<PlayerAim>();
            if (!loadout) loadout = GetComponent<GiharLoadout>();
            if (!thrower) thrower = GetComponent<GiharThrow>();
            if (!cameraFollow && Camera.main) cameraFollow = Camera.main.GetComponent<IsometricCameraFollow>();
            BuildVisual();
        }

        bool CanMelee => loadout == null || loadout.CanMelee;
        bool Blocked => thrower != null && thrower.IsAiming;

        void Update()
        {
            if (input.AttackPressed && !Blocked) _bufferedUntil = Time.time + inputBuffer;

            switch (_state)
            {
                case State.Idle:
                    if (Time.time < _bufferedUntil && CanMelee && !Blocked) StartSwing();
                    break;

                case State.Windup:
                    KeepFacing();
                    if (Time.time >= _stateEnd) EnterActive();
                    break;

                case State.Active:
                    KeepFacing();
                    DoHitCheck();
                    if (Time.time >= _stateEnd) EnterRecovery();
                    break;

                case State.Recovery:
                    if (Time.time >= _stateEnd) EnterIdle();
                    break;
            }
        }

        /// <summary>Interrompe o golpe.</summary>
        public void CancelAttack()
        {
            if (_state == State.Idle) return;
            ShowVisual(false);
            _bufferedUntil = -1f;
            EnterIdle();
        }

        // ---------------- Estados ----------------
        void StartSwing()
        {
            _bufferedUntil = -1f;
            _swingDir = aim.AimDirection;
            _state = State.Windup;
            _stateEnd = Time.time + windup;
            _shookThisSwing = false;
            motor.SpeedMultiplier = moveSpeedDuringAttack;
            KeepFacing();
        }

        void EnterActive()
        {
            _state = State.Active;
            _stateEnd = Time.time + active;
            _hitThisSwing.Clear();
            ShowVisual(true);
        }

        void EnterRecovery()
        {
            _state = State.Recovery;
            _stateEnd = Time.time + recovery;
            ShowVisual(false);
        }

        void EnterIdle()
        {
            _state = State.Idle;
            motor.SpeedMultiplier = 1f;
        }

        void KeepFacing() => motor.FaceDirectionThisFrame(_swingDir);

        // ---------------- Hit ----------------
        void DoHitCheck()
        {
            Vector3 origin = transform.position;
            int n = Physics.OverlapSphereNonAlloc(origin, range, _buffer, targetMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < n; i++)
            {
                Collider col = _buffer[i];
                var target = col.GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead || _hitThisSwing.Contains(target)) continue;

                Vector3 to = col.bounds.center - origin;
                to.y = 0f;
                float dist = to.magnitude;
                Vector3 dir = dist > 0.001f ? to / dist : _swingDir;

                // margem de 10° para compensar o tamanho do alvo
                if (dist > 0.6f && Vector3.Angle(_swingDir, dir) > arcAngle * 0.5f + 10f) continue;

                _hitThisSwing.Add(target);
                target.TakeDamage(new DamageInfo(damage, origin, dir, knockback, gameObject));
                OnHitLanded();
            }
        }

        void OnHitLanded()
        {
            if (_shookThisSwing) return;
            _shookThisSwing = true;
            if (cameraFollow) cameraFollow.Shake(shakeAmplitude, shakeDuration);
            if (hitStopDuration > 0f && !_hitStopRunning) StartCoroutine(HitStop());
        }

        IEnumerator HitStop()
        {
            _hitStopRunning = true;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1f;
            _hitStopRunning = false;
        }

        void OnDisable()
        {
            if (_hitStopRunning) { Time.timeScale = 1f; _hitStopRunning = false; }
            if (motor) motor.SpeedMultiplier = 1f;
        }

        // ---------------- Visual do golpe ----------------
        void BuildVisual()
        {
            if (!swingMaterial) return;

            _visual = new GameObject("SwingVisual");
            _visual.transform.SetParent(transform, false);
            _visual.transform.localPosition = new Vector3(0f, swingVisualYOffset, 0f);

            const int segments = 20;
            var verts = new Vector3[segments + 2];
            var tris = new int[segments * 3];
            verts[0] = Vector3.zero;
            float half = arcAngle * 0.5f * Mathf.Deg2Rad;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(-half, half, i / (float)segments);
                verts[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
            for (int i = 0; i < segments; i++)
            {
                tris[i * 3] = 0; tris[i * 3 + 1] = i + 1; tris[i * 3 + 2] = i + 2;
            }

            var mesh = new Mesh { name = "SwingFan" };
            mesh.vertices = verts;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            _visual.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = _visual.AddComponent<MeshRenderer>();
            mr.sharedMaterial = swingMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            _visual.SetActive(false);
        }

        void ShowVisual(bool show)
        {
            if (!_visual) return;
            if (show)
            {
                _visual.transform.rotation = Quaternion.LookRotation(_swingDir, Vector3.up);
                _visual.transform.localScale = new Vector3(range, 1f, range);
            }
            _visual.SetActive(show);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.9f);
            Vector3 fwd = Application.isPlaying ? _swingDir : transform.forward;
            Vector3 p = transform.position;
            Quaternion l = Quaternion.AngleAxis(-arcAngle * 0.5f, Vector3.up);
            Quaternion r = Quaternion.AngleAxis(arcAngle * 0.5f, Vector3.up);
            Gizmos.DrawLine(p, p + l * fwd * range);
            Gizmos.DrawLine(p, p + r * fwd * range);
            Gizmos.DrawWireSphere(p, range);
        }
    }
}
