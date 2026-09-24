using UnityEngine;

namespace Justiceiro
{
    /// <summary>Rojão / bomba de fogueteiro. Viaja reto, explode em área.</summary>
    public class Projectile : MonoBehaviour
    {
        public float velocidade = 34f;
        public float dano = 90f;
        public float raio = 5.5f;
        public float vida = 6f;
        public GameObject dono;
        public bool doJogador = true;
        public Color cor = new Color(1f, 0.6f, 0.2f);

        Vector3 dir;
        Light luz;

        static readonly Collider[] _cols = new Collider[64];

        public static Projectile Criar(Vector3 pos, Vector3 direcao, float dano, float vel, float raio,
                                       GameObject dono, bool doJogador = true, Color? cor = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = doJogador ? "Rojao" : "BombaInimiga";
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.28f;

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var c = cor ?? (doJogador ? new Color(1f, 0.65f, 0.2f) : new Color(1f, 0.3f, 0.25f));
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Util.MaterialSimples(c, 0f, 0.6f, c * 2f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var p = go.AddComponent<Projectile>();
            p.dir = direcao.normalized;
            p.dano = dano;
            p.velocidade = vel;
            p.raio = raio;
            p.dono = dono;
            p.doJogador = doJogador;
            p.cor = c;
            return p;
        }

        void Awake()
        {
            var go = new GameObject("luz");
            go.transform.SetParent(transform, false);
            luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = cor;
            luz.intensity = 4f;
            luz.range = 9f;
            luz.shadows = LightShadows.None;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            vida -= dt;
            if (vida <= 0f) { Explodir(transform.position, Vector3.up); return; }

            float passo = velocidade * dt;
            int mascara = doJogador ? Layers.PlayerShoot : Layers.EnemyShoot;

            if (Physics.SphereCast(transform.position, 0.16f, dir, out RaycastHit h, passo, mascara, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.root != (dono != null ? dono.transform.root : null))
                {
                    Explodir(h.point, h.normal);
                    return;
                }
            }

            transform.position += dir * passo;
            transform.Rotate(Vector3.one * 420f * dt, Space.World);
        }

        void Explodir(Vector3 pos, Vector3 normal)
        {
            Fx.Explosao(pos, raio);

            int qtd = Physics.OverlapSphereNonAlloc(pos, raio, _cols, Layers.Corpos, QueryTriggerInteraction.Ignore);
            var jaAtingidos = new System.Collections.Generic.HashSet<Transform>();

            for (int i = 0; i < qtd; i++)
            {
                var alvo = _cols[i].GetComponentInParent<IAlvo>();
                if (alvo == null || alvo.Morto) continue;
                if (alvo.Alvo == null || jaAtingidos.Contains(alvo.Alvo)) continue;
                jaAtingidos.Add(alvo.Alvo);

                // não fere quem atirou, mas dá "rocket jump" no jogador
                bool ehDono = dono != null && alvo.Alvo.root == dono.transform.root;

                float d = Vector3.Distance(pos, alvo.Alvo.position + Vector3.up);
                float queda = Mathf.Clamp01(1f - d / raio);
                float danoFinal = dano * queda * queda;

                Vector3 empurrao = (alvo.Alvo.position + Vector3.up * 0.8f - pos).normalized;

                if (ehDono)
                {
                    var pc = dono.GetComponent<PlayerController>();
                    if (pc != null) pc.Empurrar((empurrao + Vector3.up * 0.9f).normalized * queda * 16f);
                    alvo.ReceberDano(danoFinal * 0.25f, pos, empurrao, false);
                }
                else
                {
                    alvo.ReceberDano(danoFinal, pos, empurrao, true);
                }
            }

            Destroy(gameObject);
        }
    }
}
