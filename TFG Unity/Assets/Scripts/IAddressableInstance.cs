using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public interface IAddressableInstance
{
    void SetAddressableInstanceHandle(AsyncOperationHandle<GameObject> handle);
}