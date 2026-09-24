using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    public class PontoSpawn : MonoBehaviour
    {
        public TipoInimigo tipo = TipoInimigo.Moleque;
        public bool nasceNoStart;

        void Start()
        {
            if (nasceNoStart) Nascer();
        }

        public EnemyBrain Nascer()
        {
            var e = EnemyFactory.Criar(tipo, transform.position, transform.rotation);
            return e;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = tipo == TipoInimigo.Chefe ? Color.red
                         : tipo == TipoInimigo.Blindado ? new Color(1f, 0.6f, 0f)
                         : tipo == TipoInimigo.Fogueteiro ? new Color(0.8f, 0.3f, 1f)
                         : tipo == TipoInimigo.Soldado ? new Color(0.3f, 1f, 0.4f)
                         : Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.6f);
            Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 1.5f);
        }
    }
}
