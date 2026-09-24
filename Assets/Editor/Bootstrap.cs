using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Justiceiro.EditorTools
{
    /// <summary>
    /// Na primeira vez que o projeto abre, constrói e abre a fase sozinho.
    /// Depois disso não mexe mais em nada — use o menu Justiceiro > Reconstruir Fase.
    /// </summary>
    [InitializeOnLoad]
    public static class Bootstrap
    {
        const string ChaveFeito = "Justiceiro.PrimeiraConstrucao.v1";

        static Bootstrap()
        {
            EditorApplication.delayCall += Rodar;
        }

        static void Rodar()
        {
            EditorApplication.delayCall -= Rodar;

            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Rodar;
                return;
            }

            bool jaFez = SessionState.GetBool(ChaveFeito, false);
            if (jaFez) return;
            SessionState.SetBool(ChaveFeito, true);

            if (!LevelBuilder.CenaExiste())
            {
                Debug.Log("<b>JUSTICEIRO</b> — primeira abertura: montando o morro...");
                LevelBuilder.Construir(false);
                EditorUtility.DisplayDialog(
                    "JUSTICEIRO — protótipo pronto",
                    "A fase foi gerada e já está aberta.\n\n" +
                    "Aperte PLAY para jogar.\n\n" +
                    "WASD mover · SHIFT correr · ESPAÇO pulo duplo\n" +
                    "CTRL ESQ dash · 1-4 armas\n" +
                    "BOTÃO ESQ atirar · BOTÃO DIR coronhada\n" +
                    "E executar inimigo atordoado (recupera vida)\n" +
                    "ESC pausa · R reiniciar\n\n" +
                    "Para regenerar a fase: menu Justiceiro > Reconstruir Fase.",
                    "Bora");
            }
            else if (string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path))
            {
                EditorSceneManager.OpenScene(LevelBuilder.CaminhoCena, OpenSceneMode.Single);
            }
        }
    }
}
