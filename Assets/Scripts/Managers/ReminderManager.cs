using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class ReminderManager : MonoBehaviour
{
    public static ReminderManager Instance { get; private set; }

    private const string ChannelId = "ar_notes_reminders";
    private const string ReminderFormat = "yyyy-MM-dd HH:mm";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        RegisterChannel();
    }

    public static bool TryParseReminderTime(string value, out DateTime dateTime)
    {
        return DateTime.TryParseExact(value, ReminderFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dateTime);
    }

    public static string FormatReminderTime(DateTime dateTime)
    {
        return dateTime.ToString(ReminderFormat, CultureInfo.InvariantCulture);
    }

    public void RescheduleAll(IEnumerable<NoteData> notes)
    {
        if (notes == null) return;

        foreach (NoteData note in notes)
            ScheduleOrCancel(note);
    }

    public void ScheduleOrCancel(NoteData note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.id)) return;

        if (!note.hasReminder || string.IsNullOrWhiteSpace(note.reminderTime) || !TryParseReminderTime(note.reminderTime, out DateTime fireTime))
        {
            Cancel(note);
            return;
        }

        if (fireTime <= DateTime.Now)
        {
            Cancel(note);
            return;
        }

        Schedule(note, fireTime);
    }

    public void Cancel(NoteData note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.id)) return;

#if UNITY_ANDROID
        AndroidNotificationCenter.CancelScheduledNotification(GetNotificationId(note.id));
        AndroidNotificationCenter.CancelDisplayedNotification(GetNotificationId(note.id));
#endif
    }

    private void Schedule(NoteData note, DateTime fireTime)
    {
#if UNITY_ANDROID
        Cancel(note);

        AndroidNotification notification = new AndroidNotification
        {
            Title = string.IsNullOrWhiteSpace(note.title) ? "AR Note reminder" : note.title,
            Text = string.IsNullOrWhiteSpace(note.content) ? "You have a saved AR note reminder." : note.content,
            FireTime = fireTime,
            SmallIcon = "default",
            LargeIcon = "default"
        };

        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, GetNotificationId(note.id));
#else
        Debug.Log($"Reminder scheduled for {note.title} at {fireTime}.");
#endif
    }

    private void RegisterChannel()
    {
#if UNITY_ANDROID
        AndroidNotificationChannel channel = new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = "AR Note Reminders",
            Importance = Importance.High,
            Description = "Reminders for AR sticky notes and tasks"
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    private int GetNotificationId(string noteId)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in noteId)
                hash = hash * 31 + c;

            return hash & int.MaxValue;
        }
    }
}
