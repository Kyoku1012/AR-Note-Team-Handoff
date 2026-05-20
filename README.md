# AR-NOTE-TEAM-HAND-OFF

AR Note is a Unity/Vuforia Android project for placing digital sticky notes in the user's physical environment. Users can create notes in AR, edit them as tasks, style them, set reminders, and add speech-to-text input.

## Project Goal

The app supports contextual task management. A note can be placed near a real object such as a desk, door, fridge, or computer, so the reminder appears where the task is relevant.

## Main Features

- AR note placement with Vuforia Ground Plane.
- Fallback `Create Note` button for Editor testing or when plane detection is unavailable.
- Add, edit, delete, complete, show, and hide notes.
- Local JSON persistence at `Application.persistentDataPath/ar_notes.json`.
- Note styling with color labels, icons, and priority markers.
- Android local reminders with optional repeat, snooze, and dismiss behavior.
- Android speech-to-text input for note content.

## How the Project Works

The project is organized around one shared note model and several independent feature modules.

`NoteData` is the shared data contract. It stores the note text, task state, style, reminder fields, transcript fields, and AR world pose. Reminder writes should go through `NoteData.SetReminder` and `NoteData.ClearReminder` so the saved reminder fields and legacy alarm scheduler fields stay in sync.

`NoteManager` is the central note service. Other modules add, update, delete, select, and retrieve notes through it instead of saving data themselves.

`DatabaseManager` handles local JSON loading and saving. Feature modules should not write directly to the JSON file.

`NoteView` is the bridge between a saved `NoteData` record and a note prefab in the AR scene. It refreshes text, style, visibility, selection state, and button actions.

`PlaceNote` owns AR placement. It creates note anchors from Vuforia hit tests and restores saved notes from their world positions.

Feature modules update the selected note's `NoteData`, then call `NoteView.SaveAndRefresh()` or `NoteManager.UpdateNote(note)`. Reminder scheduling should go through `ReminderManager`; `AlarmManager` is the lower-level Android notification scheduler.

## Member Modules and Interfaces

| Member | Module | Owns | Public interface with other modules |
| --- | --- | --- | --- |
| Member 1 | AR placement | `Assets/Scripts/AR`, AR prefabs, Vuforia scene setup | Creates notes through `PlaceNote`, initializes each note through `NoteView.Initialize`, stores pose in `NoteData.worldPosition/worldRotation` |
| Member 2 | Task management | `NoteData`, `NoteManager`, `DatabaseManager`, edit UI | Uses `NoteManager.AddNote`, `UpdateNote`, `RemoveNote`, `GetAllNotes`, `SelectNote`; task state lives in `NoteData` |
| Member 3 | Custom styling | `Assets/Scripts/Styling`, note visual assets | Reads/writes `NoteData.colorName`, `iconId`, `priorityId`; applies visuals through `NoteStyleManager.ApplyStyle` |
| Member 4 | Reminders | `ReminderManager`, `AlarmManager`, reminder UI fields | Reads/writes reminders through `NoteData.SetReminder` and `ClearReminder`; schedules through `ReminderManager.ScheduleOrCancel`; `AlarmManager` owns Android notification delivery |
| Member 5 | Speech input | `SpeechToTextManager`, Android speech bridge | Reads/writes transcript fields; starts dictation through `SpeechToTextManager`; no voice recording or audio-file storage |

Each member should keep feature-specific logic inside their module. Shared changes should go through `NoteData`, `NoteManager`, or `NoteView` only when the data or behavior is needed by more than one module.

## Folder Map

- `Assets/Scripts/AR`: AR placement, anchors, note views, and camera-facing helpers.
- `Assets/Scripts/Data`: serializable shared data contracts.
- `Assets/Scripts/Managers`: note registry, persistence, alarms, reminders, and speech-to-text services.
- `Assets/Scripts/Styling`: color, icon, and priority application.
- `Assets/Scripts/UI`: runtime edit panel, toolbar helpers, picker drag handling, button action relay, and UI tests.
- `Assets/Editor`: readiness checks and migration helpers.
- `Assets/Plugins/Android`: Android-native speech recognition bridge.

## Local Setup

1. Install Unity `2021.3.45f2` through Unity Hub.
2. In Unity Hub, install Android Build Support with Android SDK & NDK Tools and OpenJDK.
3. Clone the team handoff repository:

```bash
git clone https://github.com/Kyoku1012/AR-Note-Team-Handoff.git
cd AR-Note-Team-Handoff
```

4. Open Unity Hub, click `Add`, and select the cloned project folder.
5. Open the project with Unity `2021.3.45f2`.
6. Let Unity import assets and let Package Manager resolve dependencies, including `com.unity.mobile.notifications` and the local Vuforia package.
7. Open `Assets/Scenes/MainScene.unity`.
8. Run `AR Note > Run Readiness Check`.

Do not commit generated local folders such as `Library`, `Logs`, `Temp`, `UserSettings`, or local tool settings.

## Run In Unity Editor

1. Open `Assets/Scenes/MainScene.unity`.
2. Press Play.
3. Confirm the Console has no red compile/runtime errors.
4. Click `Create Note` to create an Editor-friendly fallback note without waiting for Vuforia plane detection.
5. Tap the note or click `Edit Note`.
6. Edit title, content, completion, visibility, reminder, style, or speech fields where available.
7. Click `Save`.
8. Stop Play Mode, enter Play Mode again, and confirm saved notes restore from local JSON.

Editor testing can verify CRUD, styling calls, persistence, runtime UI, and most note-management logic. Real AR plane detection, Android notifications, microphone permission, and speech recognition require an Android device.

## Android Build And Run

1. Connect an Android phone with USB debugging enabled.
2. In Unity, open `File > Build Settings`.
3. Select `Android`, then click `Switch Platform`.
4. Confirm `Assets/Scenes/MainScene.unity` is included in `Scenes In Build`.
5. Open `Edit > Project Settings > Player`.
6. Confirm the Android package settings are valid for the test device.
7. Confirm the custom Android manifest at `Assets/Plugins/Android/AndroidManifest.xml` is present so camera, microphone, notification, and speech-recognition permissions are available.
8. Click `Build And Run`.
9. On the phone, accept camera, microphone, notification, and exact alarm permission prompts when they appear.
10. Move the phone slowly over a textured surface until Vuforia detects a plane.
11. Tap the detected surface to place a note.
12. Set a future reminder and verify the Android notification appears.
13. Use speech input on a note and verify recognized text is appended to the note content.
14. Close and reopen the app to confirm notes restore with position, text, style, reminder, and transcript data.

If Build And Run fails, first run `AR Note > Run Readiness Check`, then check Package Manager resolution, Android SDK/NDK installation, and Console errors.

## Demo Flow

1. Build and run on an Android device.
2. Accept camera and microphone permissions when requested.
3. Scan a surface with Vuforia Ground Plane.
4. Tap a detected surface to place a note.
5. Tap the note or `Edit Note`.
6. Edit title, content, annotation, completion, visibility, reminder, style, and speech input.
7. Apply color, icon, and priority from the style controls.
8. Close and reopen the app to confirm saved notes restore.

For Editor-only testing, use `Create Note` to create a camera-directed fallback note without waiting for Vuforia plane detection.

## More Documentation

- `TEAM_HANDOFF.md`: member boundaries, shared interfaces, and safe amendment rules.
- `SIMULATION_GUIDE.md`: Editor and Android testing steps.
- `CRUD_TEST_GUIDE.md`: focused note create/read/update/delete test checklist.
