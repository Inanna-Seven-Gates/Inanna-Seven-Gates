using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Inanna.Combat
{
    public class GidimErranteAI : MonoBehaviour
    {
        private enum State
        {
            Idle,      
            Chase,
            Attack,
            Cooldown,
            Return,    
            Dead
        }

        [Header("Referências")]
        [SerializeField] private Transform player;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;
        [SerializeField] private Health health;

        [Header("Atributos")]
        [SerializeField] private float moveSpeed = 4.1f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private float knockback = 0f;

        [Header("Percepção")]
        [Tooltip("Distância em que o inimigo passa a perseguir o jogador.")]
        [SerializeField] private float detectionRange = 10f;
        [Tooltip("Distância em que o inimigo desiste e volta ao ponto de origem. Deve ser maior que a detecção.")]
        [SerializeField] private float loseInterestRange = 14f;

        [Header("Punhalada Reta")]
        [SerializeField] private float stabRange = 1.1f;
        [SerializeField] private float stabTelegraph = 0.45f;

        [Header("Corte Vertical")]
        [SerializeField] private float verticalRange = 1.2f;
        [SerializeField] private float verticalTelegraph = 0.55f;

        [Header("Combate")]
        [SerializeField] private float attackCooldown = 1.2f;
        [Tooltip("Meio-ângulo do cone de ataque (o leque total é o dobro).")]
        [SerializeField] private float attackAngle = 70f;

        [Header("Visualização da Hitbox / Telegraph")]
        [SerializeField] private bool showHitbox = true;
        [SerializeField] private float hitboxDuration = 0.15f;
        [Tooltip("Material transparente. Se vazio, usa Sprites/Default como quebra-galho.")]
        [SerializeField] private Material attackAreaMaterial;
        [SerializeField] private Color telegraphColor = new Color(1f, 0.65f, 0.05f, 0.45f);
        [SerializeField] private Color activeAttackColor = new Color(1f, 0.15f, 0.15f, 0.65f);
        [Tooltip("Folga acima do chão. A base do collider é detectada automaticamente.")]
        [SerializeField] private float groundOffset = 0.04f;

        private const float ArriveDistance = 0.3f;

        private Health playerHealth;
        private Vector3 spawnPosition;
        private State currentState = State.Idle;
        private Coroutine attackCoroutine;
        private float nextAttackTime;

        private MeshFilter attackAreaFilter;
        private MeshRenderer attackAreaRenderer;
        private MaterialPropertyBlock attackAreaMpb;

        public bool IsDead => currentState == State.Dead;

        private bool PlayerDead => playerHealth != null && playerHealth.IsDead;

        // ---------------- Ciclo de vida ----------------

        private void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (health == null) health = GetComponent<Health>();

            if (health != null)
                health.Died += Die;
            else
                Debug.LogError("GidimErranteAI precisa de um componente Health!", this);

            if (agent != null)
            {
                agent.speed = moveSpeed;
                agent.stoppingDistance = 0.9f;
                agent.updateRotation = false;
            }

            if (player != null)
                playerHealth = FindHealth(player);
            else
                Debug.LogWarning("Gidim: Player não configurado!", this);

            spawnPosition = transform.position;
            loseInterestRange = Mathf.Max(loseInterestRange, detectionRange);

            SetupAttackAreaVisual();
        }

        private void OnDestroy()
        {
            if (health != null)
                health.Died -= Die;
        }

        private void Update()
        {
            if (IsDead || player == null || agent == null || !agent.isOnNavMesh)
                return;

            if (PlayerDead)
            {
                if (currentState == State.Attack)
                    InterruptAttack();

                if (currentState == State.Chase || currentState == State.Cooldown)
                    currentState = State.Return;
            }

            switch (currentState)
            {
                case State.Idle:
                    if (CanSeePlayer())
                        currentState = State.Chase;
                    break;

                case State.Chase:
                    ChaseTarget();
                    break;

                case State.Cooldown:
                    FaceTarget();

                    if (GetDistanceToTarget() > loseInterestRange)
                        currentState = State.Return;
                    else if (Time.time >= nextAttackTime)
                        currentState = State.Chase;
                    break;

                case State.Return:
                    ReturnToSpawn();
                    break;
            }
        }

        // ---------------- Movimento ----------------

        private bool CanSeePlayer()
        {
            return !PlayerDead && GetDistanceToTarget() <= detectionRange;
        }

        private void ChaseTarget()
        {
            float distance = GetDistanceToTarget();

            if (distance > loseInterestRange)
            {
                currentState = State.Return;
                return;
            }

            FaceTarget();

            if (distance <= verticalRange)
            {
                StopMovement();

                if (Time.time >= nextAttackTime)
                    StartAttack(distance);

                return;
            }

            agent.isStopped = false;
            agent.SetDestination(player.position);
        }

        private void ReturnToSpawn()
        {
            if (CanSeePlayer())
            {
                currentState = State.Chase;
                return;
            }

            Vector3 flat = transform.position - spawnPosition;
            flat.y = 0f;

            if (flat.magnitude <= ArriveDistance)
            {
                StopMovement();
                currentState = State.Idle;
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(spawnPosition);
        }

        private void StopMovement()
        {
            if (agent == null || !agent.isOnNavMesh)
                return;

            agent.isStopped = true;
            agent.ResetPath();
        }

        // ---------------- Ataque ----------------

        private void StartAttack(float distance)
        {
            if (currentState != State.Chase)
                return;

            currentState = State.Attack;
            StopMovement();

            bool useStab = distance <= stabRange && Random.value < 0.5f;
            attackCoroutine = StartCoroutine(PerformAttack(useStab));
        }

        private IEnumerator PerformAttack(bool useStab)
        {
            float telegraph = useStab ? stabTelegraph : verticalTelegraph;
            float range = useStab ? stabRange : verticalRange;

            if (animator != null)
                animator.SetTrigger(useStab ? "Stab" : "VerticalSlash");

            // 1) Aviso visual (telegraph)
            SetAttackAreaVisual(true, range, telegraphColor);

            float endTime = Time.time + telegraph;
            while (Time.time < endTime)
            {
                FaceTarget();
                yield return null;
            }

            // 2) Janela ativa de dano
            SetAttackAreaVisual(true, range, activeAttackColor);

            if (IsTargetInAttackRange(range))
                ApplyDamage();

            yield return new WaitForSeconds(hitboxDuration);

            // 3) Recuperação
            SetAttackAreaVisual(false);
            attackCoroutine = null;
            nextAttackTime = Time.time + attackCooldown;
            currentState = State.Cooldown;
        }

        /// <summary>Cancela o golpe em andamento (jogador morreu).</summary>
        private void InterruptAttack()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            SetAttackAreaVisual(false);
            currentState = State.Chase;
        }

        private void ApplyDamage()
        {
            if (playerHealth == null || playerHealth.IsDead)
                return;

            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : transform.forward;

            playerHealth.TakeDamage(new DamageInfo(damage, transform.position, direction, knockback, gameObject));
        }

        private bool IsTargetInAttackRange(float range)
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction.magnitude > range)
                return false;

            return Vector3.Angle(transform.forward, direction) <= attackAngle;
        }

        // ---------------- Utilidades ----------------

        private static Health FindHealth(Transform t)
        {
            Health h = t.GetComponent<Health>();
            if (h == null) h = t.GetComponentInParent<Health>();
            if (h == null) h = t.root.GetComponentInChildren<Health>(true);
            if (h == null) Debug.LogError("Gidim: Nenhum Health encontrado em " + t.root.name, t.root);
            return h;
        }

        private float GetDistanceToTarget()
        {
            Vector3 d = player.position - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private void FaceTarget()
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f)
                return;

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(direction),
                12f * Time.deltaTime
            );
        }

        public void Die()
        {
            if (IsDead)
                return;

            currentState = State.Dead;

            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            SetAttackAreaVisual(false);
            StopMovement();

            if (agent != null)
                agent.enabled = false;

            if (animator != null)
                animator.SetTrigger("Death");

            enabled = false;
        }

        // ---------------- Visualização da hitbox ----------------

        private void SetupAttackAreaVisual()
        {
            if (!showHitbox) return;

            // A base do collider define a altura do chão (o pivô pode estar no centro do corpo).
            float footY = 0f;
            Collider col = GetComponent<Collider>();
            if (col != null)
                footY = col.bounds.min.y - transform.position.y;

            var go = new GameObject("AttackAreaVisual");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, footY + groundOffset, 0f);

            attackAreaFilter = go.AddComponent<MeshFilter>();
            attackAreaFilter.sharedMesh = OverlayMeshes.Sector(attackAngle * 2f);

            attackAreaRenderer = go.AddComponent<MeshRenderer>();
            attackAreaRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            attackAreaRenderer.receiveShadows = false;
            attackAreaRenderer.sharedMaterial = attackAreaMaterial != null
                ? attackAreaMaterial
                : new Material(Shader.Find("Sprites/Default"));

            attackAreaMpb = new MaterialPropertyBlock();
            go.SetActive(false);
        }

        private void SetAttackAreaVisual(bool visible, float range = 1f, Color? color = null)
        {
            if (attackAreaRenderer == null)
                return;

            if (!visible)
            {
                attackAreaRenderer.gameObject.SetActive(false);
                return;
            }

            Color c = color ?? telegraphColor;
            attackAreaMpb.SetColor("_BaseColor", c);
            attackAreaMpb.SetColor("_Color", c);
            attackAreaRenderer.SetPropertyBlock(attackAreaMpb);

            attackAreaRenderer.transform.localScale = new Vector3(range, 1f, range);
            attackAreaRenderer.gameObject.SetActive(true);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 p = transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(p, detectionRange);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(p, loseInterestRange);

            Gizmos.color = Color.red;
            Vector3 fwd = transform.forward;
            Vector3 l = Quaternion.AngleAxis(-attackAngle, Vector3.up) * fwd;
            Vector3 r = Quaternion.AngleAxis(attackAngle, Vector3.up) * fwd;
            Gizmos.DrawLine(p, p + l * verticalRange);
            Gizmos.DrawLine(p, p + r * verticalRange);
            Gizmos.DrawWireSphere(p, stabRange);
            Gizmos.DrawWireSphere(p, verticalRange);
        }
    }
}