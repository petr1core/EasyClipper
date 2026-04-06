using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace EasyClipper
{
    public enum FileStatus { New, Unchanged, Modified }

    public class TrackedFile : INotifyPropertyChanged
    {
        // ── Static helpers ──────────────────────────────────────────────
        private static readonly Dictionary<string, string> ExtToLang =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".py"]      = "python",
                [".js"]      = "javascript",
                [".jsx"]     = "javascript",
                [".ts"]      = "typescript",
                [".tsx"]     = "typescript",
                [".cs"]      = "csharp",
                [".java"]    = "java",
                [".cpp"]     = "cpp",
                [".cc"]      = "cpp",
                [".cxx"]     = "cpp",
                [".c"]       = "cpp",
                [".go"]      = "go",
                [".rs"]      = "rust",
                [".rb"]      = "ruby",
                [".php"]     = "php",
                [".swift"]   = "swift",
                [".kt"]      = "kotlin",
                [".sh"]      = "shell",
                [".bash"]    = "shell",
                [".sql"]     = "sql",
                [".html"]    = "html",
                [".htm"]     = "html",
                [".css"]     = "css",
                [".scss"]    = "css",
                [".json"]    = "json",
                [".yaml"]    = "yaml",
                [".yml"]     = "yaml",
                [".toml"]    = "toml",
                [".md"]      = "markdown",
                [".xml"]     = "xml",
                [".xaml"]    = "xml",
                [".txt"]     = "text",
            };

        private static readonly Dictionary<string, string> LangToIcon =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["python"]     = "🐍",
                ["javascript"] = "🟨",
                ["typescript"] = "🔷",
                ["csharp"]     = "🟣",
                ["java"]       = "☕",
                ["cpp"]        = "⚙",
                ["go"]         = "🔵",
                ["rust"]       = "🦀",
                ["html"]       = "🌐",
                ["css"]        = "🎨",
                ["json"]       = "📋",
                ["markdown"]   = "📝",
                ["sql"]        = "🗄",
                ["xml"]        = "📄",
                ["text"]       = "📄",
            };

        // ── Properties ──────────────────────────────────────────────────
        public FileInfo FileInfo { get; }
        public string   Name     => FileInfo.Name;
        public string   FullName => FileInfo.FullName;
        public DateTime AddedTime          { get; }
        public DateTime LastWriteTimeAtAdd { get; }

        public string Language { get; }
        public string LangIcon => LangToIcon.TryGetValue(Language, out var ic) ? ic : "📄";

        private long _charCount;
        public long CharCount
        {
            get => _charCount;
            private set { _charCount = value; OnPropertyChanged(nameof(CharCount)); }
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
            FileStatus.New       => "Новый",
            FileStatus.Modified  => "Изменён",
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
            FileInfo           = fileInfo;
            AddedTime          = DateTime.Now;
            LastWriteTimeAtAdd = fileInfo.LastWriteTime;

            var ext = fileInfo.Extension;
            Language = ExtToLang.TryGetValue(ext, out var lang) ? lang : "other";

            RefreshContent();
        }

        // ── Public methods ───────────────────────────────────────────────
        public void UpdateStatus()
        {
            try
            {
                FileInfo.Refresh();
                Status = FileInfo.LastWriteTime > LastWriteTimeAtAdd
                    ? FileStatus.Modified
                    : FileStatus.Unchanged;
                RefreshContent();
            }
            catch { Status = FileStatus.Modified; }
        }

        public string ReadContent()
        {
            try   { return File.ReadAllText(FileInfo.FullName); }
            catch { return string.Empty; }
        }

        // ── Private ──────────────────────────────────────────────────────
        private void RefreshContent()
        {
            try   { CharCount = File.ReadAllText(FileInfo.FullName).Length; }
            catch { CharCount = 0; }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
