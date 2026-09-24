using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    public class ArenaEncounter : MonoBehaviour
    {
        public string nome = "ARENA";
        public List<Onda> ondas = new List<Onda>();
        public List<Barreira> barreiras = new List<Barreira>();
        public List<Pickup> recompensas = new List<Pickup>();
        public TipoColetavel premio = TipoColetavel.VidaGrande;
        public bool soltaChave;
        public Vector3 pontoPremio;
        public bool ativada;
        public bool concluida;

        readonly List<EnemyBrain> _atuais = new List<EnemyBrain>();
        int ondaAtual = -1;

        void Reset()
        {
            var c = GetComponent<BoxCollider>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (ativada || concluida) return;
            if (other.gameObject.layer != Layers.PlayerLayer) return;
            Ativar();
        }

        public void Ativar()
        {
            if (ativada) return;
            ativada = true;

            foreach (var b in barreiras) if (b != null) b.Fechar();

            var gm = GameManager.I;
            if (gm != null) gm.Mensagem(nome + " — SEGURA A ONDA!", 3.5f);
            Sfx.Tocar("arena_inicio", 0.8f);

            StartCoroutine(Rodar());
        }

        IEnumerator Rodar()
        {
            for (int i = 0; i < ondas.Count; i++)
            {
                ondaAtual = i;
                var onda = ondas[i];

                for (int j = 0; j < onda.pontos.Count; j++)
                {
                    var p = onda.pontos[j];
                    if (p == null) continue;
                    var e = p.Nascer();
                    if (e != null)
                    {
                        e.Acordar();
                        _atuais.Add(e);
                        e.aoMorrer += (x) => _atuais.Remove(x);
                    }
                    Fx.Luz(p.transform.position + Vector3.up, new Color(1f, 0.3f, 0.2f), 12f, 8f, 0.25f);
                    if (onda.atrasoEntreSpawns > 0f) yield return new WaitForSeconds(onda.atrasoEntreSpawns);
                }

                int limite = Mathf.Max(0, onda.restantesParaProxima);
                while (ContarVivos() > limite)
                    yield return null;

                if (i < ondas.Count - 1)
                {
                    var gm2 = GameManager.I;
                    if (gm2 != null) gm2.Mensagem("VEM MAIS!", 2f);
                    yield return new WaitForSeconds(1.1f);
                }
            }

            while (ContarVivos() > 0) yield return null;

            Concluir();
        }

        int ContarVivos()
        {
            int n = 0;
            for (int i = _atuais.Count - 1; i >= 0; i--)
            {
                var e = _atuais[i];
                if (e == null || e.Morto) { _atuais.RemoveAt(i); continue; }
                n++;
            }
            return n;
        }

        void Concluir()
        {
            concluida = true;
            foreach (var b in barreiras) if (b != null) b.Abrir();

            Sfx.Tocar("arena_fim", 0.7f);
            var gm = GameManager.I;
            if (gm != null) gm.Mensagem("ÁREA LIMPA. SEGUE SUBINDO.", 3f);

            Vector3 p = pontoPremio.sqrMagnitude > 0.01f ? pontoPremio : transform.position;

            if (soltaChave)
            {
                Pickup.Criar(TipoColetavel.ChaveVermelha, p);
                if (gm != null) gm.Mensagem("CAIU UMA CHAVE VERMELHA NA ARENA", 4f);
            }
            else
            {
                Pickup.Criar(premio, p);
                Pickup.Criar(TipoColetavel.MunicaoBala, p + new Vector3(1.6f, 0f, 0f));
                Pickup.Criar(TipoColetavel.MunicaoCartucho, p + new Vector3(-1.6f, 0f, 0f));
            }
        }

        void OnDrawGizmosSelected()
        {
            var c = GetComponent<BoxCollider>();
            if (c == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(c.center, c.size);
        }
    }
}
