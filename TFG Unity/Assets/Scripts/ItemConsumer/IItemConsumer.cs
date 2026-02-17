using UnityEngine;

public interface IItemConsumer
{
    bool CanReceive(ItemData data);

    // Reserves a spot for the item, returns true if succesful
    bool Reserve(ItemData data);

    // Releases the spot for the item
    void Release(ItemData data);

    // Position to deliver the item
    Vector3 GetReceivePosition();

    // Action to execute when the item is received
    void OnReceived(GameObject item, ItemData data);

    // For IItemConsumers that can store items that can later be taken, 
    // this reduces the current capacity
    void ConfirmRetrieval(ItemData data);
}