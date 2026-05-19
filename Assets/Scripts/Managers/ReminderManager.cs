using UnityEngine;

public class ReminderManager : MonoBehaviour
{
    public static ReminderManager Instance { get; private set; }

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

    private void EnsureAlarmManager()
    {
        if (AlarmManager.Instance != null) return;

        AlarmManager existing = FindObjectOfType<AlarmManager>();
        if (existing != null) return;

        gameObject.AddComponent<AlarmManager>();
    }
}
