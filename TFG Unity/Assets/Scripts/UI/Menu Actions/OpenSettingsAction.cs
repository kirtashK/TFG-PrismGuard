using UnityEngine;

[CreateAssetMenu(fileName = "OpenSettingsAction", menuName = "Data/UI Actions/Open Settings")]
public class OpenSettingsAction : UIAction
{
    public override void Execute()
    {
        // TODO Call whatever manages settings when implemented
        Debug.LogWarning($"{nameof(OpenSettingsAction)}: Settings UI not yet implemented");
    }
}