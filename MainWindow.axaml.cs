using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace EasyClipper
{
    public partial class MainWindow : Window
    {
        // ── State ────────────────────────────────────────────────────────
        private readonly ObservableCollection<TrackedFile> _files = new();
        private readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> TextExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".txt", ".cs", ".py", ".js", ".jsx", ".ts", ".tsx",
                ".html", ".htm", ".css", ".scss", ".xml", ".json",
                ".cpp", ".h", ".hpp", ".c", ".java", ".php", ".rb",
                ".sh", ".bat", ".cmd", ".ps1", ".md", ".config",
                ".csproj", ".sln", ".xaml", ".sql", ".log", ".ini",
                ".yaml", ".yml", ".toml", ".vue", ".svelte", ".go",
                ".rs", ".swift", ".kt", ".kts", ".groovy", ".gradle",
                ".properties",
            };

        // ── Constructor ──────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            MainListView.ItemsSource = _files;
            FileTree.ItemsSource     = _files;

            _files.CollectionChanged += (_, _) => RefreshStats();

            // Подписка на изменение IsSelected у каждого файла
            _files.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                    foreach (TrackedFile f in e.NewItems)
                        f.PropertyChanged += File_PropertyChanged;
                if (e.OldItems != null)
                    foreach (TrackedFile f in e.OldItems)
                        f.PropertyChanged -= File_PropertyChanged;
            };
        }

        private void File_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TrackedFile.IsSelected))
                RefreshStats();
        }

        // ═══════════════════════════════════════════════════════════════
        // FILE ADDING
        // ═══════════════════════════════════════════════════════════════
        private void Window_DragEnter(object? sender, DragEventArgs e)
        {
            e.DragEffects = e.Data.Contains(DataFormats.Files)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object? sender, DragEventArgs e)
        {
            if (!e.Data.Contains(DataFormats.Files)) return;
            var items = e.Data.GetFiles()?
                .Select(f => f.TryGetLocalPath())
                .Where(p => !string.IsNullOrEmpty(p))
                .Cast<string>()
                .ToArray();
            if (items is { Length: > 0 })
            {
                Cursor = new Cursor(StandardCursorType.Wait);
                try   { AddItems(items); }
                finally { Cursor = Cursor.Default; }
            }
            e.Handled = true;
        }

        private async void AddFolder_Click(object? sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = "Выберите папку" });
            if (folders.Count > 0)
                AddItems(folders
                    .Select(f => f.TryGetLocalPath())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Cast<string>()
                    .ToArray());
        }

        private async void AddFiles_Click(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Выберите файлы",
                AllowMultiple = true,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Текстовые файлы")
                    {
                        Patterns = new[]
                        {
                            "*.txt", "*.cs", "*.py", "*.js", "*.ts", "*.jsx", "*.tsx",
                            "*.html", "*.css", "*.json", "*.xml", "*.cpp", "*.h",
                            "*.java", "*.go", "*.rs", "*.md",
                        }
                    },
                    FilePickerFileTypes.All,
                },
            });
            if (files.Count > 0)
                AddItems(files
                    .Select(f => f.TryGetLocalPath())
                    .Where(p => !string.IsNullOrEmpty(p))
                    .Cast<string>()
                    .ToArray());
        }

        private void DropZone_Click(object? sender, PointerPressedEventArgs e) =>
            AddFiles_Click(sender, new RoutedEventArgs());

        private async void AddItems(string[] paths)
        {
            var collected = new List<string>();
            foreach (var p in paths)
            {
                try
                {
                    if (Directory.Exists(p))
                        CollectFromDirectory(p, collected);
                    else if (File.Exists(p) && IsText(p))
                        collected.Add(p);
                }
                catch (Exception ex) { ShowError($"Ошибка доступа к {p}:\n{ex.Message}"); }
            }

            if (collected.Count == 0)
            {
                MsgBox.Show(this, "Поддерживаемых текстовых файлов не найдено.", "Информация");
                return;
            }

            var newFiles = new List<TrackedFile>();

            foreach (var fp in collected)
            {
                if (_paths.Contains(fp)) continue;
                try
                {
                    var fi = new FileInfo(fp);
                    if (!IsText(fp)) continue;
                    var trackedFile = new TrackedFile(fi);
                    _files.Add(trackedFile);
                    _paths.Add(fp);
                    newFiles.Add(trackedFile);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Skip {fp}: {ex.Message}");
                }
            }

            UpdateVisibility();

            // Дожидаемся загрузки всех новых файлов
            foreach (var file in newFiles)
            {
                // Ждем пока CharCount станет не 0 (или таймаут)
                for (int i = 0; i < 50 && file.CharCount == 0; i++)
                {
                    await Task.Delay(100);
                }
            }

            RefreshStats();
        }

        private void CollectFromDirectory(string dir, List<string> list)
        {
            try
            {
                foreach (var f in Directory.GetFiles(dir))
                    if (IsText(f)) list.Add(f);
                foreach (var sub in Directory.GetDirectories(dir))
                {
                    try { CollectFromDirectory(sub, list); }
                    catch (UnauthorizedAccessException) { }
                }
            }
            catch (UnauthorizedAccessException) { }
        }

        private static bool IsText(string path) =>
            TextExtensions.Contains(Path.GetExtension(path));

        // ═══════════════════════════════════════════════════════════════
        // SELECTION
        // ═══════════════════════════════════════════════════════════════
        private void SelectAll_Click(object? sender, RoutedEventArgs e)    => SetAll(true);
        private void DeselectAll_Click(object? sender, RoutedEventArgs e)  => SetAll(false);
        private void SetAll(bool val) { foreach (var f in _files) f.IsSelected = val; }

        private void HeaderCheckBox_Click(object? sender, RoutedEventArgs e)
        {
            bool val = HeaderCheckBox.IsChecked == true;
            SetAll(val);
        }

        private void RowCheckBox_Click(object? sender, RoutedEventArgs e)  => RefreshStats();
        private void TreeItemCb_Click(object? sender, RoutedEventArgs e)   => RefreshStats();

        private void TreeItem_Click(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed &&
                sender is Border b && b.DataContext is TrackedFile f)
            {
                // Scroll main list to matching item
                MainListView.ScrollIntoView(f);
                MainListView.SelectedItem = f;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // REMOVE / CLEAR
        // ═══════════════════════════════════════════════════════════════
        private void RemoveFile_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: TrackedFile f })
            {
                _files.Remove(f);
                _paths.Remove(f.FullName);
                UpdateVisibility();
                RefreshStats();
            }
        }

        private void Clear_Click(object? sender, RoutedEventArgs e)
        {
            if (_files.Count == 0) return;
            if (!MsgBox.Show(this, "Очистить список файлов?", "Подтверждение",
                    MsgBox.Buttons.YesNo, MsgBox.Icon.Question))
                return;
            _files.Clear();
            _paths.Clear();
            UpdateVisibility();
            RefreshStats();
        }

        // ═══════════════════════════════════════════════════════════════
        // REFRESH STATUS
        // ═══════════════════════════════════════════════════════════════
        private void Refresh_Click(object? sender, RoutedEventArgs e)
        {
            Cursor = new Cursor(StandardCursorType.Wait);
            try   { foreach (var f in _files) f.UpdateStatus(); }
            finally { Cursor = Cursor.Default; }
            RefreshStats();
        }

        // ═══════════════════════════════════════════════════════════════
        // BUILD OUTPUT TEXT
        // ═══════════════════════════════════════════════════════════════
        private string BuildOutput(IEnumerable<TrackedFile> files)
        {
            bool usePrefix     = ChkPrefix.IsChecked == true;
            bool optImports    = ChkOptImports.IsChecked == true;
            string tpl         = TbPrefixTemplate.Text ?? string.Empty;
            int sepCount       = int.TryParse(TbSepLines.Text, out var n) ? Math.Max(0, n) : 2;
            string separator   = new string('\n', sepCount + 1); // +1 for newline after content

            var sb = new StringBuilder();
            bool first = true;

            foreach (var f in files)
            {
                if (!first) sb.Append(separator);
                first = false;

                string content = f.ReadContent();

                if (optImports)
                    content = ImportOptimizer.Optimize(content, f.Language);

                if (usePrefix)
                {
                    var prefix = tpl
                        .Replace("{name}", f.FullName)
                        .Replace("{file}", f.Name)
                        .Replace("{lang}", f.Language);
                    sb.AppendLine(prefix);
                }

                sb.Append(content);
            }

            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════
        // COPY
        // ═══════════════════════════════════════════════════════════════
        private async void Copy_Click(object? sender, RoutedEventArgs e)
        {
            var selected = _files.Where(f => f.IsSelected).ToList();
            if (selected.Count == 0)
            {
                ShowInfo("Нет выбранных файлов.");
                return;
            }

            Cursor = new Cursor(StandardCursorType.Wait);
            try
            {
                string output = BuildOutput(selected);
                var clipboard = Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(output);

                // Кратковременно меняем текст кнопки
                BtnCopy.Content = "✓ Скопировано!";
                BtnCopy.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x7A, 0x3A));
                var timer = new DispatcherTimer
                    { Interval = TimeSpan.FromSeconds(2) };
                timer.Tick += (_, _) =>
                {
                    BtnCopy.Content    = "📋 Скопировать выбранные";
                    BtnCopy.Background = new SolidColorBrush(Color.FromRgb(0x5A, 0x4F, 0xC8));
                    timer.Stop();
                };
                timer.Start();
            }
            catch (Exception ex) { ShowError($"Ошибка копирования:\n{ex.Message}"); }
            finally { Cursor = Cursor.Default; }
        }

        // ═══════════════════════════════════════════════════════════════
        // EXPORT
        // ═══════════════════════════════════════════════════════════════
        private async void ExportOnly_Click(object? sender, RoutedEventArgs e)
        {
            var selected = _files.Where(f => f.IsSelected).ToList();
            if (selected.Count == 0) { ShowInfo("Нет выбранных файлов."); return; }

            Cursor = new Cursor(StandardCursorType.Wait);
            try   { await ExportToFile(BuildOutput(selected)); }
            catch (Exception ex) { ShowError($"Ошибка экспорта:\n{ex.Message}"); }
            finally { Cursor = Cursor.Default; }
        }

        private async Task ExportToFile(string content)
        {
            var ts    = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fname = $"clipper_export_{ts}.txt";

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = fname,
                DefaultExtension  = "txt",
                FileTypeChoices   = new[]
                {
                    new FilePickerFileType("Текстовые файлы (*.txt)") { Patterns = new[] { "*.txt" } },
                    FilePickerFileTypes.All,
                },
                Title = "Сохранить экспорт",
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                var bytes = Encoding.UTF8.GetBytes(content);
                await stream.WriteAsync(bytes);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // STATS & UI STATE
        // ═══════════════════════════════════════════════════════════════
        private void RefreshStats()
        {
            var sel         = _files.Where(f => f.IsSelected).ToList();
            long totalChars = sel.Sum(f => f.CharCount);
            long totalTokens = sel.Sum(f => f.TokenCount);
            double totalKb  = sel.Sum(f => f.SizeKb);
            int modCount    = _files.Count(f => f.Status == FileStatus.Modified);

            RunTotal.Text    = _files.Count.ToString();
            RunSelected.Text = sel.Count.ToString();
            RunChars.Text    = totalChars.ToString("N0", new System.Globalization.CultureInfo("ru-RU"));
            RunTokens.Text = totalTokens.ToString("N0", new System.Globalization.CultureInfo("ru-RU"));
            RunSize.Text     = $"{totalKb:F1} КБ";
            RunModified.Text = modCount.ToString();

            TbSelectedBadge.Text = $"{sel.Count} файлов выбрано";

            // Header checkbox tri-state
            if (sel.Count == 0)
                HeaderCheckBox.IsChecked = false;
            else if (sel.Count == _files.Count)
                HeaderCheckBox.IsChecked = true;
            else
                HeaderCheckBox.IsChecked = null;
        }

        private void UpdateVisibility()
        {
            bool hasFiles = _files.Count > 0;
            EmptyState.IsVisible   = !hasFiles;
            MainListView.IsVisible = hasFiles;
        }

        // ═══════════════════════════════════════════════════════════════
        // INPUT VALIDATION
        // ═══════════════════════════════════════════════════════════════
        private void TbSepLines_TextInput(object? sender, TextInputEventArgs e)
        {
            foreach (var ch in e.Text ?? string.Empty)
                if (!char.IsDigit(ch)) { e.Handled = true; return; }
        }

        // ═══════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════
        private void ShowError(string msg) =>
            MsgBox.Show(this, msg, "Ошибка", MsgBox.Buttons.OK, MsgBox.Icon.Error);

        private void ShowInfo(string msg) =>
            MsgBox.Show(this, msg, "Информация");
    }
}
