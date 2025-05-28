// For UI Buttons, add this to your VRButton script:
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public class VRButton : MonoBehaviour
{
    private UnityEngine.UI.Button unityButton;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    private void Awake()
    {
        unityButton = GetComponent<UnityEngine.UI.Button>();
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();

        interactable.selectEntered.AddListener(_ => unityButton.onClick.Invoke());
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        UnityEngine.Debug.Log("Button pressed!");
        AppManager.Instance.PlayView();
    }
}