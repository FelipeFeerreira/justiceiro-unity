using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    /// <summary>Efeitos visuais gerados em runtime — sem prefabs, sem assets.</summary>
    public static class Fx
    {
        static Material _matTracer, _matFaisca, _matSangue, _matFogo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Resetar()
        {
            _matTracer = _matFaisca = _matSangue = _matFogo = null;
        }

        static Material Mat(ref Material campo, Color cor)
        {
            if (campo == null)
            {
                Shader sh = Shader.Find("Sprites/Default");
                if (sh == null) sh = Shader.Find("Unlit/Color");
                if (sh == null) sh = Shader.Find("Standard");
                campo = new Material(sh) { color = cor };
            }
            return campo;
        }

        public static void Tracer(Vector3 a, Vector3 b, Color cor, float largura = 0.045f, float dur = 0.05f)
        {
            var go = new GameObject("~tracer");
            var lr = go.AddComponent<LineRenderer>();
            lr.material = Mat(ref _matTracer, Color.white);
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = largura;
            lr.endWidth = largura * 0.35f;
            lr.startColor = cor;
            lr.endColor = new Color(cor.r, cor.g, cor.b, 0f);
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            go.AddComponent<AutoDestruir>().tempo = dur;
        }

        public static void Impacto(Vector3 pos, Vector3 normal, bool sangue)
        {
            Color cor = sangue ? new Color(0.75f, 0.05f, 0.05f) : new Color(1f, 0.82f, 0.35f);
            var mat = sangue ? Mat(ref _matSangue, Color.white) : Mat(ref _matFaisca, Color.white);

            int qtd = sangue ? 7 : 5;
            for (int i = 0; i < qtd; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var col = g.GetComponent<Collider>();
                if (col != null) Object.Destroy(col);
                g.name = "~fagulha";
                g.transform.position = pos + normal * 0.05f;
                g.transform.localScale = Vector3.one * Random.Range(0.04f, 0.10f);
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.material.color = cor;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var mv = g.AddComponent<Estilhaco>();
                mv.vel = (normal + Random.insideUnitSphere * (sangue ? 1.1f : 0.8f)).normalized
                         * Random.Range(2.5f, 7f);
                mv.gravidade = sangue ? -14f : -9f;
                g.AddComponent<AutoDestruir>().tempo = Random.Range(0.25f, 0.55f);
            }

            if (!sangue) Luz(pos + normal * 0.2f, new Color(1f, 0.8f, 0.4f), 3f, 2.5f, 0.06f);
        }

        public static void Explosao(Vector3 pos, float raio)
        {
            Luz(pos, new Color(1f, 0.55f, 0.15f), 40f, raio * 3f, 0.35f);

            var esfera = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var c = esfera.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            esfera.name = "~bola";
            esfera.transform.position = pos;
            esfera.transform.localScale = Vector3.one * 0.4f;
            var rd = esfera.GetComponent<Renderer>();
            rd.sharedMaterial = Mat(ref _matFogo, Color.white);
            rd.material.color = new Color(1f, 0.6f, 0.2f, 0.85f);
            rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var exp = esfera.AddComponent<BolaExpansao>();
            exp.alvo = raio * 1.6f;
            exp.dur = 0.3f;

            for (int i = 0; i < 16; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var cc = g.GetComponent<Collider>();
                if (cc != null) Object.Destroy(cc);
                g.name = "~detrito";
                g.transform.position = pos;
                g.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = Mat(ref _matFogo, Color.white);
                r.material.color = Color.Lerp(new Color(1f, 0.75f, 0.2f), new Color(0.3f, 0.3f, 0.3f), Random.value);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var mv = g.AddComponent<Estilhaco>();
                mv.vel = Random.onUnitSphere * Random.Range(6f, 16f);
                mv.gravidade = -16f;
                g.AddComponent<AutoDestruir>().tempo = Random.Range(0.6f, 1.2f);
            }

            Sfx.TocarEm("explosao", pos, 1f, Random.Range(0.92f, 1.08f), 90f);
        }

        public static void Luz(Vector3 pos, Color cor, float intensidade, float alcance, float dur)
        {
            var go = new GameObject("~flash");
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = cor;
            l.intensity = intensidade;
            l.range = alcance;
            l.shadows = LightShadows.None;
            var f = go.AddComponent<FlashLuz>();
            f.dur = dur;
            f.i0 = intensidade;
        }

        public static void JorroExecucao(Vector3 pos)
        {
            for (int i = 0; i < 26; i++)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var cc = g.GetComponent<Collider>();
                if (cc != null) Object.Destroy(cc);
                g.name = "~jorro";
                g.transform.position = pos + Random.insideUnitSphere * 0.3f;
                g.transform.localScale = Vector3.one * Random.Range(0.06f, 0.16f);
                var r = g.GetComponent<Renderer>();
                r.sharedMaterial = Mat(ref _matSangue, Color.white);
                r.material.color = new Color(0.62f, 0.03f, 0.05f);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var mv = g.AddComponent<Estilhaco>();
                mv.vel = (Vector3.up * 1.3f + Random.insideUnitSphere).normalized * Random.Range(4f, 11f);
                mv.gravidade = -18f;
                g.AddComponent<AutoDestruir>().tempo = Random.Range(0.5f, 1.0f);
            }
        }
    }
}
