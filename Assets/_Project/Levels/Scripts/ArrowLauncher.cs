using System.Collections;
using UnityEngine;

namespace Inanna.Levels
{
    /// <summary>
    /// Lançador de flechas na parede. Atira para a frente (eixo azul do objeto).
    /// Modo ritmado: atira sozinho a cada <see cref="interval"/>. Modo acionado: atira quando alguém chama
    /// <see cref="Trigger"/> (ex.: <see cref="PressurePlate"/>). Sempre brilha antes de atirar, para o jogador ler e desviar.
    /// As flechas acertam a Inanna e os inimigos (armadilha também serve de arma).
    /// </summary>
    public class ArrowLauncher : MonoBehaviour, ITriggerable
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Ritmo")]
        [Tooltip("Ligado: atira sozinho no ritmo. Desligado: só atira quando acionado.")]
        [SerializeField] bool autoFire = true;
        [SerializeField, Min(0.2f)] float interval = 2f;
        [Tooltip("Atraso do primeiro disparo, para lançadores vizinhos não atirarem juntos.")]
        [SerializeField, Min(0f)] float phaseOffset;
        [Tooltip("Tempo do brilho de aviso antes de cada disparo.")]
        [SerializeField, Min(0f)] float warningSeconds = 0.6f;

        [Header("Rajada")]
        [SerializeField, Min(1)] int arrowsPerVolley = 1;
        [Tooltip("Distância lateral entre as flechas da mesma rajada.")]
        [SerializeField, Min(0f)] float spread = 0.6f;

        [Header("Flecha")]
        [SerializeField, Min(0.1f)] float arrowSpeed = 14f;
        [SerializeField, Min(0.5f)] float maxDistance = 20f;
        [SerializeField, Range(0f, 1f)] float damageFractionOfMax = 0.15f;
        [SerializeField] LayerMask hitMask;
        [Tooltip("O que segura a flecha (paredes, chão).")]
        [SerializeField] LayerMask blockMask;
        [SerializeField] Material arrowMaterial;

        [Header("Aviso visual")]
        [SerializeField] Renderer warningIndicator;
        [SerializeField] Color idleColor = new Color(0.3f, 0.25f, 0.2f);
        [SerializeField] Color warningColor = new Color(1f, 0.55f, 0.1f);

        MaterialPropertyBlock _block;
        Coroutine _routine;

        void Reset()
        {
            hitMask = LayerMask.GetMask("Player", "Enemy");
            blockMask = LayerMask.GetMask("Default", "Ground", "Occluder");
        }

        void OnEnable()
        {
            SetWarning(false);
            if (autoFire) _routine = StartCoroutine(FireLoop());
        }

        void OnDisable()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
            SetWarning(false);
        }

        /// <summary>Atira uma rajada (com aviso), se não estiver no meio de outra.</summary>
        public void Trigger()
        {
            if (autoFire || _routine != null || !isActiveAndEnabled) return;
            _routine = StartCoroutine(TelegraphAndFire(clearRoutine: true));
        }

        IEnumerator FireLoop()
        {
            if (phaseOffset > 0f) yield return new WaitForSeconds(phaseOffset);
            while (true)
            {
                yield return TelegraphAndFire(clearRoutine: false);
                yield return new WaitForSeconds(Mathf.Max(0f, interval - warningSeconds));
            }
        }

        IEnumerator TelegraphAndFire(bool clearRoutine)
        {
            SetWarning(true);
            if (warningSeconds > 0f) yield return new WaitForSeconds(warningSeconds);
            SetWarning(false);
            FireVolley();
            if (clearRoutine) _routine = null;
        }

        void FireVolley()
        {
            var forward = transform.forward;
            var side = transform.right;
            for (int i = 0; i < arrowsPerVolley; i++)
            {
                float offset = (i - (arrowsPerVolley - 1) * 0.5f) * spread;
                var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                arrow.name = "Flecha";
                Destroy(arrow.GetComponent<Collider>());
                arrow.transform.position = transform.position + side * offset + forward * 0.4f;
                arrow.transform.localScale = new Vector3(0.08f, 0.08f, 0.7f);
                if (arrowMaterial) arrow.GetComponent<Renderer>().sharedMaterial = arrowMaterial;
                arrow.AddComponent<TrapArrow>().Launch(forward, arrowSpeed, maxDistance, damageFractionOfMax, hitMask, blockMask, gameObject);
            }
        }

        void SetWarning(bool on)
        {
            if (!warningIndicator) return;
            _block ??= new MaterialPropertyBlock();
            warningIndicator.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, on ? warningColor : idleColor);
            warningIndicator.SetPropertyBlock(_block);
        }
    }
}
