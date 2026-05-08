using UnityEngine;

[CreateAssetMenu(fileName = "NewGameAction", menuName = "Data/UI Actions/New Game")]
public class NewGameAction : UIAction
{
    public override void Execute()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(NewGameAction)}: {nameof(GameManager)} not found");
            return;
        }

        GameManager.Instance.NewGame();
    }
}