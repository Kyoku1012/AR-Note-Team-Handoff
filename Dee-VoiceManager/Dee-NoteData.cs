using UnityEngine;

[System.Serializable]
public class NoteData
{
    // --- Core Identity ---
    public string id;               // Used by team project (NoteManager, NoteView)
    public string noteId;           // Your original field - kept for compatibility

    // --- Member 2: Task Management ---
    public string title;
    public string content;
    public string annotation;
    public string textContent;
    public bool isChecked;
    public bool isCompleted;
    public bool isVisible;

    // --- Member 3: Custom Styling ---
    public string colorLabel;
    public string iconName;
    public int priorityLevel;

    // --- Member 4: Reminders ---
    public bool hasReminder;
    public string reminderTime;

    // --- Member 5: Voice Notes ---
    public bool hasVoiceNote;
    public string voiceFilePath;

    // --- Member 1: AR Placement ---
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;

    public Vector3 WorldPosition
    {
        get => new Vector3(posX, posY, posZ);
        set { posX = value.x; posY = value.y; posZ = value.z; }
    }

    public Quaternion WorldRotation
    {
        get => new Quaternion(rotX, rotY, rotZ, rotW);
        set { rotX = value.x; rotY = value.y; rotZ = value.z; rotW = value.w; }
    }

    public NoteData()
    {
        id = System.Guid.NewGuid().ToString();
        noteId = id;
        colorLabel = "yellow";
        priorityLevel = 1;
        isChecked = false;
        isCompleted = false;
        isVisible = true;
        hasReminder = false;
        hasVoiceNote = false;
    }
}