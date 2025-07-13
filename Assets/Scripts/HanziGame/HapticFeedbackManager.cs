using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.Utilities;

public class HapticFeedbackManager : MonoBehaviour
{
    public void VibrateControllers(float amplitude, float duration)
    {
        VibrateHand(CommonUsages.LeftHand, amplitude, duration);
        VibrateHand(CommonUsages.RightHand, amplitude, duration);
    }

    private void VibrateHand(InternedString usage, float amplitude, float duration)
    {
        foreach (var device in InputSystem.devices)
        {
            // Look for XR controllers with haptics
            if (device.usages.Contains(usage) && device is XRControllerWithRumble rumbleDevice)
            {
                rumbleDevice.SendImpulse(amplitude, duration);
            }
        }
    }
}