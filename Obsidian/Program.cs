using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Obsidian.Data;
using Obsidian.Services;
using Photino.Blazor;
using PhotinoAPI;
using PInvoke;
using System.Runtime.InteropServices;

namespace Obsidian;

public class Program
{
    private static IntPtr _originalWndProc;

    [STAThread]
    static unsafe void Main(string[] args)
    {
        PhotinoBlazorAppBuilder builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();

        // register root component and selector
        builder.RootComponents.Add<App>("app");

        builder.Services.AddSingleton(Config.Load());
        builder.Services.AddScoped<HashtableService>();
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.ShowCloseIcon = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3000;
            config.SnackbarConfiguration.ShowTransitionDuration = 250;
            config.SnackbarConfiguration.HideTransitionDuration = 250;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
        });

        PhotinoBlazorApp app = builder.Build();

        // customize window
        app.MainWindow.UseOsDefaultSize = false;
        app.MainWindow
            .SetIconFile("favicon.ico")
            .SetTitle("Obsidian")
            .SetContextMenuEnabled(false)
            .RegisterApi(new());

        app.MainWindow.UseOsDefaultSize = true;

#if DEBUG
        app.MainWindow.SetDevToolsEnabled(true);
#endif
        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.OpenAlertWindow("Fatal exception", error.ExceptionObject.ToString());
        };

        app.MainWindow.WindowCreated += (sender, e) =>
        {
            _originalWndProc = HijackWndProc(HandleMessage, app.MainWindow.WindowHandle);
        };

        app.Run();
    }

    private static IntPtr HijackWndProc(User32.WndProc wndProc, IntPtr hWnd)
    {
        IntPtr wndProcPtr = Marshal.GetFunctionPointerForDelegate(wndProc);

        return User32.SetWindowLongPtr(hWnd, User32.WindowLongIndexFlags.GWL_WNDPROC, wndProcPtr);
    }

    private static unsafe IntPtr HandleMessage(IntPtr hWnd, User32.WindowMessage msg, void* wParam, void* lParam)
    {
        Console.WriteLine(msg.ToString());

        return User32.DefWindowProc(hWnd, msg, (nint)wParam, (nint)lParam);
    }
}
