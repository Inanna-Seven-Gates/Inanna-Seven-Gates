using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Inanna.CameraSystem;
using Inanna.Player;

namespace Inanna.Combat
{
    /// <summary>
    /// Arremesso do gihar.
    ///  1. Segurar o botão direito: mostra o círculo de alcance e entra em modo de mira.
    ///  2. Com o direito segurado, clicar em inimigos dentro do alcance para marcá-los.
    ///  3. Soltar o direito: o gihar sai e visita os alvos pelo caminho mais próximo e depois volta como bumerangue.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class GiharThrow : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerAim aim;
        [SerializeField] PlayerMotor motor;
        [SerializeField] GiharLoadout loadout;
        [SerializeField] PlayerMelee melee;
        [SerializeField] IsometricCameraFollow cameraFollow;
        [SerializeField] Camera cam;

        [Header("Alvos")]
        [SerializeField] LayerMask targetMask;

        [Header("Alcance e marcação")]
        [Tooltip("Raio do círculo de alcance (GDD: 6).")]
        [SerializeField] float throwRange = 6f;
        [SerializeField] int maxMarks = 6;
        [Tooltip("Folga do clique: maior = mais fácil clicar no inimigo.")]
        [SerializeField] float markClickRadius = 0.4f;
        [Range(0f, 1f)] [SerializeField] float moveSpeedWhileAiming = 0.6f;

        [Header("Arremesso")]
        [Tooltip("Quantos gihars saem por arremesso (1 ou 2). Com 2, o segundo sai logo atrás do primeiro.")]
        [Range(1, 2)] [SerializeField] int giharsPerThrow = 1;
        [SerializeField] float secondGiharDelay = 0.12f;
        [Tooltip("Sem marcações: arremessa reto na direção da mira até o alcance máximo.")]
        [SerializeField] bool throwWithoutMarks = true;
        [SerializeField] float speed = 22f;
        [SerializeField] float hitRadius = 0.7f;
        [SerializeField] float baseDamage = 12f;
        [Tooltip("Quanto o dano cai a cada inimigo atingido (até o dano mínimo).")]
        [SerializeField] float damageFalloffPerHit = 1f;
        [SerializeField] float minDamage = 1f;
        [SerializeField] float knockback = 4f;
        [Tooltip("Também acerta na volta.")]
        [SerializeField] bool hitOnReturn = true;
        [SerializeField] float maxFlightTime = 5f;
        [SerializeField] float catchDistance = 0.8f;
        [SerializeField] float spinSpeed = 1080f;

        [Header("Feedback")]
        [SerializeField] float hitShakeAmplitude = 0.05f;
        [SerializeField] float hitShakeDuration = 0.08f;

