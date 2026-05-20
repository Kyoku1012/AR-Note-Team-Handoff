using UnityEngine;

public class ReminderManager : MonoBehaviour
{
    public static ReminderManager Instance { get; private set; }

    public const string RepeatNone = AlarmManager.RepeatNone;
    public const string RepeatDaily = AlarmManager.RepeatDaily;
    public const string RepeatWeekly = AlarmManager.RepeatWeekly;
    public const string StatusNone = AlarmManager.StatusNone;
    public const string StatusScheduled = AlarmManager.StatusScheduled;
    public const string StatusFired = AlarmManager.StatusFired;
    public const string StatusDismissed = AlarmManager.StatusDismissed;
    public const string StatusSnoozed = AlarmManager.StatusSnoozed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        EnsureAlarmManager();
    }

    public static bool TryParseReminderTime(string value, out System.DateTime dateTime)
    {
        return AlarmManager.TryParseAlarmTime(value, out dateTime);
    }

    public static string FormatReminderTime(System.DateTime dateTime)
    {
        return AlarmManager.FormatAlarmTime(dateTime);
    }

    public void RescheduleAll(System.Collections.Generic.IEnumerable<NoteData> notes)
    {
        EnsureAlarmManager();
        AlarmManager.Instance?.ScheduleAll(notes);
    }

    public void ScheduleOrCancel(NoteData note)
    {
        EnsureAlarmManager();
        AlarmManager.Instance?.ScheduleOrCancel(note);
    }

    public void Cancel(NoteData note)
    {
        EnsureAlarmManager();
        AlarmManager.Instance?.Cancel(note);
    }

    public void Dismiss(NoteData note)
    {
        EnsureAlarmManager();
        AlarmManager.Instance?.Dismiss(note);
    }

    public void Snooze(NoteData note, int minutes)
    {
        EnsureAlarmManager();
        AlarmManager.Instance?.Snooze(note, minutes);
    }

    private void EnsureAlarmManager()
    {
        if (AlarmManager.Instance != null) return;

        AlarmManager existing = FindObjectOfType<AlarmManager>();
        if (existing != null) return;

        gameObject.AddComponent<AlarmManager>();
    }
}
