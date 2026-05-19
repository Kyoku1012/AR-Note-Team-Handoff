using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using TMPro;

public class VoiceNoteManager : MonoBehaviour
{
    // --- Singleton ---
    public static VoiceNoteManager Instance { get; private set; }

    // --- UI References ---
    [Header("UI Buttons")]
    public Button recordButton;
    public Button stopButton;
    public Button playButton;

    [Header("UI Text")]
    public TextMeshProUGUI statusText;

    // --- Internal ---
    private AudioClip recordedClip;
    private bool isRecording = false;
    private string micDevice;
    private string lastSavedPath;

    private const int MAX_SECONDS = 60;
    private const int SAMPLE_RATE = 44100;

    // Matches the folder name in SIMULATION_GUIDE.md
    private string VoiceFolder =>
        Path.Combine(Application.persistentDataPath, "voice_notes");

    // -------------------------------------------------------
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(RequestMicPermission());
    }

    // -------------------------------------------------------
    // PERMISSION
    // -------------------------------------------------------
    private System.Collections.IEnumerator RequestMicPermission()
    {
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(
                UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(
                UnityEngine.Android.Permission.Microphone);
            yield return new WaitForSeconds(1.5f);
        }
#endif
        yield return new WaitForSeconds(0.5f);
        InitializeMicrophone();
    }

    private void InitializeMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            micDevice = Microphone.devices[0];
            SetStatus("Ready to record.");
            if (recordButton != null) recordButton.interactable = true;
        }
        else
        {
            SetStatus("No microphone detected!");
            if (recordButton != null) recordButton.interactable = false;
        }

        if (stopButton != null) stopButton.interactable = false;
        if (playButton != null) playButton.interactable = false;
    }

    // -------------------------------------------------------
    // CALLED BY NoteEditPanel — matches team project method names
    // -------------------------------------------------------

    // Toggles between recording and stopping
    // Called by NoteEditPanel's Record button
    public void ToggleRecording(NoteView note)
    {
        if (isRecording)
            StopRecording(note);
        else
            StartRecording();
    }

    // Called by NoteEditPanel's Delete voice button
    public void DeleteVoice(NoteView note)
    {
        if (note == null || note.Data == null) return;

        if (note.Data.hasVoiceNote && File.Exists(note.Data.voiceFilePath))
        {
            File.Delete(note.Data.voiceFilePath);
            Debug.Log("[VoiceNoteManager] Deleted voice file: " + note.Data.voiceFilePath);
        }

        note.Data.hasVoiceNote = false;
        note.Data.voiceFilePath = null;
        lastSavedPath = null;
        recordedClip = null;

        SetStatus("Voice memo deleted.");
        if (playButton != null) playButton.interactable = false;
    }

    // -------------------------------------------------------
    // RECORD / STOP
    // -------------------------------------------------------
    private void StartRecording()
    {
        if (micDevice == null)
        {
            SetStatus("No microphone available.");
            return;
        }

        recordedClip = Microphone.Start(micDevice, false, MAX_SECONDS, SAMPLE_RATE);
        isRecording = true;

        SetStatus("Recording...");
        if (recordButton != null) recordButton.interactable = false;
        if (stopButton != null)  stopButton.interactable = true;
        if (playButton != null)  playButton.interactable = false;
    }

    private void StopRecording(NoteView note)
    {
        if (!isRecording) return;

        Microphone.End(micDevice);
        isRecording = false;

        lastSavedPath = SaveAudioToFile(recordedClip);

        // Immediately attach to the note so it saves with the note
        if (note != null && note.Data != null)
        {
            note.Data.hasVoiceNote = true;
            note.Data.voiceFilePath = lastSavedPath;
        }

        SetStatus("Recording saved!");
        if (recordButton != null) recordButton.interactable = true;
        if (stopButton != null)  stopButton.interactable = false;
        if (playButton != null)  playButton.interactable = true;
    }

    // -------------------------------------------------------
    // PLAY
    // -------------------------------------------------------

    // Called by NoteView.PlayVoice() — plays voice for a specific note
    public void PlayFromNote(NoteData data)
    {
        if (data == null || !data.hasVoiceNote)
        {
            SetStatus("No voice note for this note.");
            return;
        }

        if (!File.Exists(data.voiceFilePath))
        {
            SetStatus("Voice file missing.");
            Debug.LogWarning("[VoiceNoteManager] File not found: " + data.voiceFilePath);
            return;
        }

        StartCoroutine(LoadAndPlay(data.voiceFilePath));
    }

    // Called by the standalone Play button (plays last recorded clip)
    public void PlayRecording()
    {
        if (recordedClip == null)
        {
            SetStatus("Nothing to play.");
            return;
        }

        AudioSource tempSource = gameObject.AddComponent<AudioSource>();
        tempSource.clip = recordedClip;
        tempSource.Play();

        SetStatus("Playing...");
        StartCoroutine(CleanupAfterPlay(tempSource, recordedClip.length));
    }

    // -------------------------------------------------------
    // LOAD — call when opening an existing note in the panel
    // -------------------------------------------------------
    public void LoadFromNote(NoteData data)
    {
        if (data != null && data.hasVoiceNote && File.Exists(data.voiceFilePath))
        {
            lastSavedPath = data.voiceFilePath;
            SetStatus("Voice memo loaded.");
            if (playButton != null) playButton.interactable = true;
        }
        else
        {
            lastSavedPath = null;
            recordedClip = null;
            SetStatus("Ready to record.");
            if (playButton != null) playButton.interactable = false;
        }
    }

    // -------------------------------------------------------
    // INTERNAL HELPERS
    // -------------------------------------------------------
    private string SaveAudioToFile(AudioClip clip)
    {
        if (!Directory.Exists(VoiceFolder))
            Directory.CreateDirectory(VoiceFolder);

        string fileName = "VoiceNote_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav";
        string fullPath = Path.Combine(VoiceFolder, fileName);

        File.WriteAllBytes(fullPath, AudioClipToWav(clip));
        Debug.Log("[VoiceNoteManager] Saved to: " + fullPath);
        return fullPath;
    }

    private byte[] AudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] bytesData = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short val = (short)(samples[i] * 32767);
            byte[] b = BitConverter.GetBytes(val);
            bytesData[i * 2]     = b[0];
            bytesData[i * 2 + 1] = b[1];
        }

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int hz       = clip.frequency;
            int channels = clip.channels;
            int dataSize = bytesData.Length;

            writer.Write(new char[] { 'R','I','F','F' });
            writer.Write(36 + dataSize);
            writer.Write(new char[] { 'W','A','V','E' });
            writer.Write(new char[] { 'f','m','t',' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(hz);
            writer.Write(hz * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(new char[] { 'd','a','t','a' });
            writer.Write(dataSize);
            writer.Write(bytesData);

            return stream.ToArray();
        }
    }

    private System.Collections.IEnumerator LoadAndPlay(string path)
    {
        string url = "file://" + path;
        using (var req = UnityEngine.Networking.UnityWebRequestMultimedia
                         .GetAudioClip(url, AudioType.WAV))
        {
            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip
                                 .GetContent(req);
                AudioSource src = gameObject.AddComponent<AudioSource>();
                src.clip = clip;
                src.Play();
                SetStatus("Playing saved note...");
                yield return new WaitForSeconds(clip.length);
                Destroy(src);
                SetStatus("Ready to record.");
            }
            else
            {
                SetStatus("Failed to load audio.");
                Debug.LogError("[VoiceNoteManager] " + req.error);
            }
        }
    }

    private System.Collections.IEnumerator CleanupAfterPlay(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(src);
        SetStatus("Ready to record.");
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log("[VoiceNoteManager] " + message);
    }
}