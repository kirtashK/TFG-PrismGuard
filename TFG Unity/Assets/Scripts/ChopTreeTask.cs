using UnityEngine;

public class ChopTreeTask : Task
{
    [SerializeField]
    private float workDuration = 3f;

    private System.Action onCompleteCallback;

    public void Execute(System.Action onComplete)
    {
        Debug.Log(name + " - Talando árbol...");
        onCompleteCallback = onComplete;
        Invoke(nameof(FinishWork), workDuration);
    }

    private void FinishWork()
    {
        Debug.Log(name + " - Árbol talado.");
        onCompleteCallback?.Invoke();
        Destroy(gameObject);
    }
}
