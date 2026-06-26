using UnityEngine;

[CreateAssetMenu(fileName = "NewItemCategory", menuName = "Data/ItemCategory")]
public class ItemCategory : BaseData
{
    [Tooltip("Parent category (optional) Used to implement a hierarchy")]
    public ItemCategory parentCategory;

    /// <summary>
    /// Returns true if this category is equal to or an ancestor of `other`
    /// Example: if this = Resources and other = Resources/Plants => true.
    /// </summary>
    public bool Matches(ItemCategory other)
    {
        if (other == null || string.IsNullOrEmpty(id))
        {
            return false;
        }

        string targetId = id;

        ItemCategory current = other;
        while (current != null)
        {
            if (current.id == targetId)
            {
                return true;
            }
            current = current.parentCategory;
        }
        return false;
    }
}