# Easy Clipper

Easy Clipper — удобное кроссплатформенное приложение (Linux / Windows / macOS), которое помогает быстро собрать содержимое нескольких файлов кода в один текст для отправки в чат и вставку в Web-LLM.

> 📌 Ветка `linux_main` — порт оригинального WPF-приложения на **Avalonia UI 11** (C#/.NET 8).
> Логика, дизайн и поведение сохранены 1:1; WPF-версия осталась в ветке `main`.

# Возможности
📁 Импорт файлов: перетаскивание (drag-and-drop) или выбор через диалог (файлы и папки)

🌐 Поддержка языков: .py, .js, .ts, .cs, .java, .cpp, .go, .rs и другие

⚙ Гибкая настройка экспорта:
- Префикс файла с шаблоном ({{name}}, {{file}}, {{lang}})
- Оптимизация импортов (сворачивание в одну строку)
- Настройка количества пустых строк-разделителей

📋 Два режима вывода:
- Копирование в буфер обмена
- Экспорт в .txt файл

🔍 Статус файлов: отслеживание изменений (Новый / Изменён / Не изменён)

🔢 Оценка токенов через TiktokenSharp (gpt-4 encoding)

# 📦 Требования
- Linux (X11/XWayland), Windows 10/11 или macOS
- .NET 8.0 Runtime

# Сборка и запуск

```bash
# Клонируйте репозиторий (ветка linux_main)
git clone -b linux_main <repository-url>
cd EasyClipper

# Запуск
dotnet run

# Сборка
dotnet build -c Release

# Публикация под Linux x64 (самодостаточная, без установленного .NET)
dotnet publish -c Release -r linux-x64 --self-contained true \
  -p:PublishSingleFile=true -o bin/publish/linux-x64
# → bin/publish/linux-x64/EasyClipper
```

> 💡 Если `api.nuget.org` недоступен из вашей сети, в репозитории есть `NuGet.config`,
> который использует универсально доступный v2-эндпоинт `https://www.nuget.org/api/v2/`.

# Использование
1. Перетащите файлы и папки через боковую панель или перетащите их в окно
2. Отметьте нужные файлы галочками
3. Настройте параметры экспорта (опционально):
☑ Префикс файла — шаблон заголовка перед содержимым
☑ Оптимизировать импорты — свернёт import/using в одну строку
☑ Экспорт в .txt — покажет кнопку сохранения в файл
Параметр "Разделитель" — количество пустых строк между файлами
Нажмите «Скопировать выбранные» 🎉
💡 Если включён чекбокс «Экспорт в .txt», появится отдельная кнопка «📥 Сохранить .txt» для экспорта в файл.

# Технические детали порта
- UI: Avalonia 11.3 (Fluent theme, принудительно светлая тема — как в оригинале)
- Порт логики 1:1: `TrackedFile.cs`, `ImportOptimizer.cs` — без изменений
- Вместо WPF `MessageBox` — собственный диалог `MsgBoxWindow` в стиле приложения
- Файловые диалоги и буфер обмена — через кроссплатформенный `StorageProvider` / `Clipboard` Avalonia
- Иконка приложения — `Assets/AppIcon.png`
- Скриншоты порта для проверки дизайна: `tools/DesignCheck/*.png`

# Скриншоты (оригинал, WPF)
<img width="1064" height="641" alt="image" src="https://github.com/user-attachments/assets/cb9f0049-1cb6-4f69-bdb2-ffc68f19c257" />
<img width="1156" height="641" alt="image" src="https://github.com/user-attachments/assets/f0d53a1b-91a4-4139-b3ee-acd794854bdc" />
<img width="1156" height="641" alt="image" src="https://github.com/user-attachments/assets/04266e07-0b49-4ee3-b60d-7eb703a8e4ea" />
