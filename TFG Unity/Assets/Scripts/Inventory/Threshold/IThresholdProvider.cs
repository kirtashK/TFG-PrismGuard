using System.Collections.Generic;

public interface IThresholdProvider
{
    /// <summary>
    /// Returns all output items
    /// </summary>
    List<ItemData> GetOutputs();

    /// <summary>
    /// Gets threshold for an output item
    /// </summary>
    int GetThreshold(ItemData itemData);

    /// <summary>
    /// Sets threshold for an output item
    /// </summary>
    void SetThreshold(ItemData itemData, int value);
}