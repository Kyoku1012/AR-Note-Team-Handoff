# AR Note Simulation and Testing Guide

This guide explains how each member can simulate, test, and amend the Unity project safely.

## 1. Required Setup

Use these versions and tools:

- Unity Editor `2021.3.45f2`.
- Android Build Support, Android SDK/NDK, and OpenJDK installed through Unity Hub.
- A real Android phone for full AR, reminder, and microphone testing.
- Git branch `freature/miki-refactor` from `https://github.com/Kyoku1012/AR-Note-Team-Handoff`.

After cloning:

1. Open the project in Unity Hub.
2. Wait until Package Manager finishes resolving packages.
3. Open `Assets/Scenes/MainScene.unity`.
4. Run `AR Note > Run Readiness Check`.
5. Fix all readiness warnings before testing on Android.

## 2. What Can Be Simulated in Unity Editor

The Unity Editor can test:

- Project compile and package resolution.
- `MainScene` loading.
- Runtime edit panel creation.
- Note data save/load code in Play Mode.
- Style UI method calls.
- General prefab references and scene wiring.

The Unity Editor cannot fully test:

- Real Vuforia Ground Plane detection.
- Real Android camera permission behavior.
- Android local notification delivery.
- Android microphone permission behavior.
- Reliable physical-world note placement.

For final marks/demo, always test on a real Android device.

## 3. Editor Simulation Steps

1. Open `Assets/Scenes/MainScene.unity`.
2. Press Play.
3. Confirm there are no red console errors.
4. Confirm the `Edit Note` button appears.
5. Press `N` or click `Test Note` to create an editor-only test note in front of the camera.
6. Use `Edit Note` to change title, content, annotation, completion, visibility, reminder fields, and voice controls.
7. Press Save and confirm the note updates.
8. Stop Play Mode, press Play again, and confirm saved test notes reload.
9. Run `AR Note > Run Readiness Check`.
10. Stop Play Mode.

The `Test Note` button and `N` shortcut bypass Vuforia plane detection. Use them only for Editor testing of note data, save/load, editing, styling, and UI panel behavior. Real AR placement still needs Android device testing.

If the project does not compile:

- Check Package Manager for missing packages.
- Confirm `com.unity.mobile.notifications` is installed.
- Confirm scripts are under `Assets/Scripts`.
- Do not move scripts without moving their `.meta` files.

## 3.1 Testing Notes Without Ground Plane Detection

Vuforia Ground Plane can fail to produce plane hits in the Unity Editor. This is expected. To test note data and UI anyway:

1. Open `MainScene`.
2. Press Play.
3. Click `Test Note`, or press the `N` key.
4. A note is created about 1.2 meters in front of the active camera.
5. The note is saved through the same `NoteManager` and `DatabaseManager` flow as real AR notes.
6. Edit the note with `Edit Note`.
7. Restart Play Mode to confirm the note reloads from local JSON.

This tests:

- `NoteData` creation.
- `NoteManager.AddNote`.
- `DatabaseManager.SaveNotes` and `LoadNotes`.
- `NoteView.RefreshFromData`.
- `NoteEditPanel` title/content/annotation/completed/visibility fields.
- Style panel integration with the selected note.

This does not test:

- Vuforia surface detection.
- Real Android anchor stability.
- Physical-world note position accuracy.

## 4. Android Build Setup

1. Go to `File > Build Settings`.
2. Select `Android`.
3. Click `Switch Platform`.
4. Confirm `Assets/Scenes/MainScene.unity` is listed and checked.
5. Go to `Edit > Project Settings > Player`.
6. Confirm:
   - Product Name: `AR-Note-Team-Handoff`.
   - Package Name: `com.Kyoku1012.ARNoteTeamHandoff`.
   - Minimum API Level: Android 10/API 29 or compatible with the test phone.
   - Active Input Handling: Both.
7. Connect an Android phone with USB debugging enabled.
8. Click `Build And Run`.

## 5. Android Simulation/Demo Flow

Use this flow for group testing:

1. Launch the app on Android.
2. Accept camera permission.
3. Move the phone slowly over a desk, floor, or wall until Vuforia detects a plane.
4. Tap a detected real-world surface.
5. Confirm one note appears at the tapped location.
6. Tap the note or `Edit Note`.
7. Edit title, content, and annotation.
8. Save and confirm the note text updates.
9. Use style buttons to change color, icon, and priority.
10. Toggle completed and confirm the note title shows `(Done)`.
11. Toggle visible and confirm the note hides/shows.
12. Set reminder time using `yyyy-MM-dd HH:mm`.
13. Record a voice memo, stop recording, then play it.
14. Close and reopen the app.
15. Confirm saved notes restore with their text, styles, task state, reminder data, and voice memo path.

