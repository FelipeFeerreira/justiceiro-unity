using UnityEngine;

namespace Justiceiro
{
    public class AutoDestruir : MonoBehaviour
    {
        public float tempo = 1f;
        void Update()
        {
            tempo -= Time.deltaTime;
            if (tempo <= 0f) Destroy(gameObject);
        }
    }
}
