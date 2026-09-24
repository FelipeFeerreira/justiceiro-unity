using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Justiceiro.EditorTools
{
    /// <summary>
    /// Constrói a fase inteira por código: geometria, luzes, inimigos, arenas, coletáveis.
    /// Menu: Justiceiro > Reconstruir Fase.
    /// </summary>
    public static class LevelBuilder
    {
        public const string CaminhoCena = "Assets/Cenas/Favela.unity";
        public const string CaminhoMenu = "Assets/Cenas/Menu.unity";

        static Transform _raiz;
        static Material mAsfalto, mTijolo, mConcreto, mLaje, mZinco, mMadeira;
        static Material mRosa, mAzul, mVerde, mAmarelo, mCreme;
        static Material mVermelhoEmissivo, mGrade, mJanela, mPoste;
        static Material mMar, mMata, mMata2, mCidade, mCidadeLuz, mSeta;
        static List<Material> _paleta;

        static uint _rs;

        [MenuItem("Justiceiro/Reconstruir Fase %#j")]
        public static void ReconstruirMenu()
        {
            Construir(true);
        }

        [MenuItem("Justiceiro/Abrir Fase")]
        public static void AbrirFase()
        {
            if (System.IO.File.Exists(CaminhoCena))
                EditorSceneManager.OpenScene(CaminhoCena, OpenSceneMode.Single);
            else
                Construir(true);
        }

        public static bool CenaExiste() => System.IO.File.Exists(CaminhoCena);

        // =================================================================== principal

        public static void Construir(bool perguntarSalvar)
        {
            if (perguntarSalvar && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ProcArt.GarantirPastas();
            ProcArt.LimparCache();
            ProcArt.LimparMalhas();
            PrepararMateriais();

            if (!AssetDatabase.IsValidFolder("Assets/Cenas"))
                AssetDatabase.CreateFolder("Assets", "Cenas");

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var raizGo = new GameObject("== MORRO ==");
            _raiz = raizGo.transform;

            ConfigurarAmbiente();

            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            var player = CriarJogador(new Vector3(0f, 1.4f, 2f));
            gm.jogador = player.transform;
            gm.vidaJogador = player.GetComponent<PlayerHealth>();

            // ---- zonas
            ZonaA_PeDoMorro();
            ZonaB_Beco();
            var arena1 = ZonaC_Laje();
            ZonaD_Viela();
            var arena2 = ZonaE_Quadra();
            ZonaF_Rampa();
            var arenaFinal = ZonaG_Topo();

            ConstruirFundo();
            ConstruirNatureza();
            SetasPiso();

            arena1.soltaChave = true;

            // saída
            var saida = new GameObject("SaidaFase");
            saida.transform.SetParent(_raiz, false);
            saida.transform.position = new Vector3(0f, 20.5f, 192f);
            var bc = saida.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(8f, 5f, 6f);
            var sf = saida.AddComponent<SaidaFase>();
            sf.arenaFinal = arenaFinal;

            // marco visual da saída (helipad)
            Caixa("Helipad", new Vector3(0f, 19.05f, 192f), new Vector3(10f, 0.15f, 10f), mConcreto, _raiz, false, 0.35f);
            Caixa("HelipadMarca", new Vector3(0f, 19.14f, 192f), new Vector3(6f, 0.12f, 1.2f), mVermelhoEmissivo, _raiz, false, 1f);
            Caixa("HelipadMarca2", new Vector3(0f, 19.14f, 192f), new Vector3(1.2f, 0.12f, 6f), mVermelhoEmissivo, _raiz, false, 1f);
            LuzPonto(new Vector3(0f, 22f, 192f), new Color(0.4f, 1f, 0.6f), 6f, 22f);

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoCena);

            InstalarBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<b>JUSTICEIRO</b> — fase construída em " + CaminhoCena + ". Aperta Play.");
        }

        /// <summary>Menu na posição 0, fase no 1. Nunca apaga outras cenas de build.</summary>
        public static void InstalarBuildSettings()
        {
            var lista = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            lista.RemoveAll(s => s.path == CaminhoCena || s.path == CaminhoMenu);

            int posMenu = 0;
            if (System.IO.File.Exists(CaminhoMenu)) { posMenu = 1; lista.Insert(0, new EditorBuildSettingsScene(CaminhoMenu, true)); }
            lista.Insert(posMenu, new EditorBuildSettingsScene(CaminhoCena, true));
            EditorBuildSettings.scenes = lista.ToArray();
        }

        // =================================================================== materiais

        static void PrepararMateriais()
        {
            mAsfalto = ProcArt.Mat("asfalto", ProcArt.TexAsfalto(), 0.08f);
            mTijolo = ProcArt.Mat("tijolo", ProcArt.TexTijolo(), 0.05f);
            mConcreto = ProcArt.Mat("concreto", ProcArt.TexConcreto(), 0.07f);
            mLaje = ProcArt.Mat("laje", ProcArt.TexLaje(), 0.09f);
            mZinco = ProcArt.Mat("zinco", ProcArt.TexZinco(), 0.42f, 0.55f);
            mMadeira = ProcArt.Mat("madeira", ProcArt.TexMadeira(), 0.1f);

            mRosa = ProcArt.Mat("reboco_rosa", ProcArt.TexReboco("rosa", new Color(0.88f, 0.52f, 0.55f), 101), 0.06f);
            mAzul = ProcArt.Mat("reboco_azul", ProcArt.TexReboco("azul", new Color(0.42f, 0.62f, 0.78f), 113), 0.06f);
            mVerde = ProcArt.Mat("reboco_verde", ProcArt.TexReboco("verde", new Color(0.50f, 0.72f, 0.48f), 127), 0.06f);
            mAmarelo = ProcArt.Mat("reboco_amarelo", ProcArt.TexReboco("amarelo", new Color(0.88f, 0.78f, 0.42f), 139), 0.06f);
            mCreme = ProcArt.Mat("reboco_creme", ProcArt.TexReboco("creme", new Color(0.86f, 0.80f, 0.68f), 149), 0.06f);

            mVermelhoEmissivo = ProcArt.Mat("vermelho_emissivo", null, 0.3f, 0f,
                new Color(0.7f, 0.08f, 0.08f), new Color(0.75f, 0.05f, 0.05f) * 1.6f);
            mGrade = ProcArt.Mat("grade_aco", null, 0.55f, 0.85f, new Color(0.22f, 0.21f, 0.22f),
                new Color(0.6f, 0.15f, 0.05f) * 0.5f);
            mJanela = ProcArt.Mat("janela", null, 0.85f, 0.2f, new Color(0.05f, 0.06f, 0.09f));
            mPoste = ProcArt.Mat("poste", null, 0.4f, 0.6f, new Color(0.25f, 0.25f, 0.27f));

            mMar = ProcArt.Mat("mar", null, 0.15f, 0.1f, new Color(0.05f, 0.11f, 0.19f),
                new Color(0.02f, 0.03f, 0.06f) * 0.25f);
            mMata = ProcArt.Mat("mata", ProcArt.TexMata(), 0.05f);
            mMata2 = ProcArt.Mat("mata_2", null, 0.05f, 0f, new Color(0.13f, 0.24f, 0.10f));
            mCidade = ProcArt.Mat("cidade", ProcArt.TexCidade(), 0.08f);
            mCidadeLuz = ProcArt.Mat("cidade_luz", ProcArt.TexFundoJanelas("fundo_janelas"), 0.15f, 0.1f,
                Color.white, new Color(1f, 0.72f, 0.35f) * 0.55f);
            mSeta = ProcArt.Mat("seta_piso", null, 0.25f, 0.1f, new Color(1f, 0.85f, 0.35f),
                new Color(1f, 0.7f, 0.15f) * 1.5f);

            _paleta = new List<Material> { mRosa, mAzul, mVerde, mAmarelo, mCreme, mTijolo, mConcreto };
        }

        // rng determinístico do builder (não mexe no UnityEngine.Random global)
        static void Srand(int s) => _rs = (uint)s * 2654435761u + 1u;
        static float Srnd() { _rs = _rs * 1103515245u + 12345u; return (_rs >> 8) / (float)(1 << 24); }
        static int SrndInt(int m) { _rs = _rs * 1103515245u + 12345u; return (int)(_rs % (uint)m); }

        static Material Paleta(int i) => _paleta[Mathf.Abs(i) % _paleta.Count];

        // =================================================================== helpers

        static GameObject Caixa(string nome, Vector3 centro, Vector3 tam, Material mat,
                                Transform pai = null, bool colisor = true, float uvPorMetro = 0.35f)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai != null ? pai : _raiz, false);
            go.transform.position = centro;

            go.AddComponent<MeshFilter>().sharedMesh = ProcArt.Caixa(tam, uvPorMetro);
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;

            if (colisor)
            {
                var bc = go.AddComponent<BoxCollider>();
                bc.size = tam;
            }
            go.isStatic = true;
            return go;
        }

        static Transform Grupo(string nome)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(_raiz, false);
            return go.transform;
        }

        /// <summary>Escada alinhada ao eixo Z.</summary>
        static void EscadaZ(string nome, float x, float largura, float z0, float z1, float y0, float y1,
                            int degraus, Material mat, Transform pai)
        {
            float passoZ = (z1 - z0) / degraus;
            float passoY = (y1 - y0) / degraus;
            for (int i = 0; i < degraus; i++)
            {
                float topo = y0 + passoY * (i + 1);
                float baseY = y0 - 1.5f;
                float altura = topo - baseY;
                float cz = z0 + passoZ * (i + 0.5f);
                Caixa(nome + "_" + i, new Vector3(x, baseY + altura * 0.5f, cz),
                      new Vector3(largura, altura, Mathf.Abs(passoZ) + 0.02f), mat, pai, true, 0.4f);
            }
        }

        static void Barraco(string nome, Vector3 baseCentro, Vector3 tam, Material parede, Transform pai,
                            bool caixaDagua = true, int semente = 0)
        {
            var g = new GameObject(nome);
            g.transform.SetParent(pai != null ? pai : _raiz, false);
            g.transform.position = baseCentro;

            Vector3 c = baseCentro + Vector3.up * (tam.y * 0.5f);
            Caixa(nome + "_corpo", c, tam, parede, g.transform, true, 0.32f);

            // laje / telhado
            Caixa(nome + "_laje", baseCentro + Vector3.up * (tam.y + 0.12f),
                  new Vector3(tam.x + 0.5f, 0.25f, tam.z + 0.5f), mLaje, g.transform, true, 0.35f);

            // janelas escuras nas 4 faces
            int jan = Mathf.Max(1, Mathf.FloorToInt(tam.x / 3f));
            for (int i = 0; i < jan; i++)
            {
                float fx = tam.x * ((i + 1f) / (jan + 1f)) - tam.x * 0.5f;
                float jy = baseCentro.y + tam.y * 0.62f;
                Caixa(nome + "_jan" + i, new Vector3(baseCentro.x + fx, jy, baseCentro.z + tam.z * 0.5f + 0.02f),
                      new Vector3(1.0f, 1.1f, 0.12f), mJanela, g.transform, false, 1f);
                Caixa(nome + "_janB" + i, new Vector3(baseCentro.x + fx, jy, baseCentro.z - tam.z * 0.5f - 0.02f),
                      new Vector3(1.0f, 1.1f, 0.12f), mJanela, g.transform, false, 1f);
            }

            if (caixaDagua)
            {
                Caixa(nome + "_cxdagua", baseCentro + new Vector3(tam.x * 0.28f, tam.y + 0.9f, tam.z * 0.25f),
                      new Vector3(1.3f, 1.3f, 1.3f), mAzul, g.transform, true, 0.6f);
            }

            // pedaço de zinco pendurado
            if ((semente % 3) == 0)
            {
                Caixa(nome + "_zinco", baseCentro + new Vector3(0f, tam.y * 0.5f, tam.z * 0.5f + 0.9f),
                      new Vector3(tam.x * 0.8f, 0.1f, 1.8f), mZinco, g.transform, true, 0.5f);
            }

            // antena parabólica no telhado
            if ((semente % 4) == 0)
            {
                var baseTelhado = baseCentro + new Vector3(tam.x * 0.30f, tam.y + 0.3f, tam.z * 0.30f);
                Caixa(nome + "_sat_pole", baseTelhado + new Vector3(0f, 0.9f, 0f),
                      new Vector3(0.14f, 1.4f, 0.14f), mPoste, g.transform, false, 1f);
                var dish = Cilindro(nome + "_sat_dish", baseTelhado + new Vector3(0f, 1.45f, 0f),
                      new Vector3(0.95f, 0.35f, 0.95f), mCreme, g.transform);
                dish.transform.rotation = Quaternion.Euler(62f, 0f, 40f);
            }

            // varal com roupa no telhado
            if ((semente % 5) == 1)
            {
                float vz = tam.z * 0.24f;
                Caixa(nome + "_varal_a", baseCentro + new Vector3(tam.x * 0.26f, tam.y + 0.55f, vz),
                      new Vector3(0.12f, 1.0f, 0.12f), mPoste, g.transform, false, 1f);
                Caixa(nome + "_varal_b", baseCentro + new Vector3(-tam.x * 0.26f, tam.y + 0.55f, vz),
                      new Vector3(0.12f, 1.0f, 0.12f), mPoste, g.transform, false, 1f);
                Caixa(nome + "_varao", baseCentro + new Vector3(0f, tam.y + 1.02f, vz),
                      new Vector3(tam.x * 0.62f, 0.07f, 0.07f), mZinco, g.transform, false, 1f);
                for (int ci = 0; ci < 3; ci++)
                {
                    float fx = (ci - 1) * 1.05f;
                    Caixa(nome + "_roupa" + ci, baseCentro + new Vector3(fx, tam.y + 0.8f, vz + 0.08f),
                          new Vector3(0.55f, 0.5f, 0.12f), Paleta(semente * 7 + ci), g.transform, false, 1f);
                }
            }
        }

        static void LuzPonto(Vector3 pos, Color cor, float intensidade, float alcance)
        {
            var go = new GameObject("Luz");
            go.transform.SetParent(_raiz, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = cor;
            l.intensity = intensidade;
            l.range = alcance;
            l.shadows = LightShadows.None;
        }

        static void Poste(Vector3 basePos, Transform pai)
        {
            var g = new GameObject("Poste");
            g.transform.SetParent(pai != null ? pai : _raiz, false);
            g.transform.position = basePos;
            Caixa("haste", basePos + Vector3.up * 3f, new Vector3(0.18f, 6f, 0.18f), mPoste, g.transform, true, 1f);
            Caixa("braco", basePos + new Vector3(0.6f, 5.9f, 0f), new Vector3(1.4f, 0.14f, 0.14f), mPoste, g.transform, false, 1f);
            var lamp = Caixa("lampada", basePos + new Vector3(1.2f, 5.75f, 0f), new Vector3(0.5f, 0.2f, 0.35f),
                ProcArt.Mat("lampada", null, 0.5f, 0f, new Color(1f, 0.85f, 0.55f), new Color(1f, 0.78f, 0.42f) * 2.2f),
                g.transform, false, 1f);
            LuzPonto(basePos + new Vector3(1.2f, 5.6f, 0f), new Color(1f, 0.72f, 0.38f), 3.2f, 17f);
        }

        static PontoSpawn Spawn(TipoInimigo tipo, Vector3 pos, Transform pai, bool noStart = false, float giroY = 180f)
        {
            var go = new GameObject("Spawn_" + tipo);
            go.transform.SetParent(pai != null ? pai : _raiz, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
            var p = go.AddComponent<PontoSpawn>();
            p.tipo = tipo;
            p.nasceNoStart = noStart;
            return p;
        }

        static Pickup Item(TipoColetavel tipo, Vector3 pos, Transform pai)
        {
            var go = new GameObject("Item_" + tipo);
            go.transform.SetParent(pai != null ? pai : _raiz, false);
            go.transform.position = pos;
            var p = go.AddComponent<Pickup>();
            p.tipo = tipo;
            p.valor = Pickup.ValorPadrao(tipo);
            return p;
        }

        static Barreira Grade(string nome, Vector3 centroFechado, Vector3 tam, Transform pai)
        {
            var go = Caixa(nome, centroFechado, tam, mGrade, pai, true, 0.8f);
            go.isStatic = false;
            var b = go.AddComponent<Barreira>();
            b.posFechada = centroFechado;
            b.posAberta = centroFechado + Vector3.down * (tam.y + 1.5f);
            b.fechada = false;
            go.transform.position = b.posAberta;
            return b;
        }

        static ArenaEncounter Arena(string nome, Vector3 centro, Vector3 tam, Transform pai)
        {
            var go = new GameObject("Arena_" + nome);
            go.transform.SetParent(pai != null ? pai : _raiz, false);
            go.transform.position = centro;
            var bc = go.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = tam;
            var a = go.AddComponent<ArenaEncounter>();
            a.nome = nome;
            return a;
        }

        // =================================================================== ambiente

        static void ConfigurarAmbiente()
        {
            var skyMat = AssetDatabase.LoadAssetAtPath<Material>(ProcArt.PastaMat + "/ceu_entardecer.mat");
            if (skyMat == null)
            {
                var sh = Shader.Find("Skybox/Procedural");
                if (sh != null)
                {
                    skyMat = new Material(sh) { name = "ceu_entardecer" };
                    AssetDatabase.CreateAsset(skyMat, ProcArt.PastaMat + "/ceu_entardecer.mat");
                }
            }
            if (skyMat != null)
            {
                if (skyMat.HasProperty("_AtmosphereThickness")) skyMat.SetFloat("_AtmosphereThickness", 1.75f);
                if (skyMat.HasProperty("_SkyTint")) skyMat.SetColor("_SkyTint", new Color(0.42f, 0.34f, 0.46f));
                if (skyMat.HasProperty("_GroundColor")) skyMat.SetColor("_GroundColor", new Color(0.10f, 0.09f, 0.11f));
                if (skyMat.HasProperty("_Exposure")) skyMat.SetFloat("_Exposure", 0.85f);
                EditorUtility.SetDirty(skyMat);
                RenderSettings.skybox = skyMat;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.26f, 0.24f, 0.34f);
            RenderSettings.ambientEquatorColor = new Color(0.20f, 0.17f, 0.20f);
            RenderSettings.ambientGroundColor = new Color(0.09f, 0.08f, 0.09f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.20f, 0.16f, 0.22f);
            RenderSettings.fogDensity = 0.0075f;

            var solGo = new GameObject("Sol (entardecer)");
            var sol = solGo.AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.color = new Color(1f, 0.72f, 0.48f);
            sol.intensity = 1.05f;
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.75f;
            solGo.transform.rotation = Quaternion.Euler(24f, 35f, 0f);
            RenderSettings.sun = sol;

            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
        }

        // =================================================================== fundo + natureza

        static GameObject Esfera(string nome, Vector3 centro, Vector3 escala, Material mat, Transform pai = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = nome;
            go.transform.SetParent(pai != null ? pai : _raiz, false);
            go.transform.position = centro;
            go.transform.localScale = escala;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        static GameObject Cilindro(string nome, Vector3 centro, Vector3 escala, Material mat, Transform pai = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = nome;
            go.transform.SetParent(pai != null ? pai : _raiz, false);
            go.transform.position = centro;
            go.transform.localScale = escala;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        /// <summary>Encosta, baía, cidade ao redor e morros distantes — tudo longe da jogabilidade.</summary>
        static void ConstruirFundo()
        {
            var f = Grupo("Fundo_Cidade");

            // encosta de mata imensa sob o morro (tapa o vazio embaixo dos níveis)
            Caixa("Encosta", new Vector3(0f, -1.2f, 80f), new Vector3(760f, 0.6f, 680f), mMata, f, false, 0.05f);

            // baía dos dois lados da baixada
            Caixa("Mar_L", new Vector3(-160f, -1.6f, 60f), new Vector3(130f, 0.45f, 320f), mMar, f, false, 0.03f);
            Caixa("Mar_R", new Vector3(160f, -1.6f, 70f), new Vector3(130f, 0.45f, 320f), mMar, f, false, 0.03f);

            // quadras da baixada (anel ao redor do morro)
            Srand(777001);
            for (int i = 0; i < 26; i++)
            {
                float lado = Srnd() < 0.5f ? -1f : 1f;
                float x = lado * (62f + Srnd() * 130f);
                float z = -40f + Srnd() * 300f;
                float h = 3.5f + Srnd() * 9f;
                float lx = 8f + Srnd() * 16f;
                float lz = 8f + Srnd() * 14f;
                Material mat = (h > 10f && Srnd() < 0.5f) ? mCidadeLuz : mCidade;
                Caixa("quadra_" + i, new Vector3(x, -0.95f + h * 0.5f, z), new Vector3(lx, h, lz), mat, f, false, 0.12f);
            }

            // silhuetas de prédios longe (cidade no entardecer — janelas acesas)
            Caixa("Silhueta_L", new Vector3(-220f, 8f, 70f), new Vector3(1.2f, 17f, 340f), mCidadeLuz, f, false, 0.09f);
            Caixa("Silhueta_R", new Vector3(220f, 8f, 70f), new Vector3(1.2f, 18f, 340f), mCidadeLuz, f, false, 0.09f);

            // morros distantes com favela acesa lá em cima
            Esfera("Morro_L", new Vector3(-340f, -6f, 50f), new Vector3(210f, 150f, 210f), mMata, f);
            Esfera("Morro_R", new Vector3(340f, -4f, 50f), new Vector3(190f, 135f, 190f), mMata, f);
            Esfera("Morro_N", new Vector3(0f, -8f, 380f), new Vector3(430f, 220f, 170f), mMata, f);

            // pontinhos acesos espalhados nos morros (favelas distantes)
            Srand(556001);
            for (int i = 0; i < 34; i++)
            {
                float lado = Srnd() < 0.5f ? -1f : 1f;
                float x = lado * (210f + Srnd() * 130f);
                float z = -30f + Srnd() * 340f;
                float y = 4f + Srnd() * 30f;
                Caixa("luz_morro_" + i, new Vector3(x, y, z), new Vector3(0.7f, 0.7f, 0.7f), mCidadeLuz, f, false, 0.5f);
            }
        }

        static void Arvore(string nome, Vector3 basePos, float altura, float raioCopa, Transform pai, float semente)
        {
            var g = new GameObject(nome);
            g.transform.SetParent(pai, false);
            g.transform.position = basePos;

            Material copa = semente > 0.5f ? mMata2 : mMata;

            Caixa(nome + "_tronco", basePos + Vector3.up * (altura * 0.45f),
                  new Vector3(0.3f, altura * 0.9f, 0.3f), mMadeira, g.transform, true, 0.5f);
            Esfera(nome + "_copa1", basePos + Vector3.up * (altura * 1.08f),
                   new Vector3(raioCopa, raioCopa * 0.75f, raioCopa), copa, g.transform);
            Esfera(nome + "_copa2", basePos + Vector3.up * (altura * 1.32f) + new Vector3(raioCopa * 0.35f, 0f, raioCopa * 0.2f),
                   new Vector3(raioCopa * 0.7f, raioCopa * 0.55f, raioCopa * 0.7f), copa, g.transform);
        }

        static void Mato(string nome, Vector3 basePos, Transform pai)
        {
            var g = new GameObject(nome);
            g.transform.SetParent(pai, false);
            g.transform.position = basePos;
            Esfera(nome + "_1", basePos + new Vector3(0f, 0.5f, 0f), new Vector3(1.4f, 0.7f, 1.2f), mMata, g.transform);
            Esfera(nome + "_2", basePos + new Vector3(0.7f, 0.3f, 0.3f), new Vector3(0.9f, 0.5f, 0.8f), mMata2, g.transform);
        }

        /// <summary>Vegetação nos cantos das zonas — só o tronco tem colisor.</summary>
        static void ConstruirNatureza()
        {
            var n = Grupo("Natureza");
            Srand(424201);

            Arvore("Arvore_A1", new Vector3(-16.5f, 0f, 5f), 6f, 3.4f, n, 0);
            Arvore("Arvore_A2", new Vector3(16f, 0f, 30f), 5f, 3f, n, 2);
            Arvore("Arvore_A3", new Vector3(16f, 0f, 8f), 4.5f, 2.8f, n, 5);

            Arvore("Arvore_C1", new Vector3(-26f, 7f, 89f), 4f, 2.6f, n, 1);
            Arvore("Arvore_C2", new Vector3(4f, 7f, 89f), 3.5f, 2.4f, n, 4);

            Arvore("Arvore_E1", new Vector3(-26f, 13f, 111f), 5f, 3.2f, n, 3);
            Arvore("Arvore_E2", new Vector3(15f, 13f, 141f), 4.5f, 2.8f, n, 6);

            Arvore("Arvore_G1", new Vector3(-19f, 19f, 196f), 3.5f, 2.2f, n, 7);
            Arvore("Arvore_G2", new Vector3(19f, 19f, 196f), 3f, 2f, n, 8);

            Mato("Mato_A1", new Vector3(-14f, 0f, 33f), n);
            Mato("Mato_A2", new Vector3(14.5f, 0f, 33f), n);
        }

        static void Seta(Transform pai, Vector3 pos, float rotY)
        {
            var go = Caixa("Seta", pos, new Vector3(2.4f, 0.12f, 0.8f), mSeta, pai, false, 1f);
            go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        }

        /// <summary>Setas amarelas no chão guiando a subida do morro.</summary>
        static void SetasPiso()
        {
            var s = Grupo("SetasNat");
            Seta(s, new Vector3(0f, 0.08f, 12f), 0f);
            Seta(s, new Vector3(0f, 0.08f, 26f), 0f);
            Seta(s, new Vector3(0f, 0.08f, 35.2f), 0f);
            Seta(s, new Vector3(-11.5f, 7.08f, 91f), 0f);
            Seta(s, new Vector3(2.5f, 13.08f, 149.5f), 0f);
            Seta(s, new Vector3(0f, 19.06f, 168f), 0f);
        }

        // =================================================================== jogador

        static GameObject CriarJogador(Vector3 pos)
        {
            var go = new GameObject("Justiceiro");
            go.layer = Layers.PlayerLayer;
            go.transform.position = pos;

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.95f, 0f);
            cc.slopeLimit = 55f;
            cc.stepOffset = 0.68f;
            cc.skinWidth = 0.05f;

            var camGo = new GameObject("CameraJogador");
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            camGo.tag = "MainCamera";
            camGo.layer = Layers.PlayerLayer;
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 100f;
            cam.nearClipPlane = 0.04f;
            cam.farClipPlane = 450f;
            cam.allowHDR = true;
            camGo.AddComponent<AudioListener>();

            var pc = go.AddComponent<PlayerController>();
            pc.cam = camGo.transform;

            go.AddComponent<PlayerHealth>();
            var wc = go.AddComponent<WeaponController>();
            wc.cam = camGo.transform;
            go.AddComponent<PlayerExecucao>();
            go.AddComponent<Hud>();

            return go;
        }

        // =================================================================== ZONA A

        static void ZonaA_PeDoMorro()
        {
            var z = Grupo("ZonaA_PeDoMorro");

            Caixa("Rua", new Vector3(0f, -0.5f, 15f), new Vector3(36f, 1f, 42f), mAsfalto, z, true, 0.3f);
            Caixa("Calcada_E", new Vector3(-15f, 0.07f, 15f), new Vector3(6f, 0.15f, 42f), mConcreto, z, true, 0.35f);
            Caixa("Calcada_D", new Vector3(15f, 0.07f, 15f), new Vector3(6f, 0.15f, 42f), mConcreto, z, true, 0.35f);

            // muros
            Caixa("Muro_E", new Vector3(-18.5f, 4f, 15f), new Vector3(1f, 9f, 42f), mTijolo, z, true, 0.3f);
            Caixa("Muro_D", new Vector3(18.5f, 4f, 15f), new Vector3(1f, 9f, 42f), mTijolo, z, true, 0.3f);
            Caixa("Muro_Tras", new Vector3(0f, 4f, -6.5f), new Vector3(38f, 9f, 1f), mConcreto, z, true, 0.3f);

            // muro da frente com a boca do beco (x de -4 a 0)
            Caixa("Muro_Frente_E", new Vector3(-11f, 4f, 35.5f), new Vector3(14f, 9f, 1f), mTijolo, z, true, 0.3f);
            Caixa("Muro_Frente_D", new Vector3(9f, 4f, 35.5f), new Vector3(18f, 9f, 1f), mTijolo, z, true, 0.3f);
            Caixa("Verga_Beco", new Vector3(-2f, 8f, 35.5f), new Vector3(4.2f, 1f, 1f), mConcreto, z, true, 0.5f);

            Barraco("Casa_A1", new Vector3(-12f, 0.15f, 8f), new Vector3(9f, 7f, 8f), mRosa, z, true, 0);
            Barraco("Casa_A2", new Vector3(13f, 0.15f, 6f), new Vector3(8f, 5.5f, 9f), mAzul, z, true, 1);
            Barraco("Casa_A3", new Vector3(11f, 0.15f, 26f), new Vector3(9f, 8f, 8f), mAmarelo, z, true, 2);
            Barraco("Casa_A4", new Vector3(-13f, 0.15f, 27f), new Vector3(7f, 6f, 7f), mCreme, z, false, 3);

            Poste(new Vector3(-8f, 0.15f, 18f), z);
            Poste(new Vector3(7f, 0.15f, 30f), z);

            // obstáculos de cobertura
            Caixa("Caçamba", new Vector3(4f, 1.1f, 14f), new Vector3(5f, 2.2f, 2.6f), mZinco, z, true, 0.5f);
            Caixa("Muretas", new Vector3(-5f, 0.7f, 24f), new Vector3(7f, 1.4f, 0.6f), mConcreto, z, true, 0.6f);
            Caixa("Muretas2", new Vector3(3f, 0.7f, 20f), new Vector3(0.6f, 1.4f, 6f), mConcreto, z, true, 0.6f);

            // primeiros inimigos — ensinam o combate
            Spawn(TipoInimigo.Moleque, new Vector3(-4f, 0.3f, 16f), z, true);
            Spawn(TipoInimigo.Moleque, new Vector3(6f, 0.3f, 22f), z, true);
            Spawn(TipoInimigo.Soldado, new Vector3(-7f, 0.3f, 29f), z, true);
            Spawn(TipoInimigo.Soldado, new Vector3(8f, 0.3f, 31f), z, true);

            Item(TipoColetavel.Colete, new Vector3(0f, 0.4f, 12f), z);
            Item(TipoColetavel.MunicaoCartucho, new Vector3(4f, 2.6f, 14f), z);
            Item(TipoColetavel.MunicaoBala, new Vector3(-5f, 0.4f, 25f), z);
        }

        // =================================================================== ZONA B

        static void ZonaB_Beco()
        {
            var z = Grupo("ZonaB_Beco");

            EscadaZ("Degrau", -2f, 4.2f, 36f, 58f, 0f, 7f, 22, mConcreto, z);

            Caixa("BecoParede_E", new Vector3(-4.6f, 7f, 47f), new Vector3(1f, 20f, 24f), mTijolo, z, true, 0.3f);
            Caixa("BecoParede_D", new Vector3(0.6f, 7f, 47f), new Vector3(1f, 20f, 24f), mRosa, z, true, 0.3f);

            // lajes atravessadas por cima — claustrofobia de beco
            Caixa("Passarela1", new Vector3(-2f, 6.2f, 42f), new Vector3(5.5f, 0.2f, 2f), mZinco, z, false, 0.6f);
            Caixa("Passarela2", new Vector3(-2f, 9.5f, 51f), new Vector3(5.5f, 0.2f, 2.4f), mMadeira, z, false, 0.6f);

            LuzPonto(new Vector3(-2f, 5.2f, 41f), new Color(1f, 0.62f, 0.3f), 2.6f, 12f);
            LuzPonto(new Vector3(-2f, 8.4f, 50f), new Color(0.5f, 0.75f, 1f), 2.2f, 12f);

            Spawn(TipoInimigo.Moleque, new Vector3(-2f, 3.2f, 46f), z, true);
            Spawn(TipoInimigo.Moleque, new Vector3(-2f, 5.6f, 53f), z, true);
            Spawn(TipoInimigo.Soldado, new Vector3(-2f, 7.3f, 57f), z, true);

            Item(TipoColetavel.Vida, new Vector3(-3f, 4.4f, 49f), z);
        }

        // =================================================================== ZONA C

        static ArenaEncounter ZonaC_Laje()
        {
            var z = Grupo("ZonaC_Laje_Arena1");

            Caixa("Piso", new Vector3(-10f, 6.5f, 75f), new Vector3(36f, 1f, 34f), mLaje, z, true, 0.3f);

            Caixa("Par_E", new Vector3(-28.5f, 10f, 75f), new Vector3(1f, 8f, 34f), mVerde, z, true, 0.3f);
            Caixa("Par_D", new Vector3(8.5f, 10f, 75f), new Vector3(1f, 8f, 34f), mCreme, z, true, 0.3f);

            Caixa("Par_S1", new Vector3(-16.5f, 10f, 58.5f), new Vector3(25f, 8f, 1f), mTijolo, z, true, 0.3f);
            Caixa("Par_S2", new Vector3(4.5f, 10f, 58.5f), new Vector3(8f, 8f, 1f), mTijolo, z, true, 0.3f);

            Caixa("Par_N1", new Vector3(-21.5f, 10f, 91.5f), new Vector3(15f, 8f, 1f), mAzul, z, true, 0.3f);
            Caixa("Par_N2", new Vector3(-0.5f, 10f, 91.5f), new Vector3(17f, 8f, 1f), mAzul, z, true, 0.3f);

            // estruturas no meio — verticalidade e cobertura
            Barraco("Casa_C1", new Vector3(-22f, 7f, 66f), new Vector3(7f, 4.5f, 7f), mAmarelo, z, true, 0);
            Barraco("Casa_C2", new Vector3(2f, 7f, 82f), new Vector3(7f, 5.5f, 7f), mRosa, z, true, 1);
            Caixa("Rampa_C", new Vector3(-16.5f, 8.6f, 66f), new Vector3(4f, 0.4f, 9f), mConcreto, z, true, 0.4f);
            Caixa("Plat_C", new Vector3(-16.5f, 10.7f, 61.5f), new Vector3(4f, 0.4f, 5f), mConcreto, z, true, 0.4f);

            Caixa("Mureta_C1", new Vector3(-12f, 7.8f, 74f), new Vector3(9f, 1.6f, 0.6f), mConcreto, z, true, 0.6f);
            Caixa("Mureta_C2", new Vector3(-4f, 7.8f, 84f), new Vector3(0.6f, 1.6f, 8f), mConcreto, z, true, 0.6f);
            Caixa("Tonel", new Vector3(-24f, 7.8f, 84f), new Vector3(1.4f, 1.6f, 1.4f), mZinco, z, true, 0.7f);

            Poste(new Vector3(-10f, 7.05f, 78f), z);
            LuzPonto(new Vector3(-20f, 11f, 70f), new Color(0.6f, 0.4f, 1f), 2f, 20f);

            // barreiras (trancam sul e norte)
            var bS = Grade("Grade_Sul", new Vector3(-2f, 10.5f, 58.5f), new Vector3(4.4f, 8f, 0.5f), z);
            var bN = Grade("Grade_Norte", new Vector3(-11.5f, 10.5f, 91.5f), new Vector3(5.4f, 8f, 0.5f), z);

            var arena = Arena("ARENA 1 — A LAJE", new Vector3(-10f, 9f, 72f), new Vector3(30f, 6f, 20f), z);
            arena.barreiras.Add(bS);
            arena.barreiras.Add(bN);
            arena.pontoPremio = new Vector3(-10f, 7.4f, 75f);

            var o1 = new Onda { atrasoEntreSpawns = 0.3f, restantesParaProxima = 1 };
            o1.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-24f, 7.2f, 88f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(4f, 7.2f, 88f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-10f, 7.2f, 89f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(-26f, 7.2f, 62f), z, false, 0f));
            o1.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(6f, 7.2f, 62f), z, false, 0f));
            arena.ondas.Add(o1);

            var o2 = new Onda { atrasoEntreSpawns = 0.35f };
            o2.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(-22f, 11.4f, 62f), z, false, 0f));
            o2.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(2f, 12.7f, 82f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-16f, 7.2f, 86f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-6f, 7.2f, 86f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Fogueteiro, new Vector3(-10f, 7.2f, 60f), z, false, 0f));
            arena.ondas.Add(o2);

            Item(TipoColetavel.MunicaoCartucho, new Vector3(-20f, 7.4f, 74f), z);
            Item(TipoColetavel.MunicaoBala, new Vector3(0f, 7.4f, 76f), z);
            Item(TipoColetavel.Vida, new Vector3(-16.5f, 11.2f, 61.5f), z);

            return arena;
        }

        // =================================================================== ZONA D

        static void ZonaD_Viela()
        {
            var z = Grupo("ZonaD_Viela");

            EscadaZ("Degrau", -11.5f, 5f, 92f, 110f, 7f, 13f, 20, mConcreto, z);

            Caixa("Viela_E", new Vector3(-14.6f, 13f, 101f), new Vector3(1f, 22f, 20f), mVerde, z, true, 0.3f);
            Caixa("Viela_D", new Vector3(-8.4f, 13f, 101f), new Vector3(1f, 22f, 20f), mTijolo, z, true, 0.3f);
            Caixa("Passarela_D", new Vector3(-11.5f, 13.4f, 100f), new Vector3(6.2f, 0.25f, 2.2f), mMadeira, z, false, 0.6f);

            LuzPonto(new Vector3(-11.5f, 11.5f, 96f), new Color(1f, 0.55f, 0.25f), 2.6f, 14f);
            LuzPonto(new Vector3(-11.5f, 15.5f, 107f), new Color(1f, 0.8f, 0.45f), 2.4f, 14f);

            Spawn(TipoInimigo.Soldado, new Vector3(-11.5f, 9.5f, 99f), z, true, 180f);
            Spawn(TipoInimigo.Moleque, new Vector3(-11.5f, 12.5f, 107f), z, true, 180f);

            Item(TipoColetavel.Vida, new Vector3(-13f, 11f, 102f), z);
        }

        // =================================================================== ZONA E

        static ArenaEncounter ZonaE_Quadra()
        {
            var z = Grupo("ZonaE_Quadra_Arena2");

            Caixa("Piso", new Vector3(-5f, 12.5f, 130f), new Vector3(46f, 1f, 42f), mConcreto, z, true, 0.3f);
            // marcação da quadra
            Caixa("Linha1", new Vector3(-5f, 13.03f, 130f), new Vector3(30f, 0.06f, 0.35f), mCreme, z, false, 1f);
            Caixa("Linha2", new Vector3(-5f, 13.03f, 130f), new Vector3(0.35f, 0.06f, 24f), mCreme, z, false, 1f);

            Caixa("Par_E", new Vector3(-28.5f, 16.5f, 130f), new Vector3(1f, 9f, 42f), mAmarelo, z, true, 0.3f);
            Caixa("Par_D", new Vector3(18.5f, 16.5f, 130f), new Vector3(1f, 9f, 42f), mRosa, z, true, 0.3f);

            Caixa("Par_S1", new Vector3(-21.5f, 16.5f, 109.5f), new Vector3(15f, 9f, 1f), mTijolo, z, true, 0.3f);
            Caixa("Par_S2", new Vector3(4.5f, 16.5f, 109.5f), new Vector3(27f, 9f, 1f), mTijolo, z, true, 0.3f);

            Caixa("Par_N1", new Vector3(-14f, 16.5f, 150.5f), new Vector3(29f, 9f, 1f), mConcreto, z, true, 0.3f);
            Caixa("Par_N2", new Vector3(11.5f, 16.5f, 150.5f), new Vector3(14f, 9f, 1f), mConcreto, z, true, 0.3f);

            // arquibancada / lajes pra subir
            Caixa("Arquib1", new Vector3(-25f, 13.8f, 130f), new Vector3(6f, 1.6f, 26f), mConcreto, z, true, 0.4f);
            Caixa("Arquib2", new Vector3(-25f, 15.2f, 130f), new Vector3(3.5f, 1.4f, 26f), mConcreto, z, true, 0.4f);
            Barraco("Casa_E1", new Vector3(14f, 13f, 120f), new Vector3(8f, 6f, 9f), mVerde, z, true, 0);
            Barraco("Casa_E2", new Vector3(13f, 13f, 142f), new Vector3(8f, 7.5f, 8f), mAzul, z, true, 1);
            Caixa("Rampa_E", new Vector3(6f, 14.6f, 142f), new Vector3(5f, 0.4f, 10f), mConcreto, z, true, 0.4f);

            Caixa("Trave1", new Vector3(-5f, 14.6f, 116f), new Vector3(7f, 0.25f, 0.25f), mZinco, z, true, 1f);
            Caixa("Trave1a", new Vector3(-8.5f, 13.8f, 116f), new Vector3(0.25f, 2.4f, 0.25f), mZinco, z, true, 1f);
            Caixa("Trave1b", new Vector3(-1.5f, 13.8f, 116f), new Vector3(0.25f, 2.4f, 0.25f), mZinco, z, true, 1f);

            Caixa("Cobertura1", new Vector3(-16f, 14.1f, 124f), new Vector3(7f, 1.2f, 1f), mConcreto, z, true, 0.6f);
            Caixa("Cobertura2", new Vector3(2f, 14.1f, 136f), new Vector3(1f, 1.2f, 7f), mConcreto, z, true, 0.6f);
            Caixa("Tonel_E", new Vector3(-12f, 13.8f, 140f), new Vector3(1.4f, 1.6f, 1.4f), mZinco, z, true, 0.7f);

            Poste(new Vector3(-20f, 13.05f, 118f), z);
            Poste(new Vector3(8f, 13.05f, 134f), z);

            var bS = Grade("Grade_Sul2", new Vector3(-11.5f, 17.2f, 109.5f), new Vector3(5.4f, 9f, 0.5f), z);
            var arena = Arena("ARENA 2 — A QUADRA", new Vector3(-5f, 15f, 128f), new Vector3(40f, 6f, 26f), z);
            arena.barreiras.Add(bS);
            arena.pontoPremio = new Vector3(-5f, 13.4f, 130f);
            arena.premio = TipoColetavel.VidaGrande;

            var o1 = new Onda { atrasoEntreSpawns = 0.28f, restantesParaProxima = 2 };
            o1.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-24f, 13.2f, 146f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-18f, 13.2f, 147f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(14f, 19.5f, 120f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(-25f, 16.2f, 138f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Fogueteiro, new Vector3(13f, 20.9f, 142f), z));
            arena.ondas.Add(o1);

            var o2 = new Onda { atrasoEntreSpawns = 0.3f, restantesParaProxima = 2 };
            o2.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(-24f, 13.2f, 114f), z, false, 0f));
            o2.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(16f, 13.2f, 147f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(0f, 13.2f, 148f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-8f, 13.2f, 148f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Blindado, new Vector3(-5f, 13.2f, 146f), z));
            arena.ondas.Add(o2);

            var o3 = new Onda { atrasoEntreSpawns = 0.35f };
            o3.pontos.Add(Spawn(TipoInimigo.Fogueteiro, new Vector3(-25f, 16.2f, 122f), z));
            o3.pontos.Add(Spawn(TipoInimigo.Blindado, new Vector3(14f, 19.5f, 120f), z));
            o3.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(-20f, 13.2f, 145f), z));
            o3.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(10f, 13.2f, 145f), z));
            o3.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-5f, 13.2f, 147f), z));
            arena.ondas.Add(o3);

            Item(TipoColetavel.MunicaoRojao, new Vector3(-25f, 16.4f, 130f), z);
            Item(TipoColetavel.MunicaoCartucho, new Vector3(-16f, 13.4f, 123f), z);
            Item(TipoColetavel.MunicaoBala, new Vector3(2f, 13.4f, 137f), z);
            Item(TipoColetavel.Colete, new Vector3(13f, 21.2f, 142f), z);

            // porta vermelha no vão norte (x de 0 a 5)
            var portaGo = Caixa("PortaVermelha", new Vector3(2.5f, 16.5f, 150.5f), new Vector3(5.2f, 9f, 0.7f),
                                mVermelhoEmissivo, z, true, 0.6f);
            portaGo.isStatic = false;
            var pk = portaGo.AddComponent<PortaChave>();
            pk.posAberta = new Vector3(2.5f, 25.5f, 150.5f);
            pk.alcance = 6f;
            LuzPonto(new Vector3(2.5f, 15f, 148.5f), new Color(1f, 0.15f, 0.12f), 3.5f, 14f);

            return arena;
        }

        // =================================================================== ZONA F

        static void ZonaF_Rampa()
        {
            var z = Grupo("ZonaF_Rampa");
            EscadaZ("Degrau", 2.5f, 5.2f, 151f, 163f, 13f, 19f, 16, mConcreto, z);
            Caixa("Rampa_E", new Vector3(-0.6f, 19f, 157f), new Vector3(1f, 22f, 14f), mTijolo, z, true, 0.3f);
            Caixa("Rampa_D", new Vector3(5.6f, 19f, 157f), new Vector3(1f, 22f, 14f), mTijolo, z, true, 0.3f);
            LuzPonto(new Vector3(2.5f, 18f, 157f), new Color(1f, 0.6f, 0.3f), 3f, 16f);
        }

        // =================================================================== ZONA G

        static ArenaEncounter ZonaG_Topo()
        {
            var z = Grupo("ZonaG_Topo_ArenaFinal");

            Caixa("Piso", new Vector3(0f, 18.5f, 180f), new Vector3(42f, 1f, 38f), mLaje, z, true, 0.3f);

            // parapeito baixo (dá pra ver a cidade lá embaixo)
            Caixa("Par_E", new Vector3(-21f, 19.8f, 180f), new Vector3(1f, 1.6f, 38f), mConcreto, z, true, 0.5f);
            Caixa("Par_D", new Vector3(21f, 19.8f, 180f), new Vector3(1f, 1.6f, 38f), mConcreto, z, true, 0.5f);
            Caixa("Par_N", new Vector3(0f, 19.8f, 199f), new Vector3(42f, 1.6f, 1f), mConcreto, z, true, 0.5f);
            Caixa("Par_S1", new Vector3(-11f, 21.5f, 161.5f), new Vector3(20f, 6f, 1f), mConcreto, z, true, 0.4f);
            Caixa("Par_S2", new Vector3(13.5f, 21.5f, 161.5f), new Vector3(15f, 6f, 1f), mConcreto, z, true, 0.4f);

            Barraco("Torre_G1", new Vector3(-15f, 19f, 172f), new Vector3(7f, 5f, 7f), mAmarelo, z, true, 0);
            Barraco("Torre_G2", new Vector3(15f, 19f, 174f), new Vector3(7f, 6.5f, 7f), mVerde, z, true, 1);
            Caixa("Rampa_G", new Vector3(-9f, 20.6f, 172f), new Vector3(4.5f, 0.4f, 10f), mConcreto, z, true, 0.4f);

            Caixa("Mureta_G1", new Vector3(-6f, 19.8f, 184f), new Vector3(10f, 1.6f, 0.6f), mConcreto, z, true, 0.6f);
            Caixa("Mureta_G2", new Vector3(8f, 19.8f, 186f), new Vector3(0.6f, 1.6f, 9f), mConcreto, z, true, 0.6f);
            Caixa("CaixaDagua_G", new Vector3(-17f, 20.6f, 190f), new Vector3(3.5f, 3.2f, 3.5f), mAzul, z, true, 0.5f);

            Poste(new Vector3(-12f, 19.05f, 182f), z);
            Poste(new Vector3(12f, 19.05f, 190f), z);
            LuzPonto(new Vector3(0f, 24f, 178f), new Color(1f, 0.4f, 0.3f), 3f, 30f);

            var bS = Grade("Grade_Topo", new Vector3(2.5f, 21.5f, 161.5f), new Vector3(5.4f, 6f, 0.5f), z);

            var arena = Arena("TOPO DO MORRO", new Vector3(0f, 21f, 176f), new Vector3(36f, 6f, 22f), z);
            arena.barreiras.Add(bS);
            arena.pontoPremio = new Vector3(0f, 19.4f, 180f);

            var o1 = new Onda { atrasoEntreSpawns = 0.25f, restantesParaProxima = 2 };
            o1.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(-18f, 19.2f, 195f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(18f, 19.2f, 195f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-6f, 19.2f, 196f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(6f, 19.2f, 196f), z));
            o1.pontos.Add(Spawn(TipoInimigo.Blindado, new Vector3(0f, 19.2f, 196f), z));
            arena.ondas.Add(o1);

            var o2 = new Onda { atrasoEntreSpawns = 0.5f };
            o2.pontos.Add(Spawn(TipoInimigo.Chefe, new Vector3(0f, 19.4f, 194f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Fogueteiro, new Vector3(-15f, 23.5f, 172f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Fogueteiro, new Vector3(15f, 25f, 174f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(-19f, 19.2f, 186f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Soldado, new Vector3(19f, 19.2f, 186f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(-10f, 19.2f, 196f), z));
            o2.pontos.Add(Spawn(TipoInimigo.Moleque, new Vector3(10f, 19.2f, 196f), z));
            arena.ondas.Add(o2);

            Item(TipoColetavel.MunicaoRojao, new Vector3(-8f, 19.4f, 170f), z);
            Item(TipoColetavel.MunicaoRojao, new Vector3(8f, 19.4f, 170f), z);
            Item(TipoColetavel.MunicaoCartucho, new Vector3(-17f, 22.8f, 190f), z);
            Item(TipoColetavel.MunicaoBala, new Vector3(0f, 19.4f, 168f), z);
            Item(TipoColetavel.Colete, new Vector3(-15f, 24.5f, 172f), z);
            Item(TipoColetavel.VidaGrande, new Vector3(15f, 26f, 174f), z);

            return arena;
        }
    }
}
