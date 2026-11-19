using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Inanna.Levels
{
    /// <summary>
    /// Ponte levadiça. Vai no pivô da dobradiça e gira o próprio transform entre levantada e abaixada.
    /// Enquanto está levantada, os <see cref="gapBlockers"/> fecham o vão para ninguém cair no rio.
    /// </summary>
    public class Drawbridge : MonoBehaviour, IActivatable
    {
        [SerializeField] Vector3 raisedEuler = new Vector3(90f, 0f, 0f);
        [SerializeField] Vector3 loweredEuler = Vector3.zero;
        [SerializeField] bool startLowered;
        [SerializeField, Min(0f)] float lowerDuration = 1.5f;
        [Tooltip("Colisores que fecham o vão enquanto a ponte está levantada.")]
        [SerializeField] Collider[] gapBlockers = new Collider[0];
        [SerializeField] UnityEvent onLowered = new UnityEvent();

        Coroutine _lowering;

        public bool IsActivated { get; private set; }

        void Awake() => SetActivatedImmediate(startLowered);

        // Desativar o GameObject mata a corrotina. Sem isto, a ponte ficaria no meio do caminho com o
        // vão bloqueado e o jogador preso. Então termina na hora.
        void OnDisable()
        {
            if (_lowering == null) return;
            _lowering = null;
            ApplyPose(true);
            onLowered.Invoke();
        }

        public void Activate()
        {
            if (IsActivated) return;
            IsActivated = true;

            if (lowerDuration <= 0f || !isActiveAndEnabled)
            {
                ApplyPose(true);
                onLowered.Invoke();
                return;
            }
            _lowering = StartCoroutine(Lower());
        }

        public void SetActivatedImmediate(bool activated)
        {
            if (_lowering != null)
            {
                StopCoroutine(_lowering);
                _lowering = null;
            }
            IsActivated = activated;
            ApplyPose(activated);
        }

        IEnumerator Lower()
        {
            var from = Quaternion.Euler(raisedEuler);
            var to = Quaternion.Euler(loweredEuler);
            for (float t = 0f; t < lowerDuration; t += Time.deltaTime)
            {
                transform.localRotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / lowerDuration));
                yield return null;
            }
            _lowering = null;
            ApplyPose(true);
            onLowered.Invoke();
        }

        void ApplyPose(bool lowered)
        {
            transform.localRotation = Quaternion.Euler(lowered ? loweredEuler : raisedEuler);
            foreach (var blocker in gapBlockers)
                if (blocker) blocker.enabled = !lowered;
        }
    }
}
