using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Justiceiro.EditorTools
{
    /// <summary>
    /// Gera texturas e materiais por código e salva como assets de verdade.
    /// Nada de baixar pack de textura — o morro inteiro é pintado aqui.
    /// </summary>
    public static class ProcArt
    {
        public const string PastaRaiz = "Assets/Gerado";
        public const string PastaTex = PastaRaiz + "/Texturas";
        public const string PastaMat = PastaRaiz + "/Materiais";
        public const string PastaMesh = PastaRaiz + "/Malhas";

        static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        public static void GarantirPastas()
        {
            if (!AssetDatabase.IsValidFolder(PastaRaiz)) AssetDatabase.CreateFolder("Assets", "Gerado");
            if (!AssetDatabase.IsValidFolder(PastaTex)) AssetDatabase.CreateFolder(PastaRaiz, "Texturas");
            if (!AssetDatabase.IsValidFolder(PastaMat)) AssetDatabase.CreateFolder(PastaRaiz, "Materiais");
            if (!AssetDatabase.IsValidFolder(PastaMesh)) AssetDatabase.CreateFolder(PastaRaiz, "Malhas");
        }

        public static void LimparCache() => _cache.Clear();

        // ------------------------------------------------------------------ ruído

        static float Hash(int x, int y, int semente)
        {
            int n = x * 374761393 + y * 668265263 + semente * 1442695040;
            n = (n ^ (n >> 13)) * 1274126177;
            return ((n ^ (n >> 16)) & 0x7fffffff) / (float)0x7fffffff;
        }

        static float RuidoSuave(float x, float y, int semente)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float xf = x - xi, yf = y - yi;
            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);
            float a = Hash(xi, yi, semente), b = Hash(xi + 1, yi, semente);
            float c = Hash(xi, yi + 1, semente), d = Hash(xi + 1, yi + 1, semente);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        static float Fbm(float x, float y, int semente, int oitavas = 4)
        {
            float s = 0f, amp = 0.5f, f = 1f, norm = 0f;
            for (int i = 0; i < oitavas; i++)
            {
                s += RuidoSuave(x * f, y * f, semente + i * 31) * amp;
                norm += amp;
                amp *= 0.5f;
                f *= 2f;
            }
            return s / norm;
        }

        // ------------------------------------------------------------------ texturas

        public static Texture2D Textura(string nome, int tam, System.Func<float, float, int, Color> pintor, int semente)
        {
            string caminho = PastaTex + "/" + nome + ".asset";
            var existente = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
            if (existente != null) return existente;

            var t = new Texture2D(tam, tam, TextureFormat.RGBA32, true);
            t.name = nome;
            t.wrapMode = TextureWrapMode.Repeat;
            t.filterMode = FilterMode.Bilinear;

            var px = new Color[tam * tam];
            for (int y = 0; y < tam; y++)
                for (int x = 0; x < tam; x++)
                    px[y * tam + x] = pintor(x / (float)tam, y / (float)tam, semente);
            t.SetPixels(px);
            t.Apply(true);

            AssetDatabase.CreateAsset(t, caminho);
            return t;
        }

        static Color Sujar(Color b, float u, float v, int semente, float forca = 0.22f)
        {
            float n = Fbm(u * 9f, v * 9f, semente);
            float manchas = Fbm(u * 3f, v * 3f, semente + 77);
            float k = 1f - (n * forca) - (manchas > 0.62f ? (manchas - 0.62f) * 0.9f : 0f);
            return new Color(b.r * k, b.g * k, b.b * k, 1f);
        }

        public static Texture2D TexTijolo()
        {
            return Textura("tijolo", 256, (u, v, s) =>
            {
                float linhas = 8f, colunas = 4f;
                float fy = v * linhas;
                int linha = Mathf.FloorToInt(fy);
                float desl = (linha % 2 == 0) ? 0f : 0.5f;
                float fx = u * colunas + desl;
                float ix = fx - Mathf.Floor(fx);
                float iy = fy - linha;

                float arg = 0.045f;
                bool junta = ix < arg || ix > 1f - arg || iy < arg * 2.2f || iy > 1f - arg * 2.2f;

                Color tijolo = Color.Lerp(new Color(0.52f, 0.27f, 0.20f), new Color(0.66f, 0.36f, 0.26f),
                                          Hash(Mathf.FloorToInt(fx), linha, s));
                Color argamassa = new Color(0.58f, 0.56f, 0.52f);
                Color b = junta ? argamassa : tijolo;
                return Sujar(b, u, v, s, 0.3f);
            }, 11);
        }

        public static Texture2D TexReboco(string nome, Color cor, int semente)
        {
            return Textura("reboco_" + nome, 256, (u, v, s) =>
            {
                float n = Fbm(u * 14f, v * 14f, s);
                Color b = Color.Lerp(cor * 0.86f, cor * 1.1f, n);

                // faixas de reboco caindo, mostrando tijolo embaixo
                float descasca = Fbm(u * 4f, v * 4f, s + 300);
                if (descasca > 0.70f)
                    b = Color.Lerp(b, new Color(0.55f, 0.30f, 0.23f), (descasca - 0.70f) * 3.0f);

                // barra de umidade na base
                if (v < 0.18f) b = Color.Lerp(b, new Color(0.30f, 0.30f, 0.27f), (0.18f - v) * 3.2f);

                return Sujar(b, u, v, s, 0.24f);
            }, semente);
        }

        public static Texture2D TexConcreto()
        {
            return Textura("concreto", 256, (u, v, s) =>
            {
                float n = Fbm(u * 18f, v * 18f, s);
                Color b = Color.Lerp(new Color(0.40f, 0.40f, 0.39f), new Color(0.56f, 0.56f, 0.55f), n);
                float trinca = Fbm(u * 6f, v * 6f, s + 91);
                if (Mathf.Abs(trinca - 0.5f) < 0.022f) b *= 0.62f;
                return Sujar(b, u, v, s, 0.18f);
            }, 23);
        }

        public static Texture2D TexAsfalto()
        {
            return Textura("asfalto", 256, (u, v, s) =>
            {
                float n = Fbm(u * 26f, v * 26f, s);
                Color b = Color.Lerp(new Color(0.13f, 0.13f, 0.14f), new Color(0.24f, 0.24f, 0.25f), n);
                float buraco = Fbm(u * 5f, v * 5f, s + 55);
                if (buraco > 0.74f) b *= 0.6f;
                return b;
            }, 31);
        }

        public static Texture2D TexZinco()
        {
            return Textura("zinco", 256, (u, v, s) =>
            {
                float onda = Mathf.Sin(u * Mathf.PI * 2f * 12f) * 0.5f + 0.5f;
                Color b = Color.Lerp(new Color(0.34f, 0.31f, 0.28f), new Color(0.58f, 0.55f, 0.52f), onda);
                float ferrugem = Fbm(u * 7f, v * 7f, s);
                if (ferrugem > 0.55f)
                    b = Color.Lerp(b, new Color(0.45f, 0.21f, 0.10f), (ferrugem - 0.55f) * 2.2f);
                return Sujar(b, u, v, s, 0.2f);
            }, 47);
        }

        public static Texture2D TexMadeira()
        {
            return Textura("madeira", 256, (u, v, s) =>
            {
                float veio = Mathf.Sin((v * 22f) + Fbm(u * 4f, v * 4f, s) * 7f) * 0.5f + 0.5f;
                Color b = Color.Lerp(new Color(0.32f, 0.21f, 0.12f), new Color(0.48f, 0.33f, 0.19f), veio);
                if (Mathf.Repeat(v * 6f, 1f) < 0.03f) b *= 0.55f;
                return Sujar(b, u, v, s, 0.2f);
            }, 59);
        }

        public static Texture2D TexLaje()
        {
            return Textura("laje", 256, (u, v, s) =>
            {
                float n = Fbm(u * 20f, v * 20f, s);
                Color b = Color.Lerp(new Color(0.44f, 0.44f, 0.42f), new Color(0.60f, 0.60f, 0.58f), n);
                if (Mathf.Repeat(u * 4f, 1f) < 0.02f || Mathf.Repeat(v * 4f, 1f) < 0.02f) b *= 0.78f;
                float musgo = Fbm(u * 3f, v * 3f, s + 12);
                if (musgo > 0.68f) b = Color.Lerp(b, new Color(0.24f, 0.32f, 0.18f), (musgo - 0.68f) * 2.4f);
                return b;
            }, 67);
        }

        /// <summary>Folhagem da mata — verde-escuro manchado pra montar morros e árvores.</summary>
        public static Texture2D TexMata()
        {
            return Textura("mata", 256, (u, v, s) =>
            {
                float n = Fbm(u * 7f, v * 7f, s);
                float det = Fbm(u * 19f, v * 19f, s + 5);
                Color b = Color.Lerp(new Color(0.16f, 0.30f, 0.12f), new Color(0.28f, 0.44f, 0.16f), n);
                if (det > 0.6f) b = Color.Lerp(b, new Color(0.09f, 0.18f, 0.08f), (det - 0.6f) * 1.7f);
                return b;
            }, 83);
        }

        /// <summary>Torres/apartamentos da cidade vista de longe (dia).</summary>
        public static Texture2D TexCidade()
        {
            return Textura("cidade", 256, (u, v, s) =>
            {
                float colunas = 5f, andares = 11f;
                int cx = Mathf.FloorToInt(u * colunas);
                int cy = Mathf.FloorToInt(v * andares);
                float ix = u * colunas - cx;
                float iy = v * andares - cy;
                float borda = 0.05f;
                bool janela = ix > borda && ix < 1f - borda && iy > borda && iy < 1f - borda;

                Color b = Color.Lerp(new Color(0.34f, 0.36f, 0.40f), new Color(0.26f, 0.27f, 0.30f), Fbm(u * 3f, v * 3f, s));
                if (!janela) b = Color.Lerp(b, new Color(0.20f, 0.20f, 0.22f), 0.55f);
                else b *= 0.55f;
                return b;
            }, 97);
        }

        /// <summary>Fachada de prédio à noite — janelas acesas por janelinha (para o fundo da cidade).</summary>
        public static Texture2D TexFundoJanelas(string nome)
        {
            return Textura(nome, 256, (u, v, s) =>
            {
                float colunas = 6f, andares = 12f;
                int cx = Mathf.FloorToInt(u * colunas);
                int cy = Mathf.FloorToInt(v * andares);
                float ix = u * colunas - cx;
                float iy = v * andares - cy;
                float borda = 0.125f;
                bool janela = ix > borda && ix < 1f - borda && iy > borda && iy < 1f - borda;

                Color b = new Color(0.04f, 0.05f, 0.09f);
                if (janela)
                {
                    float luz = Hash(cx, cy, s);
                    if (luz > 0.40f)
                        b = Color.Lerp(new Color(1f, 0.72f, 0.35f), new Color(0.78f, 0.52f, 0.30f),
                                       Hash(cx, cy + 7, s)) * 0.95f;
                    else
                        b = new Color(0.05f, 0.07f, 0.11f);
                    b *= 0.8f + 0.2f * Fbm(u * 5f, v * 5f, s + 3);
                }
                return b;
            }, 101);
        }

        // ------------------------------------------------------------------ materiais

        public static Material Mat(string nome, Texture2D tex, float liso = 0.12f, float metal = 0f,
                                   Color? tinta = null, Color? emissao = null)
        {
            if (_cache.TryGetValue(nome, out var m) && m != null) return m;

            string caminho = PastaMat + "/" + nome + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null)
            {
                Shader sh = Shader.Find("Standard");
                if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(sh) { name = nome };
                AssetDatabase.CreateAsset(m, caminho);
            }

            if (tex != null) m.mainTexture = tex;
            m.color = tinta ?? Color.white;
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", liso);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", liso);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);

            if (emissao.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emissao.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(m);
            _cache[nome] = m;
            return m;
        }

        // ------------------------------------------------------------------ malhas

        static readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();

        /// <summary>
        /// Caixa com UV proporcional ao tamanho real — a textura repete certinho
        /// em vez de esticar. É o que separa "cubo cinza" de "parede de verdade".
        /// </summary>
        public static Mesh Caixa(Vector3 tamanho, float uvPorMetro = 0.5f)
        {
            string chave = tamanho.x.ToString("F2") + "_" + tamanho.y.ToString("F2") + "_" +
                           tamanho.z.ToString("F2") + "_" + uvPorMetro.ToString("F2");
            if (_meshes.TryGetValue(chave, out var cache) && cache != null) return cache;

            string caminho = PastaMesh + "/caixa_" + chave.Replace('.', 'p') + ".asset";
            var salva = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (salva != null) { _meshes[chave] = salva; return salva; }

            Vector3 h = tamanho * 0.5f;
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void Face(Vector3 n, Vector3 eixoU, Vector3 eixoV, float largura, float altura)
            {
                int b = verts.Count;
                Vector3 c = Vector3.Scale(n, h);
                Vector3 u = eixoU * (largura * 0.5f);
                Vector3 v = eixoV * (altura * 0.5f);

                verts.Add(c - u - v); verts.Add(c + u - v); verts.Add(c + u + v); verts.Add(c - u + v);
                for (int i = 0; i < 4; i++) norms.Add(n);

                float su = largura * uvPorMetro, sv = altura * uvPorMetro;
                uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(su, 0));
                uvs.Add(new Vector2(su, sv)); uvs.Add(new Vector2(0, sv));

                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
            }

            Face(Vector3.forward, Vector3.right, Vector3.up, tamanho.x, tamanho.y);
            Face(Vector3.back, Vector3.left, Vector3.up, tamanho.x, tamanho.y);
            Face(Vector3.right, Vector3.back, Vector3.up, tamanho.z, tamanho.y);
            Face(Vector3.left, Vector3.forward, Vector3.up, tamanho.z, tamanho.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, tamanho.x, tamanho.z);
            Face(Vector3.down, Vector3.right, Vector3.back, tamanho.x, tamanho.z);

            var mesh = new Mesh { name = "caixa_" + chave };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, caminho);
            _meshes[chave] = mesh;
            return mesh;
        }

        public static void LimparMalhas() => _meshes.Clear();
    }
}
