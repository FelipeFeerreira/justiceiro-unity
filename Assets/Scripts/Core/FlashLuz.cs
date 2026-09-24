using UnityEngine;

namespace Justiceiro
{
    public class FlashLuz : MonoBehaviour
    {
        public float dur = 0.06f;
        public float i0 = 3f;
        float t;
        Light l;
        void Awake() { l = GetComponent<Light>(); }
        void Update()
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, dur);
            if (l != null) l.intensity = Mathf.Lerp(i0, 0f, t);
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
