# CRUD Test Guide

Use this checklist to test note creation, loading, editing, deletion, and persistence without depending on Vuforia plane detection.

## Setup

1. Open `Assets/Scenes/MainScene.unity`.
2. Enter Play Mode.
3. Confirm the runtime buttons appear: `Create Note`, `Edit Note`, and `Clear DB`.

If the buttons do not appear, check that the scene has `NoteManager` and no compile errors.

## Create

1. Click `Clear DB`.
2. Click `Create Note`.
3. Confirm a note appears from the camera direction and the edit panel opens.

Looking down creates a desk/floor-like note. Looking forward creates a wall/front note.

## Read and Select

1. Close the edit panel.
2. Click the note or click `Edit Note`.
3. Confirm the selected note opens with its current title and content.

## Update

1. Change title, content, annotation, completed state, visibility, reminder fields, style, or speech text.
2. Click `Save`.
3. Reopen the note.
4. Confirm the changes remain visible.

Reminder time format:

```text
yyyy-MM-dd HH:mm
```

Example:

```text
2026-05-25 09:30
```

## Delete

1. Open a note.
2. Click `Delete`.
3. Confirm the note disappears.

## Persistence

1. Create a note.
2. Edit and save it.
3. Stop Play Mode.
4. Enter Play Mode again.
5. Confirm the note is restored from local JSON.

Saved note data is managed by `DatabaseManager` at `Application.persistentDataPath/ar_notes.json`.

## Pass Criteria

- `Create Note` creates or opens a nearby center-screen note.
- `Edit Note` opens the selected note or first available note.
- `Save` persists text, task state, visibility, reminder, style, and transcript metadata.
- `Delete` removes the note from scene and saved data.
- `Clear DB` removes saved notes.
- Restarting Play Mode restores saved notes unless `Clear DB` was used.
