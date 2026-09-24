using System;
using UnityEngine;

namespace Justiceiro
{
    /// <summary>Camadas fixas do projeto (ver ProjectSettings/TagManager.asset).</summary>
    public static class Layers
    {
        public const int Default = 0;
        public const int PlayerLayer = 8;
        public const int EnemyLayer = 9;

        public static readonly int World = 1 << Default;
        public static readonly int Player = 1 << PlayerLayer;
        public static readonly int Enemy = 1 << EnemyLayer;

        /// <summary>O que o tiro do jogador pode acertar.</summary>
        public static readonly int PlayerShoot = (1 << Default) | (1 << EnemyLayer);
        /// <summary>O que o tiro do inimigo pode acertar.</summary>
        public static readonly int EnemyShoot = (1 << Default) | (1 << PlayerLayer);
        /// <summary>Tudo que é "corpo vivo".</summary>
        public static readonly int Corpos = (1 << PlayerLayer) | (1 << EnemyLayer);
    }

    public enum Faccao { Escorpiao, ZonaCinza }

    public enum TipoInimigo { Moleque, Soldado, Fogueteiro, Blindado, Chefe }

    public enum TipoMunicao { Infinita = 0, Cartucho = 1, Bala = 2, Rojao = 3 }

    /// <summary>Qualquer coisa que leva dano no jogo.</summary>
    public interface IAlvo
    {
        void ReceberDano(float dano, Vector3 ponto, Vector3 direcao, bool pesado);
        bool Morto { get; }
        Transform Alvo { get; }
    }

    public static class Util
    {
        public static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v;
        }

        public static float DistanciaPlana(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>Direção com desvio aleatório em graus.</summary>
        public static Vector3 ComDispersao(Vector3 dir, float graus)
        {
            if (graus <= 0f) return dir.normalized;
            Quaternion rot = Quaternion.Euler(
                UnityEngine.Random.Range(-graus, graus),
                UnityEngine.Random.Range(-graus, graus),
                0f);
            return (rot * dir).normalized;
        }

        public static Material MaterialSimples(Color cor, float metal = 0f, float liso = 0.25f, Color emissao = default)
        {
            Shader sh = Shader.Find("Standard");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Diffuse");
            var m = new Material(sh);
            m.color = cor;
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metal);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", liso);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", liso);
            if (emissao != default && emissao.maxColorComponent > 0.01f)
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", emissao);
            }
            return m;
        }

        public static Material MaterialTransparente(Color cor)
        {
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh);
            m.color = cor;
            return m;
        }
    }
}
