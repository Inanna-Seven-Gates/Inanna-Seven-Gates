using UnityEngine;
using Inanna.Combat;

namespace Inanna.Player
{
    /// <summary>
    /// Esquiva (Espaço). Leva a Inanna na direção em que ela está andando.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class PlayerDash : MonoBehaviour
    {
        [Header("Referências (auto-preenchidas se vazias)")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor motor;
        [SerializeField] PlayerMelee melee;
        [SerializeField] Health health;

        [Header("Dash")]
        [SerializeField] float distance = 4.5f;
        [SerializeField] float duration = 0.18f;
        [SerializeField] float cooldown = 1f;

        [Header("I-frames (desligado por padrão)")]
        [SerializeField] bool enableIFrames = false;
        [Tooltip("Segundos após o início do dash em que a invulnerabilidade começa.")]
        [SerializeField] float iFrameStart = 0f;
        [Tooltip("Segundos após o início do dash em que a invulnerabilidade termina.")]
        [SerializeField] float iFrameEnd = 0.18f;

        public bool IsDashing { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, _readyAt - Time.time);

        Vector3 _dir;
        float _t;
        float _readyAt;

        void Awake()
        {
            if (!input) input = GetComponent<PlayerInputReader>();
            if (!motor) motor = GetComponent<PlayerMotor>();
            if (!melee) melee = GetComponent<PlayerMelee>();
            if (!health) health = GetComponent<Health>();
        }

        void Update()
        {
            if (IsDashing)
            {
                _t += Time.deltaTime;
                UpdateIFrames();

                if (_t >= duration) { EndDash(); return; }

                motor.SetDashVelocity(_dir * (distance / duration));
                motor.FaceDirectionThisFrame(_dir);
                return;
            }

            if (input.DodgePressed && Time.time >= _readyAt) BeginDash();
        }

        void BeginDash()
        {
            Vector3 d = motor.MoveDirection;
            if (d.sqrMagnitude < 0.01f) d = transform.forward;
            d.y = 0f;
            _dir = d.normalized;

            if (melee) melee.CancelAttack();

            IsDashing = true;
            _t = 0f;
            motor.SetDashVelocity(_dir * (distance / duration));
            UpdateIFrames();
        }

        void EndDash()
        {
            IsDashing = false;
            motor.EndDash();
            if (health) health.Invulnerable = false;
            _readyAt = Time.time + cooldown;
        }

        void UpdateIFrames()
        {
            if (!health) return;
            health.Invulnerable = enableIFrames && _t >= iFrameStart && _t <= iFrameEnd;
        }

        void OnDisable()
        {
            if (health) health.Invulnerable = false;
            if (motor) motor.EndDash();
        }
    }
}
