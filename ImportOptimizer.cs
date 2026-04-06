using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyClipper
{
    /// <summary>
    /// Сворачивает все строки-импорты файла в одну строку вида
    /// "// using imports: X, Y, Z" (или аналог для языка).
    /// </summary>
    public static class ImportOptimizer
    {
        // ── Паттерны для каждого языка ───────────────────────────────────
        private static readonly Dictionary<string, Regex> Patterns =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["python"]     = new Regex(@"^(import\s+[\w,\s.*]+|from\s+[\w.]+\s+import\s+[\w,\s*]+)$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),

                ["javascript"] = new Regex(@"^import\s+.*?from\s+['""][^'""]+['""];?$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),

                ["typescript"] = new Regex(@"^import\s+.*?from\s+['""][^'""]+['""];?$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),

                ["csharp"]     = new Regex(@"^using\s+[\w.]+;$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),

                ["java"]       = new Regex(@"^import\s+[\w.*]+;$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),

                ["cpp"]        = new Regex(@"^#include\s*[<""][^>""]+ [>""]$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),

                ["go"]         = new Regex(@"^import\s+""[^""]+""$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),

                ["rust"]       = new Regex(@"^use\s+[\w::{}, *\n\t]+;$",
                                    RegexOptions.Multiline | RegexOptions.Compiled),
            };

        // ── Форматирование строки-результата ────────────────────────────
        private static string Format(string language, IEnumerable<string> imports)
        {
            var list = imports.Select(s => s.Trim()).ToList();

            return language.ToLowerInvariant() switch
            {
                "python"     => "# using imports: " + string.Join(", ", list),
                "javascript" => "// using imports: " + string.Join("; ", list),
                "typescript" => "// using imports: " + string.Join("; ", list),
                "csharp"     => "// using imports: " +
                                string.Join(", ",
                                    list.Select(l => l.Replace("using ", "").TrimEnd(';'))),
                "java"       => "// using imports: " +
                                string.Join(", ",
                                    list.Select(l => l.Replace("import ", "").TrimEnd(';'))),
                "cpp"        => "// using imports: " + string.Join(", ", list),
                "go"         => "// using imports: " + string.Join(", ", list),
                "rust"       => "// using imports: " +
                                string.Join(", ",
                                    list.Select(l => l.Replace("use ", "").TrimEnd(';'))),
                _            => string.Empty,
            };
        }

        // ── Public entry point ───────────────────────────────────────────
        public static string Optimize(string content, string language)
        {
            if (!Patterns.TryGetValue(language.ToLowerInvariant(), out var regex))
                return content;

            var matches = regex.Matches(content);
            if (matches.Count == 0)
                return content;

            var importLines = matches
                .Cast<Match>()
                .Select(m => m.Value.Trim())
                .Distinct()
                .ToList();

            // Удаляем все совпавшие строки из оригинала
            var cleaned = regex.Replace(content, string.Empty);

            // Убираем лишние пустые строки в начале
            cleaned = cleaned.TrimStart('\r', '\n');

            var header = Format(language, importLines);
            if (string.IsNullOrEmpty(header))
                return content;

            return header + Environment.NewLine + cleaned;
        }
    }
}
