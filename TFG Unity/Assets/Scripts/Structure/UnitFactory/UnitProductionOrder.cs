using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single production order for a unit
/// </summary>
public class UnitProductionOrder : IItemConsumer
{
    public enum OrderState
    {
        WaitingForItems,    // Waiting for inputs
        Ready,              // All items received, waiting for free concurrent slot
        Building,           // Using concurrent slot to create unit
        Completed,          // Order completed succesfully
        Cancelled           // Order cancelled
    }

    public readonly Guid orderId;
    public UnitData unitData;
    public int enqueueIndex;
    public OrderState State { get; private set; } = OrderState.WaitingForItems;

    private readonly UnitFactory ownerFactory;

    private readonly Dictionary<string, int> requiredCounts = new();
    private readonly Dictionary<string, int> reservedCounts = new();
    private readonly Dictionary<string, ItemData> requiredItemsById = new();
    private readonly List<GameObject> receivedItems = new();

    public Guid scoreReservationToken = Guid.Empty;

    public event Action<UnitProductionOrder> OnStateChanged;
    public event Action<UnitProductionOrder> OnCompleted;
    public event Action<UnitProductionOrder> OnCancelled;

    private bool isQueuedForBuild = false;

    public UnitProductionOrder(UnitData unitData, UnitFactory owner, int enqueueIndex, Guid scoreToken)
    {
        this.orderId = Guid.NewGuid();
        this.unitData = unitData;
        this.ownerFactory = owner;
        this.enqueueIndex = enqueueIndex;
        this.scoreReservationToken = scoreToken;

        // initialize requiredCounts from unitData.createCosts
        foreach (UnitData.ResourceRequirement required in unitData.createCosts)
        {
            if (required.itemData == null || string.IsNullOrEmpty(required.itemData.id))
            {
                continue;
            }

            string itemId = required.itemData.id;

            requiredCounts[itemId] = required.quantity;
            reservedCounts[itemId] = 0;
            requiredItemsById[itemId] = required.itemData;
        }
    }

    public int GetRemainingFor(ItemData data)
    {
        if (data == null || !requiredCounts.TryGetValue(data.id, out int required))
        {
            return 0;
        }

        int received = CountReceived(data);
        return Mathf.Max(0, required - received);
    }

    private int CountReceived(ItemData data)
    {
        if (data == null)
        {
            return 0;
        }

        string itemId = data.id;
        
        int count = 0;
        foreach (GameObject item in receivedItems)
        {
            if (item == null)
            {
                continue;
            }

            ItemInstance itemInstance = item.GetComponent<ItemInstance>();
            if (itemInstance != null && itemInstance.itemData != null && itemInstance.itemData.id == itemId)
            {
                count++;
            }
        }
        return count;
    }

    private int ReservedFor(ItemData data)
    {
        if (data != null && reservedCounts.TryGetValue(data.id, out int value))
        {
            return value;
        }

        return 0;
    }

    private bool AllRequirementsMet()
    {
        foreach (KeyValuePair<string, int> keyValue in requiredCounts)
        {
            if (CountReceived(requiredItemsById[keyValue.Key]) < keyValue.Value)
            {
                return false;
            }
        }
        return true;
    }

    public bool CanReceive(ItemData data)
    {
        if (State != OrderState.WaitingForItems && State != OrderState.Ready)
        {
            return false;
        }
        if (data == null || !requiredCounts.ContainsKey(data.id))
        {
            return false;
        }
        int remaining = GetRemainingFor(data);

        return remaining > 0;
    }

    public bool Reserve(ItemData data)
    {
        if (!CanReceive(data))
        {
            return false;
        }

        int remaining = GetRemainingFor(data);
        int alreadyReserved = ReservedFor(data);

        if (alreadyReserved >= remaining)
        {
            return false;
        }

        reservedCounts[data.id] = alreadyReserved + 1;

        return true;
    }

    public void Release(ItemData data)
    {
        if (data == null || !reservedCounts.ContainsKey(data.id))
        {
            return;
        }
        reservedCounts[data.id] = Mathf.Max(0, reservedCounts[data.id] - 1);
    }

    public Vector3 GetReceivePosition()
    {
        return ownerFactory != null ? ownerFactory.GetReceivePosition() : Vector3.zero;
    }

    public void OnReceived(GameObject item, ItemData data)
    {
        Release(data);

        // Disable visuals/colliders
        if (item.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
        {
            itemInstance.SetVisible(false);
            itemInstance.carrier = null;
        }

        // Change position and parent
        if (ownerFactory != null)
        {
            item.transform.SetParent(ownerFactory.GetOrderStorageParent(), worldPositionStays: false);
            item.transform.position = ownerFactory.GetOrderStoragePosition();
        }

        receivedItems.Add(item);

        // If all items received, change state to ready and notify
        if (AllRequirementsMet() && SetState(OrderState.Ready))
        {
            if (!isQueuedForBuild)
            {
                isQueuedForBuild = true;
                ownerFactory.NotifyOrderReady(this);
            }
        }
    }

    public void ConfirmRetrieval(GameObject item)
    {
        // Does nothing as UnitProductionOrder doesnt
        // store items to be picked up
    }

    private bool SetState(OrderState newState)
    {
        if (State == newState)
        {
            return false;
        }

        State = newState;
        OnStateChanged?.Invoke(this);
        return true;
    }

    public void StartBuilding()
    {
        if (State != OrderState.Ready)
        {
            return;
        }

        isQueuedForBuild = false;
        SetState(OrderState.Building);
    }

    public void Complete()
    {
        isQueuedForBuild = false;
        SetState(OrderState.Completed);
        OnCompleted?.Invoke(this);
    }

    public void Cancel(bool returnItems)
    {
        if (State == OrderState.Completed || State == OrderState.Cancelled)
        {
            return;
        }

        isQueuedForBuild = false;

        // Release score reservation
        if (scoreReservationToken != Guid.Empty)
        {
            ScoreManager.Instance.ReleaseReservation(scoreReservationToken);
            scoreReservationToken = Guid.Empty;
        }

        if (returnItems)
        {
            foreach (GameObject item in receivedItems)
            {
                if (item == null)
                {
                    continue;
                }
                if (item.TryGetComponent<ItemInstance>(out ItemInstance itemInstance))
                {
                    itemInstance.SetVisible(true);
                }
                item.transform.SetParent(null, worldPositionStays: true);
                item.transform.position = ownerFactory != null ? ownerFactory.GetOrderStoragePosition() : item.transform.position;
            }
        }
        else
        {
            ConsumeStoredItems();
        }
        receivedItems.Clear();

        SetState(OrderState.Cancelled);
        OnCancelled?.Invoke(this);
    }

    public void ConsumeStoredItems()
    {
        foreach (GameObject item in receivedItems)
        {
            if (item != null)
            {
                GameObject.Destroy(item);
            }
        }
        receivedItems.Clear();
    }
}