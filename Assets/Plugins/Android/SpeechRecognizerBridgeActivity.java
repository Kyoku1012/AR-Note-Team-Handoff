package com.arnote.speech;

import android.app.Activity;
import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.speech.RecognizerIntent;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;

public class SpeechRecognizerBridgeActivity extends Activity {
    private static final String TAG = "ARNoteSpeech";
    private static final int REQUEST_RECOGNIZE_SPEECH = 4102;
    private static final String XIAOMI_SPEECH_PACKAGE = "com.xiaomi.mibrain.speech";
    private static final int SPEECH_MINIMUM_LENGTH_MILLIS = 120000;
    private static final int SPEECH_POSSIBLY_COMPLETE_SILENCE_MILLIS = 120000;
    private static final int SPEECH_COMPLETE_SILENCE_MILLIS = 120000;

    private String unityObjectName;
    private String resultMethodName;
    private String errorMethodName;
    private boolean triedXiaomiPackage;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        Intent launchIntent = getIntent();
        unityObjectName = launchIntent.getStringExtra("unityObjectName");
        resultMethodName = launchIntent.getStringExtra("resultMethodName");
        errorMethodName = launchIntent.getStringExtra("errorMethodName");

        String xiaomiPackage = isXiaomiFamilyDevice() ? getXiaomiSpeechPackage() : null;
        triedXiaomiPackage = xiaomiPackage != null;
        startRecognition(xiaomiPackage);
    }

    private void startRecognition(String packageName) {
        Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
        intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
        intent.putExtra(RecognizerIntent.EXTRA_PROMPT, "Speak now. Tap done when finished.");
        intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
        intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, false);
        intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_MINIMUM_LENGTH_MILLIS, SPEECH_MINIMUM_LENGTH_MILLIS);
        intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS, SPEECH_POSSIBLY_COMPLETE_SILENCE_MILLIS);
        intent.putExtra(RecognizerIntent.EXTRA_SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS, SPEECH_COMPLETE_SILENCE_MILLIS);

        if (packageName != null) {
            intent.setPackage(packageName);
        }

        try {
            Log.i(TAG, "Starting speech recognition activity with package: " + (packageName == null ? "system-default" : packageName));
            startActivityForResult(intent, REQUEST_RECOGNIZE_SPEECH);
        } catch (ActivityNotFoundException ex) {
            if (packageName == null && !triedXiaomiPackage) {
                String xiaomiPackage = getXiaomiSpeechPackage();
                if (xiaomiPackage != null) {
                    triedXiaomiPackage = true;
                    startRecognition(xiaomiPackage);
                    return;
                }
            }

            if (packageName != null) {
                startRecognition(null);
                return;
            }

            sendError("Speech recognition activity is not available on this device.");
            finish();
        }
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode != REQUEST_RECOGNIZE_SPEECH) {
            finish();
            return;
        }

        if (resultCode == RESULT_OK && data != null) {
            ArrayList<String> matches = data.getStringArrayListExtra(RecognizerIntent.EXTRA_RESULTS);
            if (matches != null && matches.size() > 0) {
                UnityPlayer.UnitySendMessage(unityObjectName, resultMethodName, matches.get(0));
                finish();
                return;
            }
        }

        sendError("No speech recognized. Try speaking again.");
        finish();
    }

    private String getXiaomiSpeechPackage() {
        try {
            getPackageManager().getPackageInfo(XIAOMI_SPEECH_PACKAGE, 0);
            return XIAOMI_SPEECH_PACKAGE;
        } catch (PackageManager.NameNotFoundException ex) {
            return null;
        }
    }

    private boolean isXiaomiFamilyDevice() {
        String manufacturer = Build.MANUFACTURER == null ? "" : Build.MANUFACTURER.toLowerCase();
        String brand = Build.BRAND == null ? "" : Build.BRAND.toLowerCase();
        return manufacturer.contains("xiaomi")
            || manufacturer.contains("redmi")
            || manufacturer.contains("poco")
            || brand.contains("xiaomi")
            || brand.contains("redmi")
            || brand.contains("poco");
    }

    private void sendError(String message) {
        if (unityObjectName != null && errorMethodName != null) {
            UnityPlayer.UnitySendMessage(unityObjectName, errorMethodName, message);
        }
    }
}
