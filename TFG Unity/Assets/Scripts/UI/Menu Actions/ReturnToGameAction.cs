using UnityEngine;

/// <summary>
/// Resumes the game from a paused state
/// </summary>
[CreateAssetMenu(fileName = "ReturnToGameAction", menuName = "Data/UI Actions/Return To Game")]
public class ReturnToGameAction : UIAction
{
    public override void Execute()
    {
        if (HideElementManager.Instance != null)
        {
            HideElementManager.Instance.HideAll();
        }
    }
}