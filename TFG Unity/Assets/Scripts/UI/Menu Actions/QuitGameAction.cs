using UnityEngine;

[CreateAssetMenu(fileName = "QuitGameAction", menuName = "Data/UI Actions/Quit Game")]
public class QuitGameAction : UIAction
{
    public override void Execute()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}