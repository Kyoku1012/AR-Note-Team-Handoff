package com.arnote.speech;

import android.content.Intent;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;
import java.util.Locale;

public class SpeechRecognizerBridge {
    private static SpeechRecognizer recognizer;
    private static String unityObjectName;
    private static String resultMethodName;
    private static String errorMethodName;

    public static void startListening(String objectName, String resultMethod, String errorMethod) {
        unityObjectName = objectName;
        resultMethodName = resultMethod;
        errorMethodName = errorMethod;

        UnityPlayer.currentActivity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (!SpeechRecognizer.isRecognitionAvailable(UnityPlayer.currentActivity)) {
                    sendError("Speech recognition is not available on this device.");
                    return;
                }

                stopListening();
                recognizer = SpeechRecognizer.createSpeechRecognizer(UnityPlayer.currentActivity);
                recognizer.setRecognitionListener(new RecognitionListener() {
                    @Override public void onReadyForSpeech(Bundle params) {}
                    @Override public void onBeginningOfSpeech() {}
                    @Override public void onRmsChanged(float rmsdB) {}
                    @Override public void onBufferReceived(byte[] buffer) {}
                    @Override public void onEndOfSpeech() {}
                    @Override public void onPartialResults(Bundle partialResults) {}
                    @Override public void onEvent(int eventType, Bundle params) {}

                    @Override
                    public void onError(int error) {
                        sendError("Speech recognition error: " + error);
                    }

                    @Override
                    public void onResults(Bundle results) {
                        ArrayList<String> matches = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
                        if (matches == null || matches.size() == 0) {
                            sendError("No speech recognized.");
                            return;
                        }

                        UnityPlayer.UnitySendMessage(unityObjectName, resultMethodName, matches.get(0));
                    }
                });

                Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, Locale.getDefault());
                intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
                recognizer.startListening(intent);
            }
        });
    }

    public static void stopListening() {
        if (recognizer == null) {
            return;
        }

        recognizer.stopListening();
        recognizer.destroy();
        recognizer = null;
    }

    private static void sendError(String message) {
        if (unityObjectName != null && errorMethodName != null) {
            UnityPlayer.UnitySendMessage(unityObjectName, errorMethodName, message);
        }
    }
}
