package com.arnote.reminders;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.os.Build;

public class ArNoteExactReminderReceiver extends BroadcastReceiver {
    static final String EXTRA_NOTIFICATION_ID = "ar_note_notification_id";
    static final String EXTRA_NOTE_ID = "ar_note_id";
    static final String EXTRA_TITLE = "ar_note_title";
    static final String EXTRA_TEXT = "ar_note_text";
    static final String EXTRA_CHANNEL_ID = "ar_note_channel_id";
    static final String EXTRA_GROUP = "ar_note_group";

    @Override
    public void onReceive(Context context, Intent intent) {
        int notificationId = intent.getIntExtra(EXTRA_NOTIFICATION_ID, 0);
        String noteId = getStringExtra(intent, EXTRA_NOTE_ID, "");
        String title = getStringExtra(intent, EXTRA_TITLE, "Reminder");
        String text = getStringExtra(intent, EXTRA_TEXT, "You have a saved AR note reminder.");
        String channelId = getStringExtra(intent, EXTRA_CHANNEL_ID, "ar_notes_urgent_reminders_v3");
        String group = getStringExtra(intent, EXTRA_GROUP, "ar_notes_reminders");

        ensureChannel(context, channelId);

        Intent openIntent = context.getPackageManager().getLaunchIntentForPackage(context.getPackageName());
        if (openIntent == null) {
            openIntent = new Intent();
            openIntent.setPackage(context.getPackageName());
        }

        openIntent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TOP | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        openIntent.putExtra(EXTRA_NOTE_ID, noteId);

        PendingIntent contentIntent = PendingIntent.getActivity(
            context,
            notificationId,
            openIntent,
            PendingIntent.FLAG_UPDATE_CURRENT | immutableFlag());

        Notification.Builder builder = Build.VERSION.SDK_INT >= Build.VERSION_CODES.O
            ? new Notification.Builder(context, channelId)
            : new Notification.Builder(context);

        builder.setContentTitle(title)
            .setContentText(text)
            .setSmallIcon(getSmallIcon(context))
            .setContentIntent(contentIntent)
            .setAutoCancel(true)
            .setShowWhen(true)
            .setWhen(System.currentTimeMillis())
            .setPriority(Notification.PRIORITY_HIGH)
            .setDefaults(Notification.DEFAULT_ALL)
            .setGroup(group);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.JELLY_BEAN)
            builder.setStyle(new Notification.BigTextStyle().bigText(text));

        NotificationManager manager = (NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE);
        manager.notify(notificationId, builder.build());
    }

    private static void ensureChannel(Context context, String channelId) {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O)
            return;

        NotificationManager manager = (NotificationManager)context.getSystemService(Context.NOTIFICATION_SERVICE);
        if (manager.getNotificationChannel(channelId) != null)
            return;

        NotificationChannel channel = new NotificationChannel(channelId, "Urgent Reminders", NotificationManager.IMPORTANCE_HIGH);
        channel.setDescription("Urgent reminders for AR sticky notes and tasks");
        channel.enableLights(true);
        channel.enableVibration(true);
        manager.createNotificationChannel(channel);
    }

    private static int getSmallIcon(Context context) {
        ApplicationInfo info = context.getApplicationInfo();
        if (info != null && info.icon != 0)
            return info.icon;

        return android.R.drawable.ic_dialog_info;
    }

    private static String getStringExtra(Intent intent, String key, String fallback) {
        String value = intent.getStringExtra(key);
        return value == null || value.length() == 0 ? fallback : value;
    }

    static int immutableFlag() {
        return Build.VERSION.SDK_INT >= Build.VERSION_CODES.M ? PendingIntent.FLAG_IMMUTABLE : 0;
    }
}
