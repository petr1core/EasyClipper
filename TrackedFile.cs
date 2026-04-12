// ===== TrackedFile.cs (исправленная версия) =====

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using TiktokenSharp;

namespace EasyClipper
{
    public enum FileStatus { New, Unchanged, Modified }

    public class TrackedFile : INotifyPropertyChanged
    {
        // Статический токенизатор Tiktoken
        private static TikToken? _tokenizer;
        private static readonly object _tokenizerLock = new();
        private static Exception? _tokenizerInitError;
        private static Task<TikToken>? _initializationTask;

        private static Task<TikToken> GetTokenizerAsync()
        {
            if (_tokenizer != null)
                return Task.FromResult(_tokenizer);

            if (_tokenizerInitError != null)
                throw new Exception("Tokenizer initialization failed", _tokenizerInitError);

            lock (_tokenizerLock)
            {
                if (_tokenizer != null)
                    return Task.FromResult(_tokenizer);

                if (_initializationTask != null)
                    return _initializationTask;

                _initializationTask = InitializeTokenizerAsync();
                return _initializationTask;
            }
        }

        private static async Task<TikToken> InitializeTokenizerAsync()
        {
            try
            {
                var tokenizer = await Task.Run(() => TikToken.EncodingForModel("gpt-4"));

                lock (_tokenizerLock)
                {
                    _tokenizer = tokenizer;
                    _initializationTask = null;
                }

                return _tokenizer;
            }
            catch (Exception ex)
            {
                _tokenizerInitError = ex;
                System.Diagnostics.Debug.WriteLine($"Tokenizer init error: {ex.Message}");
                throw;
            }
        }

        // ── Static helpers ──────────────────────────────────────────────
        private static readonly Dictionary<string, string> ExtToLang =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".py"] = "python",
                [".js"] = "javascript",
                [".jsx"] = "javascript",
                [".ts"] = "typescript",
                [".tsx"] = "typescript",
                [".cs"] = "csharp",
                [".java"] = "java",
                [".cpp"] = "cpp",
                [".cc"] = "cpp",
                [".cxx"] = "cpp",
                [".c"] = "cpp",
                [".go"] = "go",
                [".rs"] = "rust",
                [".rb"] = "ruby",
                [".php"] = "php",
                [".swift"] = "swift",
                [".kt"] = "kotlin",
                [".sh"] = "shell",
                [".bash"] = "shell",
                [".sql"] = "sql",
                [".html"] = "html",
                [".htm"] = "html",
                [".css"] = "css",
                [".scss"] = "css",
                [".json"] = "json",
                [".yaml"] = "yaml",
                [".yml"] = "yaml",
                [".toml"] = "toml",
                [".md"] = "markdown",
                [".xml"] = "xml",
                [".xaml"] = "xml",
                [".txt"] = "text",
            };

        private static readonly Dictionary<string, string> LangToIcon =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["python"] = "🐍",
                ["javascript"] = "🟨",
                ["typescript"] = "🔷",
                ["csharp"] = "🟣",
                ["java"] = "☕",
                ["cpp"] = "⚙",
                ["go"] = "🔵",
                ["rust"] = "🦀",
                ["html"] = "🌐",
                ["css"] = "🎨",
                ["json"] = "📋",
                ["markdown"] = "📝",
                ["sql"] = "🗄",
                ["xml"] = "📄",
                ["text"] = "📄",
            };

        // ── Properties ──────────────────────────────────────────────────
        public FileInfo FileInfo { get; }
        public string Name => FileInfo.Name;
        public string FullName => FileInfo.FullName;
        public DateTime AddedTime { get; }
        public DateTime LastWriteTimeAtAdd { get; }

        public string Language { get; }
        public string LangIcon => LangToIcon.TryGetValue(Language, out var ic) ? ic : "📄";

        private long _charCount;
        public long CharCount
        {
            get => _charCount;
            private set
            {
                _charCount = value;
                OnPropertyChanged(nameof(CharCount));
                System.Diagnostics.Debug.WriteLine($"CharCount set to {value} for {Name}");
            }
        }

        private long _tokenCount;
        public long TokenCount
        {
            get => _tokenCount;
            private set
            {
                _tokenCount = value;
                OnPropertyChanged(nameof(TokenCount));
                System.Diagnostics.Debug.WriteLine($"TokenCount set to {value} for {Name}");
            }
        }

        public double SizeKb => FileInfo.Length / 1024.0;

        private FileStatus _status = FileStatus.New;
        public FileStatus Status
        {
            get => _status;
            set
            {
                if (_status == value) return;
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string StatusText => Status switch
        {
            FileStatus.New => "Новый",
            FileStatus.Modified => "Изменён",
            FileStatus.Unchanged => "Не изменён",
            _ => ""
        };

        private bool _isSelected = true;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        // ── Constructor ─────────────────────────────────────────────────
        public TrackedFile(FileInfo fileInfo)
        {
            FileInfo = fileInfo;
            AddedTime = DateTime.Now;
            LastWriteTimeAtAdd = fileInfo.LastWriteTime;

            var ext = fileInfo.Extension;
            Language = ExtToLang.TryGetValue(ext, out var lang) ? lang : "other";

            System.Diagnostics.Debug.WriteLine($"Created TrackedFile for {Name}, path: {FullName}");

            _ = InitializeAsync();
        }

        // ── Public methods ───────────────────────────────────────────────
        private async Task InitializeAsync()
        {
            try
            {
                await RefreshContentAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"InitializeAsync error for {Name}: {ex.Message}");
            }
        }

        public async Task UpdateStatusAsync()
        {
            try
            {
                FileInfo.Refresh();
                Status = FileInfo.LastWriteTime > LastWriteTimeAtAdd
                    ? FileStatus.Modified
                    : FileStatus.Unchanged;
                await RefreshContentAsync();
            }
            catch (Exception ex)
            {
                Status = FileStatus.Modified;
                System.Diagnostics.Debug.WriteLine($"UpdateStatusAsync error for {Name}: {ex.Message}");
            }
        }

        public void UpdateStatus()
        {
            _ = UpdateStatusAsync();
        }

        public string ReadContent()
        {
            try
            {
                var content = File.ReadAllText(FileInfo.FullName);
                System.Diagnostics.Debug.WriteLine($"ReadContent for {Name}: {content.Length} chars");
                return content;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReadContent error for {Name}: {ex.Message}");
                return string.Empty;
            }
        }

        // ── Private ──────────────────────────────────────────────────────
        private async Task RefreshContentAsync()
        {
            System.Diagnostics.Debug.WriteLine($"RefreshContentAsync started for {Name}");

            string content = string.Empty;
            try
            {
                content = await Task.Run(() => File.ReadAllText(FileInfo.FullName));
                System.Diagnostics.Debug.WriteLine($"Read {content.Length} chars from {Name}");
                CharCount = content.Length;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading file {Name}: {ex.Message}");
                CharCount = 0;
            }

            try
            {
                if (!string.IsNullOrEmpty(content))
                {
                    System.Diagnostics.Debug.WriteLine($"Counting tokens for {Name} ({content.Length} chars)");
                    var tokenizer = await GetTokenizerAsync();
                    var tokens = tokenizer.Encode(content);
                    TokenCount = tokens.Count;
                    System.Diagnostics.Debug.WriteLine($"Token count for {Name}: {TokenCount}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Empty content for {Name}");
                    TokenCount = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Token counting error for {Name}: {ex.Message}");
                TokenCount = 0;
            }

            System.Diagnostics.Debug.WriteLine($"RefreshContentAsync completed for {Name}");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}