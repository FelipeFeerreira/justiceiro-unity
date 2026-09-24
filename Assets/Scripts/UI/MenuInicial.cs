using UnityEngine;
using UnityEngine.SceneManagement;

namespace Justiceiro
{
    /// <summary>Menu inicial do jogo — IMGUI puro, sem prefabs.</summary>
    public class MenuInicial : MonoBehaviour
    {
        public string cenaJogo = "Favela";
        Texture2D _px;

        void Start()
        {
            GameManager.TravarCursor(false);
            Time.timeScale = 1f;
            _px = new Texture2D(1, 1);
            _px.SetPixel(0, 0, Color.white);
            _px.Apply();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
                return;
            }

            bool comecar = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)
                           || Input.GetMouseButtonDown(0);

            if (comecar)
            {
                Sfx.Tocar("arma_troca", 0.5f, 1.4f);
                SceneManager.LoadScene(cenaJogo);
            }
        }

        void OnGUI()
        {
            float W = Screen.width, H = Screen.height;
            if (_px == null)
            {
                _px = new Texture2D(1, 1);
                _px.SetPixel(0, 0, Color.white);
                _px.Apply();
            }

            void Caixa(Rect r, Color c)
            {
                var old = GUI.color;
                GUI.color = c;
                GUI.DrawTexture(r, _px);
                GUI.color = old;
            }

            Color vermelho = new Color(0.85f, 0.16f, 0.12f);
            Color fundo = new Color(0.02f, 0.025f, 0.045f, 0.82f);

            Caixa(new Rect(0, 0, W, H), new Color(0.03f, 0.035f, 0.05f, 0.985f));

            // faixa superior do brasão
            Caixa(new Rect(0, 0, W, 10f), vermelho);
            Caixa(new Rect(0, H - 10f, W, 10f), vermelho);

            var titulo = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(H * 0.085f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var sub = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(H * 0.028f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var corpo = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(H * 0.022f), alignment = TextAnchor.MiddleCenter };
            var dica = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(H * 0.026f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            var old = GUI.color;
            GUI.color = vermelho;
            GUI.Label(new Rect(0, H * 0.26f + 4, W, titulo.fontSize + 8), "JUSTICEIRO", titulo);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, H * 0.32f, W, H * 0.04f), "O P A Ç Ã O   M O R R O   D A   M A N G U E I R A", sub);
            GUI.color = new Color(1f, 0.85f, 0.45f);
            GUI.Label(new Rect(0, H * 0.40f, W, dica.fontSize + 6), "—  CORPO MILITAR DO ESTADO DO RIO DE JANEIRO (BOPE)  —", corpo);
            GUI.color = old;

            float by = H * 0.52f;
            Caixa(new Rect(W * 0.5f - 300f, by - 16f, 600f, H * 0.22f), fundo);
            Caixa(new Rect(W * 0.5f - 300f, by - 16f, 600f, 6f), vermelho);

            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.Label(new Rect(0, by, W, H * 0.05f), "O MORRO ESTÁ NAS MÃOS DA MILÍCIA. LIMPA A CASA GRUPO POR GRUPO.", corpo);
            GUI.Label(new Rect(0, by + H * 0.06f, W, H * 0.04f), "A T I V I D A D E   P E N A L:   CÁRCERE PRIVADO   /   TRÁFICO DE ARMAS", corpo);

            GUI.color = new Color(0.55f, 0.85f, 1f, 0.85f);
            GUI.Label(new Rect(0, by + H * 0.115f, W, H * 0.04f),
                "WASD move  •  SHIFT corre  •  ESPAÇO pulo duplo  •  CTRL ESQ dash  •  TAB roda de armas  •  E execução", corpo);
            GUI.color = old;

            float pulso = 0.55f + Mathf.PingPong(Time.time * 1.8f, 0.45f);
            GUI.color = new Color(1f, 0.9f, 0.45f, pulso);
            GUI.Label(new Rect(0, H * 0.80f, W, dica.fontSize + 8), "ESPACO  —  ENTRAR EM OPERAÇÃO", dica);

            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            GUI.Label(new Rect(0, H * 0.86f, W, H * 0.03f), "CLIQUE PARA COMEÇAR  •  ESC PARA SAIR", corpo);

            GUI.color = old;
        }
    }
}