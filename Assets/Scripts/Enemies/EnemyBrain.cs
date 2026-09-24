using System;
using UnityEngine;

namespace Justiceiro
{
    public enum EstadoInimigo { Dormindo, Perseguindo, Atacando, Reposicionando, Atordoado, EmExecucao, Morto }
    public enum AtaqueInimigo { Corpo, Tiro, Bomba, Escopeta }

    [RequireComponent(typeof(CharacterController))]
    public class EnemyBrain : MonoBehaviour, IAlvo
    {
        // ---------------- config (preenchido pelo EnemyFactory)
        public TipoInimigo tipo = TipoInimigo.Moleque;
        public AtaqueInimigo ataque = AtaqueInimigo.Corpo;
        public float vidaMax = 60f;
        public float velocidade = 6f;
        public float dano = 12f;
        public float cadencia = 1.2f;
        public int rajada = 1;
        public float alcanceAtaque = 2.4f;
        public float alcancePreferido = 2.0f;
        public float alcanceVisao = 70f;
        public float dispersao = 2.5f;
        public float raioCorpo = 0.45f;
        public float alturaCorpo = 1.85f;
        public float limiarAtordoamento = 0.3f;
        public float duracaoAtordoamento = 5f;
        public bool podeSerExecutado = true;
        public Color corBrilho = new Color(0.3f, 1f, 1f);
        public string nomeExibicao = "MOLEQUE";
        public bool ehChefe;

        // ---------------- runtime
        public EstadoInimigo estado = EstadoInimigo.Dormindo;
        public float vida;
        public Action<EnemyBrain> aoMorrer;

        [HideInInspector] public Transform[] partesBrilho;
        [HideInInspector] public Transform bracoEsq, bracoDir, pernaEsq, pernaDir, cabeca, corpo;

        CharacterController cc;
        Transform alvo;
        PlayerHealth alvoVida;
        float velY;
        float proximoAtaque;
        float atordoadoAte;
        float proximoAtordoamentoPermitido;
        float passoT;
        float strafeDir = 1f;
        float proximoStrafe;
        float alertaT;
        Material[] matsBrilho;
        Color[] emissaoOriginal;
        float flashDano;
        Vector3 empurrao;

        public bool Morto => estado == EstadoInimigo.Morto;
        public bool Atordoado => estado == EstadoInimigo.Atordoado;
        public Transform Alvo => transform;
        public Vector3 CentroCorpo => transform.position + Vector3.up * (alturaCorpo * 0.55f);
        public float VidaNorm => vidaMax <= 0f ? 0f : Mathf.Clamp01(vida / vidaMax);

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            if (vida <= 0f) vida = vidaMax;
        }

        void Start()
        {
            var gm = GameManager.I;
            if (gm != null)
            {
                gm.Registrar(this);
                alvo = gm.jogador;
                alvoVida = gm.vidaJogador;
            }
            if (alvo == null)
            {
                var pl = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (pl != null) { alvo = pl.transform; alvoVida = pl.GetComponent<PlayerHealth>(); }
            }
            CachearBrilho();
        }

        void CachearBrilho()
        {
            if (partesBrilho == null) return;
            matsBrilho = new Material[partesBrilho.Length];
            emissaoOriginal = new Color[partesBrilho.Length];
            for (int i = 0; i < partesBrilho.Length; i++)
            {
                if (partesBrilho[i] == null) continue;
                var r = partesBrilho[i].GetComponent<Renderer>();
                if (r == null) continue;
                matsBrilho[i] = r.material;   // instância própria
                emissaoOriginal[i] = matsBrilho[i].HasProperty("_EmissionColor")
                    ? matsBrilho[i].GetColor("_EmissionColor") : Color.black;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm != null && gm.estado != EstadoJogo.Jogando && gm.estado != EstadoJogo.Morto) return;
            if (estado == EstadoInimigo.Morto || estado == EstadoInimigo.EmExecucao) return;
            if (alvo == null) return;

            float dt = Time.deltaTime;
            flashDano = Mathf.MoveTowards(flashDano, 0f, dt * 4f);

            float dist = Util.DistanciaPlana(transform.position, alvo.position);

            switch (estado)
            {
                case EstadoInimigo.Dormindo:
                    if (dist < alcanceVisao && TemVisao()) Acordar();
                    break;

                case EstadoInimigo.Atordoado:
                    if (Time.time > atordoadoAte) SairDoAtordoamento();
                    AplicarGravidadeEMovimento(Vector3.zero, dt);
                    break;

                default:
                    Combater(dist, dt);
                    break;
            }

            AtualizarVisual(dt);
        }

