// Headless design check: renders the main window (empty state, with files,
// and the MsgBox dialog) without a display and saves PNG snapshots next to
// this project. Run: dotnet run --project tools/DesignCheck
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using EasyClipper;

internal static class Program
{
    private static string OutDir => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

    [STAThread]
    public static void Main(string[] args)
    {
        Directory.CreateDirectory(OutDir);

        var app = AppBuilder.Configure<App>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
        app.SetupWithoutStarting();

        var win = new MainWindow();
        win.Show();

        // 1. Empty state
        Thread.Sleep(800);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Thread.Sleep(300);
        win.CaptureRenderedFrame()?.Save(Path.Combine(OutDir, "empty.png"));
        Console.WriteLine("empty saved");

        // 2. With files
        var add = typeof(MainWindow).GetMethod("AddItems", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Dispatcher.UIThread.Invoke(() => add.Invoke(win, new object?[] { new[] {
            "/home/user/Проекты/EasyClipper/TrackedFile.cs",
            "/home/user/Проекты/EasyClipper/ImportOptimizer.cs",
            "/home/user/Проекты/EasyClipper/NuGet.config",
        }}));
        Thread.Sleep(3500);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Thread.Sleep(300);
        win.CaptureRenderedFrame()?.Save(Path.Combine(OutDir, "main.png"));
        Console.WriteLine("main saved");

        // 3. MsgBox dialog
        var dlg = new MsgBoxWindow("Очистить список файлов?", "Подтверждение",
            MsgBox.Buttons.YesNo, MsgBox.Icon.Question);
        dlg.ShowDialog(win);
        Thread.Sleep(800);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Thread.Sleep(300);
        dlg.CaptureRenderedFrame()?.Save(Path.Combine(OutDir, "msgbox.png"));
        Console.WriteLine("msgbox saved");
    }
}
