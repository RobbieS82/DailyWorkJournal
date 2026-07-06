using System;

namespace DailyWorkJournal.Models;

/// <summary>
/// Represents a single day's work log entry.
/// Stores the date, content (bullet-point notes), and last modification timestamp.
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Gets or sets the date this log entry represents.
    /// Only the date component is significant; time is ignored.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Gets or sets the text content of the log entry.
    /// Typically contains bullet-point notes about the day's work.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the local timestamp of the last save/modification.
    /// Updated each time the entry is persisted to disk.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Initializes a new <see cref="LogEntry"/> with default values.
    /// </summary>
    public LogEntry()
    {
        Date = DateTime.Today;
        LastModified = DateTime.Now;
    }

    /// <summary>
    /// Initializes a new <see cref="LogEntry"/> for the specified date.
    /// </summary>
    /// <param name="date">The date this entry represents.</param>
    public LogEntry(DateTime date)
    {
        Date = date.Date;
        LastModified = DateTime.Now;
    }

    /// <summary>
    /// Returns a string representation of the log entry for debugging purposes.
    /// </summary>
    /// <returns>A string containing the date and content preview.</returns>
    public override string ToString()
        => $"[{Date:yyyy-MM-dd}] {(Content.Length > 50 ? Content[..50] + "..." : Content)}";
}
