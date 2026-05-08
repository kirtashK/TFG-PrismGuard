using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Once managers have finished setup in Awake, 
/// this class loads the proper scene in Start
/// </summary>
public class BootstrapLoader : MonoBehaviour
{
    [SerializeField, Tooltip("Scenes listed here are not redirected away when loading Bootstrap")]
    private string[] scenesToSkipRedirect = { "SampleScene" };

    [SerializeField, Tooltip("Name of the scene to load if launching the game from any scene that can be redirected")]
    private string initialScene = "MainMenu";

    [SerializeField, Tooltip("Name of the bootstrap scene used to initialize managers, unloaded once completed")]
    private string bootstrapScene = "BootStrap";

    private void Start()
    {
        string activeScene = SceneManager.GetActiveScene().name;

        foreach (string scene in scenesToSkipRedirect)
        {
            if (activeScene == scene)
            {
                SceneManager.UnloadSceneAsync(bootstrapScene);
                return;
            }
        }

        SceneManager.LoadScene(initialScene);
    }
}