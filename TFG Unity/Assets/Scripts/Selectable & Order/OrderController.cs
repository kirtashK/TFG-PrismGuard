using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class OrdersController : MonoBehaviour
{
    public LayerMask groundLayerMask = 1 << 0;

    private InputAction pointerAction;
    private InputAction orderAction;
    private InputAction shiftAction;
    private InputAction ctrlAction;

    private void Awake()
    {
        pointerAction = new InputAction("Pointer", InputActionType.Value, "<Pointer>/position");
        orderAction = new InputAction("Order", InputActionType.Button);
        orderAction.AddBinding("<Mouse>/rightButton");
        shiftAction = new InputAction("QueueOrder", InputActionType.Button);
        shiftAction.AddBinding("<Keyboard>/leftShift");
        shiftAction.AddBinding("<Keyboard>/rightShift");
        ctrlAction = new InputAction("AttackMoveModifier", InputActionType.Button);
        ctrlAction.AddBinding("<Keyboard>/leftCtrl");
        ctrlAction.AddBinding("<Keyboard>/rightCtrl");
    }

    private void OnEnable()
    {
        pointerAction?.Enable();
        orderAction?.Enable();
        shiftAction?.Enable();
        ctrlAction?.Enable();
    }

    private void OnDisable()
    {
        pointerAction?.Disable();
        orderAction?.Disable();
        shiftAction?.Disable();
        ctrlAction?.Disable();
    }

    private void OnDestroy()
    {
        pointerAction?.Dispose();
        orderAction?.Dispose();
        shiftAction?.Dispose();
        ctrlAction?.Dispose();
    }

    private void Update()
    {
        if (orderAction == null || pointerAction == null)
        {
            return;
        }
        if (!orderAction.WasPressedThisFrame())
        {
            return;
        }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mousePosition = pointerAction.ReadValue<Vector2>();
        if (Camera.main == null)
        {
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayerMask))
        {
            return;
        }

        Vector3 targetPosition = hit.point;

        IReadOnlyList<ISelectable> selection = SelectionManager.Instance.CurrentSelection;
        if (selection == null || selection.Count == 0)
        {
            return;
        }

        List<IOrderable> selectionObjects = new();
        foreach (ISelectable selectable in selection)
        {
            if (selectable is Component component
                && component.TryGetComponent<IOrderable>(out IOrderable orderableComponent))
            {
                selectionObjects.Add(orderableComponent);
            }
        }

        if (selectionObjects.Count == 0)
        {
            return;
        }

        MoveOrderOptions options = MoveOrderOptions.Default;

        if (shiftAction != null && shiftAction.IsPressed())
        {
            options.returnToGuard = false;
        }

        if (ctrlAction != null && ctrlAction.IsPressed())
        {
            options.attackMove = false;
        }

        OrderManager.Instance.IssueMoveOrder(selectionObjects, targetPosition, options);
    }
}