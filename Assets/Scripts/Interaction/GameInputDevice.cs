using UnityEngine;
using UnityEngine.InputSystem;

public static class GameInputDevice
{
    private static InputDevice lastUsedDevice;


    // =========================================================
    // SET DEVICE
    // =========================================================

    public static void SetLastUsedDevice(InputDevice device)
    {
        if (device == null)
        {
            return;
        }

        lastUsedDevice = device;
    }


    // =========================================================
    // DEVICE CHECKS
    // =========================================================

    public static bool LastUsedDeviceIsGamepad()
    {
        return lastUsedDevice is Gamepad;
    }


    public static bool LastUsedDeviceIsKeyboardOrMouse()
    {
        return lastUsedDevice is Keyboard ||
               lastUsedDevice is Mouse;
    }
}