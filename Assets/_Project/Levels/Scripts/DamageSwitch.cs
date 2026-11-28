using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Inanna.Combat;

namespace Inanna.Levels
{
    /// <summary>
    /// Gatilho acionado por dano, por exemplo uma alavanca atingida pelo bumerangue.
    /// Só detecta o acerto e repassa para os alvos <see cref="IActivatable"/>. O retorno visual fica no <see cref="SwitchFeedback"/>.
    /// Para os ataques do player acharem o gatilho, ele precisa de um Collider na layer que eles procuram (hoje, Enemy).
    /// </summary>
    [DisallowMultipleComponent]
    public class DamageSwitch : MonoBehaviour, IDamageable, ISwitch
    {
        [Tooltip("Componentes que implementam IActivatable (ex.: Drawbridge).")]
        [SerializeField] MonoBehaviour[] targets = Array.Empty<MonoBehaviour>();
        [Tooltip("Dano mínimo para acionar.")]
        [SerializeField, Min(0f)] float minimumDamage = 0.01f;
        [Tooltip("Design: só o bumerangue deve alcançar (ex.: alavanca do outro lado do rio). O validador avisa se der no corpo a corpo.")]
        [SerializeField] bool rangedOnly;
        [SerializeField] UnityEvent onActivated = new UnityEvent();

        /// <summary>Disparado sempre que o estado muda (true = acionado).</summary>
        public event Action<bool> StateChanged;

        public bool IsActivated { get; private set; }
        public bool RangedOnly => rangedOnly;

        /// <summary>Depois de acionado, o gatilho deixa de ser alvo.</summary>
        public bool IsDead => IsActivated;

        public IEnumerable<IActivatable> Targets
        {
            get
            {
                // "if (target)" antes do "is": um componente destruído passa no "is" (que ignora o null da Unity).
                foreach (var target in targets)
                    if (target && target is IActivatable activatable)
                        yield return activatable;
            }
        }

        public void TakeDamage(DamageInfo info)
        {
            if (info.Amount < minimumDamage) return;
            Activate();
        }

        public void Activate()
        {
            if (IsActivated) return;
            SetState(true);
            foreach (var target in Targets) target.Activate();
            onActivated.Invoke();
        }

        /// <summary>Aplica o estado no gatilho e nos alvos sem animação nem eventos de jogo.</summary>
        public void SetActivatedImmediate(bool activated)
        {
            SetState(activated);
            foreach (var target in Targets) target.SetActivatedImmediate(activated);
        }

        void SetState(bool activated)
        {
            if (IsActivated == activated) return;
            IsActivated = activated;
            StateChanged?.Invoke(activated);
        }

        /// <summary>Teste sem o bumerangue: botão direito no componente, no Play mode.</summary>
        [ContextMenu("Acionar (teste)")]
        void ActivateFromContextMenu()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{name}: entre no Play mode para acionar.", this);
                return;
            }
            Activate();
        }

        void OnValidate()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i] is IActivatable) continue;
                Debug.LogWarning($"{name}: {targets[i].GetType().Name} não implementa IActivatable e foi removido dos alvos.", this);
                targets[i] = null;
            }
        }
    }
}
