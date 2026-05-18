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

    public bool hasVoiceNote;
    public string voiceFilePath;

    public Vector3 worldPosition;
    public Vector3 worldRotation;

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

        if (voiceFilePath == null)
            voiceFilePath = "";

        colorLabel = colorName;
    }
}
