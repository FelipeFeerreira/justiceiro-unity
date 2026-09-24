using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    /// <summary>
    /// Monta os inimigos com primitivas em runtime — nenhum modelo 3D precisa ser importado.
    /// Cada tipo tem silhueta e cor próprias pra você ler a ameaça de longe (regra de ouro do gênero).
    /// </summary>
    public static class EnemyFactory
    {
        static readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Resetar() => _mats.Clear();

        static Material M(string chave, Color cor, float metal = 0f, float liso = 0.2f)
        {
            if (_mats.TryGetValue(chave, out var m) && m != null) return m;
            m = Util.MaterialSimples(cor, metal, liso);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
            _mats[chave] = m;
            return m;
        }

        public static EnemyBrain Criar(TipoInimigo tipo, Vector3 pos, Quaternion rot)
        {
            var go = new GameObject("Inimigo_" + tipo);
            go.transform.position = pos;
            go.transform.rotation = rot;
            go.layer = Layers.EnemyLayer;

            var cc = go.AddComponent<CharacterController>();
            var brain = go.AddComponent<EnemyBrain>();
            brain.tipo = tipo;

            float altura, raio;
            Color corRoupa, corPele, corDetalhe;
            ConfigurarStats(brain, tipo, out altura, out raio, out corRoupa, out corPele, out corDetalhe);

            cc.height = altura;
            cc.radius = raio;
            cc.center = new Vector3(0f, altura * 0.5f, 0f);
            cc.slopeLimit = 55f;
            cc.stepOffset = 0.6f;

            MontarCorpo(brain, altura, raio, corRoupa, corPele, corDetalhe);

            return brain;
        }

        static void ConfigurarStats(EnemyBrain b, TipoInimigo t, out float altura, out float raio,
                                    out Color roupa, out Color pele, out Color detalhe)
        {
            pele = new Color(0.55f, 0.38f, 0.28f);
            altura = 1.85f;
            raio = 0.42f;

            switch (t)
            {
                case TipoInimigo.Moleque:
                    b.nomeExibicao = "MOLEQUE";
                    b.ataque = AtaqueInimigo.Corpo;
                    b.vidaMax = 45f;
                    b.velocidade = 8.2f;
                    b.dano = 13f;
                    b.cadencia = 1.4f;
                    b.alcanceAtaque = 2.3f;
                    b.alcancePreferido = 1.4f;
                    b.limiarAtordoamento = 0.34f;
                    b.duracaoAtordoamento = 4.5f;
                    altura = 1.70f; raio = 0.36f;
                    roupa = new Color(0.85f, 0.85f, 0.88f);       // regata branca
                    detalhe = new Color(0.9f, 0.2f, 0.2f);        // boné vermelho
                    b.corBrilho = new Color(0.4f, 1f, 1f);
                    break;

                case TipoInimigo.Soldado:
                    b.nomeExibicao = "SOLDADO";
                    b.ataque = AtaqueInimigo.Tiro;
                    b.vidaMax = 80f;
                    b.velocidade = 5.4f;
                    b.dano = 7.5f;
                    b.rajada = 4;
                    b.cadencia = 0.72f;
                    b.dispersao = 3.2f;
                    b.alcanceAtaque = 45f;
                    b.alcancePreferido = 13f;
                    b.limiarAtordoamento = 0.3f;
                    roupa = new Color(0.16f, 0.20f, 0.16f);
                    detalhe = new Color(0.10f, 0.45f, 0.20f);
                    b.corBrilho = new Color(0.4f, 1f, 1f);
                    break;

                case TipoInimigo.Fogueteiro:
                    b.nomeExibicao = "FOGUETEIRO";
                    b.ataque = AtaqueInimigo.Bomba;
                    b.vidaMax = 110f;
                    b.velocidade = 4.3f;
                    b.dano = 42f;
                    b.cadencia = 0.45f;
                    b.alcanceAtaque = 40f;
                    b.alcancePreferido = 18f;
                    b.limiarAtordoamento = 0.28f;
                    altura = 1.95f; raio = 0.48f;
                    roupa = new Color(0.30f, 0.16f, 0.36f);
                    detalhe = new Color(1f, 0.45f, 0.1f);
                    b.corBrilho = new Color(1f, 0.6f, 0.2f);
                    break;

                case TipoInimigo.Blindado:
                    b.nomeExibicao = "MILICIANO BLINDADO";
                    b.ataque = AtaqueInimigo.Escopeta;
                    b.vidaMax = 200f;
                    b.velocidade = 4.6f;
                    b.dano = 30f;
                    b.cadencia = 0.62f;
                    b.alcanceAtaque = 16f;
                    b.alcancePreferido = 7f;
                    b.limiarAtordoamento = 0.24f;
                    b.duracaoAtordoamento = 4f;
                    altura = 2.05f; raio = 0.55f;
                    roupa = new Color(0.10f, 0.11f, 0.13f);
                    detalhe = new Color(0.55f, 0.56f, 0.60f);     // colete/placa
                    b.corBrilho = new Color(1f, 0.85f, 0.25f);
                    break;

                default: // Chefe
                    b.nomeExibicao = "CORONEL DA ZONA CINZA";
                    b.ataque = AtaqueInimigo.Escopeta;
                    b.vidaMax = 900f;
                    b.velocidade = 5.2f;
                    b.dano = 34f;
                    b.cadencia = 0.8f;
                    b.alcanceAtaque = 22f;
                    b.alcancePreferido = 9f;
                    b.limiarAtordoamento = 0.12f;
                    b.duracaoAtordoamento = 3.2f;
                    b.ehChefe = true;
                    altura = 2.55f; raio = 0.72f;
                    roupa = new Color(0.07f, 0.07f, 0.09f);
                    detalhe = new Color(0.75f, 0.10f, 0.10f);
                    b.corBrilho = new Color(1f, 0.25f, 0.2f);
                    break;
            }

            b.alturaCorpo = altura;
            b.raioCorpo = raio;
            b.vida = b.vidaMax;
        }

        static void MontarCorpo(EnemyBrain b, float altura, float raio, Color roupa, Color pele, Color detalhe)
        {
            var root = b.transform;

            var corpoGo = new GameObject("Corpo");
            corpoGo.transform.SetParent(root, false);
            corpoGo.transform.localPosition = Vector3.zero;
            b.corpo = corpoGo.transform;

            float escalaOmbro = raio * 1.9f;
            var brilho = new List<Transform>();

            // tronco
            var tronco = Peca("Tronco", corpoGo.transform,
                new Vector3(0f, altura * 0.62f, 0f),
                new Vector3(escalaOmbro, altura * 0.40f, raio * 1.25f),
                M("roupa" + roupa.ToString(), roupa), b);
            brilho.Add(tronco);

            // quadril
            Peca("Quadril", corpoGo.transform,
                new Vector3(0f, altura * 0.40f, 0f),
                new Vector3(escalaOmbro * 0.85f, altura * 0.14f, raio * 1.2f),
                M("calca", Color.Lerp(roupa, new Color(0.12f, 0.14f, 0.22f), 0.7f)), b);

            // cabeça
            var cab = Peca("Cabeca", corpoGo.transform,
                new Vector3(0f, altura * 0.89f, 0f),
                new Vector3(raio * 1.05f, raio * 1.15f, raio * 1.05f),
                M("pele", pele), b);
            b.cabeca = cab;
            brilho.Add(cab);

            // marca da facção (boné / capacete / placa)
            var marca = Peca("Marca", corpoGo.transform,
                new Vector3(0f, altura * 0.98f, 0.05f),
                new Vector3(raio * 1.15f, raio * 0.34f, raio * 1.25f),
                M("detalhe" + detalhe.ToString(), detalhe, 0.3f, 0.4f), b);
            brilho.Add(marca);

            // braços (pivô no ombro)
            b.bracoEsq = Membro("BracoEsq", corpoGo.transform,
                new Vector3(-escalaOmbro * 0.62f, altura * 0.78f, 0f),
                new Vector3(raio * 0.42f, altura * 0.30f, raio * 0.42f),
                M("pele", pele), b);
            b.bracoDir = Membro("BracoDir", corpoGo.transform,
                new Vector3(escalaOmbro * 0.62f, altura * 0.78f, 0f),
                new Vector3(raio * 0.42f, altura * 0.30f, raio * 0.42f),
                M("pele", pele), b);

            // arma na mão direita
            var arma = Peca("Arma", b.bracoDir,
                new Vector3(0f, -altura * 0.30f, raio * 0.6f),
                new Vector3(raio * 0.30f, raio * 0.30f, altura * 0.30f),
                M("ferro", new Color(0.12f, 0.12f, 0.14f), 0.8f, 0.5f), b);
            if (b.ataque == AtaqueInimigo.Corpo)
            {
                arma.localScale = new Vector3(raio * 0.12f, raio * 0.12f, altura * 0.16f);
                var r = arma.GetComponent<Renderer>();
                r.sharedMaterial = M("faca", new Color(0.8f, 0.82f, 0.85f), 1f, 0.8f);
            }

            // pernas
            b.pernaEsq = Membro("PernaEsq", corpoGo.transform,
                new Vector3(-raio * 0.45f, altura * 0.40f, 0f),
                new Vector3(raio * 0.52f, altura * 0.22f, raio * 0.52f),
                M("calca", Color.Lerp(roupa, new Color(0.12f, 0.14f, 0.22f), 0.7f)), b);
            b.pernaDir = Membro("PernaDir", corpoGo.transform,
                new Vector3(raio * 0.45f, altura * 0.40f, 0f),
                new Vector3(raio * 0.52f, altura * 0.22f, raio * 0.52f),
                M("calca", Color.Lerp(roupa, new Color(0.12f, 0.14f, 0.22f), 0.7f)), b);

            // colete visível do blindado / chefe
            if (b.tipo == TipoInimigo.Blindado || b.tipo == TipoInimigo.Chefe)
            {
                var colete = Peca("Colete", corpoGo.transform,
                    new Vector3(0f, altura * 0.66f, -raio * 0.1f),
                    new Vector3(escalaOmbro * 1.08f, altura * 0.26f, raio * 1.5f),
                    M("colete", detalhe, 0.7f, 0.35f), b);
                brilho.Add(colete);
            }

            b.partesBrilho = brilho.ToArray();
        }

        static Transform Peca(string nome, Transform pai, Vector3 posLocal, Vector3 escala, Material mat, EnemyBrain b)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nome;
            go.transform.SetParent(pai, false);
            go.transform.localPosition = posLocal;
            go.transform.localScale = escala;
            go.layer = Layers.EnemyLayer;

            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            return go.transform;
        }

        /// <summary>Membro com pivô no topo, pra girar como ombro/quadril.</summary>
        static Transform Membro(string nome, Transform pai, Vector3 pivo, Vector3 escala, Material mat, EnemyBrain b)
        {
            var pivotGo = new GameObject(nome);
            pivotGo.transform.SetParent(pai, false);
            pivotGo.transform.localPosition = pivo;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nome + "_mesh";
            go.transform.SetParent(pivotGo.transform, false);
            go.transform.localPosition = new Vector3(0f, -escala.y * 0.5f, 0f);
            go.transform.localScale = escala;
            go.layer = Layers.EnemyLayer;

            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            go.GetComponent<Renderer>().sharedMaterial = mat;
            return pivotGo.transform;
        }
    }
}
