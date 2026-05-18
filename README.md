# AR-Note-Team-Handoff
* An augmented reality (AR) mobile application that enables users to place digital sticky notes, to-do lists, and reminders within their physical environment.

## Implemented group-project feature set

- Vuforia Ground Plane placement creates persistent AR notes with saved world position and rotation.
- Notes are stored locally in `Application.persistentDataPath/ar_notes.json`.
- A selected note can be edited through the runtime **Edit Note** panel: title, content, annotation, completed state, visibility, reminder time, delete, and voice memo controls.
- The existing style panel now edits the real selected note instead of test data, including color, icon, and priority.
- Android local reminders use Unity Mobile Notifications (`com.unity.mobile.notifications`).
- Voice memos use Unity's `Microphone` API and save WAV files under `Application.persistentDataPath/voice_notes`.

## Demo flow

1. Build and run on Android.
2. Scan for a surface with Vuforia Ground Plane.
3. Tap a real-world surface to place a note.
4. Use **Edit Note** to update note details, reminder time (`yyyy-MM-dd HH:mm`), completion, visibility, and voice memo.
5. Use the style buttons to apply color, icon, and priority to the selected note.
6. Restart the app to confirm saved notes restore from local JSON.

## Team testing and amendment

- Read `TEAM_HANDOFF.md` before changing another member's module.
- Read `SIMULATION_GUIDE.md` before testing in Unity Editor or on Android.
- In Unity, run `AR Note > Run Readiness Check` before Android testing or handoff.
- Keep `NoteData`, `NoteManager`, and `NoteView` as the shared integration points between all member features.
- Runtime scripts are organized under `Assets/Scripts` by module: `AR`, `Data`, `Managers`, `Styling`, and `UI`.
