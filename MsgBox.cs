using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace EasyClipper
{
    public enum MsgBoxResult { OK, Yes, No }

    public partial class MsgBoxWindow : Window
    {
        public MsgBoxResult Result { get; private set; } = MsgBoxResult.OK;

        // Required by the Avalonia XAML runtime loader (for preview/tooling).
        public MsgBoxWindow()
        {
            InitializeComponent();
            BtnOk.Content = "ОК";
        }

        public MsgBoxWindow(string message, string title, MsgBox.Buttons buttons, MsgBox.Icon icon)
        {
            InitializeComponent();
            Title = title;
            TxtMessage.Text = message;

            switch (icon)
            {
                case MsgBox.Icon.Question: TxtIcon.Text = "❓"; break;
                case MsgBox.Icon.Error:    TxtIcon.Text = "⛔"; break;
                default:                   TxtIcon.Text = "ℹ️"; break;
            }

            if (buttons == MsgBox.Buttons.YesNo)
            {
                BtnCancel.IsVisible = true;
                BtnCancel.Content = "Нет";
                BtnOk.Content = "Да";
            }
            else
            {
                BtnOk.Content = "ОК";
            }
        }

        private void BtnOk_Click(object? sender, RoutedEventArgs e)
        {
            Result = BtnCancel.IsVisible ? MsgBoxResult.Yes : MsgBoxResult.OK;
            Close();
        }

        private void BtnCancel_Click(object? sender, RoutedEventArgs e)
        {
            Result = MsgBoxResult.No;
            Close();
        }
    }

    /// <summary>
    /// Простая замена WPF MessageBox в стиле приложения.
    /// </summary>
    public static class MsgBox
    {
        public enum Buttons { OK, YesNo }
        public enum Icon { Info, Question, Error }

        public static bool Show(Window? owner, string message, string title,
                                Buttons buttons = Buttons.OK, Icon icon = Icon.Info)
        {
            var dlg = new MsgBoxWindow(message, title, buttons, icon);
            var win = owner ?? GetActiveWindow();
            if (win != null) dlg.ShowDialog(win);
            else             dlg.Show();
            return dlg.Result is MsgBoxResult.OK or MsgBoxResult.Yes;
        }

        private static Window? GetActiveWindow()
        {
            if (Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
                return desktop.MainWindow;
            return null;
        }
    }
}