        bool TemVisao()
        {
            if (alvo == null) return false;
            Vector3 olho = transform.position + Vector3.up * (alturaCorpo * 0.8f);
            Vector3 alvoP = alvo.position + Vector3.up * 1.3f;
            Vector3 d = alvoP - olho;
            if (Physics.Raycast(olho, d.normalized, out RaycastHit h, d.magnitude, Layers.World, QueryTriggerInteraction.Ignore))
                return false;
            return true;
        }

        public void Acordar()
        {
            if (estado != EstadoInimigo.Dormindo) return;
            estado = EstadoInimigo.Perseguindo;
            alertaT = 0.35f;
            Sfx.TocarEm("alerta", transform.position, 0.45f, UnityEngine.Random.Range(0.85f, 1.2f), 40f);
        }

        void Combater(float dist, float dt)
        {
            bool visao = TemVisao();
            Vector3 paraAlvo = Util.Plano(alvo.position - transform.position);
            Vector3 dirAlvo = paraAlvo.sqrMagnitude > 0.001f ? paraAlvo.normalized : transform.forward;

            // olha pro jogador
            Quaternion q = Quaternion.LookRotation(dirAlvo);
            transform.rotation = Quaternion.Slerp(transform.rotation, q, dt * 8f);

            Vector3 mov = Vector3.zero;

            if (!visao || dist > alcancePreferido + 1.5f)
            {
                estado = EstadoInimigo.Perseguindo;
                mov = dirAlvo;
            }
            else if (dist < alcancePreferido - 1.5f && ataque != AtaqueInimigo.Corpo)
            {
                estado = EstadoInimigo.Reposicionando;
                mov = -dirAlvo * 0.85f;
            }
            else
            {
                estado = EstadoInimigo.Atacando;
                // strafe lateral pra não virar alvo parado
                if (Time.time > proximoStrafe)
                {
                    strafeDir = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                    proximoStrafe = Time.time + UnityEngine.Random.Range(0.7f, 1.8f);
                }
                mov = Vector3.Cross(Vector3.up, dirAlvo) * strafeDir * 0.7f;
            }

            mov = Desviar(mov);
            AplicarGravidadeEMovimento(mov, dt);

            if (visao && dist <= alcanceAtaque && Time.time >= proximoAtaque)
                Atacar(dirAlvo);
        }

        /// <summary>Desvio bem simples: se tem parede na frente, escorrega pro lado.</summary>
        Vector3 Desviar(Vector3 mov)
        {
            if (mov.sqrMagnitude < 0.01f) return mov;
            Vector3 origem = transform.position + Vector3.up * 0.9f;
            Vector3 d = mov.normalized;
            if (Physics.Raycast(origem, d, raioCorpo + 0.9f, Layers.World, QueryTriggerInteraction.Ignore))
            {
                Vector3 lado = Vector3.Cross(Vector3.up, d) * strafeDir;
                if (!Physics.Raycast(origem, lado, raioCorpo + 1.2f, Layers.World, QueryTriggerInteraction.Ignore))
                    return (d * 0.35f + lado).normalized;
                return (d * 0.35f - lado).normalized;
            }
            return mov;
        }

        void AplicarGravidadeEMovimento(Vector3 mov, float dt)
        {
            if (cc == null || !cc.enabled) return;

            if (cc.isGrounded && velY < 0f) velY = -2f;
            velY += -26f * dt;

            empurrao = Vector3.Lerp(empurrao, Vector3.zero, dt * 6f);

            Vector3 v = mov * velocidade + empurrao + Vector3.up * velY;
            cc.Move(v * dt);

            if (mov.sqrMagnitude > 0.01f) passoT += dt * velocidade * 1.6f;
            else passoT = Mathf.Lerp(passoT, 0f, dt * 5f);
        }

