using UnityEngine;

namespace Justiceiro
{
    /// <summary>
    /// HUD tática estilo BOPE: limpa, sem poluição, números grandes.
    /// Vida e colete na esquerda, arma/munição na direita, objetivo no topo,
    /// roda de armas segurando TAB. Zero prefabs, tudo IMGUI.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public PlayerHealth vida;
        public WeaponController armas;
        public PlayerController pc;
        public PlayerExecucao exec;

        Texture2D _px, _circulo;
        GUIStyle _num, _numAm, _txt, _txtP, _titulo, _meio;
        bool _pronto;

        static readonly Color CorVermelho = new Color(0.85f, 0.16f, 0.12f);
        static readonly Color CorVida = new Color(0.25f, 0.95f, 0.45f);
        static readonly Color CorColete = new Color(0.35f, 0.65f, 1f);
        static readonly Color CorMunicao = new Color(1f, 0.85f, 0.3f);
        static readonly Color CorFundo = new Color(0.02f, 0.02f, 0.035f, 0.82f);

        void Awake()
        {
            if (vida == null) vida = GetComponent<PlayerHealth>();
            if (armas == null) armas = GetComponent<WeaponController>();
            if (pc == null) pc = GetComponent<PlayerController>();
            if (exec == null) exec = GetComponent<PlayerExecucao>();
        }

        void Preparar()
        {
            _px = new Texture2D(1, 1);
            _px.SetPixel(0, 0, Color.white);
            _px.Apply();

            _circulo = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x + 0.5f) / 128f - 0.5f;
                    float dy = (y + 0.5f) / 128f - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float a = d < 0.82f ? 1f : Mathf.Clamp01((1f - d) / 0.18f);
                    _circulo.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            _circulo.Apply();

