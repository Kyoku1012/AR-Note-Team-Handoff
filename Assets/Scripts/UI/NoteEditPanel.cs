using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class NoteEditPanel : MonoBehaviour
{
    private static Sprite circleSprite;

    private readonly Dictionary<string, Image> colorSwatches = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> iconButtons = new Dictionary<string, Image>();
    private readonly Dictionary<string, Image> iconSprites = new Dictionary<string, Image>();
    private readonly Dictionary<string, Text> iconFallbackLabels = new Dictionary<string, Text>();
    private readonly Dictionary<string, Image> priorityButtons = new Dictionary<string, Image>();
    private readonly Dictionary<string, Text> colorLabels = new Dictionary<string, Text>();
    private readonly Dictionary<string, Text[]> pickerOptionLabels = new Dictionary<string, Text[]>();

    private NoteView currentNote;
    private NoteData currentNoteData;

    [Header("Prefab Actions")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button okButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button completeButton;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button nowButton;
    [SerializeField] private Button clearReminderButton;
    [SerializeField] private Button micButton;
    [SerializeField] private Button createNoteButton;
    [SerializeField] private Button editNoteButton;
    [SerializeField] private Button testMenuButton;
    [SerializeField] private Button clearDbButton;
    [SerializeField] private Button noteHistoryButton;
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Button[] iconButtonsSerialized;
    [SerializeField] private Button[] priorityButtonsSerialized;

    [Header("Prefab Visuals")]
    [SerializeField] private Image serializedPanelBackground;
    [SerializeField] private Image[] colorSwatchImages;
    [SerializeField] private Image[] iconButtonImages;
    [SerializeField] private Image[] iconSpriteImages;
    [SerializeField] private Image[] priorityButtonImages;
    [SerializeField] private Text[] colorCheckLabels;
    [SerializeField] private Text[] iconFallbackLabelsSerialized;
    [SerializeField] private Text[] pickerPreviousLabels;
    [SerializeField] private Text[] pickerCurrentLabels;
    [SerializeField] private Text[] pickerNextLabels;
    [SerializeField] private Image panelBackground;
    [SerializeField] private InputField noteInput;
    [SerializeField] private InputField titleInput;
    [SerializeField] private Text reminderSummaryText;
    [SerializeField] private Toggle reminderToggle;
    [SerializeField] private Text speechStatusText;
    [SerializeField] private GameObject completeCheckmark;
    [SerializeField] private Image completeButtonBackground;
    [SerializeField] private Image completeCheckboxBackground;
    [SerializeField] private GameObject launcherRoot;

    private GameObject deleteAllNotesConfirmDialog;
    private GameObject reminderCheckmark;
    private Image reminderCheckboxBackground;
    private string selectedColorName = "yellow";
    private string selectedIconId = "";
    private string selectedPriorityId = "";
    private bool selectedCompleted;
    private bool hasSelectedReminderTime;
    private DateTime selectedReminderTime;
    private bool prefabUiInitialized;
    private bool testMenuOpen;
    private float lastSpeechTapRealtime = -1f;

    private static readonly string[] ColorNames = { "yellow", "pink", "blue", "green" };
    private static readonly string[] IconIds = { "star", "finish", "inprocess", "reminder", "work", "study", "shopping" };
    private static readonly string[] PriorityIds = { "low", "medium", "high" };
    private static readonly string[] MonthNames = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    private const float SpeechTapDebounceSeconds = 0.6f;

    private void Awake()
    {
        InitializePrefabUiIfAvailable();
    }

    public static NoteEditPanel EnsureExists()
    {
        NoteEditPanel existing = FindObjectOfType<NoteEditPanel>(true);
        if (existing != null)
        {
            existing.InitializePrefabUiIfAvailable();
            return existing;
        }

        GameObject canvasObject = new GameObject("RuntimeNoteEditCanvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        GameObject panelObject = CreatePanel(canvasObject.transform);
        NoteEditPanel panel = panelObject.AddComponent<NoteEditPanel>();
        panel.panelBackground = panelObject.GetComponent<Image>();
        panel.BuildUi(panelObject.transform);
        panel.gameObject.SetActive(false);
        panel.launcherRoot = CreateLauncher(canvasObject.transform, panel);
        return panel;
    }

    public void Open(NoteView noteView)
    {
        EnsureUiReady();
        currentNote = noteView;
        currentNoteData = currentNote == null ? null : currentNote.Data;
        if (currentNote == null || currentNote.Data == null || !HasRequiredUiReferences())
            return;

        NoteManager.Instance?.SelectNote(currentNote);
        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(currentNote);

        NoteData data = currentNote.Data;
        data.ApplyDefaults();

        titleInput.text = data.title == "New Note" ? "" : data.title;
        noteInput.text = data.content;
        selectedColorName = NormalizeChoice(data.colorName, "yellow", ColorNames);
        selectedIconId = NormalizeChoice(data.iconId, "", IconIds);
        selectedPriorityId = NormalizeChoice(data.priorityId, "", PriorityIds);
        selectedCompleted = data.isCompleted;
        reminderToggle.isOn = data.hasReminder && !string.IsNullOrWhiteSpace(data.reminderTime);
        SetReminderTime(string.IsNullOrWhiteSpace(data.reminderTime) ? data.alarmTime : data.reminderTime);
        UpdateSelectionVisuals();
        UpdateIconSprites();
        UpdateSpeechStatus("");

        gameObject.SetActive(true);
        SetLauncherVisible(false);
    }

    public void Open(NoteData noteData)
    {
        EnsureUiReady();
        currentNote = null;
        currentNoteData = noteData;
        if (currentNoteData == null || !HasRequiredUiReferences())
            return;

        currentNoteData.ApplyDefaults();

        titleInput.text = currentNoteData.title == "New Note" ? "" : currentNoteData.title;
        noteInput.text = currentNoteData.content;
        selectedColorName = NormalizeChoice(currentNoteData.colorName, "yellow", ColorNames);
        selectedIconId = NormalizeChoice(currentNoteData.iconId, "", IconIds);
        selectedPriorityId = NormalizeChoice(currentNoteData.priorityId, "", PriorityIds);
        selectedCompleted = currentNoteData.isCompleted;
        reminderToggle.isOn = currentNoteData.hasReminder && !string.IsNullOrWhiteSpace(currentNoteData.reminderTime);
        SetReminderTime(string.IsNullOrWhiteSpace(currentNoteData.reminderTime) ? currentNoteData.alarmTime : currentNoteData.reminderTime);
        UpdateSelectionVisuals();
        UpdateIconSprites();
        UpdateSpeechStatus("");

        gameObject.SetActive(true);
        SetLauncherVisible(false);
    }

    public void Close()
    {
        gameObject.SetActive(false);
        SetLauncherVisible(true);
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

    public void StartSpeechInput()
    {
        DictateNote();
    }

    private void Save()
    {
        NoteData data = currentNote != null ? currentNote.Data : currentNoteData;
        if (data == null)
            return;

        string noteText = GetInputValue(noteInput);
        string titleText = GetInputValue(titleInput);
        data.content = noteText;
        data.title = string.IsNullOrWhiteSpace(titleText) ? "New Note" : titleText;
        data.annotation = "";
        data.colorName = selectedColorName;
        data.colorLabel = selectedColorName;
        data.iconId = selectedIconId;
        data.priorityId = selectedPriorityId;
        data.isCompleted = selectedCompleted;
        data.isVisible = !selectedCompleted;
        if (!reminderToggle.isOn || !hasSelectedReminderTime)
        {
            data.ClearReminder();
            SaveCurrentData(data);
            Close();
            return;
        }

        if (selectedReminderTime <= DateTime.Now)
        {
            selectedReminderTime = DateTime.Now.AddMinutes(1);
            UpdateReminderPicker();
            UpdateReminderSummary();
        }

        data.SetReminder(ReminderManager.FormatReminderTime(selectedReminderTime), ReminderManager.RepeatNone);

        SaveCurrentData(data);
        Close();
    }

    private void DeleteCurrent()
    {
        NoteData data = currentNote != null ? currentNote.Data : currentNoteData;
        if (data == null)
            return;

        string noteId = data.id;
        Close();
        NoteManager.Instance?.RemoveNote(noteId);
    }

    private void DictateNote()
    {
        if (noteInput == null)
            return;

        if (Time.unscaledTime - lastSpeechTapRealtime < SpeechTapDebounceSeconds)
            return;

        lastSpeechTapRealtime = Time.unscaledTime;

        if (SpeechToTextManager.Instance == null)
        {
            UpdateSpeechStatus("Speech input is not ready. Reopen the scene and try again.");
            return;
        }

        if (SpeechToTextManager.Instance.IsListening)
        {
            SpeechToTextManager.Instance.StopDictation();
            UpdateSpeechStatus("Speech input stopped.");
            return;
        }

        UpdateSpeechStatus("Listening... tap Mic again to finish.");
        SpeechToTextManager.Instance.StartDictation(
            text =>
            {
                string currentText = GetInputValue(noteInput);
                noteInput.text = string.IsNullOrWhiteSpace(currentText) ? text : currentText + " " + text;

                NoteData data = currentNote != null ? currentNote.Data : currentNoteData;
                if (data != null)
                {
                    data.hasTranscript = true;
                    data.transcriptText = text;
                    data.transcriptSource = "content";
                    data.transcriptUpdatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }

                UpdateSpeechStatus("Speech added to note.");
            },
            error => UpdateSpeechStatus(error));
    }

    private void SelectColor(string colorName)
    {
        selectedColorName = colorName;
        UpdateSelectionVisuals();
    }

    private void SelectIcon(string iconId)
    {
        selectedIconId = iconId;
        UpdateSelectionVisuals();
    }

    private void SelectPriority(string priorityId)
    {
        selectedPriorityId = priorityId;
        UpdateSelectionVisuals();
    }

    private void ToggleComplete()
    {
        selectedCompleted = !selectedCompleted;
        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals()
    {
        foreach (KeyValuePair<string, Image> swatch in colorSwatches)
        {
            bool selected = swatch.Key == selectedColorName;
            if (swatch.Value != null)
                swatch.Value.color = GetColorValue(swatch.Key, selected ? 1f : 0.72f);

            if (colorLabels.TryGetValue(swatch.Key, out Text label) && label != null)
                label.text = selected ? "\u2713" : "";
        }

        foreach (KeyValuePair<string, Image> icon in iconButtons)
        {
            if (icon.Value != null)
                icon.Value.color = icon.Key == selectedIconId ? new Color(0.75f, 0.9f, 0.72f, 1f) : new Color(1f, 1f, 1f, 0.35f);
        }

        foreach (KeyValuePair<string, Image> priority in priorityButtons)
        {
            if (priority.Value == null) continue;

            if (priority.Key == selectedPriorityId)
                priority.Value.color = priority.Key == "high" ? new Color(0.8f, 0.12f, 0.14f, 1f) : new Color(0.75f, 0.9f, 0.72f, 1f);
            else
                priority.Value.color = new Color(1f, 1f, 1f, 0.45f);
        }

        if (panelBackground != null)
            panelBackground.color = new Color(0.96f, 0.97f, 0.98f, 0.98f);

        if (completeCheckmark != null)
            completeCheckmark.SetActive(selectedCompleted);

        if (completeButtonBackground != null)
            completeButtonBackground.color = selectedCompleted ? new Color(0.74f, 0.9f, 0.72f, 1f) : new Color(0.95f, 0.95f, 0.86f, 1f);

        if (completeCheckboxBackground != null)
            completeCheckboxBackground.color = selectedCompleted ? new Color(0.9f, 1f, 0.88f, 1f) : Color.white;

        UpdateReminderToggleVisuals();
    }

    private void UpdateIconSprites()
    {
        NoteStyleManager styleManager = currentNote == null ? FindObjectOfType<NoteStyleManager>(true) : currentNote.GetComponentInChildren<NoteStyleManager>(true);

        foreach (string iconId in IconIds)
        {
            Sprite sprite = GetIconSprite(styleManager, iconId);
            if (iconSprites.TryGetValue(iconId, out Image image) && image != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.gameObject.SetActive(sprite != null);
            }

            if (iconFallbackLabels.TryGetValue(iconId, out Text label) && label != null)
                label.gameObject.SetActive(sprite == null);
        }
    }

    private void UpdateSpeechStatus(string message)
    {
        if (speechStatusText == null)
            return;

        speechStatusText.text = string.IsNullOrWhiteSpace(message) ? "Speech input ready" : message;
    }

    private void EnsureUiReady()
    {
        InitializePrefabUiIfAvailable();

        if (HasRequiredUiReferences())
            return;

        if (transform.childCount == 0)
        {
            panelBackground = GetComponent<Image>();
            BuildUi(transform);
        }
    }

    private bool HasRequiredUiReferences()
    {
        return noteInput != null
            && titleInput != null
            && reminderToggle != null
            && reminderSummaryText != null;
    }

    private void InitializePrefabUiIfAvailable()
    {
        if (prefabUiInitialized)
            return;

        if (serializedPanelBackground != null && panelBackground == null)
            panelBackground = serializedPanelBackground;

        BindSerializedCollections();
        BindSerializedButtons();
        BindSerializedPickerControls();
        ConfigureLauncherButtons();

        if (reminderToggle != null)
            reminderToggle.onValueChanged.AddListener(_ =>
            {
                UpdateReminderSummary();
                UpdateReminderToggleVisuals();
            });

        ConfigureEditPanelProductStyle();
        UpdateReminderToggleVisuals();
        prefabUiInitialized = true;
    }

    private void BindSerializedCollections()
    {
        RegisterImages(colorSwatches, ColorNames, colorSwatchImages);
        RegisterImages(priorityButtons, PriorityIds, priorityButtonImages);
        RegisterImages(iconButtons, IconIds, iconButtonImages);
        RegisterImages(iconSprites, IconIds, iconSpriteImages);
        RegisterTexts(colorLabels, ColorNames, colorCheckLabels);
        RegisterTexts(iconFallbackLabels, IconIds, iconFallbackLabelsSerialized);
        RegisterPickerLabels();

        if (iconFallbackLabels.Count == 0 && iconButtonsSerialized != null)
        {
            for (int i = 0; i < iconButtonsSerialized.Length && i < IconIds.Length; i++)
            {
                Button button = iconButtonsSerialized[i];
                if (button == null)
                    continue;

                Text fallbackLabel = button.GetComponentInChildren<Text>(true);
                if (fallbackLabel != null)
                    iconFallbackLabels[IconIds[i]] = fallbackLabel;
            }
        }
    }

    private void BindSerializedButtons()
    {
        ConfigureButton(closeButton, Close);
        ConfigureButton(okButton, Save);
        ConfigureButton(deleteButton, DeleteCurrent);
        ConfigureButton(completeButton, ToggleComplete);
        ConfigureButton(saveButton, Save);
        ConfigureButton(nowButton, SetReminderNow);
        ConfigureButton(clearReminderButton, ClearReminderTime);
        ConfigureButton(micButton, DictateNote);
        ConfigureButton(createNoteButton, CreateCenterScreenNote);
        ConfigureButton(editNoteButton, OpenSelected);
        ConfigureButton(testMenuButton, ToggleTestMenu);
        ConfigureButton(clearDbButton, RequestClearAllData);
        ConfigureButton(noteHistoryButton, OpenHistory);

        ConfigureChoiceButtons(colorButtons, ColorNames, SelectColor);
        ConfigureChoiceButtons(iconButtonsSerialized, IconIds, SelectIcon);
        ConfigureChoiceButtons(priorityButtonsSerialized, PriorityIds, SelectPriority);
    }

    private void BindSerializedPickerControls()
    {
        ConfigurePickerColumn("year", () => ShiftYear(1), () => ShiftYear(-1));
        ConfigurePickerColumn("month", () => ShiftMonth(1), () => ShiftMonth(-1));
        ConfigurePickerColumn("day", () => ShiftDay(1), () => ShiftDay(-1));
        ConfigurePickerColumn("hour", () => ShiftHour(1), () => ShiftHour(-1));
        ConfigurePickerColumn("minute", () => ShiftMinute(1), () => ShiftMinute(-1));
        ConfigurePickerColumn("period", TogglePeriod, TogglePeriod);
    }

    private void RegisterPickerLabels()
    {
        pickerOptionLabels.Clear();

        if (pickerPreviousLabels == null || pickerCurrentLabels == null || pickerNextLabels == null)
            return;

        for (int i = 0; i < pickerPreviousLabels.Length && i < 6; i++)
        {
            if (pickerPreviousLabels[i] == null)
                continue;

            if (i >= pickerCurrentLabels.Length || i >= pickerNextLabels.Length)
                continue;

            pickerOptionLabels[GetPickerKey(i)] = new[]
            {
                pickerPreviousLabels[i],
                pickerCurrentLabels[i],
                pickerNextLabels[i]
            };
        }
    }

    private static string GetPickerKey(int index)
    {
        switch (index)
        {
            case 0: return "year";
            case 1: return "month";
            case 2: return "day";
            case 3: return "hour";
            case 4: return "minute";
            default: return "period";
        }
    }

    private static void RegisterImages(Dictionary<string, Image> target, string[] keys, Image[] images)
    {
        if (target == null || keys == null || images == null)
            return;

        target.Clear();
        for (int i = 0; i < keys.Length && i < images.Length; i++)
        {
            if (images[i] != null)
                target[keys[i]] = images[i];
        }
    }

    private static void RegisterTexts(Dictionary<string, Text> target, string[] keys, Text[] labels)
    {
        if (target == null || keys == null || labels == null)
            return;

        target.Clear();
        for (int i = 0; i < keys.Length && i < labels.Length; i++)
        {
            if (labels[i] != null)
                target[keys[i]] = labels[i];
        }
    }

    private static void ConfigureChoiceButtons(Button[] buttons, string[] values, Action<string> action)
    {
        if (buttons == null || values == null || action == null)
            return;

        for (int i = 0; i < buttons.Length && i < values.Length; i++)
        {
            string value = values[i];
            ConfigureButton(buttons[i], () => action(value));
        }
    }

    private static void ConfigureButton(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        RuntimeButtonActionRelay relay = button.GetComponent<RuntimeButtonActionRelay>();
        if (relay == null)
            relay = button.gameObject.AddComponent<RuntimeButtonActionRelay>();

        relay.Configure(action);
        button.onClick.RemoveListener(relay.Invoke);
        button.onClick.AddListener(relay.Invoke);
    }

    private void ConfigurePickerColumn(string key, UnityAction upAction, UnityAction downAction)
    {
        Transform column = FindChildRecursive(transform, key + "PickerColumn");
        if (column == null)
            return;

        PickerColumnDragHandler dragHandler = column.GetComponent<PickerColumnDragHandler>();
        if (dragHandler != null)
            dragHandler.Configure(upAction, downAction);

        Button[] buttons = column.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (button.name.StartsWith("^", StringComparison.Ordinal))
                ConfigureButton(button, upAction);
            else if (button.name.StartsWith("v", StringComparison.Ordinal))
                ConfigureButton(button, downAction);
        }
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform match = FindChildRecursive(child, childName);
            if (match != null)
                return match;
        }

        return null;
    }

    private void BuildUi(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        CreateBlock(parent, "HeaderBar", new Vector2(0, -28), new Vector2(430, 58), new Color(0.09f, 0.1f, 0.11f, 0.96f));
        CreateButton(parent, "X", font, new Vector2(-185, -28), new Vector2(46, 38), Close, new Color(0f, 0f, 0f, 0f), Color.white, 26);
        CreateTopAnchoredLabel(parent, "Edit AR Note", font, 24, new Vector2(0, -28), new Vector2(260, 38), Color.white);
        CreateButton(parent, "OK", font, new Vector2(185, -28), new Vector2(54, 38), Save, new Color(0f, 0f, 0f, 0f), Color.white, 20);

        CreateRowLabel(parent, "Title", font, -102);
        titleInput = CreateInput(parent, "", font, new Vector2(62, -102), new Vector2(260, 36), false, 80);

        CreateRowLabel(parent, "Note", font, -162);
        noteInput = CreateInput(parent, "", font, new Vector2(62, -168), new Vector2(260, 86), true, 4000);

        CreateRowLabel(parent, "Color", font, -250);
        BuildColorRow(parent, font, -250);

        CreateRowLabel(parent, "Icon", font, -306);
        BuildIconRow(parent, font, -306);

        CreateRowLabel(parent, "Priority", font, -366);
        BuildPriorityRow(parent, font, -366);

        CreateRowLabel(parent, "Reminder", font, -426);
        reminderSummaryText = CreateTopAnchoredLabel(parent, "No time set", font, 16, new Vector2(54, -426), new Vector2(218, 34), Color.black);
        BuildDateTimePicker(parent, font, -498);
        CreateButton(parent, "Now", font, new Vector2(-34, -578), new Vector2(70, 30), SetReminderNow, new Color(1f, 0.99f, 0.88f, 1f), Color.black, 13);
        CreateButton(parent, "Clear", font, new Vector2(50, -578), new Vector2(70, 30), ClearReminderTime, new Color(0.95f, 0.95f, 0.86f, 1f), new Color(0.7f, 0.1f, 0.1f, 1f), 13);
        reminderToggle = CreateToggle(parent, "Set Reminder", font, new Vector2(62, -616));
        reminderToggle.onValueChanged.AddListener(_ =>
        {
            UpdateReminderSummary();
            UpdateReminderToggleVisuals();
        });

        CreateRowLabel(parent, "Speech", font, -666);
        CreateButton(parent, "Mic", font, new Vector2(-12, -666), new Vector2(86, 36), DictateNote, new Color(0.95f, 0.95f, 0.86f, 1f), Color.black, 15);
        speechStatusText = CreateTopAnchoredLabel(parent, "Speech input ready", font, 13, new Vector2(120, -666), new Vector2(166, 30), Color.black);

        CreateButton(parent, "Delete", font, new Vector2(-138, -724), new Vector2(88, 38), DeleteCurrent, new Color(0.95f, 0.95f, 0.86f, 1f), new Color(0.75f, 0.1f, 0.1f, 1f), 15);
        CreateCompleteButton(parent, font, new Vector2(-20, -724), new Vector2(118, 38));
        CreateButton(parent, "Save Note", font, new Vector2(118, -724), new Vector2(122, 40), Save, new Color(0.22f, 0.72f, 0.32f, 1f), Color.white, 16);
        ConfigureEditPanelProductStyle();
    }

    private void BuildColorRow(Transform parent, Font font, float y)
    {
        colorSwatches.Clear();
        colorLabels.Clear();

        for (int i = 0; i < ColorNames.Length; i++)
        {
            string colorName = ColorNames[i];
            Button button = CreateButton(parent, "", font, new Vector2(-15 + i * 64, y), new Vector2(44, 44), () => SelectColor(colorName), GetColorValue(colorName, 0.72f), Color.black, 16);
            Image image = button.GetComponent<Image>();
            Text label = button.GetComponentInChildren<Text>();
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            label.color = Color.white;
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            colorSwatches[colorName] = image;
            colorLabels[colorName] = label;
        }
    }

    private void BuildIconRow(Transform parent, Font font, float y)
    {
        iconButtons.Clear();

        for (int i = 0; i < IconIds.Length; i++)
        {
            string iconId = IconIds[i];
            Button button = CreateButton(parent, "", font, new Vector2(-50 + i * 40, y), new Vector2(36, 34), () => SelectIcon(iconId), new Color(1f, 1f, 1f, 0.35f), Color.black, 10);
            Image spriteImage = CreateChildImage(button.transform, iconId + "Icon", new Vector2(24, 24), Color.white);
            Text fallbackLabel = CreateCenteredChildLabel(button.transform, GetIconFallbackLabel(iconId), font, 10, Color.black, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            iconButtons[iconId] = button.GetComponent<Image>();
            iconSprites[iconId] = spriteImage;
            iconFallbackLabels[iconId] = fallbackLabel;
        }
    }

    private void BuildPriorityRow(Transform parent, Font font, float y)
    {
        priorityButtons.Clear();

        for (int i = 0; i < PriorityIds.Length; i++)
        {
            string priorityId = PriorityIds[i];
            Button button = CreateButton(parent, ToTitle(priorityId), font, new Vector2(-34 + i * 94, y), new Vector2(82, 38), () => SelectPriority(priorityId), new Color(1f, 1f, 1f, 0.45f), Color.black, 15);
            priorityButtons[priorityId] = button.GetComponent<Image>();
        }
    }

    private void BuildDateTimePicker(Transform parent, Font font, float y)
    {
        pickerOptionLabels.Clear();

        CreateBlock(parent, "PickerSelectionBand", new Vector2(50, y), new Vector2(310, 32), new Color(1f, 1f, 1f, 0.35f));

        CreatePickerColumn(parent, font, "year", new Vector2(-78, y), new Vector2(56, 90), () => ShiftYear(1), () => ShiftYear(-1));
        CreatePickerColumn(parent, font, "month", new Vector2(-26, y), new Vector2(52, 90), () => ShiftMonth(1), () => ShiftMonth(-1));
        CreatePickerColumn(parent, font, "day", new Vector2(24, y), new Vector2(40, 90), () => ShiftDay(1), () => ShiftDay(-1));
        CreatePickerColumn(parent, font, "hour", new Vector2(70, y), new Vector2(40, 90), () => ShiftHour(1), () => ShiftHour(-1));
        CreatePickerColumn(parent, font, "minute", new Vector2(116, y), new Vector2(40, 90), () => ShiftMinute(1), () => ShiftMinute(-1));
        CreatePickerColumn(parent, font, "period", new Vector2(166, y), new Vector2(48, 90), TogglePeriod, TogglePeriod);
    }

    private void CreatePickerColumn(Transform parent, Font font, string key, Vector2 position, Vector2 dimensions, UnityAction upAction, UnityAction downAction)
    {
        GameObject column = new GameObject(key + "PickerColumn");
        column.transform.SetParent(parent, false);

        RectTransform rect = column.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image background = column.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.16f);
        PickerColumnDragHandler dragHandler = column.AddComponent<PickerColumnDragHandler>();
        dragHandler.Configure(upAction, downAction);

        CreateButton(column.transform, "^", font, new Vector2(0, -12), new Vector2(dimensions.x - 4, 24), upAction, new Color(1f, 1f, 1f, 0.08f), new Color(0.25f, 0.25f, 0.22f, 1f), 14);
        Text previous = CreateCenteredChildLabel(column.transform, "", font, 12, new Color(0.2f, 0.2f, 0.18f, 0.45f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        previous.rectTransform.anchorMin = new Vector2(0, 0.58f);
        previous.rectTransform.anchorMax = new Vector2(1, 0.78f);
        previous.rectTransform.offsetMin = Vector2.zero;
        previous.rectTransform.offsetMax = Vector2.zero;

        Text current = CreateCenteredChildLabel(column.transform, "", font, 17, Color.black, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        current.fontStyle = FontStyle.Bold;
        current.rectTransform.anchorMin = new Vector2(0, 0.37f);
        current.rectTransform.anchorMax = new Vector2(1, 0.63f);
        current.rectTransform.offsetMin = Vector2.zero;
        current.rectTransform.offsetMax = Vector2.zero;

        Text next = CreateCenteredChildLabel(column.transform, "", font, 12, new Color(0.2f, 0.2f, 0.18f, 0.45f), TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        next.rectTransform.anchorMin = new Vector2(0, 0.18f);
        next.rectTransform.anchorMax = new Vector2(1, 0.38f);
        next.rectTransform.offsetMin = Vector2.zero;
        next.rectTransform.offsetMax = Vector2.zero;
        CreateButton(column.transform, "v", font, new Vector2(0, -80), new Vector2(dimensions.x - 4, 24), downAction, new Color(1f, 1f, 1f, 0.08f), new Color(0.25f, 0.25f, 0.22f, 1f), 14);

        pickerOptionLabels[key] = new[] { previous, current, next };
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("NoteEditPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -20);
        rect.sizeDelta = new Vector2(430, 785);

        Image image = panel.AddComponent<Image>();
        image.color = GetPanelColor("yellow");
        return panel;
    }

    private static GameObject CreateLauncher(Transform parent, NoteEditPanel panel)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        GameObject launcher = new GameObject("NoteEditLauncher");
        launcher.transform.SetParent(parent, false);
        RectTransform rect = launcher.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.testMenuButton = CreateButton(launcher.transform, "Test Menu", font, new Vector2(-230, -40), new Vector2(150, 40), panel.ToggleTestMenu, new Color(0.95f, 0.95f, 0.95f, 1f), Color.black, 14);
        panel.createNoteButton = CreateButton(launcher.transform, "Create Note", font, new Vector2(-230, -84), new Vector2(120, 40), panel.CreateCenterScreenNote);
        panel.editNoteButton = CreateButton(launcher.transform, "Edit Note", font, new Vector2(-230, -132), new Vector2(120, 40), panel.OpenSelected);
        panel.noteHistoryButton = CreateButton(launcher.transform, "Note History", font, new Vector2(160, -40), new Vector2(150, 40), panel.OpenHistory);
        panel.clearDbButton = CreateButton(launcher.transform, "Delete All Notes", font, new Vector2(160, -88), new Vector2(150, 40), panel.RequestClearAllData);
        panel.launcherRoot = launcher;
        panel.ConfigureLauncherButtons();
        return launcher;
    }

    private void RequestClearAllData()
    {
        ShowDeleteAllNotesConfirmDialog();
    }

    private void ConfirmClearAllData()
    {
        HideDeleteAllNotesConfirmDialog();
        ClearAllData();
    }

    private void ClearAllData()
    {
        if (NoteManager.Instance == null)
            return;

        Close();
        currentNote = null;
        currentNoteData = null;
        NoteManager.Instance.ClearAllNotesAndData();
        Debug.Log("All notes and local voice files have been cleared.");
    }

    private void OpenHistory()
    {
        NoteHistoryPanel.EnsureExists().Open();
    }

    private void SaveCurrentData(NoteData data)
    {
        if (currentNote != null)
        {
            currentNote.SaveAndRefresh();
            return;
        }

        data.ApplyDefaults();
        NoteManager.Instance?.UpdateNote(data);
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
            if (EventSystem.current.GetComponent<BaseInputModule>() == null)
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

    private void SetLauncherVisible(bool visible)
    {
        if (visible)
            SetTestMenuOpen(false);
        else
            HideDeleteAllNotesConfirmDialog();

        if (launcherRoot != null)
            launcherRoot.SetActive(visible);
    }

    private void ConfigureLauncherButtons()
    {
        if (launcherRoot == null)
            return;

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (testMenuButton == null)
            testMenuButton = FindLauncherButton("Test MenuButton");

        if (testMenuButton == null)
            testMenuButton = CreateButton(launcherRoot.transform, "Test Menu", font, new Vector2(-230, -40), new Vector2(150, 40), ToggleTestMenu, new Color(0.95f, 0.95f, 0.95f, 1f), Color.black, 14);

        ConfigureButton(testMenuButton, ToggleTestMenu);
        ConfigureButton(clearDbButton, RequestClearAllData);

        ConfigureLauncherButton(createNoteButton, "Create Note", new Vector2(-230, -84), new Vector2(120, 40));
        ConfigureLauncherButton(editNoteButton, "Edit Note", new Vector2(-230, -132), new Vector2(120, 40));
        ConfigureLauncherButton(noteHistoryButton, "Note History", new Vector2(160, -40), new Vector2(150, 40));
        ConfigureLauncherButton(clearDbButton, "Delete All Notes", new Vector2(160, -88), new Vector2(150, 40));
        ConfigureLauncherButton(testMenuButton, "Test Menu", new Vector2(-230, -40), new Vector2(150, 40));
        SetTestMenuOpen(false);
    }

    private Button FindLauncherButton(string buttonName)
    {
        if (launcherRoot == null || string.IsNullOrWhiteSpace(buttonName))
            return null;

        Button[] buttons = launcherRoot.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.name == buttonName)
                return button;
        }

        return null;
    }

    private static void ConfigureLauncherButton(Button button, string label, Vector2 position, Vector2 dimensions)
    {
        if (button == null)
            return;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        ApplyLauncherButtonStyle(button, label);
    }

    private static void ApplyLauncherButtonStyle(Button button, string label)
    {
        if (button == null)
            return;

        bool isPrimary = label == "Create Note";
        bool isDanger = label == "Delete All Notes";

        Color backgroundColor = isPrimary
            ? new Color(0.16f, 0.38f, 0.74f, 1f)
            : isDanger
                ? new Color(1f, 0.96f, 0.95f, 1f)
                : new Color(0.97f, 0.98f, 0.99f, 1f);

        Color borderColor = isPrimary
            ? new Color(0.11f, 0.28f, 0.6f, 1f)
            : isDanger
                ? new Color(0.82f, 0.26f, 0.22f, 1f)
                : new Color(0.72f, 0.76f, 0.82f, 1f);

        Color textColor = isPrimary
            ? Color.white
            : isDanger
                ? new Color(0.62f, 0.12f, 0.1f, 1f)
                : new Color(0.12f, 0.16f, 0.22f, 1f);

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = backgroundColor;

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
            outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = isPrimary ? new Color(0.92f, 0.96f, 1f, 1f) : new Color(0.96f, 0.98f, 1f, 1f);
        colors.pressedColor = isPrimary ? new Color(0.78f, 0.87f, 1f, 1f) : new Color(0.9f, 0.93f, 0.96f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.8f, 0.82f, 0.86f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text labelText = button.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
            labelText.color = textColor;
            labelText.fontSize = isDanger ? 14 : 15;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
        }
    }

    private void ToggleTestMenu()
    {
        SetTestMenuOpen(!testMenuOpen);
    }

    private void SetTestMenuOpen(bool isOpen)
    {
        testMenuOpen = isOpen;

        if (createNoteButton != null)
            createNoteButton.gameObject.SetActive(testMenuOpen);

        if (editNoteButton != null)
            editNoteButton.gameObject.SetActive(testMenuOpen);
    }

    private void ConfigureEditPanelProductStyle()
    {
        StylePanelFrame();
        SetRect(titleInput, new Vector2(62, -102), new Vector2(260, 36));
        SetRect(noteInput, new Vector2(62, -168), new Vector2(260, 86));
        SetRect(reminderSummaryText, new Vector2(54, -426), new Vector2(218, 34));
        SetRect(reminderToggle, new Vector2(62, -616), new Vector2(260, 34));
        StyleInputField(titleInput);
        StyleInputField(noteInput);

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            Text label = button.GetComponentInChildren<Text>(true);
            string text = label == null ? "" : label.text.Trim();
            if (text == "OK")
                StyleEditPanelButton(button, text, EditPanelButtonRole.HeaderPrimary);
            else if (text == "Mic")
                StyleEditPanelButton(button, text, EditPanelButtonRole.Secondary);
            else if (text == "Delete")
                StyleEditPanelButton(button, text, EditPanelButtonRole.Danger);
            else if (text == "Complete")
                StyleEditPanelButton(button, text, EditPanelButtonRole.Neutral);
            else if (text == "Save Note")
                StyleEditPanelButton(button, text, EditPanelButtonRole.Primary);
        }

        Text[] labels = GetComponentsInChildren<Text>(true);
        foreach (Text label in labels)
            StyleEditPanelText(label);

        StyleToggle(reminderToggle);
    }

    private static void SetRect(Component component, Vector2 position, Vector2 dimensions)
    {
        if (component == null)
            return;

        RectTransform rect = component.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
    }

    private enum EditPanelButtonRole
    {
        Primary,
        HeaderPrimary,
        Secondary,
        Neutral,
        Danger
    }

    private void StylePanelFrame()
    {
        if (panelBackground != null)
            panelBackground.color = new Color(0.96f, 0.97f, 0.98f, 0.98f);

        Transform header = FindChildRecursive(transform, "HeaderBar");
        if (header != null)
        {
            Image headerImage = header.GetComponent<Image>();
            if (headerImage != null)
                headerImage.color = new Color(0.08f, 0.1f, 0.14f, 0.98f);
        }
    }

    private static void StyleInputField(InputField input)
    {
        if (input == null)
            return;

        Image image = input.GetComponent<Image>();
        if (image != null)
            image.color = Color.white;

        Outline outline = input.GetComponent<Outline>();
        if (outline == null)
            outline = input.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.76f, 0.8f, 0.86f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        if (input.textComponent != null)
        {
            input.textComponent.color = new Color(0.08f, 0.1f, 0.14f, 1f);
            input.textComponent.fontSize = 15;
            input.textComponent.fontStyle = FontStyle.Normal;
            input.textComponent.lineSpacing = 1f;
        }

        Text placeholder = input.placeholder as Text;
        if (placeholder != null)
        {
            placeholder.color = new Color(0.52f, 0.58f, 0.66f, 1f);
            placeholder.fontSize = 15;
            placeholder.fontStyle = FontStyle.Normal;
        }

        ColorBlock colors = input.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.98f, 1f, 1f);
        colors.pressedColor = new Color(0.93f, 0.96f, 1f, 1f);
        colors.selectedColor = new Color(0.96f, 0.98f, 1f, 1f);
        colors.disabledColor = new Color(0.86f, 0.88f, 0.91f, 0.65f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        input.colors = colors;
    }

    private static void StyleEditPanelButton(Button button, string text, EditPanelButtonRole role)
    {
        Color backgroundColor;
        Color borderColor;
        Color textColor;

        switch (role)
        {
            case EditPanelButtonRole.Primary:
                backgroundColor = new Color(0.16f, 0.38f, 0.74f, 1f);
                borderColor = new Color(0.11f, 0.28f, 0.6f, 1f);
                textColor = Color.white;
                break;
            case EditPanelButtonRole.HeaderPrimary:
                backgroundColor = new Color(0.18f, 0.42f, 0.78f, 1f);
                borderColor = new Color(0.42f, 0.63f, 0.92f, 1f);
                textColor = Color.white;
                break;
            case EditPanelButtonRole.Danger:
                backgroundColor = new Color(1f, 0.96f, 0.95f, 1f);
                borderColor = new Color(0.82f, 0.26f, 0.22f, 1f);
                textColor = new Color(0.62f, 0.12f, 0.1f, 1f);
                break;
            case EditPanelButtonRole.Neutral:
                backgroundColor = new Color(0.94f, 0.97f, 0.95f, 1f);
                borderColor = new Color(0.48f, 0.62f, 0.54f, 1f);
                textColor = new Color(0.13f, 0.22f, 0.17f, 1f);
                break;
            default:
                backgroundColor = new Color(0.97f, 0.98f, 0.99f, 1f);
                borderColor = new Color(0.72f, 0.76f, 0.82f, 1f);
                textColor = new Color(0.12f, 0.16f, 0.22f, 1f);
                break;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = backgroundColor;

        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
            outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.96f, 0.98f, 1f, 1f);
        colors.pressedColor = new Color(0.9f, 0.93f, 0.96f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.8f, 0.82f, 0.86f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text label = button.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.text = text;
            label.color = textColor;
            label.fontSize = role == EditPanelButtonRole.HeaderPrimary ? 16 : 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
        }
    }

    private static void StyleEditPanelText(Text label)
    {
        if (label == null)
            return;

        string text = label.text.Trim();
        if (text == "Edit AR Note")
        {
            label.color = Color.white;
            label.fontSize = 22;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            return;
        }

        if (text == "Title" || text == "Note" || text == "Color" || text == "Icon" || text == "Priority" || text == "Reminder" || text == "Speech")
        {
            label.color = new Color(0.2f, 0.25f, 0.33f, 1f);
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            return;
        }

        if (text == "No time set" || text == "Speech input ready")
        {
            label.color = new Color(0.36f, 0.42f, 0.5f, 1f);
            label.fontSize = text == "Speech input ready" ? 13 : 14;
            label.fontStyle = FontStyle.Normal;
        }
    }

    private void StyleToggle(Toggle toggle)
    {
        if (toggle == null)
            return;

        HideChild(toggle.transform, "SwitchTrack");
        HideChild(toggle.transform, "SwitchOn");
        EnsureReminderCheckbox(toggle);

        Text label = toggle.GetComponentInChildren<Text>(true);
        if (label != null)
        {
            label.text = "Set Reminder";
            label.color = new Color(0.12f, 0.16f, 0.22f, 1f);
            label.fontSize = 15;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;

            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect != null)
            {
                labelRect.offsetMin = new Vector2(30, 0);
                labelRect.offsetMax = Vector2.zero;
            }
        }

        Image background = toggle.GetComponent<Image>();
        if (background != null)
            background.color = new Color(0.94f, 0.97f, 0.95f, 1f);

        Outline outline = toggle.GetComponent<Outline>();
        if (outline == null)
            outline = toggle.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.48f, 0.62f, 0.54f, 1f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        toggle.targetGraphic = background;
        toggle.graphic = null;
        UpdateReminderToggleVisuals();
    }

    private void EnsureReminderCheckbox(Toggle toggle)
    {
        if (toggle == null)
            return;

        Transform checkboxTransform = toggle.transform.Find("ReminderCheckbox");
        Image checkbox = checkboxTransform == null ? null : checkboxTransform.GetComponent<Image>();
        if (checkbox == null)
        {
            checkbox = CreateChildImage(toggle.transform, "ReminderCheckbox", new Vector2(18, 18), Color.white);
            checkbox.raycastTarget = false;
            Outline outline = checkbox.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.18f, 0.2f, 0.18f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        checkbox.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        checkbox.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        checkbox.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        checkbox.rectTransform.anchoredPosition = new Vector2(18, 0);
        checkbox.rectTransform.sizeDelta = new Vector2(18, 18);
        reminderCheckboxBackground = checkbox;

        Transform checkmarkTransform = checkbox.transform.Find("ReminderCheckmark");
        reminderCheckmark = checkmarkTransform == null ? CreateCheckmarkGraphic(checkbox.transform) : checkmarkTransform.gameObject;
        reminderCheckmark.name = "ReminderCheckmark";
    }

    private void UpdateReminderToggleVisuals()
    {
        if (reminderToggle == null)
            return;

        if (reminderCheckboxBackground == null || reminderCheckmark == null)
            EnsureReminderCheckbox(reminderToggle);

        bool isOn = reminderToggle.isOn;
        if (reminderCheckmark != null)
            reminderCheckmark.SetActive(isOn);

        if (reminderCheckboxBackground != null)
            reminderCheckboxBackground.color = isOn ? new Color(0.9f, 1f, 0.88f, 1f) : Color.white;
    }

    private static void HideChild(Transform parent, string childName)
    {
        Transform child = parent == null ? null : parent.Find(childName);
        if (child != null)
            child.gameObject.SetActive(false);
    }

    private void ShowDeleteAllNotesConfirmDialog()
    {
        if (launcherRoot == null)
            return;

        if (deleteAllNotesConfirmDialog == null)
            deleteAllNotesConfirmDialog = CreateDeleteAllNotesConfirmDialog(launcherRoot.transform);

        deleteAllNotesConfirmDialog.SetActive(true);
    }

    private void HideDeleteAllNotesConfirmDialog()
    {
        if (deleteAllNotesConfirmDialog != null)
            deleteAllNotesConfirmDialog.SetActive(false);
    }

    private GameObject CreateDeleteAllNotesConfirmDialog(Transform parent)
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject overlay = new GameObject("DeleteAllNotesConfirmDialog");
        overlay.transform.SetParent(parent, false);

        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image dim = overlay.AddComponent<Image>();
        dim.color = Color.clear;

        GameObject dialog = new GameObject("Dialog");
        dialog.transform.SetParent(overlay.transform, false);

        RectTransform dialogRect = dialog.AddComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(320, 170);

        Image panelImage = dialog.AddComponent<Image>();
        panelImage.color = new Color(0.98f, 0.97f, 0.9f, 1f);

        CreateTopAnchoredLabel(dialog.transform, "Delete all notes?", font, 20, new Vector2(0, -38), new Vector2(280, 34), Color.black);
        CreateTopAnchoredLabel(dialog.transform, "This cannot be undone.", font, 15, new Vector2(0, -76), new Vector2(280, 28), new Color(0.35f, 0.12f, 0.12f, 1f));
        CreateButton(dialog.transform, "Cancel", font, new Vector2(-78, -126), new Vector2(112, 38), HideDeleteAllNotesConfirmDialog, new Color(0.95f, 0.95f, 0.86f, 1f), Color.black, 15);
        CreateButton(dialog.transform, "Delete", font, new Vector2(78, -126), new Vector2(112, 38), ConfirmClearAllData, new Color(0.72f, 0.12f, 0.12f, 1f), Color.white, 15);

        overlay.SetActive(false);
        return overlay;
    }

    private static Text CreateRowLabel(Transform parent, string text, Font font, float y)
    {
        Text label = CreateTopAnchoredLabel(parent, text, font, 16, new Vector2(-154, y), new Vector2(90, 28), Color.black);
        label.alignment = TextAnchor.MiddleLeft;
        return label;
    }

    private static Image CreateBlock(Transform parent, string name, Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateTopAnchoredLabel(Transform parent, string text, Font font, int size, Vector2 position, Vector2 dimensions, Color color)
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
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Image CreateChildImage(Transform parent, string name, Vector2 dimensions, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
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

    private static InputField CreateInput(Transform parent, string placeholder, Font font, Vector2 position, Vector2 dimensions, bool multiline = false, int characterLimit = 0)
    {
        GameObject obj = new GameObject((string.IsNullOrEmpty(placeholder) ? "Text" : placeholder) + "Input");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(1f, 0.99f, 0.88f, 1f);

        InputField input = obj.AddComponent<InputField>();
        Text text = CreateInputText(obj.transform, "Text", font, Color.black);
        Text placeholderText = CreateInputText(obj.transform, "Placeholder", font, new Color(0.45f, 0.45f, 0.45f));
        text.text = "";
        placeholderText.text = placeholder;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.text = "";
        input.characterLimit = characterLimit;
        input.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
        input.contentType = InputField.ContentType.Standard;
        return input;
    }

    private static Text CreateInputText(Transform parent, string name, Font font, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8, 5);
        rect.offsetMax = new Vector2(-8, -5);

        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.fontSize = 16;
        text.color = color;
        text.alignment = TextAnchor.UpperLeft;
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
        rect.sizeDelta = new Vector2(250, 32);

        Image background = obj.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.02f);

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.targetGraphic = background;

        Image checkbox = CreateChildImage(obj.transform, "ReminderCheckbox", new Vector2(18, 18), Color.white);
        checkbox.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        checkbox.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        checkbox.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        checkbox.rectTransform.anchoredPosition = new Vector2(18, 0);
        Outline checkboxOutline = checkbox.gameObject.AddComponent<Outline>();
        checkboxOutline.effectColor = new Color(0.18f, 0.2f, 0.18f, 1f);
        checkboxOutline.effectDistance = new Vector2(1f, -1f);
        GameObject checkmark = CreateCheckmarkGraphic(checkbox.transform);
        checkmark.name = "ReminderCheckmark";
        checkmark.SetActive(false);

        CreateCenteredChildLabel(obj.transform, label, font, 15, Color.black, TextAnchor.MiddleCenter, new Vector2(30, 0), Vector2.zero);
        return toggle;
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 position, Vector2 dimensions, UnityAction action)
    {
        return CreateButton(parent, label, font, position, dimensions, action, new Color(0.95f, 0.95f, 0.95f, 1f), Color.black, 15);
    }

    private static Button CreateButton(Transform parent, string label, Font font, Vector2 position, Vector2 dimensions, UnityAction action, Color backgroundColor, Color textColor, int fontSize)
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
        image.color = backgroundColor;

        Button button = obj.AddComponent<Button>();
        RuntimeButtonActionRelay relay = obj.AddComponent<RuntimeButtonActionRelay>();
        relay.Configure(action);
        button.onClick.AddListener(relay.Invoke);

        CreateCenteredChildLabel(obj.transform, label, font, fontSize, textColor, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
        return button;
    }

    private Button CreateCompleteButton(Transform parent, Font font, Vector2 position, Vector2 dimensions)
    {
        GameObject obj = new GameObject("CompleteButton");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.95f, 0.95f, 0.86f, 1f);
        completeButtonBackground = image;

        Button button = obj.AddComponent<Button>();
        RuntimeButtonActionRelay relay = obj.AddComponent<RuntimeButtonActionRelay>();
        relay.Configure(ToggleComplete);
        button.onClick.AddListener(relay.Invoke);

        Image checkbox = CreateChildImage(obj.transform, "CompleteCheckbox", new Vector2(18, 18), Color.white);
        checkbox.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        checkbox.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        checkbox.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        checkbox.rectTransform.anchoredPosition = new Vector2(18, 0);
        checkbox.color = new Color(1f, 1f, 1f, 1f);
        completeCheckboxBackground = checkbox;
        Outline checkboxOutline = checkbox.gameObject.AddComponent<Outline>();
        checkboxOutline.effectColor = new Color(0.18f, 0.2f, 0.18f, 1f);
        checkboxOutline.effectDistance = new Vector2(1f, -1f);

        completeCheckmark = CreateCheckmarkGraphic(checkbox.transform);
        completeCheckmark.SetActive(false);

        Text label = CreateCenteredChildLabel(obj.transform, "Complete", font, 14, Color.black, TextAnchor.MiddleCenter, new Vector2(28, 0), new Vector2(0, 0));
        label.fontStyle = FontStyle.Bold;
        return button;
    }

    private static GameObject CreateCheckmarkGraphic(Transform parent)
    {
        GameObject root = new GameObject("CompleteCheckmark");
        root.transform.SetParent(parent, false);

        RectTransform rect = root.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        CreateCheckmarkStroke(root.transform, "ShortStroke", new Vector2(-3f, -1f), new Vector2(7f, 3f), -45f);
        CreateCheckmarkStroke(root.transform, "LongStroke", new Vector2(3f, 1f), new Vector2(12f, 3f), 45f);
        return root;
    }

    private static void CreateCheckmarkStroke(Transform parent, string name, Vector2 position, Vector2 dimensions, float rotation)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.08f, 0.45f, 0.16f, 1f);
        image.raycastTarget = false;
    }

    private void SetReminderTime(string reminderTime)
    {
        if (string.IsNullOrWhiteSpace(reminderTime) || !ReminderManager.TryParseReminderTime(reminderTime, out DateTime dateTime))
        {
            selectedReminderTime = DateTime.Now;
            hasSelectedReminderTime = true;
            UpdateReminderPicker();
            UpdateReminderSummary();
            return;
        }

        selectedReminderTime = dateTime;
        hasSelectedReminderTime = true;
        UpdateReminderPicker();
        UpdateReminderSummary();
    }

    private void SetReminderNow()
    {
        selectedReminderTime = DateTime.Now;
        hasSelectedReminderTime = true;
        UpdateReminderPicker();
        UpdateReminderSummary();
    }

    private void ShiftYear(int delta)
    {
        EnsureReminderTimeSeed();
        selectedReminderTime = selectedReminderTime.AddYears(delta);
        ClampDayToMonth();
        UpdateReminderPickerAndSummary();
    }

    private void ShiftMonth(int delta)
    {
        EnsureReminderTimeSeed();
        selectedReminderTime = selectedReminderTime.AddMonths(delta);
        ClampDayToMonth();
        UpdateReminderPickerAndSummary();
    }

    private void ShiftDay(int delta)
    {
        EnsureReminderTimeSeed();
        selectedReminderTime = selectedReminderTime.AddDays(delta);
        UpdateReminderPickerAndSummary();
    }

    private void ShiftHour(int delta)
    {
        EnsureReminderTimeSeed();
        selectedReminderTime = selectedReminderTime.AddHours(delta);
        UpdateReminderPickerAndSummary();
    }

    private void ShiftMinute(int delta)
    {
        EnsureReminderTimeSeed();
        selectedReminderTime = selectedReminderTime.AddMinutes(delta);
        UpdateReminderPickerAndSummary();
    }

    private void TogglePeriod()
    {
        EnsureReminderTimeSeed();
        selectedReminderTime = selectedReminderTime.AddHours(12);
        UpdateReminderPickerAndSummary();
    }

    private void ClearReminderTime()
    {
        hasSelectedReminderTime = false;
        reminderToggle.isOn = false;
        UpdateReminderSummary();
    }

    private void EnsureReminderTimeSeed()
    {
        if (hasSelectedReminderTime)
            return;

        selectedReminderTime = DateTime.Now;
        hasSelectedReminderTime = true;
    }

    private void UpdateReminderPickerAndSummary()
    {
        UpdateReminderPicker();
        UpdateReminderSummary();
    }

    private void UpdateReminderSummary()
    {
        if (reminderSummaryText == null)
            return;

        if (!hasSelectedReminderTime)
        {
            reminderSummaryText.text = reminderToggle != null && reminderToggle.isOn ? "Pick a time first" : "No time set";
            reminderSummaryText.color = reminderToggle != null && reminderToggle.isOn ? new Color(0.72f, 0.12f, 0.1f, 1f) : Color.black;
            return;
        }

        string dayLabel;
        DateTime today = DateTime.Today;
        if (selectedReminderTime.Date == today)
            dayLabel = "Today";
        else if (selectedReminderTime.Date == today.AddDays(1))
            dayLabel = "Tomorrow";
        else
            dayLabel = selectedReminderTime.ToString("MMM d", CultureInfo.InvariantCulture);

        reminderSummaryText.text = dayLabel + ", " + selectedReminderTime.ToString("h:mm tt", CultureInfo.InvariantCulture);
        reminderSummaryText.color = selectedReminderTime <= DateTime.Now && reminderToggle != null && reminderToggle.isOn
            ? new Color(0.72f, 0.12f, 0.1f, 1f)
            : Color.black;
    }

    private void UpdateReminderPicker()
    {
        if (!hasSelectedReminderTime)
        {
            SetPickerColumn("month", "", "", "");
            SetPickerColumn("year", "", "", "");
            SetPickerColumn("day", "", "", "");
            SetPickerColumn("hour", "", "", "");
            SetPickerColumn("minute", "", "", "");
            SetPickerColumn("period", "", "", "");
            return;
        }

        SetPickerColumn("year",
            (selectedReminderTime.Year - 1).ToString(),
            selectedReminderTime.Year.ToString(),
            (selectedReminderTime.Year + 1).ToString());

        SetPickerColumn("month",
            MonthNames[Wrap(selectedReminderTime.Month - 2, 12)],
            MonthNames[selectedReminderTime.Month - 1],
            MonthNames[Wrap(selectedReminderTime.Month, 12)]);

        int daysInMonth = DateTime.DaysInMonth(selectedReminderTime.Year, selectedReminderTime.Month);
        SetPickerColumn("day",
            WrapDay(selectedReminderTime.Day - 1, daysInMonth).ToString("00"),
            selectedReminderTime.Day.ToString("00"),
            WrapDay(selectedReminderTime.Day + 1, daysInMonth).ToString("00"));

        int hour12 = selectedReminderTime.Hour % 12;
        if (hour12 == 0)
            hour12 = 12;
        SetPickerColumn("hour",
            WrapHour12(hour12 - 1).ToString("00"),
            hour12.ToString("00"),
            WrapHour12(hour12 + 1).ToString("00"));

        SetPickerColumn("minute",
            Wrap(selectedReminderTime.Minute - 1, 60).ToString("00"),
            selectedReminderTime.Minute.ToString("00"),
            Wrap(selectedReminderTime.Minute + 1, 60).ToString("00"));

        string period = selectedReminderTime.Hour >= 12 ? "PM" : "AM";
        string otherPeriod = period == "AM" ? "PM" : "AM";
        SetPickerColumn("period", otherPeriod, period, otherPeriod);
    }

    private void SetPickerColumn(string key, string previous, string current, string next)
    {
        if (!pickerOptionLabels.TryGetValue(key, out Text[] labels) || labels == null || labels.Length < 3)
            return;

        labels[0].text = previous;
        labels[1].text = current;
        labels[2].text = next;
    }

    private void ClampDayToMonth()
    {
        int daysInMonth = DateTime.DaysInMonth(selectedReminderTime.Year, selectedReminderTime.Month);
        if (selectedReminderTime.Day <= daysInMonth)
            return;

        selectedReminderTime = new DateTime(
            selectedReminderTime.Year,
            selectedReminderTime.Month,
            daysInMonth,
            selectedReminderTime.Hour,
            selectedReminderTime.Minute,
            0);
    }

    private static int Wrap(int value, int count)
    {
        return (value % count + count) % count;
    }

    private static int WrapDay(int day, int daysInMonth)
    {
        if (day < 1)
            return daysInMonth;

        if (day > daysInMonth)
            return 1;

        return day;
    }

    private static int WrapHour12(int hour)
    {
        if (hour < 1)
            return 12;

        if (hour > 12)
            return 1;

        return hour;
    }

    private static string GetInputValue(InputField input)
    {
        if (input == null)
            return "";

        string value = input.text ?? "";
        Text placeholderText = input.placeholder as Text;
        if (placeholderText != null && value == placeholderText.text)
            return "";

        return value;
    }

    private static string NormalizeChoice(string value, string fallback, string[] allowedValues)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        string normalized = value.Trim().ToLowerInvariant();
        foreach (string allowedValue in allowedValues)
        {
            if (normalized == allowedValue)
                return normalized;
        }

        return fallback;
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        return char.ToUpperInvariant(value[0]) + value.Substring(1);
    }

    private static string GetIconFallbackLabel(string iconId)
    {
        switch (iconId)
        {
            case "star": return "Star";
            case "finish": return "Done";
            case "inprocess": return "Doing";
            case "reminder": return "Bell";
            case "work": return "Work";
            case "study": return "Study";
            case "shopping": return "Shop";
            default: return iconId;
        }
    }

    private static Sprite GetIconSprite(NoteStyleManager styleManager, string iconId)
    {
        if (styleManager == null)
            return null;

        switch (iconId)
        {
            case "star": return styleManager.starIcon;
            case "finish": return styleManager.finishIcon;
            case "inprocess": return styleManager.inprocessIcon;
            case "reminder": return styleManager.reminderIcon;
            case "work": return styleManager.workIcon;
            case "study": return styleManager.studyIcon;
            case "shopping": return styleManager.shoppingIcon;
            default: return null;
        }
    }

    private static Color GetColorValue(string colorName, float alpha)
    {
        switch (colorName)
        {
            case "pink": return new Color(1f, 0.56f, 0.66f, alpha);
            case "blue": return new Color(0.55f, 0.82f, 0.92f, alpha);
            case "green": return new Color(0.56f, 0.85f, 0.48f, alpha);
            case "yellow":
            default:
                return new Color(0.96f, 0.78f, 0.18f, alpha);
        }
    }

    private static Color GetPanelColor(string colorName)
    {
        switch (colorName)
        {
            case "pink": return new Color(1f, 0.86f, 0.89f, 0.96f);
            case "blue": return new Color(0.86f, 0.95f, 0.98f, 0.96f);
            case "green": return new Color(0.88f, 0.96f, 0.82f, 0.96f);
            case "yellow":
            default:
                return new Color(1f, 0.96f, 0.68f, 0.96f);
        }
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = (size - 1) * 0.5f;
        Color clear = new Color(1f, 1f, 1f, 0f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                texture.SetPixel(x, y, alpha > 0f ? new Color(1f, 1f, 1f, alpha) : clear);
            }
        }

        texture.Apply();
        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
