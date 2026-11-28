using UnityEngine;

namespace Inanna.Levels
{
    /// <summary>
    /// Retorno visual de um <see cref="DamageSwitch"/>: gira a haste e troca a cor do indicador.
    /// Fica separado do gatilho para trocar o visual (modelo final, partícula, som) sem mexer na lógica.
    /// </summary>
    [RequireComponent(typeof(DamageSwitch))]
    public class SwitchFeedback : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform handle;
        [SerializeField] Vector3 handleOffEuler = new Vector3(0f, 0f, 35f);
        [SerializeField] Vector3 handleOnEuler = new Vector3(0f, 0f, -35f);
        [SerializeField] Renderer indicator;
        [SerializeField] Color offColor = new Color(0.85f, 0.2f, 0.15f);
        [SerializeField] Color onColor = new Color(0.25f, 0.85f, 0.3f);

        DamageSwitch _switch;
        MaterialPropertyBlock _propertyBlock;

        void Awake()
        {
            _switch = GetComponent<DamageSwitch>();
            Apply(_switch.IsActivated);
        }

        void OnEnable() => _switch.StateChanged += Apply;

        void OnDisable() => _switch.StateChanged -= Apply;

        void Apply(bool activated)
        {
            if (handle) handle.localRotation = Quaternion.Euler(activated ? handleOnEuler : handleOffEuler);
            if (!indicator) return;

            _propertyBlock ??= new MaterialPropertyBlock();
            indicator.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, activated ? onColor : offColor);
            indicator.SetPropertyBlock(_propertyBlock);
        }
    }
}
