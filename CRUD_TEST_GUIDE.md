# AR Note CRUD Test Guide

This guide is for testing note data creation, reading/loading, editing, deleting, and local JSON persistence without depending on Vuforia ground-plane detection.

## Before Testing

Open `Assets/Scenes/MainScene.unity`.

Enter Play Mode and wait until scripts finish compiling. The runtime note UI should show these launcher buttons near the left side of the screen:

- `Create Note`
- `Edit Note`
- `Clear DB`

If the buttons do not appear, confirm that `Managers` exists in the scene and has `NoteManager`, or check the Console for compile errors.

## Create

1. Click `Clear DB`.
2. Click `Create Note`.
3. Expected result:
   - A note appears in front of the camera.
   - The note front faces the active camera.
   - The edit panel opens.
   - The selected note shows a cyan selection frame.
   - Console includes:

```text
Created center-screen note without ground-plane detection.
```

If no note is selected, clicking `Edit Note` creates and opens a center-screen note.

`Create Note` creates from the center of the screen. If an existing note is already under the center point, the app opens that note instead of creating an overlapping note.

## Read / Select

1. Close the edit panel.
2. Click `Edit Note`.
3. Expected result:
   - The existing selected or first available note opens in the edit panel.
   - Title/content fields show the current note data.

If the console only shows `Touch is over blocking UI, skip plane hit test`, check for:

```text
Manually invoked runtime UI relay: Edit NoteButton
```

If that relay log is missing, the UI raycast did not reach the runtime button.

## Update

1. Open a note in the edit panel.
2. Change:
   - Title
   - Content
   - Annotation
   - Completed checkbox
   - Visible checkbox
3. Click `Save`.
4. Click `Edit Note` again.
5. Expected result:
   - The changed values are still shown.
   - The note visual text updates in the scene.

For style updates:

1. Select or create a note.
2. Click a style color/icon/priority button.
3. Expected result:
   - The selected note changes style immediately.
   - No warning about missing selected note appears.

## Delete

1. Open a note in the edit panel.
2. Click `Delete`.
3. Expected result:
   - The note disappears from the scene.
   - Clicking `Edit Note` creates a new center-screen note only if no notes remain.

## Persistence

1. Click `Clear DB`.
2. Click `Create Note`.
3. Edit title/content and click `Save`.
4. Stop Play Mode.
5. Enter Play Mode again.
6. Expected result:
   - Saved notes are loaded from local JSON.
   - Clicking `Edit Note` opens the restored note.

The JSON save file is stored under Unity's `Application.persistentDataPath` as managed by `DatabaseManager`.

## Android Build Test

1. Build and run the Android APK.
2. Tap `Clear DB`.
3. Tap `Create Note`.
4. Edit and save note fields.
5. Close and reopen the app.
6. Tap `Edit Note`.

Expected result:

- CRUD works even if Vuforia does not detect a plane.
- Ground-plane placement still works when tapping real-world space outside UI.
- UI taps should not create unwanted AR notes.

## Useful Console Logs

Successful runtime button fallback:

```text
Manually invoked runtime UI relay: Edit NoteButton
```

Successful center-screen note creation:

```text
Created center-screen note without ground-plane detection.
```

Expected UI blocking log:

```text
Touch is over blocking UI, skip plane hit test.
```

This log is normal when tapping UI. It only indicates a problem if no button action log appears before it.

## Quick Pass Criteria

CRUD is considered working when:

- `Create Note` creates a normal note from screen center, unless an existing note is under the center point.
- Center-screen notes face the active camera when created.
- `Edit Note` opens the selected or first note.
- Selected notes show a visible selection frame, and non-selected notes do not.
- `Save` persists title/content/annotation/toggles.
- Style buttons update the selected note.
- `Delete` removes the note from scene and saved data.
- `Clear DB` removes all notes and saved voice files.
- Stop Play Mode and re-enter Play Mode restores saved notes unless `Clear DB` was used.
