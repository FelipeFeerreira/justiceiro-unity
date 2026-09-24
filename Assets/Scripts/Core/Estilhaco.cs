using UnityEngine;

namespace Justiceiro
{
    public class Estilhaco : MonoBehaviour
    {
        public Vector3 vel;
        public float gravidade = -12f;
        void Update()
        {
            vel.y += gravidade * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            transform.Rotate(vel * Time.deltaTime * 30f, Space.World);
        }
    }
}
