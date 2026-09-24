using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    public enum TipoDisparo { Hitscan, Espingarda, Projetil }

    [System.Serializable]
    public class Arma
    {
        public string nome;
        public string apelido;
        public TipoDisparo tipo;
        public TipoMunicao municao;
        public float dano = 20f;
        public int projeteis = 1;
        public float dispersao = 0.4f;
        public float cadencia = 5f;      // tiros por segundo
        public bool automatica;
        public float alcance = 140f;
        public float recuo = 1.6f;
        public float empurrao = 3f;
        public int gastoPorTiro = 1;
        public Color corTracer = new Color(1f, 0.92f, 0.55f);
        public Color corArma = new Color(0.16f, 0.16f, 0.18f);
        public Vector3 tamanhoCano = new Vector3(0.08f, 0.08f, 0.55f);
        public string som = "pistola";
    }

    public class WeaponController : MonoBehaviour
    {
        public Transform cam;
        public List<Arma> armas = new List<Arma>();
        public int indice;

        [Header("Munição")]
        public int[] municao = new int[4];
        public int[] municaoMax = new int[4];

        [Header("Coronhada")]
        public float danoCoronhada = 30f;
        public float alcanceCoronhada = 2.6f;
        public float recargaCoronhada = 0.75f;

        [Header("Roda de armas")]
        public KeyCode teclaRoda = KeyCode.Tab;

        float proximoTiro;
        float proximaCoronhada;

        // viewmodel
        Transform vmRaiz;
        Vector3 vmPosBase = new Vector3(0.28f, -0.24f, 0.45f);
        Vector3 vmRecuo;
        float vmRecuoAng;
        Light luzCano;
        float luzT;
        readonly List<Transform> _pecas = new List<Transform>();

        static readonly Dictionary<string, Material> _vmMats = new Dictionary<string, Material>();
        bool _rodaAberta;
        int _rodaHover = -1;

        PlayerController pc;
        PlayerExecucao exec;

        /// <summary>Roda aberta (estado global — desliga o mouse look do jogador).</summary>
        public static bool RodaAbertaGlobal { get; private set; }
        public bool rodaAberta => _rodaAberta;
        public int rodaHover => _rodaHover;

        public Arma Atual => (indice >= 0 && indice < armas.Count) ? armas[indice] : null;
        public int MunicaoAtual => Atual == null ? 0 : Municao(Atual.municao);
        public bool MunicaoInfinita => Atual != null && Atual.municao == TipoMunicao.Infinita;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetarEstaticos()
        {
            RodaAbertaGlobal = false;
            _vmMats.Clear();
        }

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            exec = GetComponent<PlayerExecucao>();
            if (cam == null && pc != null) cam = pc.cam;

            if (armas.Count == 0) MontarArsenal();

            municaoMax = new int[4] { 0, 48, 320, 24 };
            municao = new int[4] { 0, 16, 120, 5 };

            ConstruirViewmodel();
        }

        void MontarArsenal()
        {
            armas.Add(new Arma
            {
                nome = "PISTOLA .40",
                apelido = ".40",
                tipo = TipoDisparo.Hitscan,
                municao = TipoMunicao.Infinita,
                dano = 22f,
                cadencia = 5.5f,
                dispersao = 0.35f,
                automatica = false,
                recuo = 1.5f,
                empurrao = 2.5f,
                corArma = new Color(0.14f, 0.14f, 0.16f),
                tamanhoCano = new Vector3(0.07f, 0.07f, 0.34f),
                som = "pistola"
            });

            armas.Add(new Arma
            {
                nome = "DOZE (CANO DUPLO)",
                apelido = "DOZE",
                tipo = TipoDisparo.Espingarda,
                municao = TipoMunicao.Cartucho,
                dano = 11.5f,
                projeteis = 10,
                dispersao = 4.6f,
                cadencia = 1.35f,
                automatica = false,
                alcance = 45f,
                recuo = 5.5f,
                empurrao = 9f,
                corArma = new Color(0.32f, 0.20f, 0.10f),
                corTracer = new Color(1f, 0.75f, 0.35f),
                tamanhoCano = new Vector3(0.12f, 0.09f, 0.62f),
                som = "espingarda"
            });

            armas.Add(new Arma
            {
                nome = "RAJADA",
                apelido = "RAJADA",
                tipo = TipoDisparo.Hitscan,
                municao = TipoMunicao.Bala,
                dano = 11f,
                cadencia = 11.5f,
                dispersao = 1.5f,
                automatica = true,
                recuo = 1.0f,
                empurrao = 1.2f,
                corArma = new Color(0.20f, 0.22f, 0.20f),
                corTracer = new Color(0.8f, 1f, 0.8f),
                tamanhoCano = new Vector3(0.08f, 0.08f, 0.70f),
                som = "rajada"
            });

            armas.Add(new Arma
            {
                nome = "ROJÃO",
                apelido = "ROJÃO",
                tipo = TipoDisparo.Projetil,
                municao = TipoMunicao.Rojao,
                dano = 95f,
                cadencia = 0.9f,
                dispersao = 0f,
                automatica = false,
                recuo = 6.5f,
                empurrao = 0f,
                corArma = new Color(0.35f, 0.14f, 0.10f),
                tamanhoCano = new Vector3(0.17f, 0.17f, 0.85f),
                som = "rojao_disparo"
            });
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.estado != EstadoJogo.Jogando)
            {
                FecharRodaForcada();
                AnimarViewmodel();
                return;
            }
            if (exec != null && exec.executando) { AnimarViewmodel(); return; }

            if (AtualizarRoda())
            {
                AnimarViewmodel();
                return;
            }

            Trocar();

            bool querAtirar = Atual != null && (Atual.automatica ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0));
            if (querAtirar && Time.time >= proximoTiro) Atirar();

            if (Input.GetMouseButtonDown(1) && Time.time >= proximaCoronhada) Coronhada();

            AnimarViewmodel();
        }

        // ------------------------------------------------------------ roda de armas

        bool AtualizarRoda()
        {
            if (Input.GetKeyDown(teclaRoda))
            {
                _rodaAberta = true;
                RodaAbertaGlobal = true;
                _rodaHover = indice;
                GameManager.TravarCursor(false);
            }

            if (!_rodaAberta) return false;

            if (Input.GetKeyUp(teclaRoda))
            {
                _rodaAberta = false;
                RodaAbertaGlobal = false;
                GameManager.TravarCursor(true);
                if (_rodaHover >= 0 && _rodaHover != indice) Selecionar(_rodaHover);
                return true;
            }

            Vector2 dif = (Vector2)Input.mousePosition - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (dif.sqrMagnitude < 30f * 30f)
            {
                _rodaHover = indice;
            }
            else
            {
                float ang = Mathf.Atan2(dif.y, dif.x) * Mathf.Rad2Deg;
                ang = (ang + 45f + 360f) % 360f;
                _rodaHover = Mathf.FloorToInt(ang / 90f) % armas.Count;
            }
            return true;
        }

        void FecharRodaForcada()
        {
            if (!_rodaAberta) return;
            _rodaAberta = false;
            RodaAbertaGlobal = false;
            if (GameManager.I != null) GameManager.TravarCursor(true);
        }

        public void Selecionar(int idx)
        {
            idx = Mathf.Clamp(idx, 0, armas.Count - 1);
            if (idx == indice) return;
            indice = idx;
            proximoTiro = Time.time + 0.18f;
            Sfx.Tocar("arma_troca", 0.4f, 1f);
            AtualizarViewmodel();
        }

        void Trocar()
        {
            int novo = indice;
            if (Input.GetKeyDown(KeyCode.Alpha1)) novo = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) novo = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) novo = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4)) novo = 3;

            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll > 0.01f) novo = (indice + 1) % armas.Count;
            else if (scroll < -0.01f) novo = (indice - 1 + armas.Count) % armas.Count;

            if (novo != indice) Selecionar(novo);
        }

        int Municao(TipoMunicao t) => t == TipoMunicao.Infinita ? 999 : municao[(int)t];

        public void DarMunicao(TipoMunicao t, int qtd)
        {
            if (t == TipoMunicao.Infinita) return;
            int i = (int)t;
            municao[i] = Mathf.Min(municaoMax[i], municao[i] + qtd);
        }

        public void DarMunicaoTudo(float fracao)
        {
            for (int i = 1; i < 4; i++)
                municao[i] = Mathf.Min(municaoMax[i], municao[i] + Mathf.CeilToInt(municaoMax[i] * fracao));
        }

        void Atirar()
        {
            var a = Atual;
            if (a == null || cam == null) return;

            if (a.municao != TipoMunicao.Infinita)
            {
                int i = (int)a.municao;
                if (municao[i] < a.gastoPorTiro)
                {
                    proximoTiro = Time.time + 0.25f;
                    Sfx.Tocar("vazio", 0.5f);
                    return;
                }
                municao[i] -= a.gastoPorTiro;
            }

            proximoTiro = Time.time + 1f / Mathf.Max(0.05f, a.cadencia);

            Vector3 origem = cam.position;
            Vector3 frente = cam.forward;

            switch (a.tipo)
            {
                case TipoDisparo.Hitscan:
                    Raio(origem, Util.ComDispersao(frente, a.dispersao), a, false);
                    break;

                case TipoDisparo.Espingarda:
                    for (int p = 0; p < a.projeteis; p++)
                        Raio(origem, Util.ComDispersao(frente, a.dispersao), a, p == 0);
                    break;

                case TipoDisparo.Projetil:
                    Projectile.Criar(origem + frente * 0.9f, frente, a.dano, 34f, 5.5f, gameObject);
                    break;
            }

            // recuo do viewmodel + câmera
            vmRecuo += new Vector3(0f, 0.012f * a.recuo, -0.045f * a.recuo);
            vmRecuoAng -= a.recuo * 2.6f;
            luzT = 0.05f;

            if (pc != null && a.empurrao > 0f)
                pc.Empurrar(-cam.forward * a.empurrao * 0.35f);

            Sfx.Tocar(a.som, 0.85f, Random.Range(0.94f, 1.07f));
        }

        void Raio(Vector3 origem, Vector3 dir, Arma a, bool fazTracer)
        {
            Vector3 fim = origem + dir * a.alcance;
            if (Physics.Raycast(origem, dir, out RaycastHit h, a.alcance, Layers.PlayerShoot, QueryTriggerInteraction.Ignore))
            {
                fim = h.point;
                var alvo = h.collider.GetComponentInParent<IAlvo>();
                bool sangue = false;
                if (alvo != null && !alvo.Morto)
                {
                    alvo.ReceberDano(a.dano, h.point, dir, a.tipo == TipoDisparo.Espingarda);
                    sangue = true;
                }
                Fx.Impacto(h.point, h.normal, sangue);
                if (!sangue) Sfx.TocarEm("impacto", h.point, 0.35f, Random.Range(0.9f, 1.2f), 25f);
            }

            if (fazTracer || a.tipo != TipoDisparo.Espingarda)
                Fx.Tracer(origem + dir * 0.6f, fim, a.corTracer);
        }

        void Coronhada()
        {
            if (cam == null) return;
            proximaCoronhada = Time.time + recargaCoronhada;
            vmRecuo += new Vector3(-0.10f, 0.03f, -0.15f);
            vmRecuoAng -= 26f;

            var cols = Physics.OverlapSphere(cam.position + cam.forward * 1.2f, alcanceCoronhada * 0.65f,
                                             Layers.Enemy, QueryTriggerInteraction.Ignore);
            bool acertou = false;
            for (int i = 0; i < cols.Length; i++)
            {
                var e = cols[i].GetComponentInParent<EnemyBrain>();
                if (e == null || e.Morto) continue;
                if (Vector3.Angle(cam.forward, e.CentroCorpo - cam.position) > 75f) continue;
                e.ReceberDano(danoCoronhada, e.CentroCorpo, cam.forward, true);
                e.ForcarAtordoamento(2.2f);
                acertou = true;
            }

            Sfx.Tocar(acertou ? "carne" : "impacto", 0.7f, acertou ? 0.9f : 1.4f);
        }

        // ------------------------------------------------------------ viewmodel

        static Material MatVM(string chave, Color cor, float metal, float liso)
        {
            if (_vmMats.TryGetValue(chave, out var m) && m != null) return m;
            m = Util.MaterialSimples(cor, metal, liso);
            _vmMats[chave] = m;
            return m;
        }

        void ConstruirViewmodel()
        {
            if (cam == null) return;

            var raiz = new GameObject("Viewmodel");
            raiz.transform.SetParent(cam, false);
            raiz.transform.localPosition = vmPosBase;
            raiz.layer = gameObject.layer;
            vmRaiz = raiz.transform;

            var luzGo = new GameObject("LuzCano");
            luzGo.transform.SetParent(vmRaiz, false);
            luzCano = luzGo.AddComponent<Light>();
            luzCano.type = LightType.Point;
            luzCano.range = 9f;
            luzCano.intensity = 0f;
            luzCano.color = new Color(1f, 0.85f, 0.5f);
            luzCano.shadows = LightShadows.None;

            AtualizarViewmodel();
        }

        void LimparPecas()
        {
            for (int i = 0; i < _pecas.Count; i++)
            {
                var p = _pecas[i];
                if (p == null) continue;
                var rs = p.GetComponentsInChildren<Renderer>();
                for (int j = 0; j < rs.Length; j++) rs[j].enabled = false;
                Destroy(p.gameObject);
            }
            _pecas.Clear();
        }

        Transform PecaVM(Vector3 pos, Vector3 esc, Vector3 rot, Material mat, Arma a = null)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(g.GetComponent<Collider>());
            g.name = "vm_peca";
            g.layer = gameObject.layer;
            g.transform.SetParent(vmRaiz, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(rot);
            g.transform.localScale = esc;
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            if (a != null)
            {
                var m = new Material(mat);
                m.color = Color.Lerp(mat.color, a.corArma, 0.35f);
                r.sharedMaterial = m;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _pecas.Add(g.transform);
            return g.transform;
        }

        void AtualizarViewmodel()
        {
            var a = Atual;
            if (a == null) return;
            MontarArmaVM(a);

            if (luzCano != null)
                luzCano.transform.localPosition = new Vector3(0f, 0.03f, a.tipo == TipoDisparo.Projetil ? 0.38f : 0.28f);
        }

        /// <summary>Monta a silhueta da arma na mão — forma e cor leem a arma de longe.</summary>
        void MontarArmaVM(Arma a)
        {
            LimparPecas();

            var fer = MatVM("vm_fer", new Color(0.08f, 0.08f, 0.09f), 0.85f, 0.5f);
            var ferClaro = MatVM("vm_fer_claro", new Color(0.18f, 0.18f, 0.20f), 0.8f, 0.45f);
            var preto = MatVM("vm_preto", new Color(0.05f, 0.05f, 0.06f), 0.6f, 0.3f);
            var madeira = MatVM("vm_madeira", new Color(0.38f, 0.23f, 0.10f), 0.1f, 0.2f);
            var oliva = MatVM("vm_oliva", new Color(0.27f, 0.31f, 0.18f), 0.4f, 0.25f);

            switch (a.tipo)
            {
                case TipoDisparo.Espingarda:
                    // DOZE — coronha de madeira, cano duplo
                    PecaVM(new Vector3(0f, 0f, 0.02f), new Vector3(0.10f, 0.08f, 0.42f), Vector3.zero, madeira, a);
                    PecaVM(new Vector3(0f, 0.025f, 0.30f), new Vector3(0.065f, 0.06f, 0.28f), Vector3.zero, fer, a);
                    PecaVM(new Vector3(0f, 0.025f, 0.30f), new Vector3(0.038f, 0.038f, 0.30f), Vector3.zero, ferClaro, a);
                    PecaVM(new Vector3(0.012f, 0.03f, 0.45f), new Vector3(0.015f, 0.015f, 0.06f), Vector3.zero, fer, a); // alça de mira
                    PecaVM(new Vector3(0f, -0.025f, -0.16f), new Vector3(0.09f, 0.055f, 0.13f), new Vector3(10f, 0f, 0f), madeira, a);
                    PecaVM(new Vector3(0f, -0.05f, 0.07f), new Vector3(0.085f, 0.045f, 0.11f), Vector3.zero, fer, a); // bomba
                    break;

                case TipoDisparo.Projetil:
                    // ROJÃO — tubo verde-oliva com boca aberta
                    PecaVM(new Vector3(0f, 0f, 0.02f), new Vector3(0.16f, 0.16f, 0.44f), Vector3.zero, oliva, a);
                    PecaVM(new Vector3(0f, 0f, 0.25f), new Vector3(0.14f, 0.14f, 0.12f), new Vector3(-18f, 0f, 0f), fer, a);
                    PecaVM(new Vector3(0f, 0f, 0.30f), new Vector3(0.19f, 0.02f, 0.02f), Vector3.zero, ferClaro, a); // boca
                    PecaVM(new Vector3(0f, -0.06f, -0.10f), new Vector3(0.06f, 0.10f, 0.12f), new Vector3(18f, 0f, 0f), preto, a);
                    PecaVM(new Vector3(0f, -0.02f, -0.28f), new Vector3(0.07f, 0.07f, 0.10f), Vector3.zero, fer, a); // mira
                    break;

                case TipoDisparo.Hitscan when a.municao == TipoMunicao.Bala:
                    // RAJADA — corpo curto, carregador inclinado
                    PecaVM(new Vector3(0f, 0f, 0.02f), new Vector3(0.07f, 0.08f, 0.30f), Vector3.zero, oliva, a);
                    PecaVM(new Vector3(0f, 0.005f, 0.26f), new Vector3(0.04f, 0.04f, 0.16f), Vector3.zero, fer, a); // cano
                    PecaVM(new Vector3(0f, -0.045f, 0.03f), new Vector3(0.045f, 0.08f, 0.09f), new Vector3(-16f, 0f, 0f), fer, a);
                    PecaVM(new Vector3(0f, -0.02f, -0.17f), new Vector3(0.055f, 0.05f, 0.11f), new Vector3(8f, 0f, 0f), ferClaro, a); // coronha
                    PecaVM(new Vector3(0f, 0f, 0.15f), new Vector3(0.02f, 0.035f, 0.02f), Vector3.zero, preto, a); // alça
                    break;

                default:
                    // PISTOLA .40 — slide baixo, cano curto, empunhadura
                    PecaVM(new Vector3(0f, 0f, 0.04f), new Vector3(0.06f, 0.065f, 0.20f), Vector3.zero, ferClaro, a);
                    PecaVM(new Vector3(0f, 0.02f, 0.17f), new Vector3(0.038f, 0.038f, 0.10f), Vector3.zero, fer, a);
                    PecaVM(new Vector3(0.012f, 0.03f, 0.20f), new Vector3(0.012f, 0.02f, 0.025f), Vector3.zero, fer, a); // alça de mira
                    PecaVM(new Vector3(0f, -0.065f, -0.02f), new Vector3(0.055f, 0.12f, 0.07f), new Vector3(14f, 0f, 0f), preto, a);
                    PecaVM(new Vector3(0f, -0.09f, -0.05f), new Vector3(0.04f, 0.06f, 0.05f), new Vector3(10f, 0f, 0f), preto, a); // carregador
                    break;
            }
        }

        void AnimarViewmodel()
        {
            if (vmRaiz == null) return;
            float dt = Time.deltaTime;

            vmRecuo = Vector3.Lerp(vmRecuo, Vector3.zero, dt * 13f);
            vmRecuoAng = Mathf.Lerp(vmRecuoAng, 0f, dt * 13f);

            float vel = pc != null ? Util.Plano(pc.VelocidadeHorizontal).magnitude : 0f;
            float sway = Mathf.Sin(Time.time * 7f) * 0.012f * Mathf.Clamp01(vel / 10f);
            float sway2 = Mathf.Cos(Time.time * 3.5f) * 0.016f * Mathf.Clamp01(vel / 10f);

            float mx = -Input.GetAxisRaw("Mouse X") * 0.006f;
            float my = -Input.GetAxisRaw("Mouse Y") * 0.006f;
            mx = Mathf.Clamp(mx, -0.05f, 0.05f);
            my = Mathf.Clamp(my, -0.05f, 0.05f);

            vmRaiz.localPosition = vmPosBase + vmRecuo + new Vector3(sway + mx, sway2 + my, 0f);
            vmRaiz.localRotation = Quaternion.Euler(vmRecuoAng, 0f, vmRecuoAng * 0.25f);

            if (luzCano != null)
            {
                luzT -= dt;
                luzCano.intensity = luzT > 0f ? 9f : Mathf.Lerp(luzCano.intensity, 0f, dt * 25f);
            }
        }
    }
}