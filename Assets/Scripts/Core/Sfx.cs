using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    /// <summary>
    /// Áudio 100% procedural — nenhum arquivo de som precisa ser importado.
    /// Todos os clipes são sintetizados na primeira vez que são pedidos.
    /// </summary>
    public static class Sfx
    {
        const int SR = 44100;

        static readonly Dictionary<string, AudioClip> _cache = new Dictionary<string, AudioClip>();
        static AudioSource _2d;
        static System.Random _rng = new System.Random(1337);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Resetar()
        {
            _cache.Clear();
            _2d = null;
            _rng = new System.Random(1337);
        }

        static AudioSource Fonte2D
        {
            get
            {
                if (_2d == null)
                {
                    var go = new GameObject("~Sfx2D");
                    _2d = go.AddComponent<AudioSource>();
                    _2d.spatialBlend = 0f;
                    _2d.playOnAwake = false;
                }
                return _2d;
            }
        }

        public static void Tocar(string nome, float vol = 1f, float pitch = 1f)
        {
            var c = Clipe(nome);
            if (c == null) return;
            var f = Fonte2D;
            f.pitch = pitch;
            f.PlayOneShot(c, vol);
        }

        public static void TocarEm(string nome, Vector3 pos, float vol = 1f, float pitch = 1f, float alcance = 45f)
        {
            var c = Clipe(nome);
            if (c == null) return;
            var go = new GameObject("~Sfx3D");
            go.transform.position = pos;
            var src = go.AddComponent<AudioSource>();
            src.clip = c;
            src.volume = vol;
            src.pitch = pitch;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 3f;
            src.maxDistance = alcance;
            src.Play();
            Object.Destroy(go, c.length / Mathf.Max(0.05f, pitch) + 0.2f);
        }

        public static AudioClip Clipe(string nome)
        {
            if (_cache.TryGetValue(nome, out var c) && c != null) return c;
            c = Sintetizar(nome);
            _cache[nome] = c;
            return c;
        }

        // ---------------------------------------------------------------- síntese

        static AudioClip Sintetizar(string nome)
        {
            switch (nome)
            {
                case "pistola": return Tiro(0.22f, 900f, 0.55f, 0.0008f);
                case "espingarda": return Tiro(0.45f, 420f, 1.00f, 0.0022f);
                case "rajada": return Tiro(0.14f, 1400f, 0.45f, 0.0006f);
                case "rojao_disparo": return Tiro(0.40f, 300f, 0.80f, 0.0030f);
                case "explosao": return Explosao();
                case "impacto": return Impacto();
                case "carne": return Carne();
                case "dano_player": return DanoPlayer();
                case "coleta": return Blip(660f, 1320f, 0.14f, 0.35f);
                case "arma_troca": return Blip(320f, 520f, 0.09f, 0.22f);
                case "chave": return Blip(520f, 1040f, 0.35f, 0.35f);
                case "porta": return Porta();
                case "execucao": return Execucao();
                case "alerta": return Alerta();
                case "arena_inicio": return Grave(55f, 1.2f, 0.5f);
                case "arena_fim": return Blip(220f, 880f, 0.7f, 0.4f);
                case "morte_player": return Grave(70f, 1.6f, 0.6f);
                case "vitoria": return Vitoria();
                case "vazio": return Blip(180f, 120f, 0.07f, 0.2f);
                default: return Blip(440f, 440f, 0.1f, 0.2f);
            }
        }

        static float Ruido() => (float)(_rng.NextDouble() * 2.0 - 1.0);

        static AudioClip Criar(string nome, float[] dados)
        {
            var c = AudioClip.Create(nome, dados.Length, 1, SR, false);
            c.SetData(dados, 0);
            return c;
        }

        /// <summary>Estampido: ruído filtrado + "thump" grave.</summary>
        static AudioClip Tiro(float dur, float corte, float corpo, float decayThump)
        {
            int n = Mathf.RoundToInt(SR * dur);
            var d = new float[n];
            float lp = 0f;
            float k = Mathf.Clamp01(corte / (SR * 0.5f));
            float faseGrave = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Exp(-t * 14f);
                float atk = Mathf.Min(1f, i / (SR * 0.0012f));

                lp += k * (Ruido() - lp);
                float s = lp * env * atk;

                // thump grave que desce
                float f = Mathf.Lerp(150f, 45f, Mathf.Clamp01(t / 0.25f));
                faseGrave += f / SR * Mathf.PI * 2f;
                s += Mathf.Sin(faseGrave) * Mathf.Exp(-t / Mathf.Max(0.02f, decayThump * 120f)) * corpo * 0.7f;

                d[i] = Mathf.Clamp(s * 0.9f, -1f, 1f);
            }
            return Criar("tiro", d);
        }

        static AudioClip Explosao()
        {
            float dur = 1.15f;
            int n = Mathf.RoundToInt(SR * dur);
            var d = new float[n];
            float lp = 0f, lp2 = 0f, fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float env = Mathf.Exp(-t * 5.5f);
                lp += 0.06f * (Ruido() - lp);
                lp2 += 0.35f * (Ruido() - lp2);
                float f = Mathf.Lerp(90f, 28f, t);
                fase += f / SR * Mathf.PI * 2f;
                float s = lp * 2.2f + lp2 * 0.35f * Mathf.Exp(-t * 18f) + Mathf.Sin(fase) * 0.8f;
                d[i] = Mathf.Clamp(s * env, -1f, 1f);
            }
            return Criar("explosao", d);
        }

        static AudioClip Impacto()
        {
            int n = Mathf.RoundToInt(SR * 0.10f);
            var d = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                lp += 0.5f * (Ruido() - lp);
                d[i] = lp * Mathf.Exp(-t * 32f) * 0.6f;
            }
            return Criar("impacto", d);
        }

        static AudioClip Carne()
        {
            int n = Mathf.RoundToInt(SR * 0.16f);
            var d = new float[n];
            float lp = 0f, fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                lp += 0.09f * (Ruido() - lp);
                fase += Mathf.Lerp(190f, 70f, t) / SR * Mathf.PI * 2f;
                d[i] = (lp * 1.6f + Mathf.Sin(fase) * 0.4f) * Mathf.Exp(-t * 16f);
            }
            return Criar("carne", d);
        }

        static AudioClip DanoPlayer()
        {
            int n = Mathf.RoundToInt(SR * 0.3f);
            var d = new float[n];
            float lp = 0f, fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                lp += 0.12f * (Ruido() - lp);
                fase += Mathf.Lerp(120f, 60f, t) / SR * Mathf.PI * 2f;
                d[i] = (lp * 0.9f + Mathf.Sin(fase) * 0.5f) * Mathf.Exp(-t * 7f);
            }
            return Criar("dano_player", d);
        }

        static AudioClip Blip(float f0, float f1, float dur, float vol)
        {
            int n = Mathf.RoundToInt(SR * dur);
            var d = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                fase += Mathf.Lerp(f0, f1, t) / SR * Mathf.PI * 2f;
                float onda = Mathf.Sin(fase) * 0.6f + Mathf.Sign(Mathf.Sin(fase)) * 0.25f;
                d[i] = onda * Mathf.Exp(-t * 5f) * vol;
            }
            return Criar("blip", d);
        }

        static AudioClip Grave(float f, float dur, float vol)
        {
            int n = Mathf.RoundToInt(SR * dur);
            var d = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                fase += Mathf.Lerp(f, f * 0.55f, t) / SR * Mathf.PI * 2f;
                d[i] = (Mathf.Sin(fase) + Mathf.Sin(fase * 1.5f) * 0.3f) * Mathf.Exp(-t * 2.4f) * vol;
            }
            return Criar("grave", d);
        }

        static AudioClip Porta()
        {
            int n = Mathf.RoundToInt(SR * 1.0f);
            var d = new float[n];
            float lp = 0f, fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                lp += 0.02f * (Ruido() - lp);
                fase += 42f / SR * Mathf.PI * 2f;
                float env = Mathf.Sin(t * Mathf.PI);
                d[i] = (lp * 3f + Mathf.Sin(fase) * 0.4f) * env * 0.5f;
            }
            return Criar("porta", d);
        }

        static AudioClip Execucao()
        {
            int n = Mathf.RoundToInt(SR * 0.55f);
            var d = new float[n];
            float lp = 0f, fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                lp += 0.25f * (Ruido() - lp);
                fase += Mathf.Lerp(260f, 55f, t) / SR * Mathf.PI * 2f;
                float crunch = lp * Mathf.Exp(-t * 6f) * 1.4f;
                d[i] = Mathf.Clamp(crunch + Mathf.Sin(fase) * Mathf.Exp(-t * 4f) * 0.7f, -1f, 1f);
            }
            return Criar("execucao", d);
        }

        static AudioClip Alerta()
        {
            int n = Mathf.RoundToInt(SR * 0.35f);
            var d = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float f = 300f + Mathf.Sin(t * 40f) * 90f;
                fase += f / SR * Mathf.PI * 2f;
                d[i] = Mathf.Sign(Mathf.Sin(fase)) * Mathf.Exp(-t * 6f) * 0.25f;
            }
            return Criar("alerta", d);
        }

        static AudioClip Vitoria()
        {
            float[] notas = { 261.6f, 392f, 523.2f, 784f };
            int n = Mathf.RoundToInt(SR * 1.6f);
            var d = new float[n];
            for (int k = 0; k < notas.Length; k++)
            {
                int ini = Mathf.RoundToInt(SR * 0.16f * k);
                float fase = 0f;
                for (int i = ini; i < n; i++)
                {
                    float t = (float)(i - ini) / SR;
                    fase += notas[k] / SR * Mathf.PI * 2f;
                    d[i] += Mathf.Sin(fase) * Mathf.Exp(-t * 2.2f) * 0.18f;
                }
            }
            for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(d[i], -1f, 1f);
            return Criar("vitoria", d);
        }
    }
}
