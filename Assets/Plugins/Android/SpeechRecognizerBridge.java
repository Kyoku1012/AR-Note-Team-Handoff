package com.arnote.speech;

import android.Manifest;
import android.content.ActivityNotFoundException;
import android.content.ComponentName;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;

public class SpeechRecognizerBridge {
    private static final String TAG = "ARNoteSpeech";
    private static final String BRIDGE_VERSION = "system-speech-fallback-v2";
    private static final String GOOGLE_SPEECH_PACKAGE = "com.google.android.googlequicksearchbox";
    private static final String GOOGLE_RECOGNITION_SERVICE = "com.google.android.voicesearch.serviceapi.GoogleRecognitionService";
    private static final String XIAOMI_SPEECH_PACKAGE = "com.xiaomi.mibrain.speech";
    private static final String XIAOMI_ASR_SERVICE = "com.xiaomi.mibrain.speech.asr.AsrService";
    private static final int SPEECH_TIMEOUT_MILLIS = 120000;

    private static SpeechRecognizer recognizer;
    private static String unityObjectName;
    private static String resultMethodName;
    private static String errorMethodName;
    private static String lastPartialResult;
    private static boolean stopRequested;
    private static int sessionId;

    public static void startListening(String objectName, String resultMethod, String errorMethod) {
        unityObjectName = objectName;
        resultMethodName = resultMethod;
        errorMethodName = errorMethod;
        lastPartialResult = "";
        stopRequested = false;

        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                final int activeSessionId = ++sessionId;

                if (UnityPlayer.currentActivity.checkSelfPermission(Manifest.permission.RECORD_AUDIO) != PackageManager.PERMISSION_GRANTED) {
                    sendError(activeSessionId, "Microphone permission is not granted. Enable microphone permission in Android settings to use dictation.");
                    return;
                }

                releaseRecognizer(false);
                final SpeechRecognizer activeRecognizer = createSpeechRecognizer();
                if (activeRecognizer == null) {
                    launchSpeechRecognitionActivity(activeSessionId);
                    return;
                }

                recognizer = activeRecognizer;
                Log.i(TAG, "Starting speech recognition bridge " + BRIDGE_VERSION + ".");

                activeRecognizer.setRecognitionListener(new RecognitionListener() {
                    @Override public void onReadyForSpeech(Bundle params) {}
                    @Override public void onBeginningOfSpeech() {}
                    @Override public void onRmsChanged(float rmsdB) {}
                    @Override public void onBufferReceived(byte[] buffer) {}
                    @Override public void onEndOfSpeech() {}
                    @Override public void onEvent(int eventType, Bundle params) {}

                    @Override
                    public void onPartialResults(Bundle partialResults) {
                        String text = firstResult(partialResults);
                        if (text != null) {
                            lastPartialResult = text;
                        }
                    }

                    @Override
                    public void onError(int error) {
                        Log.w(TAG, "Speech recognizer error " + error + ".");
                        releaseRecognizer(activeSessionId, activeRecognizer, false);

                        if (stopRequested && lastPartialResult != null && lastPartialResult.trim().length() > 0) {
                            sendResult(activeSessionId, lastPartialResult);
                            return;
                        }

                        sendError(activeSessionId, getErrorMessage(error));
                    }

                    @Override
                    public void onResults(Bundle results) {
                        String text = firstResult(results);
                        releaseRecognizer(activeSessionId, activeRecognizer, false);

                        if (text == null || text.trim().length() == 0) {
                            text = lastPartialResult;
                        }

                        if (text == null || text.trim().length() == 0) {
                            sendError(activeSessionId, "No speech recognized. Try speaking again.");
                            return;
                        }

                        sendResult(activeSessionId, text);
                    }
                });

                Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
                intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
                intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
                intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_MINIMUM_LENGTH_MILLIS, SPEECH_TIMEOUT_MILLIS);
                intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, SPEECH_TIMEOUT_MILLIS);
                intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, SPEECH_TIMEOUT_MILLIS);

                try {
                    activeRecognizer.startListening(intent);
                } catch (Exception ex) {
                    releaseRecognizer(activeSessionId, activeRecognizer, false);
                    sendError(activeSessionId, "Speech recognition failed to start: " + ex.getMessage());
                }
            }
        });
    }

    public static void stopListening() {
        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                stopRequested = true;

                if (recognizer == null) {
                    return;
                }

                try {
                    recognizer.stopListening();
                    Log.i(TAG, "Stop requested for speech recognition service.");
                } catch (Exception ex) {
                    Log.w(TAG, "Failed to stop speech recognizer.", ex);
                    releaseRecognizer(false);
                }
            }
        });
    }

    public static String getBridgeVersion() {
        return BRIDGE_VERSION;
    }

    private static SpeechRecognizer createSpeechRecognizer() {
        if (isXiaomiFamilyDevice()) {
            SpeechRecognizer xiaomiRecognizer = createSpeechRecognizerForService(new ComponentName(XIAOMI_SPEECH_PACKAGE, XIAOMI_ASR_SERVICE));
            if (xiaomiRecognizer != null) {
                return xiaomiRecognizer;
            }
        }

        try {
            if (SpeechRecognizer.isRecognitionAvailable(UnityPlayer.currentActivity)) {
                Log.i(TAG, "Using default system speech recognition service.");
                return SpeechRecognizer.createSpeechRecognizer(UnityPlayer.currentActivity);
            }
        } catch (Exception ex) {
            Log.w(TAG, "Default speech recognition service unavailable.", ex);
        }

        SpeechRecognizer googleRecognizer = createSpeechRecognizerForService(
            new ComponentName(GOOGLE_SPEECH_PACKAGE, GOOGLE_RECOGNITION_SERVICE));
        if (googleRecognizer != null) {
            return googleRecognizer;
        }

        return isXiaomiFamilyDevice()
            ? null
            : createSpeechRecognizerForService(new ComponentName(XIAOMI_SPEECH_PACKAGE, XIAOMI_ASR_SERVICE));
    }

    private static SpeechRecognizer createSpeechRecognizerForService(ComponentName service) {
        try {
            UnityPlayer.currentActivity.getPackageManager().getServiceInfo(service, 0);
            Log.i(TAG, "Using speech recognition service: " + service.flattenToShortString());
            return SpeechRecognizer.createSpeechRecognizer(UnityPlayer.currentActivity, service);
        } catch (Exception ex) {
            Log.w(TAG, "Speech recognition service unavailable: " + service.flattenToShortString(), ex);
            return null;
        }
    }

    private static void launchSpeechRecognitionActivity(int activeSessionId) {
        try {
            Intent intent = new Intent(UnityPlayer.currentActivity, SpeechRecognizerBridgeActivity.class);
            intent.putExtra("unityObjectName", unityObjectName);
            intent.putExtra("resultMethodName", resultMethodName);
            intent.putExtra("errorMethodName", errorMethodName);
            UnityPlayer.currentActivity.startActivity(intent);
            Log.i(TAG, "Started speech recognition activity fallback.");
        } catch (ActivityNotFoundException ex) {
            sendError(activeSessionId, "Speech recognition is not available on this device. Install or enable a speech recognition service.");
        } catch (Exception ex) {
            sendError(activeSessionId, "Speech recognition failed to start: " + ex.getMessage());
        }
    }

    private static boolean isXiaomiFamilyDevice() {
        String manufacturer = Build.MANUFACTURER == null ? "" : Build.MANUFACTURER.toLowerCase();
        String brand = Build.BRAND == null ? "" : Build.BRAND.toLowerCase();
        return manufacturer.contains("xiaomi")
            || manufacturer.contains("redmi")
            || manufacturer.contains("poco")
            || brand.contains("xiaomi")
            || brand.contains("redmi")
            || brand.contains("poco");
    }

    private static String firstResult(Bundle results) {
        if (results == null) {
            return null;
        }

        ArrayList<String> matches = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches == null || matches.size() == 0) {
            return null;
        }

        return matches.get(0);
    }

    private static void releaseRecognizer(boolean cancelFirst) {
        if (recognizer == null) {
            return;
        }

        SpeechRecognizer oldRecognizer = recognizer;
        recognizer = null;
        releaseRecognizerInstance(oldRecognizer, cancelFirst);
    }

    private static void releaseRecognizer(int callbackSessionId, SpeechRecognizer callbackRecognizer, boolean cancelFirst) {
        if (callbackSessionId != sessionId || callbackRecognizer != recognizer) {
            return;
        }

        recognizer = null;
        releaseRecognizerInstance(callbackRecognizer, cancelFirst);
    }

    private static void releaseRecognizerInstance(SpeechRecognizer recognizerToRelease, boolean cancelFirst) {
        if (recognizerToRelease == null) {
            return;
        }

        try {
            if (cancelFirst) {
                recognizerToRelease.cancel();
            }
        } catch (Exception ex) {
            Log.w(TAG, "Failed to cancel speech recognizer.", ex);
        }

        recognizerToRelease.destroy();
    }

    private static void sendResult(int callbackSessionId, String text) {
        if (callbackSessionId == sessionId && unityObjectName != null && resultMethodName != null) {
            UnityPlayer.UnitySendMessage(unityObjectName, resultMethodName, text);
        }
    }

    private static void sendError(int callbackSessionId, String message) {
        if (callbackSessionId == sessionId && unityObjectName != null && errorMethodName != null) {
            UnityPlayer.UnitySendMessage(unityObjectName, errorMethodName, message);
        }
    }

    private static String getErrorMessage(int error) {
        switch (error) {
            case SpeechRecognizer.ERROR_AUDIO:
                return "Speech recognition audio error (code " + error + "). Check the microphone and try again.";
            case SpeechRecognizer.ERROR_CLIENT:
                return "Speech recognition client error (code " + error + "). Try dictation again.";
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS:
                return "Speech recognition service does not have microphone permission (code " + error + "). Check this app and your phone's speech service microphone permissions.";
            case SpeechRecognizer.ERROR_LANGUAGE_NOT_SUPPORTED:
                return "Speech recognition language is not supported on this device (code " + error + ").";
            case SpeechRecognizer.ERROR_LANGUAGE_UNAVAILABLE:
                return "Speech recognition language is unavailable on this device (code " + error + ").";
            case SpeechRecognizer.ERROR_NETWORK:
                return "Speech recognition network error (code " + error + "). Check your connection and try again.";
            case SpeechRecognizer.ERROR_NETWORK_TIMEOUT:
                return "Speech recognition network timed out (code " + error + "). Check your connection and try again.";
            case SpeechRecognizer.ERROR_NO_MATCH:
                return "No speech recognized (code " + error + "). Try speaking again.";
            case SpeechRecognizer.ERROR_RECOGNIZER_BUSY:
                return "Speech recognition is busy (code " + error + "). Wait a moment and try again.";
            case SpeechRecognizer.ERROR_SERVER:
                return "Speech recognition service error (code " + error + "). Try again later.";
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT:
                return "No speech heard (code " + error + "). Try speaking after tapping Mic.";
            default:
                return "Speech recognition error: " + error;
        }
    }
}
