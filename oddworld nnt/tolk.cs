using System.Runtime.InteropServices;
using MelonLoader;
using System;

public static class TolkHelper
{
    private const string DllName = "Tolk.dll"; 
    private const string NvdaDllName = "nvdaControllerClient32.dll";

    [DllImport(DllName, EntryPoint = "Tolk_Load", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_Load();

    [DllImport(DllName, EntryPoint = "Tolk_Unload", CallingConvention = CallingConvention.Cdecl)]
    private static extern void Tolk_Unload();

    [DllImport(DllName, EntryPoint = "Tolk_Speak", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool Tolk_Speak([MarshalAs(UnmanagedType.LPWStr)] string text, bool interrupt);

    [DllImport(NvdaDllName, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    private static extern int nvdaController_brailleMessage([MarshalAs(UnmanagedType.LPWStr)] string text);

    [DllImport(NvdaDllName, CallingConvention = CallingConvention.StdCall)]
    private static extern int nvdaController_testIfRunning();

    public static void Load()
    {
        try { Tolk_Load(); }
        catch (Exception ex) { MelonLogger.Error($"Tolk Load Error: {ex.Message}"); }
    }

    public static void Unload()
    {
        Tolk_Unload();
    }

    public static void Speak(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        
        try { Tolk_Speak(text, true); }
        catch (Exception ex) { MelonLogger.Warning($"Tolk Speak Error: {ex.Message}"); }

        try
        {
            if (nvdaController_testIfRunning() == 0)
            {
                nvdaController_brailleMessage(text);
            }
        }
        catch {}
    }
}