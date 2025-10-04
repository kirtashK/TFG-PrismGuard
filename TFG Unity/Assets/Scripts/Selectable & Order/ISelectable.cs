using UnityEngine;

public interface ISelectable
{
    string GetDisplayName();

    Sprite GetIcon();

    Transform GetTransform();

    void OnSelected();

    void OnDeselected();

    object GetSelectionData();
}