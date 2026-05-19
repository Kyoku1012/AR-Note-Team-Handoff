using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

public class AlarmManager : MonoBehaviour
{
    public static AlarmManager Instance { get; private set; }

    public const string TimeFormat = "yyyy-MM-dd HH:mm";
    public const string RepeatNone = "none";
    public const string RepeatDaily = "daily";
    public const string RepeatWeekly = "weekly";
    public const string StatusNone = "none";
    public const string StatusScheduled = "scheduled";
    public const string StatusFired = "fired";
    public const string StatusDismissed = "dismissed";
    public const string StatusSnoozed = "snoozed";

    private const string ChannelId = "ar_notes_alarms";

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

    public static bool TryParseAlarmTime(string value, out DateTime dateTime)
    {
        return DateTime.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime)
            || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dateTime);
    }

    public static string FormatAlarmTime(DateTime dateTime)
    {
        return dateTime.ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

    public void ScheduleAll(IEnumerable<NoteData> notes)
    {
        if (notes == null) return;

        foreach (NoteData note in notes)
            ScheduleOrCancel(note);
    }

    public void ScheduleOrCancel(NoteData note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.id)) return;

        note.ApplyDefaults();
        if (!note.hasAlarm || string.IsNullOrWhiteSpace(note.alarmTime) || !TryParseAlarmTime(note.alarmTime, out DateTime fireTime))
        {
            Cancel(note);
            MarkAlarmDisabled(note);
            return;
        }

        fireTime = MoveToNextFutureFireTime(note, fireTime);
        if (fireTime <= DateTime.Now)
        {
            Cancel(note);
            note.alarmStatus = StatusFired;
            note.alarmLastFiredTime = FormatAlarmTime(DateTime.Now);
            SyncLegacyReminderFields(note);
            return;
        }

        note.alarmTime = FormatAlarmTime(fireTime);
        note.alarmStatus = StatusScheduled;
        SyncLegacyReminderFields(note);
        Schedule(note, fireTime);
    }

    public void Cancel(NoteData note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.id)) return;

#if UNITY_ANDROID
        int notificationId = GetNotificationId(note.id);
        AndroidNotificationCenter.CancelScheduledNotification(notificationId);
        AndroidNotificationCenter.CancelDisplayedNotification(notificationId);
#endif
    }

    public void Dismiss(NoteData note)
    {
        if (note == null) return;

        Cancel(note);
        note.hasAlarm = false;
        note.alarmStatus = StatusDismissed;
        note.alarmSnoozeUntil = "";
        SyncLegacyReminderFields(note);
    }

    public void Snooze(NoteData note, int minutes)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.id)) return;

        int snoozeMinutes = Mathf.Max(1, minutes);
        DateTime fireTime = DateTime.Now.AddMinutes(snoozeMinutes);

        note.hasAlarm = true;
        note.alarmTime = FormatAlarmTime(fireTime);
        note.alarmSnoozeUntil = note.alarmTime;
        note.alarmSnoozeMinutes = snoozeMinutes;
        note.alarmStatus = StatusSnoozed;
        SyncLegacyReminderFields(note);
        Schedule(note, fireTime);
    }

    public static string NormalizeRepeatRule(string repeatRule)
    {
        if (string.Equals(repeatRule, RepeatDaily, StringComparison.OrdinalIgnoreCase))
            return RepeatDaily;

        if (string.Equals(repeatRule, RepeatWeekly, StringComparison.OrdinalIgnoreCase))
            return RepeatWeekly;

        return RepeatNone;
    }

    private DateTime MoveToNextFutureFireTime(NoteData note, DateTime fireTime)
    {
        string repeatRule = NormalizeRepeatRule(note.alarmRepeatRule);
        note.alarmRepeatRule = repeatRule;

        if (repeatRule == RepeatNone)
            return fireTime;

        DateTime now = DateTime.Now;
        while (fireTime <= now)
        {
            fireTime = repeatRule == RepeatDaily ? fireTime.AddDays(1) : fireTime.AddDays(7);
        }

        return fireTime;
    }

    private void Schedule(NoteData note, DateTime fireTime)
    {
#if UNITY_ANDROID
        Cancel(note);

        AndroidNotification notification = new AndroidNotification
        {
            Title = string.IsNullOrWhiteSpace(note.title) ? "AR Note alarm" : note.title,
            Text = string.IsNullOrWhiteSpace(note.content) ? "You have a saved AR note alarm." : note.content,
            FireTime = fireTime,
            SmallIcon = "default",
            LargeIcon = "default",
            IntentData = note.id
        };

        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, GetNotificationId(note.id));
#else
        Debug.Log($"Alarm scheduled for {note.title} at {fireTime}.");
#endif
    }

    private void RegisterChannel()
    {
#if UNITY_ANDROID
        AndroidNotificationChannel channel = new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = "AR Note Alarms",
            Importance = Importance.High,
            Description = "Alarms for AR sticky notes and tasks"
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

    private void MarkAlarmDisabled(NoteData note)
    {
        note.hasAlarm = false;
        note.alarmStatus = StatusNone;
        note.alarmSnoozeUntil = "";
        SyncLegacyReminderFields(note);
    }

    private void SyncLegacyReminderFields(NoteData note)
    {
        note.hasReminder = note.hasAlarm;
        note.reminderTime = note.alarmTime ?? "";
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
