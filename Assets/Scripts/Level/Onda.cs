using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    /// <summary>Uma leva de inimigos dentro de uma arena.</summary>
    [System.Serializable]
    public class Onda
    {
        public List<PontoSpawn> pontos = new List<PontoSpawn>();
        public float atrasoEntreSpawns = 0.25f;
        /// <summary>Próxima onda entra quando sobram só N inimigos (0 = limpar tudo).</summary>
        public int restantesParaProxima = 0;
    }
}
