using UnityEngine;

[CreateAssetMenu(fileName = "SaveGameAction", menuName = "Data/UI Actions/Save Game")]
public class SaveGameAction : UIAction
{
    public override void Execute()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(SaveGameAction)}: {nameof(GameManager)} not found");
            return;
        }

        GameManager.Instance.SaveGame();
    }
}