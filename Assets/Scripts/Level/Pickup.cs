using UnityEngine;

namespace Justiceiro
{
    public enum TipoColetavel { Vida, VidaGrande, Colete, MunicaoCartucho, MunicaoBala, MunicaoRojao, ChaveVermelha }

    public class Pickup : MonoBehaviour
    {
        public TipoColetavel tipo = TipoColetavel.Vida;
        public float valor = 25f;
        public float duracao = 0f;      // 0 = eterno
        public bool girar = true;

        float t;
        float nascido;
        Transform visual;

        public static Pickup Criar(TipoColetavel tipo, Vector3 pos, float duracao = 0f, float valorCustom = -1f)
        {
            var go = new GameObject("Coletavel_" + tipo);
            go.transform.position = pos + Vector3.up * 0.55f;

            var p = go.AddComponent<Pickup>();
            p.tipo = tipo;
            p.duracao = duracao;
            p.valor = valorCustom >= 0f ? valorCustom : ValorPadrao(tipo);
            p.Montar();
            return p;
        }

        public static float ValorPadrao(TipoColetavel t)
        {
            switch (t)
            {
                case TipoColetavel.Vida: return 15f;
                case TipoColetavel.VidaGrande: return 45f;
                case TipoColetavel.Colete: return 50f;
                case TipoColetavel.MunicaoCartucho: return 8f;
                case TipoColetavel.MunicaoBala: return 50f;
                case TipoColetavel.MunicaoRojao: return 3f;
                default: return 1f;
            }
        }

        public static Color CorDe(TipoColetavel t)
        {
            switch (t)
            {
                case TipoColetavel.Vida: return new Color(0.2f, 1f, 0.35f);
                case TipoColetavel.VidaGrande: return new Color(0.1f, 1f, 0.5f);
                case TipoColetavel.Colete: return new Color(0.25f, 0.6f, 1f);
                case TipoColetavel.MunicaoCartucho: return new Color(1f, 0.55f, 0.15f);
                case TipoColetavel.MunicaoBala: return new Color(1f, 0.9f, 0.3f);
                case TipoColetavel.MunicaoRojao: return new Color(1f, 0.3f, 0.2f);
                default: return new Color(1f, 0.1f, 0.1f);
            }
        }

        void Awake() { nascido = Time.time; }

        void OnDrawGizmos()
        {
            Gizmos.color = CorDe(tipo);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.9f);
        }

        void Montar()
        {
            if (visual != null) return;
            var jaTem = transform.Find("visual");
            if (jaTem != null) { visual = jaTem; return; }

            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.95f;

            PrimitiveType forma = PrimitiveType.Cube;
            float escala = 0.34f;
            switch (tipo)
            {
                case TipoColetavel.Vida: forma = PrimitiveType.Cube; escala = 0.28f; break;
                case TipoColetavel.VidaGrande: forma = PrimitiveType.Cube; escala = 0.46f; break;
                case TipoColetavel.Colete: forma = PrimitiveType.Cube; escala = 0.42f; break;
                case TipoColetavel.MunicaoCartucho: forma = PrimitiveType.Cylinder; escala = 0.26f; break;
                case TipoColetavel.MunicaoBala: forma = PrimitiveType.Cube; escala = 0.3f; break;
                case TipoColetavel.MunicaoRojao: forma = PrimitiveType.Cylinder; escala = 0.32f; break;
                case TipoColetavel.ChaveVermelha: forma = PrimitiveType.Cube; escala = 0.4f; break;
            }

            var g = GameObject.CreatePrimitive(forma);
            g.name = "visual";
            var c2 = g.GetComponent<Collider>();
            if (c2 != null) Destroy(c2);
            g.transform.SetParent(transform, false);
            g.transform.localScale = Vector3.one * escala;
            if (tipo == TipoColetavel.ChaveVermelha)
                g.transform.localScale = new Vector3(0.22f, 0.5f, 0.22f);

            Color cor = CorDe(tipo);
            var r = g.GetComponent<Renderer>();
            r.sharedMaterial = Util.MaterialSimples(cor, 0.4f, 0.7f, cor * 1.4f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visual = g.transform;

            var luzGo = new GameObject("luz");
            luzGo.transform.SetParent(transform, false);
            var l = luzGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = cor;
            l.intensity = tipo == TipoColetavel.ChaveVermelha ? 4f : 1.6f;
            l.range = tipo == TipoColetavel.ChaveVermelha ? 12f : 5f;
            l.shadows = LightShadows.None;
        }

        void Start() { Montar(); }

        void Update()
        {
            t += Time.deltaTime;
            if (visual != null && girar)
            {
                visual.localRotation = Quaternion.Euler(0f, t * 110f, tipo == TipoColetavel.ChaveVermelha ? 0f : 22f);
                visual.localPosition = new Vector3(0f, Mathf.Sin(t * 2.6f) * 0.12f, 0f);
            }

            if (duracao > 0f && Time.time - nascido > duracao) Destroy(gameObject);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != Layers.PlayerLayer) return;

            var saude = other.GetComponentInParent<PlayerHealth>();
            var armas = other.GetComponentInParent<WeaponController>();
            if (saude == null) return;

            bool pegou = false;
            switch (tipo)
            {
                case TipoColetavel.Vida:
                case TipoColetavel.VidaGrande:
                    if (saude.vida < saude.vidaMax || duracao > 0f)
                    {
                        saude.Curar(valor, duracao > 0f);
                        pegou = true;
                    }
                    break;

                case TipoColetavel.Colete:
                    if (saude.colete < saude.coleteMax) { saude.DarColete(valor); pegou = true; }
                    break;

                case TipoColetavel.MunicaoCartucho:
                    if (armas != null) { armas.DarMunicao(TipoMunicao.Cartucho, Mathf.RoundToInt(valor)); pegou = true; }
                    break;

                case TipoColetavel.MunicaoBala:
                    if (armas != null) { armas.DarMunicao(TipoMunicao.Bala, Mathf.RoundToInt(valor)); pegou = true; }
                    break;

                case TipoColetavel.MunicaoRojao:
                    if (armas != null) { armas.DarMunicao(TipoMunicao.Rojao, Mathf.RoundToInt(valor)); pegou = true; }
                    break;

                case TipoColetavel.ChaveVermelha:
                    if (GameManager.I != null) GameManager.I.PegarChave();
                    pegou = true;
                    break;
            }

            if (!pegou) return;

            if (tipo != TipoColetavel.ChaveVermelha)
                Sfx.Tocar("coleta", 0.45f, tipo == TipoColetavel.Colete ? 0.8f : 1.1f);

            Fx.Luz(transform.position, CorDe(tipo), 8f, 6f, 0.15f);
            Destroy(gameObject);
        }
    }
}
