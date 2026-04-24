using System.Runtime.InteropServices;

namespace ZenTimeBox.Demo;

internal static class ImeSuppressor
{
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

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
}
