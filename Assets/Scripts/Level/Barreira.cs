using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    public class Barreira : MonoBehaviour
    {
        public Vector3 posFechada;
        public Vector3 posAberta;
        public float velocidade = 9f;
        public bool fechada;

        Vector3 alvo;

        void Awake()
        {
            if (posAberta == Vector3.zero && posFechada == Vector3.zero)
            {
                posFechada = transform.position;
                posAberta = transform.position + Vector3.down * 8f;
            }
            transform.position = fechada ? posFechada : posAberta;
            alvo = transform.position;
        }

        void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, alvo, velocidade * Time.deltaTime);
        }

        public void Fechar()
        {
            fechada = true;
            alvo = posFechada;
            Sfx.TocarEm("porta", transform.position, 0.7f, 0.9f, 60f);
        }

        public void Abrir()
        {
            fechada = false;
            alvo = posAberta;
            Sfx.TocarEm("porta", transform.position, 0.6f, 1.1f, 60f);
        }
    }
}
