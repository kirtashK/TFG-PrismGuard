using UnityEngine;
using System.Collections;

public class IdleState : IWorkerState
{
    private float searchRetryDelay = 1f;
    private bool isSearching = false;

    public void EnterState(Worker worker)
    {
        //Debug.Log(worker.name + " - Entrando en estado IDLE");
    }

    public void UpdateState(Worker worker)
    {
        if (isSearching || worker.CurrentTask != null)
        {
            return;
        }

        ITask task = TaskManager.Instance.RequestTask(worker.transform.position);

        if (task != null)
        {
            worker.CurrentTask = task;
            worker.ChangeState(new MovingState());
        }
        else
        {
            isSearching = true;
            worker.StartCoroutine(RetrySearch(worker));
        }
    }

    public void ExitState(Worker worker)
    {
        isSearching = false;
    }

    private IEnumerator RetrySearch(Worker worker)
    {
        yield return new WaitForSeconds(searchRetryDelay);
        isSearching = false;
    }
}
