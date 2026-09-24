using UnityEngine;

namespace Justiceiro
{
    /// <summary>
    /// "Glory kill" brasileiro: inimigo atordoado brilha, você chega perto e aperta E
    /// para tomar a rua dele — devolve vida e munição. É o motor do combate agressivo.
    /// </summary>
    public class PlayerExecucao : MonoBehaviour
    {
        public float alcance = 3.2f;
        public float anguloMax = 60f;
        public float vidaGanha = 28f;
        public float duracao = 0.55f;

        [HideInInspector] public EnemyBrain candidato;
        [HideInInspector] public bool executando;

        Transform cam;
        PlayerHealth vida;
        PlayerController pc;
        WeaponController armas;

        float t;
        EnemyBrain atual;
        Vector3 camLocalOrig;

        static readonly Collider[] _buf = new Collider[32];

        void Awake()
        {
            pc = GetComponent<PlayerController>();
            vida = GetComponent<PlayerHealth>();
            armas = GetComponent<WeaponController>();
            cam = pc != null ? pc.cam : null;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.estado != EstadoJogo.Jogando) return;
            if (WeaponController.RodaAbertaGlobal) return;

            if (executando) { Executando(); return; }

            candidato = Procurar();

            if (candidato != null && Input.GetKeyDown(KeyCode.E))
                Iniciar(candidato);
        }

        EnemyBrain Procurar()
        {
            if (cam == null) return null;
            EnemyBrain melhor = null;
            float melhorAng = anguloMax;

            int qtd = Physics.OverlapSphereNonAlloc(transform.position, alcance + 1.5f, _buf, Layers.Enemy,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < qtd; i++)
            {
                var e = _buf[i].GetComponentInParent<EnemyBrain>();
                if (e == null || e.Morto || !e.Atordoado) continue;

                Vector3 d = e.CentroCorpo - cam.position;
                if (d.magnitude > alcance + e.raioCorpo) continue;

                float ang = Vector3.Angle(cam.forward, d);
                if (ang < melhorAng) { melhorAng = ang; melhor = e; }
            }
            return melhor;
        }

        void Iniciar(EnemyBrain e)
        {
            atual = e;
            executando = true;
            t = 0f;
            e.EntrarEmExecucao();
            Sfx.Tocar("execucao", 0.9f, Random.Range(0.95f, 1.05f));
            if (cam != null) camLocalOrig = cam.localPosition;
        }

        void Executando()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracao);

            if (atual != null && cam != null)
            {
                // trava a mira no alvo e sacode a câmera
                Vector3 alvo = atual.CentroCorpo;
                Vector3 dir = (alvo - cam.position).normalized;
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion q = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                        Quaternion.Euler(0f, q.eulerAngles.y, 0f), Time.deltaTime * 18f);
                }

                float shake = (1f - k) * 0.08f;
                cam.localPosition = camLocalOrig + (Vector3)(Random.insideUnitSphere * shake);
            }

            if (k >= 1f)
            {
                Finalizar();
            }
        }

        void Finalizar()
        {
            executando = false;
            if (atual != null)
            {
                Fx.JorroExecucao(atual.CentroCorpo);
                Vector3 p = atual.transform.position;
                atual.MorrerPorExecucao();

                if (vida != null) vida.Curar(vidaGanha, true);
                if (armas != null) armas.DarMunicao(TipoMunicao.Cartucho, 4);
                if (armas != null) armas.DarMunicao(TipoMunicao.Bala, 25);

                Sfx.TocarEm("carne", p, 1f, 0.8f);
            }
            atual = null;
        }
    }
}
