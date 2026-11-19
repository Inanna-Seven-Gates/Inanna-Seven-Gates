using System.Collections;
using UnityEngine;

namespace Inanna.Levels
{
    /// <summary>
    /// Grade de porta de sala. Trancada, a grade sobe e bloqueia a passagem. Aberta, desce para dentro do chão
    /// e o colisor desliga. Vai no centro da porta, na altura do chão; a grade é um filho (<see cref="bars"/>).
    /// </summary>
    public class RoomGate : MonoBehaviour, ILockable
    {
        [Tooltip("A grade que sobe e desce (precisa de Collider e Renderer; layer Occluder, como as paredes).")]
        [SerializeField] Transform bars;
        [Tooltip("Altura local do centro da grade quando trancada.")]
        [SerializeField] float lockedHeight = 1.5f;
        [Tooltip("Altura local do centro da grade quando aberta (escondida dentro do chão).")]
        [SerializeField] float openHeight = -1.6f;
        [SerializeField, Min(0f)] float moveDuration = 0.4f;
        [SerializeField] bool startLocked;

        Collider _barsCollider;
        Coroutine _moving;

        public bool IsLocked { get; private set; }

        void Awake()
        {
            CacheCollider();
            SetLockedImmediate(startLocked);
        }

        // Desativar mata a corrotina: termina o movimento na hora para a grade não ficar no meio.
        void OnDisable()
        {
            if (_moving == null) return;
            _moving = null;
            ApplyPose(IsLocked);
        }

        public void Lock() => MoveTo(true);

        public void Unlock() => MoveTo(false);

        public void SetLockedImmediate(bool locked)
        {
            if (_moving != null)
            {
                StopCoroutine(_moving);
                _moving = null;
            }
            IsLocked = locked;
            ApplyPose(locked);
        }

        void MoveTo(bool locked)
        {
            if (IsLocked == locked) return;
            IsLocked = locked;

            if (_moving != null) StopCoroutine(_moving);
            if (moveDuration <= 0f || !isActiveAndEnabled)
            {
                ApplyPose(locked);
                return;
            }
            _moving = StartCoroutine(Move(locked));
        }

        IEnumerator Move(bool locked)
        {
            // Trancando, o colisor liga já no começo: ninguém passa enquanto a grade sobe.
            if (locked) SetColliderEnabled(true);

            float from = bars.localPosition.y;
            float to = locked ? lockedHeight : openHeight;
            for (float t = 0f; t < moveDuration; t += Time.deltaTime)
            {
                SetBarsHeight(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / moveDuration)));
                yield return null;
            }
            _moving = null;
            ApplyPose(locked);
        }

        void ApplyPose(bool locked)
        {
            SetBarsHeight(locked ? lockedHeight : openHeight);
            SetColliderEnabled(locked);
        }

        void SetBarsHeight(float height)
        {
            if (!bars) return;
            var position = bars.localPosition;
            bars.localPosition = new Vector3(position.x, height, position.z);
        }

        void SetColliderEnabled(bool enabled)
        {
            CacheCollider();
            if (_barsCollider) _barsCollider.enabled = enabled;
        }

        void CacheCollider()
        {
            if (!_barsCollider && bars) _barsCollider = bars.GetComponent<Collider>();
        }
    }
}