        void Atacar(Vector3 dir)
        {
            proximoAtaque = Time.time + 1f / Mathf.Max(0.05f, cadencia);
            Vector3 olho = transform.position + Vector3.up * (alturaCorpo * 0.8f);

            switch (ataque)
            {
                case AtaqueInimigo.Corpo:
                    if (Util.DistanciaPlana(transform.position, alvo.position) <= alcanceAtaque + 0.3f && alvoVida != null)
                    {
                        alvoVida.ReceberDano(dano, alvo.position, dir, false);
                        Sfx.TocarEm("carne", transform.position, 0.6f, 1.15f, 30f);
                    }
                    BalancarBraco();
                    break;

                case AtaqueInimigo.Tiro:
                    StartCoroutine(Rajada(olho));
                    break;

                case AtaqueInimigo.Escopeta:
                    for (int i = 0; i < 7; i++) TiroUnico(olho, dano * 0.32f, 6.5f, new Color(1f, 0.7f, 0.3f));
                    Sfx.TocarEm("espingarda", olho, 0.7f, 1f, 55f);
                    BalancarBraco();
                    break;

                case AtaqueInimigo.Bomba:
                    Vector3 mira = (alvo.position + Vector3.up * 1.0f - olho).normalized;
                    Projectile.Criar(olho + mira * 0.8f, Util.ComDispersao(mira, 2.5f), dano, 22f, 4.5f,
                                     gameObject, false, new Color(1f, 0.35f, 0.25f));
                    Sfx.TocarEm("rojao_disparo", olho, 0.7f, 1.1f, 60f);
                    BalancarBraco();
                    break;
            }
        }

        System.Collections.IEnumerator Rajada(Vector3 olho)
        {
            for (int i = 0; i < rajada; i++)
            {
                if (Morto || estado == EstadoInimigo.Atordoado) yield break;
                TiroUnico(transform.position + Vector3.up * (alturaCorpo * 0.8f), dano, dispersao, new Color(1f, 0.85f, 0.5f));
                Sfx.TocarEm("rajada", transform.position, 0.5f, UnityEngine.Random.Range(0.9f, 1.1f), 55f);
                BalancarBraco();
                yield return new WaitForSeconds(0.09f);
            }
        }

        void TiroUnico(Vector3 olho, float d, float disp, Color cor)
        {
            if (alvo == null) return;
            Vector3 dir = Util.ComDispersao((alvo.position + Vector3.up * 1.2f - olho).normalized, disp);
            Vector3 fim = olho + dir * 120f;

            if (Physics.Raycast(olho, dir, out RaycastHit h, 120f, Layers.EnemyShoot, QueryTriggerInteraction.Ignore))
            {
                fim = h.point;
                var a = h.collider.GetComponentInParent<IAlvo>();
                if (a != null && !a.Morto && !(a is EnemyBrain))
                {
                    a.ReceberDano(d, h.point, dir, false);
                    Fx.Impacto(h.point, h.normal, true);
                }
                else Fx.Impacto(h.point, h.normal, false);
            }
            Fx.Tracer(olho, fim, cor, 0.035f, 0.045f);
        }

        void BalancarBraco()
        {
            if (bracoDir != null) bracoDir.localRotation = Quaternion.Euler(-95f, 0f, 0f);
        }

        // ---------------------------------------------------------------- dano

        public void ReceberDano(float d, Vector3 ponto, Vector3 direcao, bool pesado)
        {
            if (Morto || estado == EstadoInimigo.EmExecucao || d <= 0f) return;

            vida -= d;
            flashDano = 1f;
            empurrao += Util.Plano(direcao).normalized * Mathf.Min(7f, d * (pesado ? 0.22f : 0.08f));

            if (estado == EstadoInimigo.Dormindo) Acordar();

            if (vida <= 0f)
            {
                Morrer(false);
                return;
            }

            bool podeCair = podeSerExecutado
                            && estado != EstadoInimigo.Atordoado
                            && Time.time > proximoAtordoamentoPermitido
                            && VidaNorm <= limiarAtordoamento;

            if (podeCair || (pesado && podeSerExecutado && UnityEngine.Random.value < 0.18f && VidaNorm < 0.6f))
                ForcarAtordoamento(duracaoAtordoamento);
        }

        public void ForcarAtordoamento(float dur)
        {
            if (Morto || !podeSerExecutado) return;
            estado = EstadoInimigo.Atordoado;
            atordoadoAte = Time.time + dur;
            StopAllCoroutines();
            Sfx.TocarEm("alerta", transform.position, 0.35f, 0.7f, 30f);
        }

        void SairDoAtordoamento()
        {
            estado = EstadoInimigo.Perseguindo;
            proximoAtordoamentoPermitido = Time.time + 6f;
            vida = Mathf.Max(vida, vidaMax * (limiarAtordoamento + 0.12f));
        }

        public void EntrarEmExecucao()
        {
            estado = EstadoInimigo.EmExecucao;
            StopAllCoroutines();
            if (cc != null) cc.enabled = false;
        }

        public void MorrerPorExecucao() => Morrer(true);

