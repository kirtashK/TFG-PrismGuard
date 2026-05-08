using UnityEngine;

[CreateAssetMenu(fileName = "LoadGameAction", menuName = "Data/UI Actions/Load Game")]
public class LoadGameAction : UIAction
{
    public override void Execute()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(LoadGameAction)}: {nameof(GameManager)} not found");
            return;
        }

        GameManager.Instance.LoadGame();
    }
}