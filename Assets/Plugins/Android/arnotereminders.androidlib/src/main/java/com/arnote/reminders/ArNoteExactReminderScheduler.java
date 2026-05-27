package com.arnote.reminders;

import android.app.AlarmManager;
import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;

public class ArNoteExactReminderScheduler {
    public static boolean schedule(
        Context context,
        int notificationId,
        long triggerAtMillis,
        String noteId,
        String title,
        String text,
        String channelId,
        String group) {

        if (context == null)
            return false;

        Context appContext = context.getApplicationContext();
        AlarmManager alarmManager = (AlarmManager)appContext.getSystemService(Context.ALARM_SERVICE);
        if (alarmManager == null)
            return false;

        PendingIntent broadcast = createBroadcast(
            appContext,
            notificationId,
            noteId,
            title,
            text,
            channelId,
            group);

        Intent showIntent = appContext.getPackageManager().getLaunchIntentForPackage(appContext.getPackageName());
        if (showIntent == null)
            showIntent = new Intent();

        showIntent.putExtra(ArNoteExactReminderReceiver.EXTRA_NOTE_ID, noteId);
        PendingIntent alarmInfoIntent = PendingIntent.getActivity(
            appContext,
            notificationId,
            showIntent,
            PendingIntent.FLAG_UPDATE_CURRENT | ArNoteExactReminderReceiver.immutableFlag());

        AlarmManager.AlarmClockInfo alarmClockInfo = new AlarmManager.AlarmClockInfo(triggerAtMillis, alarmInfoIntent);
        alarmManager.setAlarmClock(alarmClockInfo, broadcast);
        return true;
    }

    public static void cancel(Context context, int notificationId) {
        if (context == null)
            return;

        Context appContext = context.getApplicationContext();
        AlarmManager alarmManager = (AlarmManager)appContext.getSystemService(Context.ALARM_SERVICE);
        PendingIntent broadcast = createBroadcast(appContext, notificationId, "", "", "", "", "");
        if (alarmManager != null)
            alarmManager.cancel(broadcast);

        android.app.NotificationManager notificationManager =
            (android.app.NotificationManager)appContext.getSystemService(Context.NOTIFICATION_SERVICE);
        if (notificationManager != null)
            notificationManager.cancel(notificationId);
    }

    private static PendingIntent createBroadcast(
        Context context,
        int notificationId,
        String noteId,
        String title,
        String text,
        String channelId,
        String group) {

        Intent intent = new Intent(context, ArNoteExactReminderReceiver.class);
        intent.putExtra(ArNoteExactReminderReceiver.EXTRA_NOTIFICATION_ID, notificationId);
        intent.putExtra(ArNoteExactReminderReceiver.EXTRA_NOTE_ID, noteId);
        intent.putExtra(ArNoteExactReminderReceiver.EXTRA_TITLE, title);
        intent.putExtra(ArNoteExactReminderReceiver.EXTRA_TEXT, text);
        intent.putExtra(ArNoteExactReminderReceiver.EXTRA_CHANNEL_ID, channelId);
        intent.putExtra(ArNoteExactReminderReceiver.EXTRA_GROUP, group);

        return PendingIntent.getBroadcast(
            context,
            notificationId,
            intent,
            PendingIntent.FLAG_UPDATE_CURRENT | ArNoteExactReminderReceiver.immutableFlag());
    }
}
