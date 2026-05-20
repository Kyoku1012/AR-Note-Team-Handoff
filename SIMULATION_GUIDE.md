# Simulation and Testing Guide

Use this guide to test the Unity project in the Editor and on Android.

## Required Setup

- Unity Editor `2021.3.45f2`.
- Android Build Support, Android SDK/NDK, and OpenJDK installed through Unity Hub.
- A physical Android device for final AR, notification, microphone, and speech-to-text testing.
- `Assets/Scenes/MainScene.unity` opened before testing.

Run `AR Note > Run Readiness Check` before Android builds or group handoff.

## What the Unity Editor Can Test

- Script compile and package resolution.
- `MainScene` loading.
- Runtime note edit UI.
- Note create/read/update/delete logic.
- Local JSON save/load flow.
- Style method calls.
- General prefab and scene references.

## What Requires Android

- Vuforia Ground Plane behavior.
- Camera permission flow.
- Local notification delivery.
- Microphone permission for speech-to-text.
- Android speech recognition.
- Physical-world note placement accuracy.

## Editor Test Flow

1. Open `MainScene`.
2. Press Play.
3. Confirm no red Console errors.
4. Click `Create Note`.
5. Edit title, content, annotation, completion, visibility, alarm, style, and speech fields where possible.
6. Click `Save`.
7. Stop Play Mode and enter Play Mode again.
8. Confirm saved notes restore.

`Create Note` bypasses Vuforia plane detection and creates a fallback note from the current camera direction.

## Android Build Flow

1. Go to `File > Build Settings`.
2. Select `Android` and click `Switch Platform`.
3. Confirm `Assets/Scenes/MainScene.unity` is included.
4. In Player Settings, confirm the package settings are valid for the test phone.
5. Connect a phone with USB debugging enabled.
6. Click `Build And Run`.

## Android Demo Flow

1. Launch the app.
2. Accept permissions.
3. Move the phone slowly over a textured surface.
4. Tap a detected surface to place a note.
5. Tap the note or `Edit Note`.
6. Edit and save task fields.
7. Apply color, icon, and priority.
8. Set a future alarm.
9. Use speech input on note content.
11. Close and reopen the app.
12. Confirm saved notes restore with their data and styling.

## Member Test Areas

| Member | Test focus | Main files |
| --- | --- | --- |
| 1 | Plane detection, note placement, note orientation, restored anchors | `PlaceNote.cs`, `NoteView.cs`, note prefab, `MainScene.unity` |
| 2 | Add, edit, delete, complete, show/hide, save/load | `NoteData.cs`, `NoteManager.cs`, `DatabaseManager.cs`, `NoteEditPanel.cs` |
| 3 | Color, icon, priority, visual persistence | `StylePanelController.cs`, `NoteStyleManager.cs`, note prefab visuals |
| 4 | Alarm schedule, repeat, snooze, dismiss, delete cancellation | `AlarmManager.cs`, `ReminderManager.cs`, alarm UI |
| 5 | Speech-to-text and transcript persistence | `SpeechToTextManager.cs`, `SpeechRecognizerBridge.java`, speech UI |

## Common Problems

- No plane detected: improve lighting, use a textured surface, and move the phone slowly.
- UI taps create notes: confirm an `EventSystem` exists in `MainScene`.
- Style buttons do nothing: place or select a note first.
- Alarm does not fire: test on Android, set a future time, and check notification permission.
- Speech input does not start: use Android with speech recognition available.
- Notes do not save: check Console errors from `DatabaseManager`.
