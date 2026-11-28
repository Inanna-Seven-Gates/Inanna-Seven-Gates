using UnityEngine;

namespace Inanna.Combat
{
    /// <summary>
    /// Anel no chão sob um inimigo marcado.
    /// </summary>
    public class TargetMarker : MonoBehaviour
    {
        Transform _target;
        float _footOffset, _radius, _fadeOutTime, _alpha;
        const float FadeInTime = 0.08f;
        bool _removing;
        Color _color;
        Renderer _renderer;
        MaterialPropertyBlock _mpb;

        public static TargetMarker Create(Collider col, Material mat, Color color, float fadeOutTime)
        {
            var go = new GameObject("TargetMarker");
            var m = go.AddComponent<TargetMarker>();
            go.AddComponent<MeshFilter>().sharedMesh = OverlayMeshes.Ring(0.8f);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            Bounds b = col.bounds;
            m._target = col.transform;
            m._footOffset = b.min.y - col.transform.position.y + 0.06f;
            m._radius = Mathf.Max(b.extents.x, b.extents.z) + 0.35f;
            m._color = color;
            m._fadeOutTime = Mathf.Max(0.01f, fadeOutTime);
            m._renderer = r;
            m._mpb = new MaterialPropertyBlock();
            m._alpha = 0f;
            m.Apply();
            return m;
        }

        /// <summary>Faz o ícone se esvair rápido e se destruir.</summary>
        public void Remove() => _removing = true;

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            if (_target)
            {
                Vector3 p = _target.position;
                p.y += _footOffset;
                transform.position = p;
            }

            if (_removing)
            {
                _alpha -= dt / _fadeOutTime;
                if (_alpha <= 0f) { Destroy(gameObject); return; }
            }
            else _alpha = Mathf.MoveTowards(_alpha, 1f, dt / FadeInTime);

            float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 8f);
            float grow = _removing ? 1f + (1f - _alpha) * 0.5f : 1f;
            float s = _radius * pulse * grow;
            transform.localScale = new Vector3(s, 1f, s);

            Apply();
        }

        void Apply()
        {
            Color c = _color;
            c.a *= _alpha;
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color", c);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}