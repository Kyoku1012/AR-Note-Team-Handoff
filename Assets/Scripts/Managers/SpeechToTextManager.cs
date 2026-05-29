using System;
using System.Collections;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class SpeechToTextManager : MonoBehaviour
{
    public static SpeechToTextManager Instance { get; private set; }

    private Action<string> resultCallback;
    private Action<string> errorCallback;

#if UNITY_ANDROID && !UNITY_EDITOR
    private PermissionCallbacks microphonePermissionCallbacks;
#endif

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
        if (IsListening)
            StopDictation();

        resultCallback = onResult;
        errorCallback = onError;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            RequestMicrophonePermission();
            return;
        }

        BeginAndroidDictation();
#else
        OnSpeechError("Speech-to-text requires an Android device with a speech recognition service.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void RequestMicrophonePermission()
    {
        microphonePermissionCallbacks = new PermissionCallbacks();
        microphonePermissionCallbacks.PermissionGranted += permission =>
        {
            microphonePermissionCallbacks = null;
            if (permission == Permission.Microphone)
                StartCoroutine(BeginAndroidDictationAfterPermissionResume());
        };
        microphonePermissionCallbacks.PermissionDenied += permission =>
        {
            microphonePermissionCallbacks = null;
            if (permission == Permission.Microphone)
                OnSpeechError("Microphone permission denied. Enable microphone permission in Android settings to use dictation.");
        };
        microphonePermissionCallbacks.PermissionDeniedAndDontAskAgain += permission =>
        {
            microphonePermissionCallbacks = null;
            if (permission == Permission.Microphone)
                OnSpeechError("Microphone permission is blocked. Enable microphone permission in Android settings to use dictation.");
        };

        Permission.RequestUserPermission(Permission.Microphone, microphonePermissionCallbacks);
    }

    private void BeginAndroidDictation()
    {
        try
        {
            string callbackObjectName = gameObject.name;
            using (AndroidJavaClass bridge = new AndroidJavaClass("com.arnote.speech.SpeechRecognizerBridge"))
            {
                string bridgeVersion = bridge.CallStatic<string>("getBridgeVersion");
                Debug.Log("Using Android speech bridge " + bridgeVersion + ".");
                bridge.CallStatic("startListening", callbackObjectName, "OnSpeechResult", "OnSpeechError");
            }

            IsListening = true;
            Debug.Log("Started Android speech recognition on " + callbackObjectName + ".");
        }
        catch (Exception ex)
        {
            OnSpeechError("Speech recognition failed to start: " + ex.Message);
        }
    }

    private IEnumerator BeginAndroidDictationAfterPermissionResume()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        BeginAndroidDictation();
    }
#endif

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
        Debug.Log("Android speech recognition returned text.");
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
