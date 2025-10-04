using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OrdersController : MonoBehaviour
{
    public LayerMask groundLayerMask = 1 << 0;

    private void Update()
    {
        // Move order with right click
        if (Input.GetMouseButtonDown(1))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
            {
                Vector3 targetPosition = hit.point;

                IReadOnlyList<ISelectable> selection = SelectionManager.Instance.CurrentSelection;
                if (selection == null || selection.Count == 0)
                {
                    return;
                }

                List<IOrderable> selectionObjects = new();
                foreach (ISelectable selectable in selection)
                {
                    if (selectable is Component comp)
                    {
                        if (comp.TryGetComponent<IOrderable>(out IOrderable orderableComponent))
                        {
                            selectionObjects.Add(orderableComponent);
                        }
                    }
                }

                if (selectionObjects.Count == 0)
                {
                    return;
                }

                // Default options
                MoveOrderOptions options = MoveOrderOptions.Default;

                // If Shift is held: set return to guard false
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    options.returnToGuard = false;
                }

                // If Ctrl is held: set attackMove to false
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    options.attackMove = false;
                }

                OrderManager.Instance.IssueMoveOrder(selectionObjects, targetPosition, options);
            }
        }
    }
}