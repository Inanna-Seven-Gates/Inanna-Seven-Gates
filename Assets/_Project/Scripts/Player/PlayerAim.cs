using UnityEngine;

namespace Inanna.Player
{
    /// <summary>
    /// Converte a posição do mouse em um ponto no mundo (plano horizontal na altura do peito da Inanna).
    /// Expõe AimPoint e AimDirection (sempre horizontal) para o combate, o gihar e a câmera.
    /// </summary>
    public class PlayerAim : MonoBehaviour
    {
        [SerializeField] PlayerInputReader input;
        [SerializeField] Camera cam;
        [Tooltip("Altura do plano de mira acima dos pés. ~altura do peito.")]
        [SerializeField] float aimHeight = 1f;
        [Tooltip("Objeto opcional (esfera/decal) para visualizar o ponto de mira.")]
        [SerializeField] Transform aimMarker;

        public Vector3 AimPoint { get; private set; }
        /// <summary>Direção horizontal normalizada do jogador até o mouse.</summary>
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public float AimDistance { get; private set; }

        void Awake()
        {
            if (!input) input = GetComponent<PlayerInputReader>();
            if (!cam) cam = Camera.main;
        }

        void Update()
        {
            if (!cam) return;

            Ray ray = cam.ScreenPointToRay(input.PointerPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y + aimHeight, 0f));

            if (plane.Raycast(ray, out float enter))
            {
                AimPoint = ray.GetPoint(enter);

                Vector3 flat = AimPoint - transform.position;
                flat.y = 0f;
                AimDistance = flat.magnitude;
                if (AimDistance > 0.05f) AimDirection = flat / AimDistance;
            }

            if (aimMarker) aimMarker.position = AimPoint;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + Vector3.up * aimHeight, AimPoint);
            Gizmos.DrawWireSphere(AimPoint, 0.2f);
        }
    }
}
