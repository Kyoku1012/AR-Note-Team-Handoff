# AR Note Team Handoff

This project is set up so each member can test and amend their module without breaking the shared AR note flow.

## How to Open and Check

1. Open the project with Unity `2021.3.45f2`.
2. Let Package Manager finish resolving packages, especially `com.unity.mobile.notifications`.
3. Open `Assets/Scenes/MainScene.unity`.
4. Run `AR Note > Run Readiness Check` from the Unity top menu.
5. Fix any listed readiness issues before building an APK.

## Shared Architecture

- `NoteData` is the shared save model. Add new fields here only when the data must survive app restarts.
- `NoteManager` is the single source of truth for all notes in memory.
- `DatabaseManager` saves and loads local JSON from `Application.persistentDataPath/ar_notes.json`.
- `NoteView` binds one AR prefab instance to one `NoteData` record.
- `PlaceNote` owns Vuforia surface hit testing, note creation, and restore from saved world poses.
- `StylePanelController` updates the currently selected `NoteView`.
- `ReminderManager` schedules or cancels Android local reminders.
- `VoiceNoteManager` records, saves, plays, and deletes WAV voice memos.
- `NoteEditPanel` is a runtime fallback edit UI, so the app stays testable even if scene UI is incomplete.

## Member Boundaries

- Member 1 should edit `PlaceNote`, `NoteView`, AR prefabs, Vuforia scene objects, and placement/orientation behavior.
- Member 2 should edit task fields and UI through `NoteEditPanel`, `NoteManager`, and `NoteData`.
- Member 3 should edit style assets, `NoteStyleManager`, `StylePanelController`, and note prefab visuals.
- Member 4 should edit reminder UI and `ReminderManager`.
- Member 5 should edit voice controls and `VoiceNoteManager`.

Avoid duplicating save logic in feature scripts. Change the selected note's `NoteData`, then call `NoteView.SaveAndRefresh()` or `NoteManager.UpdateNote(note)`.

## Android Demo Checklist

1. Build target is Android.
2. `MainScene` is in Build Settings.
3. App launches and requests camera permission.
4. Vuforia detects a surface.
5. Tapping a surface creates a note.
6. Tapping UI does not create a note.
7. Edit title/content/annotation and save.
8. Apply color, icon, and priority from the style panel.
9. Toggle completed and visible.
10. Set reminder time in `yyyy-MM-dd HH:mm` format and confirm the notification fires.
11. Record and play a voice memo.
12. Restart the app and confirm notes, styles, tasks, reminders, and voice paths persist.

## Safe Amendment Rules

- Keep field names in `NoteData` stable unless all loading/saving code is updated.
- Keep note prefab child names `TitleText`, `ContentText`, `CheckButton`, `EditButton`, `DeleteButton`, and `VoiceButton` if you want `NoteView` auto-wiring to work.
- If a scene object is removed, run the readiness check before committing.
- Test on a real Android device for AR, notifications, and microphone; Editor testing cannot fully validate these features.
