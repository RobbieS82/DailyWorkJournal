using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DailyWorkJournal.Models;

namespace DailyWorkJournal.Services;

/// <summary>
/// Handles all file I/O operations for the Daily Work Journal log file.
/// The log file is stored at <c>%APPDATA%\DailyWorkJournal\logs\daily-log.log</c>
/// in a structured, AI-friendly plain-text format.
/// </summary>
public static class LogFileService
{
    // ──────────────────────────────────────────────────────────────────────────
    // Constants
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>The folder name used under %APPDATA%.</summary>
    private const string AppFolderName = "DailyWorkJournal";

    /// <summary>The sub-folder that contains the log file.</summary>
    private const string LogSubFolder = "logs";

    /// <summary>The name of the aggregate log file.</summary>
    private const string LogFileName = "daily-log.log";

    /// <summary>
    /// Regex that matches the opening delimiter of an entry, capturing the ISO date.
    /// Example: <c>===== ENTRY: 2026-07-07 (Monday) =====</c>
    /// </summary>
    private static readonly Regex EntryHeaderRegex = new(
        @"^===== ENTRY: (\d{4}-\d{2}-\d{2}) \(\w+\) =====$",
        RegexOptions.Compiled);

    /// <summary>
    /// Regex that matches the LAST_MODIFIED metadata line.
    /// Example: <c>LAST_MODIFIED: 2026-07-07T14:30:00</c>
    /// </summary>
    private static readonly Regex LastModifiedRegex = new(
        @"^LAST_MODIFIED: (.+)$",
        RegexOptions.Compiled);

    /// <summary>The closing delimiter for an entry block.</summary>
    private const string EntryEnd = "===== END ENTRY =====";

    /// <summary>The content section header.</summary>
    private const string ContentHeader = "CONTENT:";

