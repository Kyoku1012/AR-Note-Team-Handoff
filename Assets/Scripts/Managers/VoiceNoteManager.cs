using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class VoiceNoteManager : MonoBehaviour
{
    public static VoiceNoteManager Instance { get; private set; }

    [Header("Optional UI")]
    public Button recordButton;
    public Button stopButton;
    public Button playButton;
    public Text statusText;
    public TextMeshProUGUI tmpStatusText;

    private const int SampleRate = 44100;
    private const int MaxRecordSeconds = 180;

    private NoteView recordingNote;
    private AudioClip recordingClip;
    private string recordingDevice;

    private string VoiceFolder => Path.Combine(Application.persistentDataPath, "voice_notes");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(RequestMicrophonePermissionThenInitialize());
    }

    public void ToggleRecording(NoteView noteView)
    {
        if (noteView == null || noteView.Data == null) return;

        if (recordingNote == noteView && IsRecording(noteView))
        {
            StopRecording();
            return;
        }

        StartRecording(noteView);
    }

    public bool IsRecording(NoteView noteView)
    {
        return noteView != null
            && recordingNote == noteView
            && !string.IsNullOrWhiteSpace(recordingDevice)
            && Microphone.IsRecording(recordingDevice);
    }

    public void StartRecording(NoteView noteView)
    {
        if (noteView == null || noteView.Data == null) return;

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            return;
        }
#endif

        if (Microphone.devices.Length == 0)
        {
            SetStatus("No microphone device found.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(recordingDevice) && Microphone.IsRecording(recordingDevice))
            StopRecording();

        recordingNote = noteView;
        recordingDevice = Microphone.devices[0];
        recordingClip = Microphone.Start(recordingDevice, false, MaxRecordSeconds, SampleRate);
        UpdateButtonState(recording: true, canPlay: false);
        SetStatus("Recording...");
    }

    public void StopRecording()
    {
        if (recordingNote == null || recordingClip == null)
            return;

        int samples = Microphone.GetPosition(recordingDevice);
        Microphone.End(recordingDevice);

        if (samples <= 0)
        {
            ClearRecording();
            SetStatus("Recording was empty.");
            return;
        }

        samples = Mathf.Min(samples, recordingClip.samples);
        AudioClip trimmedClip = TrimClip(recordingClip, samples);
        Directory.CreateDirectory(VoiceFolder);

        string path = Path.Combine(VoiceFolder, recordingNote.Data.id + ".wav");
        SaveWav(path, trimmedClip);

        recordingNote.Data.hasVoiceNote = true;
        recordingNote.Data.voiceFilePath = path;
        recordingNote.SaveAndRefresh();

        ClearRecording();
        UpdateButtonState(recording: false, canPlay: true);
        SetStatus("Voice note saved.");
    }

    public void DeleteVoice(NoteView noteView)
    {
        if (noteView == null || noteView.Data == null) return;

        if (!string.IsNullOrWhiteSpace(noteView.Data.voiceFilePath) && File.Exists(noteView.Data.voiceFilePath))
            File.Delete(noteView.Data.voiceFilePath);

        noteView.Data.hasVoiceNote = false;
        noteView.Data.voiceFilePath = "";
        noteView.SaveAndRefresh();
        UpdateButtonState(recording: false, canPlay: false);
        SetStatus("Voice note deleted.");
    }

    public void Play(string path, AudioSource audioSource)
    {
        if (audioSource == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            SetStatus("Voice file missing.");
            return;
        }

        StartCoroutine(LoadAndPlay(path, audioSource, false));
    }

    public void PlayFromNote(NoteData data)
    {
        if (data == null || !data.hasVoiceNote || string.IsNullOrWhiteSpace(data.voiceFilePath))
        {
            SetStatus("No voice note for this note.");
            return;
        }

        PlayFromNote(data, null);
    }

    public void PlayFromNote(NoteData data, AudioSource audioSource)
    {
        if (data == null || !data.hasVoiceNote || string.IsNullOrWhiteSpace(data.voiceFilePath))
        {
            SetStatus("No voice note for this note.");
            return;
        }

        if (!File.Exists(data.voiceFilePath))
        {
            SetStatus("Voice file missing.");
            Debug.LogWarning("Voice file not found: " + data.voiceFilePath);
            return;
        }

        AudioSource targetSource = audioSource;
        bool destroyWhenDone = false;
        if (targetSource == null)
        {
            targetSource = gameObject.AddComponent<AudioSource>();
            destroyWhenDone = true;
        }

        StartCoroutine(LoadAndPlay(data.voiceFilePath, targetSource, destroyWhenDone));
    }

    public bool LoadFromNote(NoteData data)
    {
        bool hasPlayableVoice = data != null
            && data.hasVoiceNote
            && !string.IsNullOrWhiteSpace(data.voiceFilePath)
            && File.Exists(data.voiceFilePath);

        UpdateButtonState(recording: false, canPlay: hasPlayableVoice);
        SetStatus(hasPlayableVoice ? "Voice note loaded." : "Ready to record.");
        return hasPlayableVoice;
    }

    public void ClearAllVoiceFiles()
    {
        try
        {
            if (Directory.Exists(VoiceFolder))
                Directory.Delete(VoiceFolder, true);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to clear voice notes: {ex.Message}");
        }
    }

    private void ClearRecording()
    {
        recordingNote = null;
        recordingClip = null;
        recordingDevice = null;
    }

    private AudioClip TrimClip(AudioClip source, int samples)
    {
        float[] data = new float[samples * source.channels];
        source.GetData(data, 0);

        AudioClip clip = AudioClip.Create(source.name + "_trimmed", samples, source.channels, source.frequency, false);
        clip.SetData(data, 0);
        return clip;
    }

    private void SaveWav(string path, AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        byte[] bytes = ConvertToWav(samples, clip.channels, clip.frequency);
        File.WriteAllBytes(path, bytes);
    }

    private byte[] ConvertToWav(float[] samples, int channels, int frequency)
    {
        byte[] pcmData = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            short value = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
            byte[] bytes = BitConverter.GetBytes(value);
            pcmData[i * 2] = bytes[0];
            pcmData[i * 2 + 1] = bytes[1];
        }

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcmData.Length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(pcmData.Length);
            writer.Write(pcmData);
            return stream.ToArray();
        }
    }

    private IEnumerator RequestMicrophonePermissionThenInitialize()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            yield return new WaitForSeconds(1.5f);
        }
