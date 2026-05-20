# Team Handoff

Use this file when changing or testing another member's area. The goal is to keep each feature independent while sharing a small, clear interface.

## Shared Interfaces

| Shared script | Purpose | Use it for | Do not use it for |
| --- | --- | --- | --- |
| `NoteData` | Serializable note data contract | Fields that must persist after restart | Temporary UI state |
| `NoteManager` | Runtime note registry and save gateway | Add, update, delete, select, and retrieve notes | Feature-specific UI or platform logic |
| `DatabaseManager` | Local JSON persistence | Saving/loading the full note list | Direct feature calls from AR, styling, reminders, or speech code |
| `NoteView` | Scene adapter for one note prefab | Refreshing visuals and saving selected note changes | Owning feature-specific business rules |
| `PlaceNote` | AR placement service | Creating/restoring anchored note views | Editing task, style, reminder, or speech data |

Standard update flow:

1. Get the current `NoteView` from selection or creation.
2. Change fields on `noteView.Data`.
3. Call `noteView.SaveAndRefresh()`.

Use `NoteManager.UpdateNote(note)` only when there is no active `NoteView`.

## Member Boundaries

| Member | Main responsibility | Main files | Interface fields/methods |
| --- | --- | --- | --- |
| 1 | AR placement and restored note anchors | `Assets/Scripts/AR/PlaceNote.cs`, `Assets/Scripts/AR/NoteView.cs`, AR scene objects, note prefab | `PlaceNote.CreateCenterScreenNote`, `PlaceNote.AnchorCreated`, `NoteView.Initialize`, `NoteData.worldPosition`, `NoteData.worldRotation` |
| 2 | Task CRUD and local persistence | `Assets/Scripts/Data/NoteData.cs`, `Assets/Scripts/Managers/NoteManager.cs`, `Assets/Scripts/Managers/DatabaseManager.cs`, `Assets/Scripts/UI/NoteEditPanel.cs` | `NoteManager.AddNote`, `UpdateNote`, `RemoveNote`, `GetNote`, `GetAllNotes`, `SelectNote`; `NoteData.title`, `content`, `annotation`, `isCompleted`, `isVisible` |
| 3 | Styling | `Assets/Scripts/Styling/StylePanelController.cs`, `Assets/Scripts/Styling/NoteStyleManager.cs`, style sprites and prefab visuals | `StylePanelController.SetSelectedNote`, `NoteStyleManager.ApplyStyle`; `NoteData.colorName`, `colorLabel`, `iconId`, `priorityId` |
| 4 | Reminders and Android notification scheduling | `Assets/Scripts/Managers/ReminderManager.cs`, `Assets/Scripts/Managers/AlarmManager.cs`, reminder controls in `NoteEditPanel.cs` | `ReminderManager.ScheduleOrCancel`, `Cancel`, `Snooze`, `Dismiss`; `NoteData.SetReminder`, `ClearReminder`; `NoteData.hasReminder`, `reminderTime` |
| 5 | Speech input and integration checks | `Assets/Scripts/Managers/SpeechToTextManager.cs`, `Assets/Plugins/Android/SpeechRecognizerBridge.java`, speech controls in `NoteEditPanel.cs` | `SpeechToTextManager.StartDictation`; `NoteData.hasTranscript`, `transcriptText`, `transcriptSource` |

## Independence Rules

- Keep feature logic inside the owning module.
- Add fields to `NoteData` only when the value must be saved or shared.
- Use `ReminderManager` as the public reminder API. `AlarmManager` is implementation detail for local notifications and legacy alarm-field synchronization.
- Do not duplicate JSON save/load logic outside `DatabaseManager`.
- Do not create a second note registry; use `NoteManager`.
- Do not make styling, reminder, or speech scripts instantiate AR notes directly; go through `PlaceNote` or an existing `NoteView`.
- Preserve note prefab child names used by auto-wiring: `TitleText`, `ContentText`, `CheckButton`, `EditButton`, `DeleteButton`, and `VoiceButton`. `VoiceButton` now starts speech-to-text input; it does not record or play saved audio.

## Refactor Notes

- `VoiceNoteManager` and the old `Dee-VoiceManager` copy have been removed. Voice scope is speech-to-text only.
- `NoteEditPanel.cs` still owns the runtime edit UI, but generic helper components now live in `PickerColumnDragHandler.cs` and `RuntimeButtonActionRelay.cs`.
- `NoteData` still contains legacy `alarm*` fields for saved-data compatibility. New code should prefer `hasReminder`, `reminderTime`, `SetReminder`, and `ClearReminder`.

## Safe Amendment Checklist

1. Pull the latest project files.
2. Open `Assets/Scenes/MainScene.unity`.
3. Run `AR Note > Run Readiness Check`.
4. Test your own feature in Play Mode.
5. Test on Android if your feature uses AR, notification, microphone, or speech recognition.
6. Check `git status` before sharing changes.
7. Do not commit generated folders such as `Library`, `Logs`, `Temp`, `UserSettings`, builds, APKs, or keystore files.
