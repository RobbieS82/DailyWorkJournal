# Daily Work Journal

> A lightweight C# WPF desktop application for capturing and organizing daily work notes — with auto-save, week-based navigation, and an AI-friendly log format.

---

## Table of Contents

1. [Strategic Overview](#strategic-overview)
2. [Quick Start & TL;DR](#quick-start--tldr)
3. [Features](#features)
4. [Architecture Overview](#architecture-overview)
5. [Configuration](#configuration)
6. [Build & Installation](#build--installation)
7. [Deployment](#deployment)
8. [Log File Format (API Reference)](#log-file-format-api-reference)
9. [Development Guide](#development-guide)

---

## Strategic Overview

Keeping a daily work journal is one of the most effective habits for staying organized, building a searchable career history, and communicating progress to your team. The problem? Most note-taking tools are either too heavy (full project management suites) or too lightweight (plain text files with no structure).

**Daily Work Journal** solves this by providing:

- **A frictionless capture experience** — one large text area per day, no complicated forms.
- **A structured, AI-readable log file** — entries are written with clear delimiters so you can feed the log directly to an AI assistant (ChatGPT, Copilot, etc.) and ask it to summarize your week, find recurring themes, or generate a status report.
- **Work-week-centric navigation** — the app opens straight to the current Mon–Fri week, matching how most professionals think about their work.
- **Zero cloud dependency** — everything lives in a single plain-text file on your machine (`%APPDATA%\DailyWorkJournal\logs\daily-log.log`).

---

## Quick Start & TL;DR

```bash
# 1. Clone the repository
git clone https://github.com/RobbieS82/DailyWorkJournal.git
cd DailyWorkJournal

# 2. Build (requires .NET 8 SDK on Windows)
dotnet build DailyWorkJournal.csproj

# 3. Run
dotnet run --project DailyWorkJournal.csproj
```

Or open `DailyWorkJournal.slnx` in **Visual Studio 2022** (or later) and press **F5**.

---

## Features

- 📅 **Week-based view** — Monday through Friday displayed side-by-side in a single screen.
- 🗓️ **Calendar navigation** — built-in WPF calendar control to jump to any week instantly.
- ✍️ **Free-text entry per day** — large, scrollable text area for bullet-point notes.
- 💾 **Auto-save every 5 minutes** — you never lose work due to a forgotten save.
- ⚠️ **Unsaved-changes prompt** — closing the window with unsaved edits triggers a save dialog.
- 🟠 **Dirty-state indicator** — a small orange dot appears on a day's tab when it has unsaved changes.
- ✅ **Status bar** — shows the last save timestamp or an "Unsaved changes" warning at all times.
- 🔍 **AI-friendly log format** — the aggregate `.log` file uses clear delimiters that are trivial to parse with regex or to pass directly to an AI summarization service.
- 📄 **View and copy log file** — open a dedicated window to inspect and copy the raw log file contents for sharing or AI summarization.
- 🏠 **First-run setup** — the application automatically creates the `%APPDATA%\DailyWorkJournal\logs\` directory on first launch.
- ⌨️ **Ctrl+S keyboard shortcut** — save all dirty entries without touching the mouse.

---

## Architecture Overview

The application follows the **MVVM (Model–View–ViewModel)** pattern to keep UI and business logic cleanly separated.

```
┌──────────────────────────────────────────────────────┐
│                        Views                          │
│  MainWindow.xaml / MainWindow.xaml.cs                 │
│  • WPF Window + XAML data bindings                    │
│  • Code-behind only for UI events (Closing, Calendar) │
└───────────────────┬──────────────────────────────────┘
                    │  binds to
┌───────────────────▼──────────────────────────────────┐
│                     ViewModels                        │
│  MainViewModel      — week state, commands, dirty     │
│  LogEntryViewModel  — per-day content + IsDirty flag  │
│  ViewModelBase      — INotifyPropertyChanged helper   │
│  RelayCommand       — ICommand delegate wrapper       │
└─────────┬──────────────────────┬─────────────────────┘
          │                      │
          │ reads / writes       │ timer events
┌─────────▼──────────┐  ┌───────▼──────────────────────┐
│      Services       │  │          Services             │
│  LogFileService     │  │  AutoSaveService              │
│  • Load all entries │  │  • DispatcherTimer (5 min)    │
│  • Upsert entries   │  │  • Raises AutoSaveTriggered   │
│  • Atomic file I/O  │  └──────────────────────────────┘
└─────────┬──────────┘
          │ reads / writes
┌─────────▼──────────┐
│       Models        │
│  LogEntry           │  — Date, Content, LastModified
│  WorkWeek           │  — Mon–Fri entry collection
└────────────────────┘
          │
          │ persisted to
┌─────────▼──────────────────────────────────────────┐
│  %APPDATA%\DailyWorkJournal\logs\daily-log.log      │
│  (plain UTF-8 text, structured with delimiters)     │
└────────────────────────────────────────────────────┘
```

### Data Flow

1. **Startup** — `App.OnStartup` calls `LogFileService.EnsureLogDirectoryExists()`. `MainViewModel` constructor calls `LogFileService.LoadAllEntries()` to populate the in-memory dictionary, then builds the current work week's `LogEntryViewModel` collection.
2. **User edits** — the `TextBox` in each day panel binds to `LogEntryViewModel.Content`. Any change sets `IsDirty = true`, which propagates up to `MainViewModel.HasUnsavedChanges` and updates the status bar.
3. **Save** — triggered by Ctrl+S, the "Save Now" button, or the auto-save timer. `MainViewModel.SaveAll()` flushes each dirty VM's content into its model, then calls `LogFileService.SaveEntries()`. The file service merges the new data with any previously saved entries and writes atomically via a `.tmp` file.
4. **Close** — `MainWindow.Window_Closing` calls `MainViewModel.PromptSaveOnClose()`. If there are dirty entries, a `MessageBox` gives the user Yes / No / Cancel options before the window is allowed to close.

---

## Configuration

| Setting | Value | Notes |
|---------|-------|-------|
| Log file location | `%APPDATA%\DailyWorkJournal\logs\daily-log.log` | Created automatically on first run |
| Auto-save interval | 5 minutes | Configurable via `AutoSaveService.Interval` |
| Target framework | `.NET 8.0-windows` | Requires Windows; WPF is Windows-only |

The log file path is defined in `Services/LogFileService.cs`:

```csharp
public static string LogFilePath { get; } = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "DailyWorkJournal",
    "logs",
    "daily-log.log");
```

---

## Build & Installation

### Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| Windows OS | Windows 10 / 11 | — |
| .NET 8 SDK | 8.0 or later | https://dotnet.microsoft.com/download/dotnet/8.0 |
| Visual Studio (optional) | 2022 17.8+ | https://visualstudio.microsoft.com/ |

### Build via CLI

```bash
# Restore NuGet packages
dotnet restore DailyWorkJournal.csproj

# Debug build
dotnet build DailyWorkJournal.csproj

# Release build
dotnet build DailyWorkJournal.csproj -c Release

# Run directly
dotnet run --project DailyWorkJournal.csproj
```

### Build via Visual Studio

1. Open `DailyWorkJournal.slnx` in Visual Studio 2022.
2. Select **Build → Build Solution** (or press `Ctrl+Shift+B`).
3. Press **F5** to run with debugging, or **Ctrl+F5** to run without.

---

## Deployment

### Self-contained single-file executable

```bash
dotnet publish DailyWorkJournal.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -o ./publish
```

The resulting `publish/DailyWorkJournal.exe` can be copied to any Windows 10/11 machine and run without installing .NET.

### Framework-dependent deployment

If the target machine already has the .NET 8 runtime installed:

```bash
dotnet publish DailyWorkJournal.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -o ./publish
```

### ClickOnce / MSIX (optional)

For enterprise deployment or Windows Store distribution, the project can be published as a ClickOnce application or packaged as MSIX using the **Windows Application Packaging Project** template in Visual Studio.

---

## Log File Format (API Reference)

The aggregate log file is a plain UTF-8 text file. Each day's entry is wrapped in a clearly delimited block:

```
===== ENTRY: 2026-07-07 (Monday) =====
LAST_MODIFIED: 2026-07-07T14:30:00
CONTENT:
• Completed project review
• Updated documentation  
• Team standup meeting
===== END ENTRY =====

===== ENTRY: 2026-07-08 (Tuesday) =====
LAST_MODIFIED: 2026-07-08T09:15:00
CONTENT:
• Code review for PR #42
• Fixed bug in authentication module
===== END ENTRY =====
```

### Format Specification

| Element | Pattern | Notes |
|---------|---------|-------|
| Entry header | `===== ENTRY: YYYY-MM-DD (DayName) =====` | Opens each entry block |
| Last-modified | `LAST_MODIFIED: YYYY-MM-DDTHH:mm:ss` | Local time, ISO 8601 |
| Content header | `CONTENT:` | All text after this line is user content |
| Entry footer | `===== END ENTRY =====` | Closes each entry block |
| Block separator | One blank line | Separates consecutive entries |

### Parsing with Regex (example in Python)

```python
import re

ENTRY_HEADER = re.compile(r"^===== ENTRY: (\d{4}-\d{2}-\d{2}) \(\w+\) =====$")
LAST_MODIFIED = re.compile(r"^LAST_MODIFIED: (.+)$")
ENTRY_END = "===== END ENTRY ====="

entries = {}
current_date = None
in_content = False
content_lines = []

with open("daily-log.log", encoding="utf-8") as f:
    for line in f:
        line = line.rstrip("\n")
        m = ENTRY_HEADER.match(line)
        if m:
            current_date = m.group(1)
            in_content = False
            content_lines = []
        elif line.strip() == ENTRY_END and current_date:
            entries[current_date] = "\n".join(content_lines).strip()
            current_date = None
        elif line.strip() == "CONTENT:":
            in_content = True
        elif in_content:
            content_lines.append(line)
```

### Using with AI Summarization

Because the file uses consistent, clearly-named delimiters and ISO dates, you can paste the entire file (or a week's worth of entries) into any AI chat interface and ask questions like:

- *"Summarize my work from the week of July 7th."*
- *"What recurring topics appeared across the last two weeks?"*
- *"Draft a status report based on my entries for July."*

### Viewing the Raw Log File

Click the **📄 View Log** button in the main window header to open a dedicated viewer for the complete log file contents.

- **Copy All** copies the entire raw log to the clipboard with one click.
- **Refresh** reloads the file from disk so the viewer reflects the latest saved changes.
- The viewer uses a monospace font and horizontal scrolling so the structured delimiter format is easy to read and copy.
- The status bar shows entry count, character count, and the full file path for the loaded log.

This makes it easy to:

- Copy your full work history into AI services such as ChatGPT or Claude
- Extract a specific week or month from the stored log
- Share the exact persisted log format with a colleague or manager
- Review the raw file without leaving the application

---

## Development Guide

### Project Structure

```
DailyWorkJournal/
├── Models/
│   ├── LogEntry.cs          # Data model: Date, Content, LastModified
│   └── WorkWeek.cs          # Mon–Fri container with navigation helpers
├── Services/
│   ├── LogFileService.cs    # File I/O: load / upsert / atomic write
│   └── AutoSaveService.cs   # DispatcherTimer wrapper (5-min auto-save)
├── ViewModels/
│   ├── ViewModelBase.cs     # INotifyPropertyChanged base + SetProperty<T>
│   ├── RelayCommand.cs      # ICommand delegate implementation
│   ├── LogEntryViewModel.cs # Per-day VM: Content, IsDirty, MarkSaved
│   ├── LogFileViewerViewModel.cs # Raw log viewer VM: copy / refresh / status
│   └── MainViewModel.cs     # Root VM: week state, commands, save logic
├── Views/
│   ├── MainWindow.xaml      # UI layout (WPF XAML)
│   ├── MainWindow.xaml.cs   # Code-behind (Closing / Calendar events only)
│   ├── LogFileViewerWindow.xaml # Read-only raw log viewer UI
│   └── LogFileViewerWindow.xaml.cs # Minimal viewer window code-behind
├── App.xaml                 # Application resources & styles
├── App.xaml.cs              # Application entry point
├── AssemblyInfo.cs          # Assembly metadata
├── DailyWorkJournal.csproj  # MSBuild project file (net8.0-windows)
└── DailyWorkJournal.slnx    # Visual Studio solution file
```

### Code Style

- **C# 12** language features with nullable reference types enabled (`<Nullable>enable</Nullable>`).
- All public types and members must have **XML documentation comments** (`/// <summary>…`).
- MVVM is strictly observed: no business logic in code-behind; code-behind handles only UI events.
- `RelayCommand` is used for all `ICommand` bindings; no anonymous event handlers in XAML.
- Error handling uses try/catch in service methods; exceptions are surfaced to the user via the status bar.

### Adding a New Feature

1. **Model change** — add/modify properties in `Models/LogEntry.cs` or `Models/WorkWeek.cs`. Update `LogFileService` parsing/serialisation accordingly.
2. **New service** — create a new class under `Services/`, following the pattern established by `AutoSaveService.cs`.
3. **New ViewModel property** — add to the relevant view-model and call `OnPropertyChanged` (or use `SetProperty<T>`).
4. **UI update** — bind to the new property in `Views/MainWindow.xaml`; prefer data triggers over code-behind.

### Running the Build

```bash
# Debug
dotnet build DailyWorkJournal.csproj

# Release
dotnet build DailyWorkJournal.csproj -c Release
```

### Contributing

1. Fork the repository.
2. Create a feature branch: `git checkout -b feature/my-new-feature`.
3. Commit your changes with descriptive messages.
4. Open a pull request against `main`.

Please ensure all new code includes XML documentation comments, and that the project builds without warnings before submitting a PR.

---

*Daily Work Journal — built with C#, WPF, and .NET 8*
