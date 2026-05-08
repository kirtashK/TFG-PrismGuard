using System.Collections;
using UnityEngine;

public class PauseUIManager : BasePanel
{
    private bool isListeningForPauseToggle = false;
    private bool wasInitiallyPaused = false;

    #region Unity methods

    protected override void OnDisable()
    {
        base.OnDisable();

        if (isListeningForPauseToggle && InputManager.Instance != null)
        {
            InputManager.Instance.OnPauseToggleRequested -= TogglePanel;
            isListeningForPauseToggle = false;
        }
    }

    #endregion

    #region BasePanel hooks

    protected override void OnPanelReady()
    {
        if (!isListeningForPauseToggle)
        {
            InputManager.Instance.OnPauseToggleRequested += TogglePanel;
            isListeningForPauseToggle = true;
        }
    }

    protected override void OnPanelShown()
    {
        if (GameManager.Instance != null)
        {
            wasInitiallyPaused = GameSpeedManager.Instance.CurrentSpeed == 0f;
            GameManager.Instance.PauseGame();
        }
    }

    protected override void OnPanelHidden()
    {
        if (!wasInitiallyPaused && GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    #endregion

    #region Private methods

    public override void TogglePanel()
    {
        if (!IsVisible && GameManager.Instance != null 
            && GameManager.Instance.IsDefeated)
        {
            return;
        }

        base.TogglePanel();
    }

    #endregion
}