## 6. Member-Specific Testing

### Member 1: AR Placement

Test:

- Plane detection starts.
- UI taps do not place notes.
- Surface taps place notes.
- Multiple notes can be placed.
- Notes face the camera/readable direction.
- Notes restore after app restart.

Main files:

- `Assets/Scripts/AR/PlaceNote.cs`
- `Assets/Scripts/AR/NoteView.cs`
- `Assets/Prefabs/NotePrefab_NEW.prefab`
- `Assets/Scenes/MainScene.unity`

### Member 2: Task Management

Test:

- Edit title/content/annotation.
- Check/uncheck completed.
- Delete note.
- Show/hide note.
- Restart app and confirm data persists.

Main files:

- `Assets/Scripts/UI/NoteEditPanel.cs`
- `Assets/Scripts/Managers/NoteManager.cs`
- `Assets/Scripts/Data/NoteData.cs`
- `Assets/Scripts/Managers/DatabaseManager.cs`

### Member 3: Custom Styling

Test:

- Select a note.
- Apply yellow, pink, blue, and green.
- Apply each icon.
- Apply high, medium, and low priority.
- Hide icon and hide priority.
- Restart app and confirm style persists.

Main files:

- `Assets/Scripts/Styling/StylePanelController.cs`
- `Assets/Scripts/Styling/NoteStyleManager.cs`
- `Assets/Scenes/UI`
- `Assets/Prefabs/NotePrefab_NEW.prefab`

### Member 4: Reminders

Test:

- Set a reminder a few minutes ahead.
- Confirm notification appears on Android.
- Edit reminder time and confirm only the new reminder fires.
- Delete note and confirm no reminder fires for it.

Main files:

- `Assets/Scripts/Managers/ReminderManager.cs`
- `Assets/Scripts/UI/NoteEditPanel.cs`
- `Assets/Scripts/Data/NoteData.cs`

Reminder format:

```text
yyyy-MM-dd HH:mm
```

Example:

```text
2026-05-25 09:30
```

### Member 5: Voice Notes and Integration

Test:

- First recording asks for microphone permission.
- Record a memo.
- Stop recording.
- Play memo.
- Replace memo with a new recording.
- Delete memo.
- Restart app and confirm the voice path remains saved.

Main files:

- `Assets/Scripts/Managers/VoiceNoteManager.cs`
- `Assets/Scripts/UI/NoteEditPanel.cs`
- `Assets/Scripts/AR/NoteView.cs`

Voice files are saved under:

```text
Application.persistentDataPath/voice_notes
```

## 7. Persistence Testing

Saved note data lives at:

```text
Application.persistentDataPath/ar_notes.json
```

To test persistence:

1. Create two notes.
2. Edit each note with different title, completion, style, reminder, and voice memo.
3. Fully close the app.
4. Reopen the app.
5. Confirm both notes restore.

To reset all saved notes during testing:

- Delete the app from the Android phone, then reinstall it.
- Or clear app storage from Android Settings.

## 8. Common Problems and Fixes

- **No plane detected**: use a textured surface, improve lighting, and move the phone slowly.
- **Tapping UI creates a note**: confirm an `EventSystem` exists in `MainScene`.
- **Style buttons do nothing**: place/select a note first, then press the style button.
- **Reminder does not fire**: test on Android, set a future time, and check notification permission/settings.
- **Voice recording does not start**: allow microphone permission and test on a physical phone.
- **Notes do not save**: check console for `DatabaseManager` errors and confirm app storage is available.
- **Scripts missing after moving folders**: move `.cs` and `.cs.meta` together.
- **Git shows Library/Temp changes**: do not add them; they are generated and ignored.

## 9. Before Pushing Changes

Each member should:

1. Run `AR Note > Run Readiness Check`.
2. Test their feature in Play Mode where possible.
3. Test on Android if their feature uses AR, notification, or microphone behavior.
4. Check `git status`.
5. Do not commit `Library`, `Temp`, `Logs`, `UserSettings`, builds, APKs, or keystore files.
6. Update `TEAM_HANDOFF.md` or this guide if they change a shared workflow.
