using UnityEngine;

namespace Inanna.Combat
{
    /// <summary>
    /// Alvo de teste: pisca de branco ao apanhar, leva knockback, "morre" e reaparece.
    /// Referência de como um inimigo reage ao Health.
    /// </summary>
    [RequireComponent(typeof(Health), typeof(CharacterController))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] float knockbackDecay = 30f;
        [SerializeField] float flashTime = 0.08f;
        [SerializeField] float respawnDelay = 2f;

        Health _health;
        CharacterController _cc;
        Renderer[] _renderers;
        MaterialPropertyBlock _mpb;
        Vector3 _knock;
        Vector3 _startPos;
        float _flashEnd;
        bool _flashing;

        void Awake()
        {
            _health = GetComponent<Health>();
            _cc = GetComponent<CharacterController>();
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _startPos = transform.position;
        }

        void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        void OnDamaged(DamageInfo info)
        {
            _knock = info.Direction * info.Knockback;
            _knock.y = 0f;
            SetFlash(true);
            _flashEnd = Time.time + flashTime;
        }

        void OnDied()
        {
            SetFlash(false);
            foreach (var r in _renderers) r.enabled = false;
            _cc.enabled = false;
            Invoke(nameof(Respawn), respawnDelay);
        }

        void Respawn()
        {
            transform.position = _startPos;
            _knock = Vector3.zero;
            _health.ResetHealth();
            foreach (var r in _renderers) r.enabled = true;
            _cc.enabled = true;
        }

        void Update()
        {
            if (_flashing && Time.time >= _flashEnd) SetFlash(false);

            if (_cc.enabled)
            {
                _cc.Move((_knock + Vector3.down * 2f) * Time.deltaTime);
                _knock = Vector3.MoveTowards(_knock, Vector3.zero, knockbackDecay * Time.deltaTime);
            }
        }

        void SetFlash(bool on)
        {
            _flashing = on;
            foreach (var r in _renderers)
            {
                if (on)
                {
                    _mpb.SetColor("_BaseColor", Color.white);
                    _mpb.SetColor("_Color", Color.white);
                    r.SetPropertyBlock(_mpb);
                }
                else r.SetPropertyBlock(null);
            }
        }
    }
}
