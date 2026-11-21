using UnityEngine;

namespace Inanna.Combat
{
    /// <summary>Malhas procedurais planas (no plano XZ) para círculo de alcance e marcadores.</summary>
    public static class OverlayMeshes
    {
        static Mesh _disc, _ring;

        /// <summary>Disco</summary>
        public static Mesh Disc()
        {
            if (_disc) return _disc;
            const int seg = 48;
            var v = new Vector3[seg + 2];
            var t = new int[seg * 3];
            v[0] = Vector3.zero;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                v[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            }
            for (int i = 0; i < seg; i++) { t[i * 3] = 0; t[i * 3 + 1] = i + 1; t[i * 3 + 2] = i + 2; }
            _disc = Build("OverlayDisc", v, t);
            return _disc;
        }

        /// <summary>Anel de raio externo raio interno.</summary>
        public static Mesh Ring(float inner = 0.8f)
        {
            if (_ring) return _ring;
            const int seg = 48;
            var v = new Vector3[(seg + 1) * 2];
            var t = new int[seg * 6];
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v[i * 2] = dir;
                v[i * 2 + 1] = dir * inner;
            }
            for (int i = 0; i < seg; i++)
            {
                int o0 = i * 2, i0 = i * 2 + 1, o1 = i * 2 + 2, i1 = i * 2 + 3;
                int k = i * 6;
                t[k] = o0; t[k + 1] = o1; t[k + 2] = i0;
                t[k + 3] = i0; t[k + 4] = o1; t[k + 5] = i1;
            }
            _ring = Build("OverlayRing", v, t);
            return _ring;
        }

        static Mesh Build(string name, Vector3[] v, int[] t)
        {
            var m = new Mesh { name = name, vertices = v, triangles = t };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }

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
