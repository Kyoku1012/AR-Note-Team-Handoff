using System;
using System.IO;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class VoiceNoteManager : MonoBehaviour
{
    public static VoiceNoteManager Instance { get; private set; }

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

    public void ToggleRecording(NoteView noteView)
    {
        if (noteView == null || noteView.Data == null) return;

        if (recordingNote == noteView && Microphone.IsRecording(recordingDevice))
        {
            StopRecording();
            return;
        }

        StartRecording(noteView);
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
            Debug.LogWarning("No microphone device found.");
            return;
        }

        if (Microphone.IsRecording(recordingDevice))
            StopRecording();

        recordingNote = noteView;
        recordingDevice = Microphone.devices[0];
        recordingClip = Microphone.Start(recordingDevice, false, MaxRecordSeconds, SampleRate);
        Debug.Log("Voice note recording started.");
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
            return;
        }

        AudioClip trimmedClip = TrimClip(recordingClip, samples);
        Directory.CreateDirectory(VoiceFolder);

        string path = Path.Combine(VoiceFolder, recordingNote.Data.id + ".wav");
        SaveWav(path, trimmedClip);

        recordingNote.Data.hasVoiceNote = true;
        recordingNote.Data.voiceFilePath = path;
        recordingNote.SaveAndRefresh();

        ClearRecording();
        Debug.Log("Voice note recording saved.");
    }

    public void DeleteVoice(NoteView noteView)
    {
        if (noteView == null || noteView.Data == null) return;

        if (!string.IsNullOrWhiteSpace(noteView.Data.voiceFilePath) && File.Exists(noteView.Data.voiceFilePath))
            File.Delete(noteView.Data.voiceFilePath);

        noteView.Data.hasVoiceNote = false;
        noteView.Data.voiceFilePath = "";
        noteView.SaveAndRefresh();
    }

    public void Play(string path, AudioSource audioSource)
    {
        if (audioSource == null || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return;

        AudioClip clip = LoadWav(path);
        if (clip == null) return;

        audioSource.clip = clip;
        audioSource.Play();
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

    private AudioClip LoadWav(string path)
    {
        byte[] fileBytes = File.ReadAllBytes(path);
        if (fileBytes.Length <= 44) return null;

        int channels = BitConverter.ToInt16(fileBytes, 22);
        int frequency = BitConverter.ToInt32(fileBytes, 24);
        int sampleCount = (fileBytes.Length - 44) / 2;
        float[] samples = new float[sampleCount];

        int offset = 44;
        for (int i = 0; i < sampleCount; i++)
        {
            short value = BitConverter.ToInt16(fileBytes, offset);
            samples[i] = value / 32768f;
            offset += 2;
        }

        AudioClip clip = AudioClip.Create(Path.GetFileNameWithoutExtension(path), sampleCount / channels, channels, frequency, false);
        clip.SetData(samples, 0);
        return clip;
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
}
