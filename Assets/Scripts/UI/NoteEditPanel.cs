using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class NoteEditPanel : MonoBehaviour
{
    private NoteView currentNote;
    private InputField titleInput;
    private InputField contentInput;
    private InputField annotationInput;
    private InputField reminderInput;
    private InputField snoozeInput;
    private Toggle completedToggle;
    private Toggle visibleToggle;
    private Toggle reminderToggle;
    private Dropdown alarmRepeatDropdown;
    private Text voiceStatusText;
    private Text speechStatusText;

    public static NoteEditPanel EnsureExists()
    {
        NoteEditPanel existing = FindObjectOfType<NoteEditPanel>(true);
        if (existing != null)
            return existing;

        GameObject canvasObject = new GameObject("RuntimeNoteEditCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        GameObject panelObject = CreatePanel(canvasObject.transform);
        NoteEditPanel panel = panelObject.AddComponent<NoteEditPanel>();
        panel.BuildUi(panelObject.transform);
        panel.gameObject.SetActive(false);
        CreateLauncher(canvasObject.transform, panel);
        return panel;
    }

    public void Open(NoteView noteView)
    {
        currentNote = noteView;
        if (currentNote == null || currentNote.Data == null)
            return;

        NoteManager.Instance?.SelectNote(currentNote);
        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(currentNote);

        NoteData data = currentNote.Data;
        titleInput.text = data.title;
        contentInput.text = data.content;
        annotationInput.text = data.annotation;
        completedToggle.isOn = data.isCompleted;
        visibleToggle.isOn = data.isVisible;
        reminderToggle.isOn = data.hasAlarm;
        reminderInput.text = data.alarmTime;
        snoozeInput.text = data.alarmSnoozeMinutes.ToString();
        SetAlarmRepeatDropdown(data.alarmRepeatRule);
        VoiceNoteManager.Instance?.LoadFromNote(data);
        UpdateVoiceStatus();
        UpdateSpeechStatus("");

        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void OpenSelected()
    {
        NoteView selected = NoteManager.Instance == null ? null : NoteManager.Instance.SelectedNote ?? NoteManager.Instance.GetFirstView();
        if (selected != null)
        {
            Open(selected);
            return;
        }

        Debug.Log("Edit Note clicked with no selected note; creating a center-screen note for editing.");
        PlaceNote placeNote = FindObjectOfType<PlaceNote>();
        if (placeNote != null)
        {
            NoteView note = placeNote.CreateCenterScreenNote();
            if (note != null)
                Open(note);
        }
    }

    private void Save()
    {
        if (currentNote == null || currentNote.Data == null)
            return;

        NoteData data = currentNote.Data;
        data.title = titleInput.text;
        data.content = contentInput.text;
        data.annotation = annotationInput.text;
        data.isCompleted = completedToggle.isOn;
        data.isVisible = visibleToggle.isOn;
        data.hasAlarm = reminderToggle.isOn;
        data.alarmTime = reminderInput.text;
        data.alarmRepeatRule = GetSelectedAlarmRepeatRule();
        data.alarmSnoozeMinutes = ParsePositiveInt(snoozeInput.text, 5);
        data.hasReminder = data.hasAlarm;
        data.reminderTime = data.alarmTime;

        if (data.hasAlarm && !AlarmManager.TryParseAlarmTime(data.alarmTime, out DateTime _))
        {
            Debug.LogWarning("Alarm time must be like 2026-05-25 09:30.");
            data.hasAlarm = false;
            data.hasReminder = false;
            reminderToggle.isOn = false;
        }

        currentNote.SaveAndRefresh();
        Close();
    }

    private void DeleteCurrent()
    {
        if (currentNote == null || currentNote.Data == null)
            return;

        string noteId = currentNote.Data.id;
        Close();
        NoteManager.Instance?.RemoveNote(noteId);
    }

    private void SnoozeAlarm()
    {
        if (currentNote == null || currentNote.Data == null)
            return;

        int minutes = ParsePositiveInt(snoozeInput.text, currentNote.Data.alarmSnoozeMinutes);
        AlarmManager.Instance?.Snooze(currentNote.Data, minutes);
        currentNote.SaveAndRefresh();
        Open(currentNote);
    }

    private void DismissAlarm()
    {
        if (currentNote == null || currentNote.Data == null)
            return;

        AlarmManager.Instance?.Dismiss(currentNote.Data);
        currentNote.SaveAndRefresh();
        Open(currentNote);
    }

    private void ToggleRecord()
    {
        if (currentNote == null) return;

        VoiceNoteManager.Instance?.ToggleRecording(currentNote);
        UpdateVoiceStatus();
    }

    private void PlayVoice()
    {
        currentNote?.PlayVoice();
    }

    private void DeleteVoice()
    {
        if (currentNote == null) return;

        VoiceNoteManager.Instance?.DeleteVoice(currentNote);
        UpdateVoiceStatus();
    }

    private void DictateTitle()
    {
        StartDictation("title", titleInput);
    }

    private void DictateContent()
    {
        StartDictation("content", contentInput);
    }

    private void DictateAnnotation()
    {
        StartDictation("annotation", annotationInput);
    }

    private void StartDictation(string targetField, InputField input)
    {
        if (input == null)
            return;

        UpdateSpeechStatus("Listening for " + targetField + "...");
        SpeechToTextManager.Instance?.StartDictation(
            text =>
            {
                input.text = string.IsNullOrWhiteSpace(input.text) ? text : input.text + " " + text;
                if (currentNote != null && currentNote.Data != null)
                {
                    currentNote.Data.hasTranscript = true;
                    currentNote.Data.transcriptText = text;
                    currentNote.Data.transcriptSource = targetField;
                    currentNote.Data.transcriptUpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }
                UpdateSpeechStatus("Speech added to " + targetField + ".");
            },
            error => UpdateSpeechStatus(error));
    }

    private void UpdateVoiceStatus()
    {
        if (voiceStatusText == null || currentNote == null || currentNote.Data == null)
            return;

        if (VoiceNoteManager.Instance != null && VoiceNoteManager.Instance.IsRecording(currentNote))
        {
            voiceStatusText.text = "Recording voice memo...";
            return;
        }

        voiceStatusText.text = currentNote.Data.hasVoiceNote ? "Voice memo attached" : "No voice memo";
    }

    private void UpdateSpeechStatus(string message)
    {
        if (speechStatusText == null)
            return;

        speechStatusText.text = string.IsNullOrWhiteSpace(message) ? "Speech input ready" : message;
    }

    private void BuildUi(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateTopAnchoredLabel(parent, "Edit AR Note", font, 24, new Vector2(0, -24), new Vector2(340, 34));
        titleInput = CreateInput(parent, "Title", font, new Vector2(0, -72), new Vector2(340, 34));
        CreateButton(parent, "Mic", font, new Vector2(186, -72), new Vector2(48, 34), DictateTitle);
        contentInput = CreateInput(parent, "Content", font, new Vector2(0, -116), new Vector2(340, 34));
        CreateButton(parent, "Mic", font, new Vector2(186, -116), new Vector2(48, 34), DictateContent);
        annotationInput = CreateInput(parent, "Annotation", font, new Vector2(0, -160), new Vector2(340, 34));
        CreateButton(parent, "Mic", font, new Vector2(186, -160), new Vector2(48, 34), DictateAnnotation);
        speechStatusText = CreateTopAnchoredLabel(parent, "Speech input ready", font, 13, new Vector2(0, -196), new Vector2(340, 22));

        completedToggle = CreateToggle(parent, "Completed", font, new Vector2(-105, -230));
        visibleToggle = CreateToggle(parent, "Visible", font, new Vector2(95, -230));
        reminderToggle = CreateToggle(parent, "Alarm", font, new Vector2(-105, -272));
        reminderInput = CreateInput(parent, "yyyy-MM-dd HH:mm", font, new Vector2(70, -272), new Vector2(205, 32));
        alarmRepeatDropdown = CreateDropdown(parent, font, new Vector2(-95, -314), new Vector2(140, 32), "None", "Daily", "Weekly");
        snoozeInput = CreateInput(parent, "Snooze min", font, new Vector2(92, -314), new Vector2(150, 32));
        CreateButton(parent, "Snooze", font, new Vector2(-72, -356), new Vector2(100, 32), SnoozeAlarm);
        CreateButton(parent, "Dismiss", font, new Vector2(72, -356), new Vector2(100, 32), DismissAlarm);

        voiceStatusText = CreateTopAnchoredLabel(parent, "No voice memo", font, 15, new Vector2(0, -396), new Vector2(340, 24));

        CreateButton(parent, "Record", font, new Vector2(-126, -434), new Vector2(94, 34), ToggleRecord);
        CreateButton(parent, "Play", font, new Vector2(0, -434), new Vector2(94, 34), PlayVoice);
        CreateButton(parent, "Remove", font, new Vector2(126, -434), new Vector2(94, 34), DeleteVoice);

        CreateButton(parent, "Save", font, new Vector2(-126, -482), new Vector2(94, 38), Save);
        CreateButton(parent, "Delete", font, new Vector2(0, -482), new Vector2(94, 38), DeleteCurrent);
        CreateButton(parent, "Close", font, new Vector2(126, -482), new Vector2(94, 38), Close);
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("NoteEditPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -24);
        rect.sizeDelta = new Vector2(430, 550);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.08f, 0.08f, 0.08f, 0.88f);
        return panel;
    }

    private static void CreateLauncher(Transform parent, NoteEditPanel panel)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        CreateButton(parent, "Create Note", font, new Vector2(-230, -40), new Vector2(120, 40), panel.CreateCenterScreenNote);
        CreateButton(parent, "Edit Note", font, new Vector2(-230, -88), new Vector2(120, 40), panel.OpenSelected);
        CreateButton(parent, "Clear DB", font, new Vector2(-230, -136), new Vector2(120, 40), panel.ClearAllData);
    }

    private void ClearAllData()
    {
        if (NoteManager.Instance == null)
            return;

        Close();
        currentNote = null;
        NoteManager.Instance.ClearAllNotesAndData();
        Debug.Log("All notes and local voice files have been cleared.");
    }

    private void CreateCenterScreenNote()
    {
        PlaceNote placeNote = FindObjectOfType<PlaceNote>();
        if (placeNote == null)
        {
            Debug.LogWarning("No PlaceNote component found in the scene. Open MainScene before creating a center-screen note.");
            return;
        }

        NoteView note = placeNote.CreateCenterScreenNote();
        if (note != null)
            Open(note);
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.GetComponent<StandaloneInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("StandaloneInputModule added to existing EventSystem for note UI compatibility.");
            }
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        Debug.Log("Runtime EventSystem created for note UI.");
    }

    private static Text CreateTopAnchoredLabel(Transform parent, string text, Font font, int size, Vector2 position, Vector2 dimensions)
    {
        GameObject obj = new GameObject(text + "Label");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Text CreateCenteredChildLabel(Transform parent, string text, Font font, int size, Color color, TextAnchor alignment, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject obj = new GameObject(text + "Label");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.font = font;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private static InputField CreateInput(Transform parent, string placeholder, Font font, Vector2 position, Vector2 dimensions)
    {
        GameObject obj = new GameObject(placeholder + "Input");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = Color.white;

        InputField input = obj.AddComponent<InputField>();
        Text text = CreateInputText(obj.transform, "Text", font, Color.black);
        Text placeholderText = CreateInputText(obj.transform, "Placeholder", font, new Color(0.45f, 0.45f, 0.45f));
        placeholderText.text = placeholder;
        input.textComponent = text;
        input.placeholder = placeholderText;
        return input;
    }

    private static Text CreateInputText(Transform parent, string name, Font font, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8, 4);
        rect.offsetMax = new Vector2(-8, -4);

        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 16;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private static Toggle CreateToggle(Transform parent, string label, Font font, Vector2 position)
    {
        GameObject obj = new GameObject(label + "Toggle");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(150, 30);

        Image background = obj.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.02f);

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.targetGraphic = background;
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(obj.transform, false);
        RectTransform checkRect = checkmark.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f);
        checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.sizeDelta = new Vector2(22, 22);
        checkRect.anchoredPosition = new Vector2(14, 0);
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = new Color(0.2f, 0.8f, 0.35f);
        toggle.graphic = checkImage;

        CreateCenteredChildLabel(obj.transform, label, font, 15, Color.white, TextAnchor.MiddleLeft, new Vector2(30, 0), new Vector2(0, 0));
        return toggle;
    }

    private static Dropdown CreateDropdown(Transform parent, Font font, Vector2 position, Vector2 dimensions, params string[] options)
    {
        GameObject obj = new GameObject("AlarmRepeatDropdown");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = Color.white;

        Dropdown dropdown = obj.AddComponent<Dropdown>();
        Text label = CreateCenteredChildLabel(obj.transform, "Label", font, 15, Color.black, TextAnchor.MiddleLeft, new Vector2(8, 0), new Vector2(-8, 0));
        dropdown.captionText = label;
        CreateDropdownTemplate(obj.transform, dropdown, font, dimensions);
        dropdown.options.Clear();
        foreach (string option in options)
            dropdown.options.Add(new Dropdown.OptionData(option));

        dropdown.value = 0;
        dropdown.RefreshShownValue();
        return dropdown;
    }

    private static void CreateDropdownTemplate(Transform parent, Dropdown dropdown, Font font, Vector2 dimensions)
    {
        GameObject templateObject = new GameObject("Template");
        templateObject.transform.SetParent(parent, false);
        RectTransform templateRect = templateObject.AddComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0, -2);
        templateRect.sizeDelta = new Vector2(0, dimensions.y * 3f);
        Image templateImage = templateObject.AddComponent<Image>();
        templateImage.color = Color.white;
        ScrollRect scrollRect = templateObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(templateObject.transform, false);
        RectTransform viewportRect = viewportObject.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = Color.white;
        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, dimensions.y * 3f);

        GameObject itemObject = new GameObject("Item");
        itemObject.transform.SetParent(contentObject.transform, false);
        RectTransform itemRect = itemObject.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 1);
        itemRect.anchorMax = new Vector2(1, 1);
        itemRect.pivot = new Vector2(0.5f, 1);
        itemRect.anchoredPosition = Vector2.zero;
        itemRect.sizeDelta = new Vector2(0, dimensions.y);

        Toggle itemToggle = itemObject.AddComponent<Toggle>();
        Image itemBackground = itemObject.AddComponent<Image>();
        itemBackground.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        itemToggle.targetGraphic = itemBackground;

        GameObject checkmarkObject = new GameObject("Item Checkmark");
        checkmarkObject.transform.SetParent(itemObject.transform, false);
        RectTransform checkRect = checkmarkObject.AddComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0, 0.5f);
        checkRect.anchorMax = new Vector2(0, 0.5f);
        checkRect.anchoredPosition = new Vector2(10, 0);
        checkRect.sizeDelta = new Vector2(12, 12);
        Image checkImage = checkmarkObject.AddComponent<Image>();
        checkImage.color = new Color(0.2f, 0.8f, 0.35f);
        itemToggle.graphic = checkImage;

        Text itemLabel = CreateCenteredChildLabel(itemObject.transform, "Item Label", font, 15, Color.black, TextAnchor.MiddleLeft, new Vector2(28, 0), new Vector2(-8, 0));

        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        dropdown.itemImage = itemBackground;
        templateObject.SetActive(false);
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 position, Vector2 dimensions, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(label + "Button");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        Button button = obj.AddComponent<Button>();
        RuntimeButtonActionRelay relay = obj.AddComponent<RuntimeButtonActionRelay>();
        relay.Configure(action);
        button.onClick.AddListener(relay.Invoke);

        CreateCenteredChildLabel(obj.transform, label, font, 15, Color.black, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        return button;
    }

    private void SetAlarmRepeatDropdown(string repeatRule)
    {
        if (alarmRepeatDropdown == null)
            return;

        string normalized = AlarmManager.NormalizeRepeatRule(repeatRule);
        alarmRepeatDropdown.value = normalized == AlarmManager.RepeatDaily ? 1 : normalized == AlarmManager.RepeatWeekly ? 2 : 0;
        alarmRepeatDropdown.RefreshShownValue();
    }

    private string GetSelectedAlarmRepeatRule()
    {
        if (alarmRepeatDropdown == null)
            return AlarmManager.RepeatNone;

        if (alarmRepeatDropdown.value == 1)
            return AlarmManager.RepeatDaily;

        if (alarmRepeatDropdown.value == 2)
            return AlarmManager.RepeatWeekly;

        return AlarmManager.RepeatNone;
    }

    private int ParsePositiveInt(string value, int fallback)
    {
        return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : Mathf.Max(1, fallback);
    }
}

public class RuntimeButtonActionRelay : MonoBehaviour
{
    private UnityAction action;

    public void Configure(UnityAction newAction)
    {
        action = newAction;
    }

    public void Invoke()
    {
        if (action == null)
        {
            Debug.LogWarning("Runtime button action is missing on " + name);
            return;
        }

        action.Invoke();
    }
}
