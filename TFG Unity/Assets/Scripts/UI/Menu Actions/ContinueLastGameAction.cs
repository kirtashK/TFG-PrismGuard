using UnityEngine;

/// <summary>
/// Continues the most recent save
/// </summary>
[CreateAssetMenu(fileName = "ContinueLastGameAction", menuName = "Data/UI Actions/Continue Last Game")]
public class ContinueLastGameAction : UIAction
{
    public override void Execute()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(ContinueLastGameAction)}: {nameof(GameManager)} not found");
            return;
        }

        GameManager.Instance.ContinueLastGame();
    }
}