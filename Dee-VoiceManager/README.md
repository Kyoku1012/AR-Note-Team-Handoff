# Dee Voice Manager Reference

This folder contains reference/prototype scripts from Dee's voice-note work.
Do not copy these files directly into `Assets/` because they declare the same
runtime class names as the production scripts:

- `NoteData`
- `NoteView`
- `VoiceNoteManager`

The production implementation lives under:

- `Assets/Scripts/Data/NoteData.cs`
- `Assets/Scripts/AR/NoteView.cs`
- `Assets/Scripts/Managers/VoiceNoteManager.cs`

Voice-note behavior should be merged into the production scripts instead of
replacing them wholesale.
