using System;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class SpeechToTextManager : MonoBehaviour
{
    public static SpeechToTextManager Instance { get; private set; }

    private Action<string> resultCallback;
    private Action<string> errorCallback;

    public bool IsListening { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        gameObject.name = gameObject.name == "Managers" ? gameObject.name : "SpeechToTextManager";
    }

    public void StartDictation(Action<string> onResult, Action<string> onError)
    {
        resultCallback = onResult;
        errorCallback = onError;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
            OnSpeechError("Microphone permission requested. Tap dictation again after granting permission.");
            return;
        }

        try
        {
            using (AndroidJavaClass bridge = new AndroidJavaClass("com.arnote.speech.SpeechRecognizerBridge"))
            {
                bridge.CallStatic("startListening", gameObject.name, "OnSpeechResult", "OnSpeechError");
            }

            IsListening = true;
        }
        catch (Exception ex)
        {
            OnSpeechError("Speech recognition failed to start: " + ex.Message);
        }
#else
        OnSpeechError("Speech-to-text requires an Android device with Google speech recognition.");
#endif
    }

    public void StopDictation()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass bridge = new AndroidJavaClass("com.arnote.speech.SpeechRecognizerBridge"))
            {
                bridge.CallStatic("stopListening");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to stop speech recognition: " + ex.Message);
        }
#endif
        IsListening = false;
    }

    public void OnSpeechResult(string text)
    {
        IsListening = false;
        if (string.IsNullOrWhiteSpace(text))
        {
            OnSpeechError("Speech recognition returned no text.");
            return;
        }

        resultCallback?.Invoke(text);
        resultCallback = null;
        errorCallback = null;
    }

    public void OnSpeechError(string message)
    {
        IsListening = false;
        Debug.LogWarning(message);
        errorCallback?.Invoke(message);
        resultCallback = null;
        errorCallback = null;
    }
}