        [Header("Visual (sem modelos por enquanto)")]
        [Tooltip("Material transparente (URP Unlit, Surface = Transparent). O editor cria o M_Overlay.")]
        [SerializeField] Material overlayMaterial;
        [SerializeField] Color rangeFillColor = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] Color rangeEdgeColor = new Color(1f, 1f, 1f, 0.45f);
        [SerializeField] Color markColor = new Color(1f, 0.3f, 0.2f, 0.9f);
        [SerializeField] float markFadeOutTime = 0.12f;
        [SerializeField] float rangeCircleYOffset = -0.9f;
        [Tooltip("Opcional: prefab do gihar. Se vazio, usa uma barra simples.")]
        [SerializeField] GameObject giharVisualPrefab;
        [SerializeField] Material giharMaterial;

        public bool IsAiming { get; private set; }

        class Mark
        {
            public Collider Col;
            public IDamageable Dmg;
            public TargetMarker Marker;
        }

        readonly List<Mark> _marks = new();
        GameObject _rangeCircle;
        Transform _fill, _edge;
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            if (!input) input = GetComponent<PlayerInputReader>();
            if (!aim) aim = GetComponent<PlayerAim>();
            if (!motor) motor = GetComponent<PlayerMotor>();
            if (!loadout) loadout = GetComponent<GiharLoadout>();
            if (!melee) melee = GetComponent<PlayerMelee>();
            if (!cam) cam = Camera.main;
            if (!cameraFollow && cam) cameraFollow = cam.GetComponent<IsometricCameraFollow>();
            _mpb = new MaterialPropertyBlock();
            BuildRangeCircle();
        }

        void Update()
        {
            if (!IsAiming)
            {
                bool meleeBusy = melee && melee.IsAttacking;
                if (input.ThrowPressed && loadout.CanThrow && !meleeBusy) BeginAim();
                return;
            }

            if (!loadout.CanThrow) { EndAim(); return; }

            UpdateMarks();
            UpdateRangeCircle();
            if (input.AttackPressed) TryToggleMark();

            if (!input.ThrowHeld) Release();
        }

        // ---------------- Mira ----------------
        void BeginAim()
        {
            IsAiming = true;
            motor.SpeedMultiplier = moveSpeedWhileAiming;
            if (_rangeCircle) _rangeCircle.SetActive(true);
            UpdateRangeCircle();
        }

        void EndAim()
        {
            IsAiming = false;
            motor.SpeedMultiplier = 1f;
            if (_rangeCircle) _rangeCircle.SetActive(false);
            ClearMarks();
        }

        void ClearMarks()
        {
            foreach (var m in _marks) if (m.Marker) m.Marker.Remove();
            _marks.Clear();
        }

        void TryToggleMark()
        {
            if (!cam) return;
            Ray ray = cam.ScreenPointToRay(input.PointerPosition);
            if (!Physics.SphereCast(ray, markClickRadius, out RaycastHit hit, 200f, targetMask, QueryTriggerInteraction.Ignore))
                return;

            var dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg == null || dmg.IsDead) return;

            // já marcado -> desmarca
            for (int i = 0; i < _marks.Count; i++)
            {
                if (_marks[i].Dmg != dmg) continue;
                if (_marks[i].Marker) _marks[i].Marker.Remove();
                _marks.RemoveAt(i);
                return;
            }

            if (_marks.Count >= maxMarks) return;
            if (!InRange(hit.collider)) return;

            Collider col = hit.collider;
            _marks.Add(new Mark
            {
                Col = col,
                Dmg = dmg,
                Marker = overlayMaterial ? TargetMarker.Create(col, overlayMaterial, markColor, markFadeOutTime) : null
            });
        }

        void UpdateMarks()
        {
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                Mark m = _marks[i];
                bool invalid = !m.Col || !m.Col.enabled || m.Dmg.IsDead || !InRange(m.Col);
                if (!invalid) continue;
                if (m.Marker) m.Marker.Remove();
                _marks.RemoveAt(i);
            }
        }

        bool InRange(Collider col)
        {
            Vector3 d = col.bounds.center - transform.position;
            d.y = 0f;
            return d.magnitude <= throwRange;
        }

        // ---------------- Lançamento ----------------
        void Release()
        {
            UpdateMarks();

            var targets = OrderByNearestPath();
            bool hasMarks = targets.Count > 0;

            // limpa mira e marcações
            EndAim();

            if (!hasMarks && !throwWithoutMarks) return;

            int count = Mathf.Min(giharsPerThrow, loadout.InHand);
            if (!loadout.TryTake(count)) return;

            Vector3 straight = transform.position + Vector3.up + aim.AimDirection * throwRange;
            StartCoroutine(LaunchRoutine(targets, straight, count));
        }

        /// <summary>Caminho mais próximo: Inanna → alvo mais perto -> o mais perto dele -> ...</summary>
        List<Collider> OrderByNearestPath()
        {
            var remaining = new List<Collider>();
            foreach (var m in _marks) remaining.Add(m.Col);

            var ordered = new List<Collider>();
            Vector3 cur = transform.position;
            while (remaining.Count > 0)
            {
                int best = 0;
                float bestD = float.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    float d = (remaining[i].bounds.center - cur).sqrMagnitude;
                    if (d < bestD) { bestD = d; best = i; }
                }
                ordered.Add(remaining[best]);
                cur = remaining[best].bounds.center;
                remaining.RemoveAt(best);
            }
            return ordered;
        }

        IEnumerator LaunchRoutine(List<Collider> targets, Vector3 straightPoint, int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnGihar(targets, straightPoint);
                if (i < count - 1) yield return new WaitForSeconds(secondGiharDelay);
            }
        }

        void SpawnGihar(List<Collider> targets, Vector3 straightPoint)
        {
            var root = new GameObject("GiharProjectile");

            Transform visual;
            if (giharVisualPrefab)
            {
                visual = Instantiate(giharVisualPrefab, root.transform).transform;
                visual.localPosition = Vector3.zero;
            }
            else
            {
                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(bar.GetComponent<Collider>());
                bar.transform.SetParent(root.transform, false);
                bar.transform.localScale = new Vector3(0.12f, 0.12f, 0.9f);
                if (giharMaterial) bar.GetComponent<Renderer>().sharedMaterial = giharMaterial;
                visual = bar.transform;
            }

            var settings = new GiharFlightSettings
            {
                Speed = speed,
                HitRadius = hitRadius,
                BaseDamage = baseDamage,
                DamageFalloffPerHit = damageFalloffPerHit,
                MinDamage = minDamage,
                Knockback = knockback,
                HitOnReturn = hitOnReturn,
                MaxFlightTime = maxFlightTime,
                CatchDistance = catchDistance,
                SpinSpeed = spinSpeed,
                TargetMask = targetMask
            };

            var proj = root.AddComponent<GiharProjectile>();
            proj.Launch(transform, visual, transform.position + Vector3.up + aim.AimDirection * 0.6f,
                        new List<Collider>(targets), straightPoint, settings,
                        p => loadout.ReturnOne(),
                        () => { if (cameraFollow) cameraFollow.Shake(hitShakeAmplitude, hitShakeDuration); });
        }

        // ---------------- Círculo de alcance ----------------
        void BuildRangeCircle()
        {
            if (!overlayMaterial) return;

            _rangeCircle = new GameObject("ThrowRangeCircle");
            _rangeCircle.transform.SetParent(transform, false);

            _fill = MakeLayer("Fill", OverlayMeshes.Disc(), rangeFillColor);
            _edge = MakeLayer("Edge", OverlayMeshes.Ring(0.97f), rangeEdgeColor);
            _rangeCircle.SetActive(false);
        }

        Transform MakeLayer(string name, Mesh mesh, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_rangeCircle.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = overlayMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            _mpb.SetColor("_BaseColor", color);
            _mpb.SetColor("_Color", color);
            r.SetPropertyBlock(_mpb);
            return go.transform;
        }

        void UpdateRangeCircle()
        {
            if (!_rangeCircle) return;
            // fica no chão sem girar junto com a Inanna
            _rangeCircle.transform.position = transform.position + Vector3.up * rangeCircleYOffset;
            _rangeCircle.transform.rotation = Quaternion.identity;
            Vector3 s = new Vector3(throwRange, 1f, throwRange);
            _fill.localScale = s;
            _edge.localScale = s;
        }

        void OnDisable()
        {
            if (IsAiming && motor) motor.SpeedMultiplier = 1f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, throwRange);
        }
    }
}
