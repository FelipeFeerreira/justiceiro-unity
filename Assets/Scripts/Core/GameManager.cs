using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Justiceiro
{
    public enum EstadoJogo { Jogando, Morto, Vitoria, Pausado }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I { get; private set; }

        public EstadoJogo estado = EstadoJogo.Jogando;
        public int abates;
        public int execucoes;
        public float tempo;
        public bool temChaveVermelha;

        public string objetivo = "SOBE O MORRO. LIMPA O CAMINHO.";
        public float objetivoAte;

        readonly List<EnemyBrain> _vivos = new List<EnemyBrain>();
        public IReadOnlyList<EnemyBrain> Vivos => _vivos;

        public Transform jogador;
        public PlayerHealth vidaJogador;

        void Awake()
        {
            I = this;
            estado = EstadoJogo.Jogando;
            Application.targetFrameRate = 144;
        }

        void Start()
        {
            TravarCursor(true);
            Mensagem("SOBE O MORRO. LIMPA O CAMINHO.", 5f);
        }

        void Update()
        {
            if (estado == EstadoJogo.Jogando) tempo += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.R) && (estado == EstadoJogo.Morto || estado == EstadoJogo.Vitoria))
                Reiniciar();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (estado == EstadoJogo.Jogando) Pausar(true);
                else if (estado == EstadoJogo.Pausado) Pausar(false);
            }

            if (estado == EstadoJogo.Pausado && Input.GetKeyDown(KeyCode.F5)) Reiniciar();
        }

        public void Pausar(bool p)
        {
            estado = p ? EstadoJogo.Pausado : EstadoJogo.Jogando;
            Time.timeScale = p ? 0f : 1f;
            TravarCursor(!p);
        }

        public static void TravarCursor(bool travar)
        {
            Cursor.lockState = travar ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !travar;
        }

        public void Registrar(EnemyBrain e)
        {
            if (!_vivos.Contains(e)) _vivos.Add(e);
        }

        public void Remover(EnemyBrain e, bool porExecucao)
        {
            _vivos.Remove(e);
            abates++;
            if (porExecucao) execucoes++;
        }

        public void Mensagem(string texto, float dur = 3.5f)
        {
            objetivo = texto;
            objetivoAte = Time.unscaledTime + dur;
        }

        public void PegarChave()
        {
            temChaveVermelha = true;
            Mensagem("CHAVE VERMELHA PEGA — A PORTA LÁ EM CIMA ABRIU", 4f);
            Sfx.Tocar("chave", 0.8f);
        }

        public void Morrer()
        {
            if (estado != EstadoJogo.Jogando) return;
            estado = EstadoJogo.Morto;
            Time.timeScale = 0.35f;
            TravarCursor(false);
            Sfx.Tocar("morte_player", 0.9f);
        }

        public void Vencer()
        {
            if (estado != EstadoJogo.Jogando) return;
            estado = EstadoJogo.Vitoria;
            Time.timeScale = 1f;
            TravarCursor(false);
            Sfx.Tocar("vitoria", 0.8f);
        }

        public void Reiniciar()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Inimigo vivo mais próximo de um ponto (usado por arenas e pela IA).</summary>
        public EnemyBrain MaisProximo(Vector3 p, float raio)
        {
            EnemyBrain melhor = null;
            float d2 = raio * raio;
            for (int i = 0; i < _vivos.Count; i++)
            {
                var e = _vivos[i];
                if (e == null) continue;
                float d = (e.transform.position - p).sqrMagnitude;
                if (d < d2) { d2 = d; melhor = e; }
            }
            return melhor;
        }
    }
}
