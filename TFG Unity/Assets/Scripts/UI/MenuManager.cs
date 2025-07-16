using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
       public void NewGame()
    {
        Debug.Log("NewGame() todavía no implementado");
        //SceneManager.LoadScene("GameScene");
    }

    public void ContinueLastGame()
    {
        // TODO Hacer que cargue la última partida, no SampleScene siempre
        SceneManager.LoadScene("SampleScene");
    }

    public void LoadGame()
    {
        Debug.Log("LoadGame() todavía no implementado");
    }

    public void OpenSettings()
    {
        Debug.Log("OpenSettings() todavía no implementado");
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}
