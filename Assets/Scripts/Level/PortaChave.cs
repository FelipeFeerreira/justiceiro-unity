using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    public class PortaChave : MonoBehaviour
    {
        public float alcance = 4.5f;
        public Vector3 posAberta;
        Vector3 posFechada;
        bool aberta;
        float avisoT;

        void Start()
        {
            posFechada = transform.position;
            if (posAberta == Vector3.zero) posAberta = posFechada + Vector3.up * 7f;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.jogador == null) return;

            if (!aberta)
            {
                float d = Vector3.Distance(transform.position, gm.jogador.position);
                if (d < alcance)
                {
                    if (gm.temChaveVermelha)
                    {
                        aberta = true;
                        Sfx.TocarEm("porta", transform.position, 0.9f, 1f, 70f);
                        gm.Mensagem("PORTA ABERTA — TOPO DO MORRO", 3f);
                    }
                    else if (Time.time > avisoT)
                    {
                        avisoT = Time.time + 2.5f;
                        gm.Mensagem("TRANCADA. FALTA A CHAVE VERMELHA.", 2.2f);
                        Sfx.Tocar("vazio", 0.5f);
                    }
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, posAberta, 5f * Time.deltaTime);
            }
        }
    }
}
