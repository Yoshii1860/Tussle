using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static Controls;

[CreateAssetMenu(fileName = "InputReader", menuName = "ScriptableObjects/InputReader", order = 1)]
public class InputReader : ScriptableObject, IPlayerActions
{
    public event Action<bool> PrimaryAttackEvent;
    public event Action InteractEvent;
    public event Action<int> ChangeAttackEvent;
    public event Action<Vector2> MoveEvent;
    public event Action<bool> SprintEvent;
    public event Action<bool> SecondaryAttackEvent;
    public event Action<Vector2> ZoomEvent;

    private Controls controls;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new Controls();
            controls.Player.SetCallbacks(this);
        }
        controls.Enable();
    }
    
    private void OnDisable()
    {
        controls.Disable();
    }

    public void OnPrimaryAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PrimaryAttackEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            PrimaryAttackEvent?.Invoke(false);
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InteractEvent?.Invoke();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SprintEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            SprintEvent?.Invoke(false);
        }
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            MoveEvent?.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnSecondaryAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            SecondaryAttackEvent?.Invoke(true);
        }
        else if (context.canceled)
        {
            SecondaryAttackEvent?.Invoke(false);
        }
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        if (context.performed || context.canceled)
        {
            ZoomEvent?.Invoke(context.ReadValue<Vector2>());
        }
    }

    public void OnChangeAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            var control = context.control;
            if (control != null)
            {
                Debug.Log($"ChangeAttack pressed: {control.name}");
                int attackIndex = -1;
                if (int.TryParse(control.name, out attackIndex))
                {
                    ChangeAttackEvent?.Invoke(attackIndex-1);
                }
            }
        }
    }
}