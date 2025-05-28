using UnityEngine;
using UnityEngine.InputSystem;

public class BButtonAction : MonoBehaviour
{
    [SerializeField] private InputActionReference buttonAction;

    private void OnEnable()
    {
        buttonAction.action.Enable();
        buttonAction.action.performed += OnPressed;
    }

    private void OnDisable()
    {
        buttonAction.action.performed -= OnPressed;
    }

    private void OnPressed(InputAction.CallbackContext context)
    {
        // Your action here
        UnityEngine.Debug.Log("B Button Pressed!");
        AppManager.Instance.StartView();
    }
}