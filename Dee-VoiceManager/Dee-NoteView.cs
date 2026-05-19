using UnityEngine;

public class NoteView : MonoBehaviour
{
    public NoteData Data { get; private set; }

    void Start()
    {
        // Auto-create test data so voice buttons work immediately
        if (Data == null)
        {
            Data = new NoteData();
            Debug.Log("[NoteView] Auto-created NoteData: " + Data.id);
        }
    }

    public void Init(NoteData data)
    {
        Data = data;
    }

    public void PlayVoice()
    {
        VoiceNoteManager.Instance?.PlayFromNote(Data);
    }

    public void SaveAndRefresh()
    {
        Debug.Log("[NoteView] SaveAndRefresh called for: "
            + (Data != null ? Data.id : "null"));
    }
}