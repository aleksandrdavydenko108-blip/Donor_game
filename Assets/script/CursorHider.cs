using UnityEngine;
using UnityEngine.SceneManagement;

// Прячет и блокирует курсор во всех сценах, кроме главного меню.
// Создаётся автоматически при запуске игры, на сцену добавлять не нужно.
public class CursorHider : MonoBehaviour
{
    private const string MenuSceneName = "SampleScene";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var go = new GameObject(nameof(CursorHider));
        DontDestroyOnLoad(go);
        go.AddComponent<CursorHider>();
    }

    private void LateUpdate()
    {
        bool inMenu = SceneManager.GetActiveScene().name == MenuSceneName;

        Cursor.visible = inMenu;
        Cursor.lockState = inMenu ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
