# AR-NOTE-TEAM-HAND-OFF

AR Note is a Unity/Vuforia Android project for placing digital sticky notes in the user's physical environment. Users can create AR notes, edit them as tasks, style them, set local reminders, browse note history, and add note content with Android speech recognition.

## Project Goal

The app supports contextual task management. A note can be placed near a real object such as a desk, door, fridge, or computer, so the reminder appears where the task is relevant.

## Main Features

- AR note placement with Vuforia Ground Plane hit tests.
- `Create Note` fallback that creates a note in front of the camera for Editor testing or when plane detection is unavailable.
- Smart fallback placement: notes are placed as a wall/front note or a horizontal-surface note based on the camera direction.
- Add, edit, delete, complete, select, show, and hide notes.
- Runtime edit UI that can be created automatically if no edit panel prefab exists in the scene.
- Note history panel with `all`, `low`, `medium`, and `high` priority filters.
- Local JSON persistence at `Application.persistentDataPath/ar_notes.json`.
- Note styling with color panels, icons, and priority markers.
- Android local reminders with exact-alarm scheduling, notification permissions, notification tap handling, snooze/dismiss support in the alarm layer, and automatic rescheduling on app focus.
- Android speech-to-text input for note content through a native Android speech bridge.

## How the Project Works

The project is organized around one shared note model and several independent feature modules.

`NoteData` is the shared data contract. It stores note text, annotation, task state, visibility, style, reminder/alarm fields, transcript fields, and AR world pose. Reminder writes should go through `NoteData.SetReminder` and `NoteData.ClearReminder` so the user-facing reminder fields and legacy alarm scheduler fields stay in sync.

`NoteManager` is the central note service. Other modules add, update, delete, select, and retrieve notes through it instead of saving data directly. On startup it loads saved notes, ensures runtime services exist, and asks `ReminderManager` to reschedule reminders.

`DatabaseManager` handles local JSON loading, saving, deletion, and clearing. Feature modules should not write directly to the JSON file.

`NoteView` is the bridge between a saved `NoteData` record and a note prefab in the AR scene. It refreshes title/content/reminder text, style, visibility, selection frame, collider sizing, and note button actions. It also writes the anchor pose back to `NoteData` before saving.

`PlaceNote` owns AR placement. It creates note anchors from Vuforia hit tests, restores saved notes from their world positions, opens existing notes when the user taps them, and provides the center-screen fallback used by the `Create Note` button.

`NoteEditPanel` owns the runtime edit workflow. It edits title, content, completion, reminder time, color, icon, priority, and speech input. If no panel is already present, `NoteEditPanel.EnsureExists()` builds a screen-space runtime UI and launcher buttons.

`NoteHistoryPanel` displays saved notes in a scrollable history view, sorted by priority and title. It can open the live AR note when a view exists, or edit the saved note data directly when the AR view has not been restored yet.

`ReminderManager` is the feature-facing reminder API. `AlarmManager` is the lower-level Android notification scheduler. Android notifications store the note id in `IntentData`; when the user taps a reminder, the app opens the matching note editor when the note view is ready.

`SpeechToTextManager` starts and stops dictation. Speech input is Android-device only; in the Unity Editor it returns a clear unsupported-device message. Recognized speech is appended to note content and stored in transcript fields.

## Member Modules and Interfaces

| Member | Module | Owns | Public interface with other modules |
| --- | --- | --- | --- |
| Member 1 | AR placement | `Assets/Scripts/AR`, AR prefabs, Vuforia scene setup | Creates notes through `PlaceNote`, initializes each note through `NoteView.Initialize`, stores pose in `NoteData.worldPosition/worldRotation` |
| Member 2 | Task management and persistence | `NoteData`, `NoteManager`, `DatabaseManager`, edit UI | Uses `NoteManager.AddNote`, `UpdateNote`, `RemoveNote`, `GetAllNotes`, `SelectNote`; task state lives in `NoteData` |
| Member 3 | Custom styling | `Assets/Scripts/Styling`, note visual assets | Reads/writes `NoteData.colorName`, `iconId`, `priorityId`; applies visuals through `NoteStyleManager.ApplyStyle` |
| Member 4 | Reminders | `ReminderManager`, `AlarmManager`, reminder UI fields | Reads/writes reminders through `NoteData.SetReminder` and `ClearReminder`; schedules through `ReminderManager.ScheduleOrCancel`; `AlarmManager` owns Android notification delivery |
| Member 5 | Speech input | `SpeechToTextManager`, Android speech bridge | Starts dictation through `SpeechToTextManager.StartDictation`; writes transcript metadata to `NoteData`; no voice recording or audio-file storage |

Each member should keep feature-specific logic inside their module. Shared changes should go through `NoteData`, `NoteManager`, or `NoteView` only when the data or behavior is needed by more than one module.

## Folder Map

