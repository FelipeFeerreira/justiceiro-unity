using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Justiceiro
{
    public class SaidaFase : MonoBehaviour
    {
        public bool exigeArenaLimpa = true;
        public ArenaEncounter arenaFinal;

        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != Layers.PlayerLayer) return;
            var gm = GameManager.I;
            if (gm == null) return;

            if (exigeArenaLimpa && arenaFinal != null && !arenaFinal.concluida)
            {
                gm.Mensagem("AINDA TEM GENTE ARMADA AQUI EM CIMA.", 2.5f);
                return;
            }
            gm.Vencer();
        }
    }
}