    // ──────────────────────────────────────────────────────────────────────────
    // Public Properties
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the fully-qualified path to the log file.
    /// Typically <c>%APPDATA%\DailyWorkJournal\logs\daily-log.log</c>.
    /// </summary>
    public static string LogFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName,
        LogSubFolder,
        LogFileName);

    // ──────────────────────────────────────────────────────────────────────────
    // Initialisation
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Ensures the directory containing the log file exists.
    /// Creates it (and any parent directories) if absent.
    /// Safe to call multiple times; does nothing when the directory already exists.
    /// </summary>
    public static void EnsureLogDirectoryExists()
    {
        string? directory = Path.GetDirectoryName(LogFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Reading
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads and parses the entire log file, returning a dictionary keyed by
    /// normalised date (<c>yyyy-MM-dd</c>) mapped to its <see cref="LogEntry"/>.
    /// Returns an empty dictionary when the file does not exist or is empty.
    /// </summary>
    /// <returns>
    /// A <see cref="Dictionary{TKey,TValue}"/> mapping date strings to
    /// <see cref="LogEntry"/> objects.
    /// </returns>
    /// <exception cref="IOException">Thrown if the file cannot be read.</exception>
    public static Dictionary<string, LogEntry> LoadAllEntries()
    {
        var entries = new Dictionary<string, LogEntry>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(LogFilePath))
            return entries;

        string[] lines = File.ReadAllLines(LogFilePath, Encoding.UTF8);

        LogEntry? current = null;
        bool inContent = false;
        var contentBuilder = new StringBuilder();

        foreach (string line in lines)
        {
            // ── Opening delimiter ────────────────────────────────────────────
            Match headerMatch = EntryHeaderRegex.Match(line);
            if (headerMatch.Success)
            {
                // Save any previously buffered entry
                if (current != null)
                {
                    current.Content = contentBuilder.ToString().TrimEnd('\r', '\n');
                    entries[current.Date.ToString("yyyy-MM-dd")] = current;
                }

                if (DateTime.TryParse(headerMatch.Groups[1].Value, out DateTime date))
                {
                    current = new LogEntry(date);
                }
                inContent = false;
                contentBuilder.Clear();
                continue;
            }

            // ── Closing delimiter ────────────────────────────────────────────
            if (line.Trim() == EntryEnd)
            {
                if (current != null)
                {
                    current.Content = contentBuilder.ToString().TrimEnd('\r', '\n');
                    entries[current.Date.ToString("yyyy-MM-dd")] = current;
                    current = null;
                }
                inContent = false;
                contentBuilder.Clear();
                continue;
            }

            if (current == null)
                continue;

            // ── Metadata lines ───────────────────────────────────────────────
            Match lastModMatch = LastModifiedRegex.Match(line);
            if (lastModMatch.Success)
            {
                if (DateTime.TryParse(lastModMatch.Groups[1].Value, out DateTime lm))
                    current.LastModified = lm;
                continue;
            }

            // ── Content section ──────────────────────────────────────────────
            if (line.Trim() == ContentHeader)
            {
                inContent = true;
                continue;
            }

            if (inContent)
            {
                contentBuilder.AppendLine(line);
            }
        }

        // Handle file that doesn't end with a closing delimiter
        if (current != null)
        {
            current.Content = contentBuilder.ToString().TrimEnd('\r', '\n');
            entries[current.Date.ToString("yyyy-MM-dd")] = current;
        }

        return entries;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Writing
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves a collection of <see cref="LogEntry"/> objects to the log file.
    /// Existing entries for the same dates are replaced; entries for other dates
    /// are preserved.  The file is written atomically via a temporary file to
    /// guard against data loss on crash.
    /// </summary>
    /// <param name="entriesToSave">
    /// One or more entries to upsert into the log file.
    /// Only entries with non-empty content are written.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entriesToSave"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    public static void SaveEntries(IEnumerable<LogEntry> entriesToSave)
    {
        if (entriesToSave == null) throw new ArgumentNullException(nameof(entriesToSave));

        EnsureLogDirectoryExists();

        // Load the existing file content so we can merge/upsert
        Dictionary<string, LogEntry> existing = LoadAllEntries();

        // Merge / overwrite with the entries being saved
        foreach (LogEntry entry in entriesToSave)
        {
            string key = entry.Date.ToString("yyyy-MM-dd");
            entry.LastModified = DateTime.Now;
            existing[key] = entry;
        }

        // Serialise all entries sorted by date
        var sb = new StringBuilder();
        var sortedKeys = new List<string>(existing.Keys);
        sortedKeys.Sort(StringComparer.OrdinalIgnoreCase);

        foreach (string key in sortedKeys)
        {
            LogEntry entry = existing[key];

            // Skip completely empty entries to keep the file tidy
            if (string.IsNullOrWhiteSpace(entry.Content))
                continue;

            AppendEntryBlock(sb, entry);
        }

        // Write atomically
        string tempFile = LogFilePath + ".tmp";
        File.WriteAllText(tempFile, sb.ToString(), Encoding.UTF8);
        File.Move(tempFile, LogFilePath, overwrite: true);
    }

    /// <summary>
    /// Saves a single <see cref="LogEntry"/> to the log file.
    /// </summary>
    /// <param name="entry">The entry to save.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entry"/> is <c>null</c>.
    /// </exception>
    public static void SaveEntry(LogEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));
        SaveEntries(new[] { entry });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends the formatted block for a single <see cref="LogEntry"/> to
    /// <paramref name="sb"/> using the AI-friendly delimiter format.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
    /// <param name="entry">The entry to serialise.</param>
    private static void AppendEntryBlock(StringBuilder sb, LogEntry entry)
    {
        string dayName = entry.Date.DayOfWeek.ToString();
        string dateStr = entry.Date.ToString("yyyy-MM-dd");

        sb.AppendLine($"===== ENTRY: {dateStr} ({dayName}) =====");
        sb.AppendLine($"LAST_MODIFIED: {entry.LastModified:yyyy-MM-ddTHH:mm:ss}");
        sb.AppendLine("CONTENT:");
        sb.AppendLine(entry.Content);
        sb.AppendLine("===== END ENTRY =====");
        sb.AppendLine(); // blank line between entries
    }
}
