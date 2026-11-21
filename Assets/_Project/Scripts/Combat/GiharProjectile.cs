using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inanna.Combat
{
    public struct GiharFlightSettings
    {
        public float Speed;
        public float HitRadius;
        public float BaseDamage;
        public float DamageFalloffPerHit;
        public float MinDamage;
        public float Knockback;
        public bool HitOnReturn;
        public float MaxFlightTime;
        public float CatchDistance;
        public float SpinSpeed;
        public LayerMask TargetMask;
    }

    /// <summary>
    /// Um gihar em voo. Vai até cada alvo pela menor distância, volta até a
    /// Inanna e avisa que retornou. Causa dano em tudo que atravessa.
    /// Dano: BaseDamage, diminuindo FalloffPerHit a cada inimigo atingido, até MinDamage.
    /// </summary>
    public class GiharProjectile : MonoBehaviour
    {
        enum Phase { Outbound, Return, Done }

        struct Waypoint
        {
            public bool HasCollider;
            public Collider Col;
            public IDamageable Dmg;
            public Vector3 Point;
        }

        Transform _owner;
        Transform _visual;
        GiharFlightSettings _s;
        readonly List<Waypoint> _waypoints = new();
        int _index;
        Phase _phase;
        float _flightTime;
        int _hitCount;
        Action<GiharProjectile> _onReturned;
        Action _onHit;

        readonly HashSet<IDamageable> _hitOut = new();
        readonly HashSet<IDamageable> _hitBack = new();
        readonly Collider[] _buf = new Collider[32];

        public void Launch(Transform owner, Transform visual, Vector3 startPos, List<Collider> targets,
                            Vector3 straightPoint, GiharFlightSettings settings,
                            Action<GiharProjectile> onReturned, Action onHit)
        {
            _owner = owner;
            _visual = visual;
            _s = settings;
            _onReturned = onReturned;
            _onHit = onHit;
            transform.position = startPos;

            if (targets != null && targets.Count > 0)
            {
                foreach (var c in targets)
                {
                    if (!c) continue;
                    var d = c.GetComponentInParent<IDamageable>();
                    if (d == null) continue;
                    _waypoints.Add(new Waypoint { HasCollider = true, Col = c, Dmg = d });
                }
            }
            if (_waypoints.Count == 0)
                _waypoints.Add(new Waypoint { HasCollider = false, Point = straightPoint });

            _phase = Phase.Outbound;
        }

        void Update()
        {
            if (_phase == Phase.Done) return;
            if (!_owner) { Destroy(gameObject); return; }

            _flightTime += Time.deltaTime;
            if (_visual) _visual.Rotate(Vector3.up, _s.SpinSpeed * Time.deltaTime, Space.Self);

            if (_phase == Phase.Outbound)
            {
                if (_flightTime > _s.MaxFlightTime || !TryGetDestination(out Vector3 dest))
                {
                    _phase = Phase.Return;
                }
                else
                {
                    MoveToward(dest);
                    if ((dest - transform.position).sqrMagnitude < 0.09f) _index++;
                }
            }

            if (_phase == Phase.Return)
            {
                Vector3 home = _owner.position + Vector3.up;
                MoveToward(home);
                if ((home - transform.position).magnitude <= _s.CatchDistance)
                {
                    Finish();
                    return;
                }
            }

            HitCheck();
        }

        bool TryGetDestination(out Vector3 dest)
        {
            while (_index < _waypoints.Count)
            {
                Waypoint w = _waypoints[_index];
                if (w.HasCollider)
                {
                    if (!w.Col || !w.Col.enabled || w.Dmg.IsDead) { _index++; continue; }
                    dest = w.Col.bounds.center;
                    return true;
                }
                dest = w.Point;
                return true;
            }
            dest = default;
            return false;
        }

        void MoveToward(Vector3 dest)
        {
            Vector3 d = dest - transform.position;
            float step = _s.Speed * Time.deltaTime;
            if (d.magnitude <= step) transform.position = dest;
            else transform.position += d.normalized * step;
        }

        void HitCheck()
        {
            if (_phase == Phase.Return && !_s.HitOnReturn) return;
            var set = _phase == Phase.Outbound ? _hitOut : _hitBack;

            int n = Physics.OverlapSphereNonAlloc(transform.position, _s.HitRadius, _buf,
                                                    _s.TargetMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider col = _buf[i];
                var dmg = col.GetComponentInParent<IDamageable>();
                if (dmg == null || dmg.IsDead || set.Contains(dmg)) continue;

                set.Add(dmg);

                float amount = Mathf.Max(_s.MinDamage, _s.BaseDamage - _s.DamageFalloffPerHit * _hitCount);
                _hitCount++;

                Vector3 dir = col.bounds.center - transform.position;
                dir.y = 0f;
                dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : _owner.forward;

                dmg.TakeDamage(new DamageInfo(amount, transform.position, dir, _s.Knockback, _owner.gameObject));
                _onHit?.Invoke();
            }
        }

        void Finish()
        {
            _phase = Phase.Done;
            _onReturned?.Invoke(this);
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            // Se for destruído sem ter voltado, não deixa o jogador sem gihar.
            if (_phase != Phase.Done && _onReturned != null && _owner)
            {
                _phase = Phase.Done;
                _onReturned.Invoke(this);
            }
        }
    }
}
