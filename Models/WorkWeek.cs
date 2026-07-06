using System;
using System.Collections.Generic;
using System.Linq;

namespace DailyWorkJournal.Models;

/// <summary>
/// Represents a Monday-to-Friday work week, containing up to five <see cref="LogEntry"/> instances.
/// Provides helpers for navigating between weeks and retrieving the entry for a specific day.
/// </summary>
public class WorkWeek
{
    /// <summary>Gets the Monday date that anchors this work week.</summary>
    public DateTime WeekStart { get; }

    /// <summary>Gets the Friday date that closes this work week.</summary>
    public DateTime WeekEnd { get; }

    /// <summary>
    /// Gets the five <see cref="LogEntry"/> objects for Monday through Friday.
    /// Entries are always pre-populated (with empty content) so the UI always
    /// has a model to bind to for each day.
    /// </summary>
    public IReadOnlyList<LogEntry> Entries { get; }

    /// <summary>
    /// Initializes a new <see cref="WorkWeek"/> anchored on the Monday
    /// of the week that contains <paramref name="anyDateInWeek"/>.
    /// </summary>
    /// <param name="anyDateInWeek">Any date belonging to the desired week.</param>
    public WorkWeek(DateTime anyDateInWeek)
    {
        WeekStart = GetMondayOfWeek(anyDateInWeek);
        WeekEnd = WeekStart.AddDays(4); // Friday

        var entries = new List<LogEntry>(5);
        for (int i = 0; i < 5; i++)
        {
            entries.Add(new LogEntry(WeekStart.AddDays(i)));
        }
        Entries = entries.AsReadOnly();
    }

    /// <summary>
    /// Returns a display label for this work week, e.g. "Week of Jul 7 – Jul 11, 2026".
    /// </summary>
    public string WeekLabel =>
        $"Week of {WeekStart:MMM d} \u2013 {WeekEnd:MMM d, yyyy}";

    /// <summary>
    /// Returns the <see cref="LogEntry"/> for the specified <paramref name="date"/>,
    /// or <c>null</c> if the date does not fall within this work week.
    /// </summary>
    /// <param name="date">The date whose entry is requested.</param>
    /// <returns>The matching <see cref="LogEntry"/>, or <c>null</c>.</returns>
    public LogEntry? GetEntryForDate(DateTime date)
        => Entries.FirstOrDefault(e => e.Date.Date == date.Date);

    /// <summary>
    /// Determines whether this work week contains the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><c>true</c> if the date is Monday–Friday of this week.</returns>
    public bool ContainsDate(DateTime date)
        => date.Date >= WeekStart && date.Date <= WeekEnd;

    /// <summary>
    /// Calculates the Monday of the week containing <paramref name="date"/>.
    /// </summary>
    /// <param name="date">Any date.</param>
    /// <returns>The Monday of the same ISO week.</returns>
    public static DateTime GetMondayOfWeek(DateTime date)
    {
        int dayOfWeek = (int)date.DayOfWeek;
        // DayOfWeek.Sunday == 0; we treat Sunday as "day 7" so Monday is always day 1
        int daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
        return date.Date.AddDays(-daysFromMonday);
    }
}
