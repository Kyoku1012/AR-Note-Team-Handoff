using UnityEngine;

[System.Serializable]
public class NoteData
{
    public string id;
    public string title;
    public string content;
    public string annotation;

    public bool isCompleted;
    public bool isVisible = true;

    public string colorLabel;
    public string colorName;
    public string iconId;
    public string priorityId;

    public bool hasReminder;
    public string reminderTime; // yyyy-MM-dd HH:mm, local device time

    public bool hasAlarm;
    public string alarmTime; // yyyy-MM-dd HH:mm, local device time
    public string alarmRepeatRule; // none, daily, weekly
    public string alarmStatus; // none, scheduled, fired, dismissed, snoozed
    public string alarmSnoozeUntil; // yyyy-MM-dd HH:mm, local device time
    public int alarmSnoozeMinutes = 5;
    public string alarmLastFiredTime;

    public bool hasVoiceNote;
    public string voiceFilePath;

    public bool hasTranscript;
    public string transcriptText;
    public string transcriptSource; // title, content, annotation
    public string transcriptUpdatedAt;

    public Vector3 worldPosition;
    public Vector3 worldRotation;

    public NoteData Clone()
    {
        return new NoteData
        {
            id = id,
            title = title,
            content = content,
            annotation = annotation,
            isCompleted = isCompleted,
            isVisible = isVisible,
            colorLabel = colorLabel,
            colorName = colorName,
            iconId = iconId,
            priorityId = priorityId,
            hasReminder = hasReminder,
            reminderTime = reminderTime,
            hasAlarm = hasAlarm,
            alarmTime = alarmTime,
            alarmRepeatRule = alarmRepeatRule,
            alarmStatus = alarmStatus,
            alarmSnoozeUntil = alarmSnoozeUntil,
            alarmSnoozeMinutes = alarmSnoozeMinutes,
            alarmLastFiredTime = alarmLastFiredTime,
            hasVoiceNote = hasVoiceNote,
            voiceFilePath = voiceFilePath,
            hasTranscript = hasTranscript,
            transcriptText = transcriptText,
            transcriptSource = transcriptSource,
            transcriptUpdatedAt = transcriptUpdatedAt,
            worldPosition = worldPosition,
            worldRotation = worldRotation
        };
    }

    public void ApplyDefaults()
    {
        if (string.IsNullOrWhiteSpace(id))
            id = System.Guid.NewGuid().ToString();

        if (string.IsNullOrWhiteSpace(title))
            title = "New Note";

        if (content == null)
            content = "";

        if (annotation == null)
            annotation = "";

        if (string.IsNullOrWhiteSpace(colorName))
            colorName = string.IsNullOrWhiteSpace(colorLabel) ? "yellow" : colorLabel;

        if (iconId == null)
            iconId = "";

        if (priorityId == null)
            priorityId = "";

        if (reminderTime == null)
            reminderTime = "";

        if (!hasAlarm && hasReminder)
        {
            hasAlarm = true;
            alarmTime = reminderTime;
        }

        if (alarmTime == null)
            alarmTime = "";

        if (string.IsNullOrWhiteSpace(alarmRepeatRule))
            alarmRepeatRule = "none";

        if (string.IsNullOrWhiteSpace(alarmStatus))
            alarmStatus = hasAlarm ? "scheduled" : "none";

        if (alarmSnoozeUntil == null)
            alarmSnoozeUntil = "";

        if (alarmSnoozeMinutes <= 0)
            alarmSnoozeMinutes = 5;

        if (alarmLastFiredTime == null)
            alarmLastFiredTime = "";

        hasReminder = hasAlarm;
        reminderTime = alarmTime;

        if (voiceFilePath == null)
            voiceFilePath = "";

        if (transcriptText == null)
            transcriptText = "";

        if (transcriptSource == null)
            transcriptSource = "";

        if (transcriptUpdatedAt == null)
            transcriptUpdatedAt = "";

        hasTranscript = !string.IsNullOrWhiteSpace(transcriptText);
        colorLabel = colorName;
    }
}