#endif

        yield return null;

        bool hasMicrophone = Microphone.devices.Length > 0;
        UpdateButtonState(recording: false, canPlay: false);

        if (recordButton != null)
            recordButton.interactable = hasMicrophone;

        SetStatus(hasMicrophone ? "Ready to record." : "No microphone device found.");
    }

    private IEnumerator LoadAndPlay(string path, AudioSource audioSource, bool destroyWhenDone)
    {
        string url = new Uri(path).AbsoluteUri;
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                SetStatus("Failed to load voice note.");
                Debug.LogError("Failed to load voice note: " + request.error);

                if (destroyWhenDone && audioSource != null)
                    Destroy(audioSource);

                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                SetStatus("Failed to decode voice note.");

                if (destroyWhenDone && audioSource != null)
                    Destroy(audioSource);

                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();
            SetStatus("Playing voice note...");

            yield return new WaitForSeconds(clip.length);

            if (destroyWhenDone && audioSource != null)
                Destroy(audioSource);

            SetStatus("Ready to record.");
        }
    }

    private void UpdateButtonState(bool recording, bool canPlay)
    {
        if (recordButton != null)
            recordButton.interactable = !recording && Microphone.devices.Length > 0;

        if (stopButton != null)
            stopButton.interactable = recording;

        if (playButton != null)
            playButton.interactable = !recording && canPlay;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (tmpStatusText != null)
            tmpStatusText.text = message;

        Debug.Log("[VoiceNoteManager] " + message);
    }
}
