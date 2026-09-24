using UnityEngine;

namespace Justiceiro
{
    public class BolaExpansao : MonoBehaviour
    {
        public float alvo = 3f;
        public float dur = 0.3f;
        float t;
        Renderer r;
        void Awake() { r = GetComponent<Renderer>(); }
        void Update()
        {
            t += Time.deltaTime / dur;
            float k = Mathf.Clamp01(t);
            transform.localScale = Vector3.one * Mathf.Lerp(0.4f, alvo, 1f - Mathf.Pow(1f - k, 3f));
            if (r != null)
            {
                var c = r.material.color;
                c.a = 1f - k;
                r.material.color = c;
            }
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