        void Morrer(bool porExecucao)
        {
            if (estado == EstadoInimigo.Morto) return;
            estado = EstadoInimigo.Morto;
            StopAllCoroutines();

            var gm = GameManager.I;
            if (gm != null) gm.Remover(this, porExecucao);
            aoMorrer?.Invoke(this);

            Vector3 p = CentroCorpo;
            Sfx.TocarEm("carne", p, 0.8f, UnityEngine.Random.Range(0.8f, 1.0f), 40f);
            Fx.Impacto(p, Vector3.up, true);

            // solta recurso: execução dá vida, tiro comum dá munição
            if (porExecucao)
            {
                Pickup.Criar(TipoColetavel.Vida, p + Vector3.up * 0.2f, 12f);
                Pickup.Criar(TipoColetavel.Vida, p + Vector3.up * 0.2f + UnityEngine.Random.insideUnitSphere * 0.6f, 12f);
            }
            else if (UnityEngine.Random.value < (ehChefe ? 1f : 0.42f))
            {
                var t = UnityEngine.Random.value < 0.5f ? TipoColetavel.MunicaoBala : TipoColetavel.MunicaoCartucho;
                if (ehChefe) t = TipoColetavel.MunicaoRojao;
                Pickup.Criar(t, p, 0f);
            }

            Desmontar();
            Destroy(gameObject, 0.05f);
        }

        /// <summary>Explode o boneco em pedaços que caem — "ragdoll de pobre", barato e satisfatório.</summary>
        void Desmontar()
        {
            var renders = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renders.Length; i++)
            {
                var r = renders[i];
                if (r == null) continue;
                var go = r.gameObject;
                go.transform.SetParent(null, true);

                var c = go.GetComponent<Collider>();
                if (c != null) Destroy(c);
                var l = go.GetComponentInChildren<Light>();
                if (l != null) Destroy(l.gameObject);

                var e = go.AddComponent<Estilhaco>();
                e.vel = (UnityEngine.Random.insideUnitSphere + Vector3.up * 0.8f).normalized
                        * UnityEngine.Random.Range(3f, 8f);
                e.gravidade = -20f;
                go.AddComponent<AutoDestruir>().tempo = UnityEngine.Random.Range(1.6f, 2.6f);
            }
        }

        // ---------------------------------------------------------------- visual

        void AtualizarVisual(float dt)
        {
            // andar: balanço de pernas e braços
            float sw = Mathf.Sin(passoT) * 38f;
            if (pernaEsq != null) pernaEsq.localRotation = Quaternion.Euler(sw, 0f, 0f);
            if (pernaDir != null) pernaDir.localRotation = Quaternion.Euler(-sw, 0f, 0f);
            if (bracoEsq != null) bracoEsq.localRotation = Quaternion.Euler(-sw * 0.5f - 15f, 0f, 0f);

            if (bracoDir != null)
            {
                float alvoAng = (estado == EstadoInimigo.Atacando && ataque != AtaqueInimigo.Corpo) ? -80f : (sw * 0.5f - 15f);
                bracoDir.localRotation = Quaternion.Slerp(bracoDir.localRotation,
                    Quaternion.Euler(alvoAng, 0f, 0f), dt * 12f);
            }

            bool atordoado = estado == EstadoInimigo.Atordoado || estado == EstadoInimigo.EmExecucao;
            float pulso = atordoado ? (0.6f + Mathf.PingPong(Time.time * 3.2f, 0.9f)) : 0f;

            if (matsBrilho != null)
            {
                for (int i = 0; i < matsBrilho.Length; i++)
                {
                    var m = matsBrilho[i];
                    if (m == null || !m.HasProperty("_EmissionColor")) continue;
                    Color c = emissaoOriginal[i];
                    if (atordoado) c = corBrilho * pulso;
                    if (flashDano > 0.01f) c = Color.Lerp(c, Color.white * 1.6f, flashDano);
                    m.SetColor("_EmissionColor", c);
                    m.EnableKeyword("_EMISSION");
                }
            }

            if (atordoado)
            {
                // cambaleia
                float t = Mathf.Sin(Time.time * 9f) * 8f;
                if (corpo != null) corpo.localRotation = Quaternion.Euler(12f, 0f, t);
            }
            else if (corpo != null)
            {
                corpo.localRotation = Quaternion.Slerp(corpo.localRotation, Quaternion.identity, dt * 8f);
            }

            if (alertaT > 0f) alertaT -= dt;
        }
    }
}
