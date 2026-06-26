using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry point of the game, loads Bootstrap scene alongisde the current scene
/// </summary>
public static class Bootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (SceneManager.GetSceneByName("Bootstrap").isLoaded)
        {
            return;
        }

        // Once managers are set up, BootstrapLoader loads the first
        // scene using Single mode, cleaning up Bootstrap scene
        SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);
    }
}