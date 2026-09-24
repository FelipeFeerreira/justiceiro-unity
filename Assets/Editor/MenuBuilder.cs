using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Justiceiro.EditorTools
{
    /// <summary>
    /// Constrói a cena de menu (Assets/Cenas/Menu.unity) e garante o build
    /// com Menu no índice 0 e Favela no 1.
    /// Menu: Justiceiro > Reconstruir Menu.
    /// </summary>
    public static class MenuBuilder
    {
        public const string CaminhoMenu = "Assets/Cenas/Menu.unity";

        [MenuItem("Justiceiro/Reconstruir Menu")]
        public static void ReconstruirMenu()
        {
            ConstruirMenu();
        }

        public static void ConstruirMenu()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Cenas"))
                AssetDatabase.CreateFolder("Assets", "Cenas");

            var cena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // câmera só para cor de fundo (IMGUI desenha a interface)
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.035f, 0.05f, 1f);
            cam.fieldOfView = 60f;

            var luz = new GameObject("Luz");
            luz.AddComponent<Light>().type = LightType.Directional;

            var menu = new GameObject("MenuInicial");
            menu.AddComponent<MenuInicial>();

            EditorSceneManager.MarkSceneDirty(cena);
            EditorSceneManager.SaveScene(cena, CaminhoMenu);

            LevelBuilder.InstalarBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<b>JUSTICEIRO</b> — menu construído em " + CaminhoMenu + ".");
        }

        [MenuItem("Justiceiro/Abrir Menu")]
        public static void AbrirMenu()
        {
            if (System.IO.File.Exists(CaminhoMenu))
                EditorSceneManager.OpenScene(CaminhoMenu, OpenSceneMode.Single);
            else
                ConstruirMenu();
        }
    }
}