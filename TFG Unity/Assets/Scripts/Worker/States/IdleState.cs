using UnityEngine;
using System.Collections;

public class IdleState : IWorkerState
{
    private readonly float searchRetryDelay = 1f;
    private bool isSearching = false;

    public void EnterState(Worker worker)
    {
        //Debug.Log(worker.name + " - Entrando en estado IDLE");
    }

    public void UpdateState(Worker worker)
    {
        if (isSearching || worker.currentTask != null)
        {
            return;
        }

        ITask task = TaskManager.Instance.RequestTask(worker.transform.position);

        if (task != null)
        {
            worker.currentTask = task;
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
