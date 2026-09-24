using System;
using UnityEngine;

namespace Justiceiro
{
    /// <summary>Componente genérico de vida.</summary>
    public class Vida : MonoBehaviour
    {
        public float maximo = 100f;
        public float atual = -1f;
        public bool invulneravel;

        public event Action<float, Vector3> aoLevarDano;
        public event Action aoMorrer;

        public bool Morto { get; private set; }
        public float Normalizado => maximo <= 0f ? 0f : Mathf.Clamp01(atual / maximo);

        void Awake()
        {
            if (atual < 0f) atual = maximo;
        }

        public void Dano(float qtd, Vector3 ponto)
        {
            if (Morto || invulneravel || qtd <= 0f) return;
            atual -= qtd;
            aoLevarDano?.Invoke(qtd, ponto);
            if (atual <= 0f)
            {
                atual = 0f;
                Morto = true;
                aoMorrer?.Invoke();
            }
        }

        public void Curar(float qtd)
        {
            if (Morto) return;
            atual = Mathf.Min(maximo, atual + qtd);
        }

        public void Definir(float novoMax)
        {
            maximo = novoMax;
            atual = novoMax;
            Morto = false;
        }
    }
}
