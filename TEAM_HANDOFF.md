# AR Note Team Handoff

This project is set up so each member can test and amend their module without breaking the shared AR note flow.

## How to Open and Check

1. Open the project with Unity `2021.3.45f2`.
2. Let Package Manager finish resolving packages, especially `com.unity.mobile.notifications`.
3. Open `Assets/Scenes/MainScene.unity`.
4. Run `AR Note > Run Readiness Check` from the Unity top menu.
5. Fix any listed readiness issues before building an APK.

## Shared Architecture

- `Assets/Scripts/Data/NoteData.cs` is the shared save model. Add new fields here only when the data must survive app restarts.
- `Assets/Scripts/Managers/NoteManager.cs` is the single source of truth for all notes in memory.
- `Assets/Scripts/Managers/DatabaseManager.cs` saves and loads local JSON from `Application.persistentDataPath/ar_notes.json`.
- `Assets/Scripts/AR/NoteView.cs` binds one AR prefab instance to one `NoteData` record.
- `Assets/Scripts/AR/PlaceNote.cs` owns Vuforia surface hit testing, note creation, and restore from saved world poses.
- `Assets/Scripts/Styling/StylePanelController.cs` updates the currently selected `NoteView`.
- `Assets/Scripts/Managers/ReminderManager.cs` schedules or cancels Android local reminders.
- `Assets/Scripts/Managers/VoiceNoteManager.cs` records, saves, plays, and deletes WAV voice memos.
- `Assets/Scripts/UI/NoteEditPanel.cs` is a runtime fallback edit UI, so the app stays testable even if scene UI is incomplete.
- The runtime UI exposes `Create Note`, `Edit Note`, and `Clear DB`; `Create Note` creates a center-screen note facing the active camera.

## Folder Map

- `Assets/Scripts/AR`: AR placement, anchors, note views, camera-facing helpers.
- `Assets/Scripts/Data`: serializable data contracts shared by every member.
- `Assets/Scripts/Managers`: persistence, note registry, reminders, and voice services.
- `Assets/Scripts/Styling`: note visual style application and style-panel controls.
- `Assets/Scripts/UI`: runtime UI panels, toolbar helpers, and simple UI tests.
- `Assets/Editor`: editor-only checks and migration helpers.
- `Assets/Scenes`: Unity scenes only.
- `Assets/Prefabs`: reusable note and test prefabs.

## Member Boundaries

- Member 1 should edit `Assets/Scripts/AR`, AR prefabs, Vuforia scene objects, and placement/orientation behavior.
- Member 2 should edit task fields and UI through `Assets/Scripts/UI/NoteEditPanel.cs`, `Assets/Scripts/Managers/NoteManager.cs`, and `Assets/Scripts/Data/NoteData.cs`.
- Member 3 should edit style assets, `Assets/Scripts/Styling`, and note prefab visuals.
- Member 4 should edit reminder UI and `Assets/Scripts/Managers/ReminderManager.cs`.
- Member 5 should edit voice controls and `Assets/Scripts/Managers/VoiceNoteManager.cs`.

Avoid duplicating save logic in feature scripts. Change the selected note's `NoteData`, then call `NoteView.SaveAndRefresh()` or `NoteManager.UpdateNote(note)`.

## Android Demo Checklist

1. Build target is Android.
2. `MainScene` is in Build Settings.
3. App launches and requests camera permission.
4. Vuforia detects a surface.
5. Tapping a surface creates a note.
6. `Create Note` creates a center-screen note when plane detection is unavailable.
7. Tapping UI does not create a note.
8. Edit title/content/annotation and save.
9. Apply color, icon, and priority from the style panel.
10. Toggle completed and visible.
11. Set reminder time in `yyyy-MM-dd HH:mm` format and confirm the notification fires.
12. Record and play a voice memo.
13. Restart the app and confirm notes, styles, tasks, reminders, and voice paths persist.

## Safe Amendment Rules

- Keep field names in `NoteData` stable unless all loading/saving code is updated.
- Keep note prefab child names `TitleText`, `ContentText`, `CheckButton`, `EditButton`, `DeleteButton`, and `VoiceButton` if you want `NoteView` auto-wiring to work.
- If a scene object is removed, run the readiness check before committing.
- Test on a real Android device for AR, notifications, and microphone; Editor testing cannot fully validate these features.
