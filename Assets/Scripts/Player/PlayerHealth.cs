using UnityEngine;

namespace Justiceiro
{
    /// <summary>Vida + colete do justiceiro. Colete absorve 60% do dano enquanto durar.</summary>
    public class PlayerHealth : MonoBehaviour, IAlvo
    {
        public float vidaMax = 100f;
        public float vida = 100f;
        public float coleteMax = 100f;
        public float colete = 0f;

        public float vidaExcedenteMax = 200f;   // overheal de execução

        [HideInInspector] public float flashDano;      // 0..1 para o HUD
        [HideInInspector] public float flashCura;
        [HideInInspector] public float ultimoDano;

        public bool Morto { get; private set; }
        public Transform Alvo => transform;

        PlayerController pc;
        Transform cam;

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            cam = pc != null ? pc.cam : null;
        }

        void Update()
        {
            flashDano = Mathf.MoveTowards(flashDano, 0f, Time.deltaTime * 1.6f);
            flashCura = Mathf.MoveTowards(flashCura, 0f, Time.deltaTime * 2.2f);

            // vida excedente decai devagar até 100
            if (vida > vidaMax)
                vida = Mathf.MoveTowards(vida, vidaMax, Time.deltaTime * 3f);
        }

        public void ReceberDano(float dano, Vector3 ponto, Vector3 direcao, bool pesado)
        {
            if (Morto || dano <= 0f) return;
            var gm = GameManager.I;
            if (gm != null && gm.estado != EstadoJogo.Jogando) return;

            if (colete > 0f)
            {
                float absorvido = Mathf.Min(colete, dano * 0.6f);
                colete -= absorvido;
                dano -= absorvido;
            }

            vida -= dano;
            ultimoDano = Time.time;
            flashDano = Mathf.Clamp01(flashDano + dano / 45f);

            Sfx.Tocar("dano_player", Mathf.Clamp01(0.3f + dano / 60f), Random.Range(0.92f, 1.06f));

            if (pc != null && direcao.sqrMagnitude > 0.001f)
                pc.Empurrar(Util.Plano(direcao).normalized * Mathf.Min(6f, dano * 0.14f));

            if (vida <= 0f)
            {
                vida = 0f;
                Morto = true;
                if (gm != null) gm.Morrer();
            }
        }

        public void Curar(float qtd, bool permiteExcedente = false)
        {
            if (Morto) return;
            float teto = permiteExcedente ? vidaExcedenteMax : vidaMax;
            if (vida >= teto) return;
            vida = Mathf.Min(teto, vida + qtd);
            flashCura = Mathf.Clamp01(flashCura + qtd / 40f);
        }

        public void DarColete(float qtd)
        {
            colete = Mathf.Min(coleteMax, colete + qtd);
            flashCura = Mathf.Clamp01(flashCura + qtd / 60f);
        }
    }
}