            _num = new GUIStyle(GUI.skin.label) { fontSize = 52, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _numAm = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            _txt = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            _txtP = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleLeft };
            _titulo = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _meio = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _pronto = true;
        }

        void Caixa(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _px);
            GUI.color = old;
        }

        void Barra(Rect r, float k, Color c)
        {
            Caixa(r, new Color(0f, 0f, 0f, 0.6f));
            Caixa(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(k), r.height - 4), c);
        }

        void Rotulo(Rect r, string s, GUIStyle st, Color c)
        {
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.85f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
            GUI.color = c;
            GUI.Label(r, s, st);
            GUI.color = old;
        }

        void RotuloC(Rect r, string s, GUIStyle st, Color c)
        {
            Rotulo(r, s, new GUIStyle(st) { alignment = TextAnchor.MiddleCenter }, c);
        }

        void OnGUI()
        {
            if (!_pronto) Preparar();

            float W = Screen.width, H = Screen.height;
            var gm = GameManager.I;

            DesenharFlashes(W, H);
            DesenharMira(W, H);

            // ---------------- placar superior
            if (gm != null)
            {
                Caixa(new Rect(W - 212, 12, 196, 62), CorFundo);
                Rotulo(new Rect(W - 204, 15, 184, 20), "BAIXADOS  " + gm.abates, _txtP, new Color(1f, 1f, 1f, 0.85f));
                Rotulo(new Rect(W - 204, 35, 184, 20), "EXECUTADOS  " + gm.execucoes, _txtP, new Color(1f, 0.7f, 0.3f, 0.85f));
                Rotulo(new Rect(W - 204, 53, 184, 16), "NA ÁREA  " + gm.Vivos.Count, _txtP, new Color(1f, 1f, 1f, 0.5f));
            }

            // ---------------- identificação
            Rotulo(new Rect(14, 10, 200, 20), "JUSTICEIRO", _txt, CorVermelho);
            Rotulo(new Rect(15, 30, 220, 16), "OPERADOR BOPE — MORRO DA MANGUEIRA", _txtP, new Color(1f, 1f, 1f, 0.55f));
            if (gm != null && gm.temChaveVermelha)
            {
                Caixa(new Rect(14, 54, 14, 18), new Color(1f, 0.15f, 0.15f, 0.95f));
                Rotulo(new Rect(34, 54, 120, 18), "CHAVE VERMELHA", _txtP, new Color(1f, 0.6f, 0.6f));
            }

            // ---------------- objetivo
            if (gm != null && Time.unscaledTime < gm.objetivoAte)
            {
                var tam = _txt.CalcSize(new GUIContent(gm.objetivo));
                Caixa(new Rect(W * 0.5f - tam.x * 0.5f - 18, 14, tam.x + 36, 36), CorFundo);
                Rotulo(new Rect(W * 0.5f - tam.x * 0.5f, 16, tam.x + 8, 32), gm.objetivo, _txt, new Color(1f, 0.85f, 0.45f));
            }

            DesenharBarraChefe(W, H);

            // ---------------- bloco de vida (esquerda)
            DesenharVida(W, H);

            // ---------------- bloco de arma + munição (direita)
            DesenharArma(W, H);

            // ---------------- prompt de execução
            if (exec != null && exec.candidato != null && !exec.executando)
            {
                string s = "[E]  TOMAR A RUA";
                var tam = _txt.CalcSize(new GUIContent(s));
                float pulso = 0.6f + Mathf.PingPong(Time.time * 2.4f, 0.4f);
                Caixa(new Rect(W * 0.5f - tam.x * 0.5f - 14, H * 0.5f + 60, tam.x + 28, 34), new Color(0f, 0f, 0f, 0.65f));
                Rotulo(new Rect(W * 0.5f - tam.x * 0.5f, H * 0.5f + 62, tam.x + 8, 30), s, _txt,
                    new Color(0.4f, 1f, 1f, pulso));
            }

            // ---------------- roda de armas
            DesenharRoda(W, H);

            // ---------------- telas
            if (gm != null && gm.estado == EstadoJogo.Morto) TelaMorte(W, H, gm);
            else if (gm != null && gm.estado == EstadoJogo.Vitoria) TelaVitoria(W, H, gm);
            else if (gm != null && gm.estado == EstadoJogo.Pausado) TelaPausa(W, H);
        }

        void DesenharVida(float W, float H)
        {
            if (vida == null) return;

            float bx = 24f, by = H - 116f;

            float vidaK = vida.vida / Mathf.Max(1f, vida.vidaMax);
            Color cv = vida.vida > vida.vidaMax ? new Color(0.5f, 1f, 0.9f) : CorVida;
            string num = Mathf.CeilToInt(vida.vida).ToString();

            var tam = _num.CalcSize(new GUIContent(num));
            Caixa(new Rect(bx - 8, by - 44, tam.x + 96, 74), new Color(0f, 0f, 0f, 0.5f));

            Rotulo(new Rect(bx, by - 42, 200, 52), num, _num, cv);
            Rotulo(new Rect(bx + tam.x + 14, by - 22, 64, 20), "VIDA", _txt, new Color(0.75f, 0.95f, 0.8f));
            Barra(new Rect(bx, by - 4, 250, 10), vidaK, cv);

            Rotulo(new Rect(bx, by + 12, 120, 22), Mathf.CeilToInt(vida.colete).ToString(), _txt, CorColete);
            Rotulo(new Rect(bx + 56, by + 14, 64, 18), "COLETE", _txtP, new Color(0.7f, 0.85f, 1f, 0.8f));
            Barra(new Rect(bx + 120, by + 16, 154, 8), vida.colete / Mathf.Max(1f, vida.coleteMax), CorColete);

            // cargas de dash
            if (pc != null)
            {
                float dy = by + 36f;
                for (int i = 0; i < pc.dashCargasMax; i++)
                {
                    bool cheio = i < pc.DashCargas;
                    Caixa(new Rect(bx + i * 32f, dy, 26f, 6f),
                        cheio ? new Color(0.4f, 0.9f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.15f));
                }
                Rotulo(new Rect(bx + pc.dashCargasMax * 32f + 6f, dy - 5f, 120, 18), "DASH", _txtP, new Color(0.6f, 0.9f, 1f, 0.75f));
            }
        }

        void DesenharArma(float W, float H)
        {
            if (armas == null || armas.Atual == null) return;

            var a = armas.Atual;
            float lx = W - 306f, ly = H - 116f;

            Caixa(new Rect(lx, ly, 282f, 74f), new Color(0f, 0f, 0f, 0.5f));
            Caixa(new Rect(lx, ly, 4f, 74f), CorVermelho);

            string mun = armas.MunicaoInfinita ? "∞" : armas.MunicaoAtual.ToString();
            var tam = _numAm.CalcSize(new GUIContent(mun));
            Rotulo(new Rect(lx + 14, ly + 6, tam.x + 40, 60), mun, _numAm, CorMunicao);

            string reserve = armas.MunicaoInfinita ? "SEM LIMITE" : "RESERVA " + armas.MunicaoAtual + " / " + armas.municaoMax[(int)a.municao];
            Rotulo(new Rect(lx + 14, ly + 50, 200, 18), reserve, _txtP, new Color(1f, 1f, 1f, 0.6f));

            var tam2 = _txt.CalcSize(new GUIContent(a.apelido));
            Rotulo(new Rect(lx + 220 - tam2.x, ly + 12, tam2.x + 10, 24), a.apelido, _txt, new Color(1f, 0.78f, 0.45f));
            Rotulo(new Rect(lx + 214, ly + 40, 70, 18), "[TAB] RODA", _txtP, new Color(1f, 1f, 1f, 0.45f));
        }

        void DesenharRoda(float W, float H)
        {
            if (armas == null || !armas.rodaAberta || armas.armas.Count == 0) return;

            Vector2 c = new Vector2(W * 0.5f, H * 0.5f);
            float raio = Mathf.Max(180f, Mathf.Min(W, H) * 0.27f);

            GUI.color = new Color(0f, 0f, 0f, 0.70f);
            GUI.DrawTexture(new Rect(c.x - raio, c.y - raio, raio * 2f, raio * 2f), _circulo);
            GUI.color = Color.white;

            int q = armas.armas.Count;
            for (int i = 0; i < q; i++)
            {
                float ang = i * 90f * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                Vector2 pos = c + dir * (raio * 0.78f);
                bool hover = i == armas.rodaHover;

                var box = new Rect(pos.x - 62f, pos.y - 26f, 124f, 52f);
                Caixa(box, hover ? new Color(0.85f, 0.16f, 0.12f, 0.92f) : new Color(0.08f, 0.08f, 0.10f, 0.85f));
                if (hover) Caixa(new Rect(box.x - 1, box.y - 1, box.width + 2, 2), new Color(1f, 1f, 1f, 0.85f));
                RotuloC(new Rect(box.x, box.y + 3, box.width, 24), armas.armas[i].apelido, _meio,
                    hover ? Color.white : new Color(1f, 1f, 1f, 0.65f));
                RotuloC(new Rect(box.x, box.y + 27, box.width, 18), (i + 1).ToString() + "  •  " + armas.armas[i].nome.Split('(')[0].Trim(),
                    new GUIStyle(_txtP) { alignment = TextAnchor.MiddleCenter }, hover ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.4f));
            }

            // hub central
            GUI.color = new Color(0.85f, 0.16f, 0.12f, 0.95f);
            GUI.DrawTexture(new Rect(c.x - 70f, c.y - 70f, 140f, 140f), _circulo);
            GUI.color = Color.white;
            string atual = armas.Atual != null ? armas.Atual.apelido : "—";
            RotuloC(new Rect(c.x - 60f, c.y - 34f, 120f, 30f), atual, _meio, Color.white);
            RotuloC(new Rect(c.x - 60f, c.y + 2f, 120f, 20f), "SOLTE [TAB]", new GUIStyle(_txtP) { alignment = TextAnchor.MiddleCenter },
                new Color(1f, 1f, 1f, 0.85f));
        }

        void DesenharFlashes(float W, float H)
        {
            if (vida == null) return;
            if (vida.flashDano > 0.01f)
                Caixa(new Rect(0, 0, W, H), new Color(0.7f, 0.05f, 0.05f, Mathf.Clamp01(vida.flashDano) * 0.42f));
            if (vida.flashCura > 0.01f)
                Caixa(new Rect(0, 0, W, H), new Color(0.2f, 0.9f, 0.4f, Mathf.Clamp01(vida.flashCura) * 0.16f));

            float k = vida.vida / Mathf.Max(1f, vida.vidaMax);
            if (k < 0.34f && !vida.Morto)
            {
                float p = 0.10f + Mathf.PingPong(Time.time * 1.6f, 0.10f);
                Caixa(new Rect(0, 0, W, H * 0.14f), new Color(0.7f, 0f, 0f, p * (1f - k)));
                Caixa(new Rect(0, H * 0.86f, W, H * 0.14f), new Color(0.7f, 0f, 0f, p * (1f - k)));
            }
        }

        void DesenharMira(float W, float H)
        {
            float cx = W * 0.5f, cy = H * 0.5f;
            float gap = 6f, len = 9f, esp = 2f;
            Color c = new Color(1f, 1f, 1f, 0.85f);
            if (exec != null && exec.candidato != null) c = new Color(0.4f, 1f, 1f, 1f);

            Caixa(new Rect(cx - gap - len, cy - esp * 0.5f, len, esp), c);
            Caixa(new Rect(cx + gap, cy - esp * 0.5f, len, esp), c);
            Caixa(new Rect(cx - esp * 0.5f, cy - gap - len, esp, len), c);
            Caixa(new Rect(cx - esp * 0.5f, cy + gap, esp, len), c);
            Caixa(new Rect(cx - 1, cy - 1, 2, 2), c);
        }

        void DesenharBarraChefe(float W, float H)
        {
            var gm = GameManager.I;
            if (gm == null) return;
            EnemyBrain chefe = null;
            for (int i = 0; i < gm.Vivos.Count; i++)
                if (gm.Vivos[i] != null && gm.Vivos[i].ehChefe) { chefe = gm.Vivos[i]; break; }
            if (chefe == null) return;

            float w = W * 0.5f, x = W * 0.25f, y = H - 150f;
            Rotulo(new Rect(x, y - 22, w, 20), chefe.nomeExibicao, _txt, new Color(1f, 0.4f, 0.35f));
            Barra(new Rect(x, y, w, 14), chefe.VidaNorm, new Color(0.9f, 0.15f, 0.15f));
        }

        void TelaMorte(float W, float H, GameManager gm)
        {
            Caixa(new Rect(0, 0, W, H), new Color(0.20f, 0f, 0f, 0.62f));
            RotuloC(new Rect(0, H * 0.32f, W, 50), "OPERADOR CAÍDO", _titulo, new Color(1f, 0.28f, 0.22f));
            RotuloC(new Rect(0, H * 0.42f, W, 26), "O MORRO VENCEU DESTA VEZ",
                new GUIStyle(_txt) { alignment = TextAnchor.MiddleCenter }, new Color(1f, 1f, 1f, 0.75f));
            RotuloC(new Rect(0, H * 0.48f, W, 26), "BAIXADOS: " + gm.abates + "   •   EXECUTADOS: " + gm.execucoes,
                new GUIStyle(_txt) { alignment = TextAnchor.MiddleCenter }, new Color(1f, 1f, 1f, 0.8f));
            RotuloC(new Rect(0, H * 0.57f, W, 30), "[ R ]  RETOMAR OPERAÇÃO",
                new GUIStyle(_txt) { alignment = TextAnchor.MiddleCenter }, new Color(1f, 0.85f, 0.4f));
        }

        void TelaVitoria(float W, float H, GameManager gm)
        {
            Caixa(new Rect(0, 0, W, H), new Color(0f, 0.06f, 0.02f, 0.62f));
            RotuloC(new Rect(0, H * 0.30f, W, 50), "MORRO LIMPO", _titulo, new Color(0.5f, 1f, 0.6f));
            RotuloC(new Rect(0, H * 0.40f, W, 26), "OPERAÇÃO CONCLUÍDA",
                new GUIStyle(_txt) { alignment = TextAnchor.MiddleCenter }, new Color(1f, 1f, 1f, 0.75f));
            var ct = new GUIStyle(_txt) { alignment = TextAnchor.MiddleCenter };
            RotuloC(new Rect(0, H * 0.46f, W, 26), "TEMPO: " + Mathf.FloorToInt(gm.tempo / 60f) + "m " +
                   Mathf.FloorToInt(gm.tempo % 60f) + "s   •   BAIXADOS: " + gm.abates, ct, new Color(1f, 1f, 1f, 0.8f));
            RotuloC(new Rect(0, H * 0.51f, W, 26), "EXECUTADOS: " + gm.execucoes, ct, new Color(1f, 0.7f, 0.3f, 0.8f));
            RotuloC(new Rect(0, H * 0.58f, W, 30), "[ R ]  OPERAÇÃO NOVA",
                new GUIStyle(_txt) { alignment = TextAnchor.MiddleCenter }, new Color(1f, 0.85f, 0.4f));
        }

        void TelaPausa(float W, float H)
        {
            Caixa(new Rect(0, 0, W, H), new Color(0f, 0f, 0f, 0.72f));
            RotuloC(new Rect(0, H * 0.20f, W, 46), "OPERAÇÃO PAUSADA", _titulo, Color.white);
            RotuloC(new Rect(0, H * 0.27f, W, 20), "BOPE — TERMINAL DO OPERADOR", new GUIStyle(_txtP) { alignment = TextAnchor.MiddleCenter },
                new Color(1f, 0.7f, 0.3f, 0.8f));

            var ct = new GUIStyle(_txtP) { alignment = TextAnchor.MiddleCenter, fontSize = 15 };
            string[] linhas =
            {
                "WASD  mover        SHIFT  correr        ESPAÇO  pulo duplo",
                "CTRL ESQ  dash (2 cargas)      TAB  roda de armas      1-4  atalho",
                "BOTÃO ESQ  atirar       BOTÃO DIR  coronhada (atordoa)",
                "E  execução quando o inimigo brilha      R  reiniciar (após morrer)",
                "",
                "[ ESC ]  VOLTAR AO COMBATE        [ F5 ]  REINICIAR"
            };
            for (int i = 0; i < linhas.Length; i++)
                RotuloC(new Rect(0, H * 0.34f + i * 26f, W, 24), linhas[i], ct, new Color(1f, 1f, 1f, 0.82f));
        }
    }
}