- `Assets/Scripts/AR`: AR placement, anchors, note views, and camera-facing helpers.
- `Assets/Scripts/Data`: serializable shared data contracts.
- `Assets/Scripts/Managers`: note registry, persistence, alarms, reminders, and speech-to-text services.
- `Assets/Scripts/Styling`: color, icon, and priority application.
- `Assets/Scripts/UI`: runtime edit panel, note history panel, toolbar helpers, picker drag handling, button action relay, and UI tests.
- `Assets/Editor`: readiness checks, migration helpers, Vuforia package setup, and Vuforia build fixes.
- `Assets/Plugins/Android`: custom Android manifest and native speech-recognition bridge.
- `Packages/manifest.json`: Unity package dependencies, including Vuforia Engine, Mobile Notifications, TextMeshPro, UGUI, and the Unity Test Framework.

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
6. Let Unity import assets and let Package Manager resolve dependencies, including `com.unity.mobile.notifications` and the local Vuforia package `com.ptc.vuforia.engine-11.4.4.tgz`.
7. Open `Assets/Scenes/MainScene.unity`.
8. Run `AR Note > Run Readiness Check`.

Do not commit generated local folders such as `Library`, `Logs`, `Temp`, `UserSettings`, or local tool settings.

## Run In Unity Editor

1. Open `Assets/Scenes/MainScene.unity`.
2. Press Play.
3. Confirm the Console has no red compile/runtime errors.
4. Click `Create Note` to create an Editor-friendly fallback note without waiting for Vuforia plane detection.
5. Tap/click a note, or click `Edit Note`, to open the edit panel.
6. Edit title, content, completion, reminder time, color, icon, priority, or speech fields where available.
7. Click `Save`.
8. Open `Note History` to verify saved notes appear and can be filtered by priority.
9. Stop Play Mode, enter Play Mode again, and confirm saved notes restore from local JSON.

Editor testing can verify CRUD, styling calls, persistence, runtime UI, history view, selection, and most note-management logic. Real AR plane detection, Android notifications, microphone permission, and speech recognition require an Android device.

## Android Build And Run

1. Connect an Android phone with USB debugging enabled.
2. In Unity, open `File > Build Settings`.
3. Select `Android`, then click `Switch Platform`.
4. Confirm `Assets/Scenes/MainScene.unity` is included in `Scenes In Build`.
5. Open `Edit > Project Settings > Player`.
6. Confirm the Android package settings are valid for the test device.
7. Confirm `Assets/Plugins/Android/AndroidManifest.xml` is present so camera, microphone, notification, exact-alarm, and speech-recognition permissions are available.
8. Click `Build And Run`.
9. On the phone, accept camera, microphone, notification, exact alarm, and battery-optimization prompts when they appear.
10. Move the phone slowly over a textured surface until Vuforia detects a plane.
11. Tap the detected surface to place a note.
12. Set a future reminder and verify the Android notification appears.
13. Tap the notification and verify the app opens the matching note editor.
14. Use speech input on a note and verify recognized text is appended to the note content.
15. Close and reopen the app to confirm notes restore with position, text, style, reminder, and transcript data.

If Build And Run fails, first run `AR Note > Run Readiness Check`, then check Package Manager resolution, Android SDK/NDK installation, Android permissions, and Console errors.

## Demo Flow

1. Build and run on an Android device.
2. Accept camera, microphone, notification, exact-alarm, and battery-optimization prompts when requested.
3. Scan a surface with Vuforia Ground Plane.
4. Tap a detected surface to place a note.
5. Tap the note or `Edit Note`.
6. Edit title, content, completion, reminder, color, icon, priority, and speech input.
7. Save the note and open `Note History` to show the saved task list and priority filters.
8. Set a future reminder, wait for the notification, then tap it to reopen the note.
9. Close and reopen the app to confirm saved notes restore.

For Editor-only testing, use `Create Note` to create a camera-directed fallback note without waiting for Vuforia plane detection.

## Implementation Notes

- `NoteData.ApplyDefaults()` normalizes ids, default text, style fields, reminder/alarm compatibility fields, transcript state, and color labels.
- `NoteManager` prevents duplicate note ids at runtime and while loading saved data.
- Completed notes are marked `(Done)` on the AR note and hidden by the edit-panel save flow.
- `NoteView` uses a cyan `LineRenderer` selection frame and normalizes the note collider so taps are reliable.
- UI taps are filtered in `PlaceNote` so buttons, input fields, toggles, history rows, and business UI do not accidentally create new AR notes.
- Reminder times use local device time in `yyyy-MM-dd HH:mm` format.
- Speech-to-text depends on Google/Android speech recognition. The app stores recognized text, source, and timestamp, but does not store audio.

## More Documentation

- `TEAM_HANDOFF.md`: member boundaries, shared interfaces, and safe amendment rules.
- `SIMULATION_GUIDE.md`: Editor and Android testing steps.
- `CRUD_TEST_GUIDE.md`: focused note create/read/update/delete test checklist.
