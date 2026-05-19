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

    private const string ChannelId = "ar_notes_urgent_reminders_v3";
    private const string LegacyChannelId = "ar_notes_alarms";
    private const string PreviousUrgentChannelId = "ar_notes_urgent_reminders_v1";
    private const string PreviousUrgentChannelIdV2 = "ar_notes_urgent_reminders_v2";

#if UNITY_ANDROID
    private PermissionRequest permissionRequest;
    private bool exactSchedulingRequestOpened;
    private bool batteryOptimizationRequestOpened;
    private string lastHandledNotificationNoteId;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        RegisterChannel();
        ProcessLastNotificationIntent();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

        RegisterChannel();
#if UNITY_ANDROID
        ProcessLastNotificationIntent();

        if (AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed && AndroidNotificationCenter.UsingExactScheduling)
        {
            ReminderManager.Instance?.RescheduleAll(NoteManager.Instance?.GetAllNotes());
            NoteManager.Instance?.SaveNotes();
        }
#endif
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
        if (!note.hasReminder && !note.hasAlarm)
        {
            Cancel(note);
            MarkAlarmDisabled(note);
            return;
        }

        if (string.IsNullOrWhiteSpace(note.alarmTime))
            note.alarmTime = note.reminderTime;

        note.hasAlarm = true;
        note.hasReminder = true;

        if (string.IsNullOrWhiteSpace(note.alarmTime) || !TryParseAlarmTime(note.alarmTime, out DateTime fireTime))
        {
            Cancel(note);
            note.hasAlarm = false;
            note.hasReminder = false;
            note.alarmTime = "";
            note.reminderTime = "";
            note.alarmStatus = StatusNone;
            SyncReminderFields(note);
            return;
        }

        fireTime = MoveToNextFutureFireTime(note, fireTime);
        if (fireTime <= DateTime.Now)
        {
            Cancel(note);
            note.alarmStatus = StatusFired;
            note.alarmLastFiredTime = FormatAlarmTime(DateTime.Now);
            SyncReminderFields(note);
            return;
        }

        note.alarmTime = FormatAlarmTime(fireTime);
        note.alarmStatus = Schedule(note, fireTime) ? StatusScheduled : StatusNone;
        SyncReminderFields(note);
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
        SyncReminderFields(note);
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
        SyncReminderFields(note);
        note.alarmStatus = Schedule(note, fireTime) ? StatusSnoozed : StatusNone;
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

    private bool Schedule(NoteData note, DateTime fireTime)
    {
#if UNITY_ANDROID
        if (!EnsurePostPermissionReady())
        {
            Debug.LogWarning("Notification permission is not available; alarm was saved but not scheduled.");
            return false;
        }

        if (!EnsureExactSchedulingReady())
        {
            Debug.LogWarning("Exact alarm permission is required for on-time reminders. Grant it and return to the app; reminders will be rescheduled automatically.");
            return false;
        }

        RequestBatteryOptimizationExemptionIfNeeded();
        Cancel(note);

        string body = GetNotificationBody(note);
        AndroidNotification notification = new AndroidNotification
        {
            Title = GetNotificationTitle(note),
            Text = body,
            FireTime = fireTime,
            SmallIcon = "default",
            LargeIcon = "default",
            IntentData = note.id,
            Style = NotificationStyle.BigTextStyle,
            ShouldAutoCancel = true,
            ShowTimestamp = true,
            ShowInForeground = true,
            Group = "ar_notes_reminders",
            Color = new Color(1f, 0.82f, 0.12f, 1f)
        };

        AndroidNotificationCenter.SendNotificationWithExplicitID(notification, ChannelId, GetNotificationId(note.id));
        return true;
#else
        Debug.Log($"Alarm scheduled for {note.title} at {fireTime}.");
        return true;
#endif
    }

    private void RegisterChannel()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.DeleteNotificationChannel(LegacyChannelId);
        AndroidNotificationCenter.DeleteNotificationChannel(PreviousUrgentChannelId);
        AndroidNotificationCenter.DeleteNotificationChannel(PreviousUrgentChannelIdV2);

        AndroidNotificationChannel channel = new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = "Urgent Reminders",
            Importance = Importance.High,
            Description = "Urgent reminders for AR sticky notes and tasks",
            LockScreenVisibility = LockScreenVisibility.Public,
            EnableVibration = true,
            EnableLights = true,
            CanShowBadge = true
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif
    }

#if UNITY_ANDROID
    private void ProcessLastNotificationIntent()
    {
        AndroidNotificationIntentData data = AndroidNotificationCenter.GetLastNotificationIntent();
        if (data == null)
            return;

        string noteId = data.Notification.IntentData;
        if (string.IsNullOrWhiteSpace(noteId) || noteId == lastHandledNotificationNoteId)
            return;

        lastHandledNotificationNoteId = noteId;
        NoteManager.Instance?.OpenNoteFromNotification(noteId);
    }
#endif

    private void MarkAlarmDisabled(NoteData note)
    {
        note.hasAlarm = false;
        note.hasReminder = false;
        note.alarmTime = "";
        note.reminderTime = "";
        note.alarmStatus = StatusNone;
        note.alarmSnoozeUntil = "";
        SyncReminderFields(note);
    }

    private void SyncReminderFields(NoteData note)
    {
        if (!string.IsNullOrWhiteSpace(note.alarmTime))
            note.reminderTime = note.alarmTime;

        if (note.reminderTime == null)
            note.reminderTime = "";

        note.hasReminder = !string.IsNullOrWhiteSpace(note.reminderTime);
    }

    private string GetNotificationBody(NoteData note)
    {
        if (note == null)
            return "You have a saved AR note reminder.";

        if (!string.IsNullOrWhiteSpace(note.content))
            return note.content;

        if (!string.IsNullOrWhiteSpace(note.title))
            return note.title;

        return "You have a saved AR note reminder.";
    }

    private string GetNotificationTitle(NoteData note)
    {
        if (note == null || string.IsNullOrWhiteSpace(note.title) || note.title == "New Note")
            return "Reminder";

        return "Reminder: " + note.title;
    }

#if UNITY_ANDROID
    private bool EnsurePostPermissionReady()
    {
        PermissionStatus status = AndroidNotificationCenter.UserPermissionToPost;
        if (status == PermissionStatus.Allowed)
            return true;

        if (status == PermissionStatus.NotRequested || status == PermissionStatus.Denied || status == PermissionStatus.DeniedDontAskAgain)
        {
            permissionRequest = new PermissionRequest();
            return permissionRequest.Status == PermissionStatus.Allowed;
        }

        return false;
    }

    private bool EnsureExactSchedulingReady()
    {
        if (AndroidNotificationCenter.UsingExactScheduling)
            return true;

        if (!exactSchedulingRequestOpened)
        {
            exactSchedulingRequestOpened = true;
            AndroidNotificationCenter.RequestExactScheduling();
        }

        return AndroidNotificationCenter.UsingExactScheduling;
    }

    private void RequestBatteryOptimizationExemptionIfNeeded()
    {
        if (batteryOptimizationRequestOpened || AndroidNotificationCenter.IgnoringBatteryOptimizations)
            return;

        batteryOptimizationRequestOpened = true;
        AndroidNotificationCenter.RequestIgnoreBatteryOptimizations();
    }
#endif

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
