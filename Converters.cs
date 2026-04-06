using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace EasyClipper
{
    // ── Language → badge background ─────────────────────────────────────
    public class LangToBadgeBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var lang = value as string ?? "other";
            return lang.ToLowerInvariant() switch
            {
                "python"     => new SolidColorBrush(Color.FromRgb(0xDF, 0xF0, 0xE8)),
                "javascript" => new SolidColorBrush(Color.FromRgb(0xFE, 0xF6, 0xD0)),
                "typescript" => new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF8)),
                "csharp"     => new SolidColorBrush(Color.FromRgb(0xE8, 0xDC, 0xF8)),
                "java"       => new SolidColorBrush(Color.FromRgb(0xFD, 0xE8, 0xDC)),
                "cpp"        => new SolidColorBrush(Color.FromRgb(0xF0, 0xEC, 0xF8)),
                "go"         => new SolidColorBrush(Color.FromRgb(0xDC, 0xF0, 0xF8)),
                "rust"       => new SolidColorBrush(Color.FromRgb(0xF8, 0xE8, 0xDC)),
                _            => new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xEC)),
            };
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    // ── Language → badge foreground ─────────────────────────────────────
    public class LangToBadgeFgConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var lang = value as string ?? "other";
            return lang.ToLowerInvariant() switch
            {
                "python"     => new SolidColorBrush(Color.FromRgb(0x1A, 0x6A, 0x3A)),
                "javascript" => new SolidColorBrush(Color.FromRgb(0x7A, 0x58, 0x00)),
                "typescript" => new SolidColorBrush(Color.FromRgb(0x1A, 0x4A, 0x80)),
                "csharp"     => new SolidColorBrush(Color.FromRgb(0x5A, 0x1A, 0x80)),
                "java"       => new SolidColorBrush(Color.FromRgb(0x7A, 0x2A, 0x10)),
                "cpp"        => new SolidColorBrush(Color.FromRgb(0x4A, 0x3A, 0x70)),
                "go"         => new SolidColorBrush(Color.FromRgb(0x0A, 0x50, 0x60)),
                "rust"       => new SolidColorBrush(Color.FromRgb(0x7A, 0x30, 0x10)),
                _            => new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
            };
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    // ── FileStatus → text color ──────────────────────────────────────────
    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileStatus s)
                return s switch
                {
                    FileStatus.New       => new SolidColorBrush(Color.FromRgb(0x18, 0x5F, 0xA5)),
                    FileStatus.Modified  => new SolidColorBrush(Color.FromRgb(0xE0, 0x40, 0x40)),
                    FileStatus.Unchanged => new SolidColorBrush(Color.FromRgb(0x1A, 0x7A, 0x3A)),
                    _ => Brushes.Black,
                };
            return Brushes.Black;
        }
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    // ── bytes → "N.N КБ" ────────────────────────────────────────────────
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is long b ? $"{b / 1024.0:F1} КБ" : "—";
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }

    // ── long chars → "N N N" with RU separators ─────────────────────────
    public class CharCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is long n ? n.ToString("N0", new CultureInfo("ru-RU")) : "—";
        public object ConvertBack(object v, Type t, object p, CultureInfo c) => throw new NotImplementedException();
    }
}
