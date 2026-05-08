using UnityEngine;

[CreateAssetMenu(fileName = "GoToMainMenuAction", menuName = "Data/UI Actions/Go To Main Menu")]
public class GoToMainMenuAction : UIAction
{
    public override void Execute()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(GoToMainMenuAction)}: {nameof(GameManager)} not found");
            return;
        }

        GameManager.Instance.GoToMainMenu();
    }
}