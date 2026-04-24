using System.Runtime.InteropServices;

namespace ZenTimeBox.Demo;

internal static class ImeSuppressor
{
    public static void DisableForCurrentThread()
    {
        try
        {
            ImmDisableIME(0);
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "IME thread suppression failed");
        }
    }

    public static void DisableFor(Control control)
    {
        control.ImeMode = ImeMode.Disable;

        if (control.IsHandleCreated)
        {
            DisableForHandle(control.Handle);
        }
    }

    public static void DisableForHandle(IntPtr handle)
    {
        if (handle == IntPtr.Zero)
        {
            return;
        }

        try
        {
            ImmAssociateContext(handle, IntPtr.Zero);
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "IME suppression failed");
        }
    }

    public static void MatchForegroundKeyboardLayout()
    {
        try
        {
            IntPtr foregroundWindow = GetForegroundWindow();
            if (foregroundWindow == IntPtr.Zero)
            {
                return;
            }

            uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
            if (foregroundThreadId == 0)
            {
                return;
            }

            IntPtr keyboardLayout = GetKeyboardLayout(foregroundThreadId);
            if (keyboardLayout != IntPtr.Zero)
            {
                ActivateKeyboardLayout(keyboardLayout, 0);
            }
        }
        catch (Exception exception)
        {
            AppDiagnostics.LogException(exception, "Keyboard layout sync failed");
        }
    }

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);

    [DllImport("imm32.dll")]
    private static extern bool ImmDisableIME(uint idThread);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern IntPtr ActivateKeyboardLayout(IntPtr hkl, uint flags);
}
