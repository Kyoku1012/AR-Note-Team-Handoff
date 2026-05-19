# AR-NOTE-TEAM-HAND-OFF

AR Note is a Unity/Vuforia Android project for placing digital sticky notes in the user's physical environment. Users can create notes in AR, edit them as tasks, style them, set alarms, and attach voice or speech-to-text input.

## Project Goal

The app supports contextual task management. A note can be placed near a real object such as a desk, door, fridge, or computer, so the reminder appears where the task is relevant.

## Main Features

- AR note placement with Vuforia Ground Plane.
- Fallback `Create Note` button for Editor testing or when plane detection is unavailable.
- Add, edit, delete, complete, show, and hide notes.
- Local JSON persistence at `Application.persistentDataPath/ar_notes.json`.
- Note styling with color labels, icons, and priority markers.
- Android local alarms with optional repeat, snooze, and dismiss behavior.
- Voice memo recording and playback with WAV files saved under `Application.persistentDataPath/voice_notes`.
- Android speech-to-text input for title, content, and annotation fields.

## How the Project Works

The project is organized around one shared note model and several independent feature modules.

`NoteData` is the shared data contract. It stores the note text, task state, style, alarm fields, voice memo path, transcript fields, and AR world pose.

`NoteManager` is the central note service. Other modules add, update, delete, select, and retrieve notes through it instead of saving data themselves.

`DatabaseManager` handles local JSON loading and saving. Feature modules should not write directly to the JSON file.

`NoteView` is the bridge between a saved `NoteData` record and a note prefab in the AR scene. It refreshes text, style, visibility, selection state, and button actions.

`PlaceNote` owns AR placement. It creates note anchors from Vuforia hit tests and restores saved notes from their world positions.

Feature modules update the selected note's `NoteData`, then call `NoteView.SaveAndRefresh()` or `NoteManager.UpdateNote(note)`.

## Member Modules and Interfaces

| Member | Module | Owns | Public interface with other modules |
| --- | --- | --- | --- |
| Member 1 | AR placement | `Assets/Scripts/AR`, AR prefabs, Vuforia scene setup | Creates notes through `PlaceNote`, initializes each note through `NoteView.Initialize`, stores pose in `NoteData.worldPosition/worldRotation` |
| Member 2 | Task management | `NoteData`, `NoteManager`, `DatabaseManager`, edit UI | Uses `NoteManager.AddNote`, `UpdateNote`, `RemoveNote`, `GetAllNotes`, `SelectNote`; task state lives in `NoteData` |
| Member 3 | Custom styling | `Assets/Scripts/Styling`, note visual assets | Reads/writes `NoteData.colorName`, `iconId`, `priorityId`; applies visuals through `NoteStyleManager.ApplyStyle` |
| Member 4 | Reminders and alarms | `AlarmManager`, reminder UI fields | Reads/writes `NoteData.hasAlarm`, `alarmTime`, `alarmRepeatRule`, `alarmStatus`; schedules through `AlarmManager.ScheduleOrCancel` |
| Member 5 | Voice notes and speech input | `VoiceNoteManager`, `SpeechToTextManager`, Android speech bridge | Reads/writes `NoteData.hasVoiceNote`, `voiceFilePath`, transcript fields; starts recording through `VoiceNoteManager` and dictation through `SpeechToTextManager` |

Each member should keep feature-specific logic inside their module. Shared changes should go through `NoteData`, `NoteManager`, or `NoteView` only when the data or behavior is needed by more than one module.

## Folder Map

- `Assets/Scripts/AR`: AR placement, anchors, note views, and camera-facing helpers.
- `Assets/Scripts/Data`: serializable shared data contracts.
- `Assets/Scripts/Managers`: note registry, persistence, alarms, reminders, voice memos, and speech-to-text services.
- `Assets/Scripts/Styling`: color, icon, and priority application.
- `Assets/Scripts/UI`: runtime edit panel, toolbar helpers, and UI tests.
- `Assets/Editor`: readiness checks and migration helpers.
- `Assets/Plugins/Android`: Android-native speech recognition bridge.

## Setup

1. Open the project in Unity `2021.3.45f2`.
2. Let Package Manager resolve dependencies, including `com.unity.mobile.notifications`.
3. Open `Assets/Scenes/MainScene.unity`.
4. Run `AR Note > Run Readiness Check`.
5. Switch build target to Android for real AR, microphone, notification, and speech tests.

## Demo Flow

1. Build and run on an Android device.
2. Accept camera and microphone permissions when requested.
3. Scan a surface with Vuforia Ground Plane.
4. Tap a detected surface to place a note.
5. Tap the note or `Edit Note`.
6. Edit title, content, annotation, completion, visibility, alarm, voice memo, and speech input.
7. Apply color, icon, and priority from the style controls.
8. Close and reopen the app to confirm saved notes restore.

For Editor-only testing, use `Create Note` to create a camera-directed fallback note without waiting for Vuforia plane detection.

## More Documentation

- `TEAM_HANDOFF.md`: member boundaries, shared interfaces, and safe amendment rules.
- `SIMULATION_GUIDE.md`: Editor and Android testing steps.
- `CRUD_TEST_GUIDE.md`: focused note create/read/update/delete test checklist.
