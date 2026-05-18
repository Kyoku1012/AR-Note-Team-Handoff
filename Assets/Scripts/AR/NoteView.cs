using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class NoteView : MonoBehaviour, IPointerClickHandler
{
    public NoteData Data { get; private set; }
    public GameObject AnchorRoot { get; private set; }

    private TMP_Text titleText;
    private TMP_Text contentText;
    private NoteStyleManager styleManager;
    private Canvas[] noteCanvases;
    private AudioSource audioSource;

    public void Initialize(NoteData data, GameObject anchorRoot, bool selectOnCreate = true)
    {
        Data = data;
        AnchorRoot = anchorRoot;
        Data.ApplyDefaults();

        CacheComponents();
        WireButtons();
        RefreshFromData();

        NoteManager.Instance?.RegisterView(this);
        if (selectOnCreate)
            NoteManager.Instance?.SelectNote(this);
    }

    private void OnDestroy()
    {
        NoteManager.Instance?.UnregisterView(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Select();
    }

    private void OnMouseDown()
    {
        Select();
    }

    public void Select()
    {
        NoteManager.Instance?.SelectNote(this);

        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(this);
    }

    public void RefreshFromData()
    {
        if (Data == null) return;

        Data.ApplyDefaults();
        CacheComponents();

        if (titleText != null)
            titleText.text = Data.isCompleted ? Data.title + " (Done)" : Data.title;

        if (contentText != null)
            contentText.text = string.IsNullOrWhiteSpace(Data.content) ? Data.annotation : Data.content;

        if (styleManager != null)
            styleManager.ApplyStyle(Data);

        SetCanvasesVisible(Data.isVisible);
    }

    public void ToggleCompleted()
    {
        if (Data == null) return;
        Data.isCompleted = !Data.isCompleted;
        SaveAndRefresh();
    }

    public void ToggleVisible()
    {
        if (Data == null) return;
        Data.isVisible = !Data.isVisible;
        SaveAndRefresh();
    }

    public void OpenEditor()
    {
        Select();
        NoteEditPanel.EnsureExists().Open(this);
    }

    public void DeleteNote()
    {
        if (Data == null) return;
        NoteManager.Instance?.RemoveNote(Data.id);
    }

    public void ToggleVoiceRecording()
    {
        Select();
        VoiceNoteManager.Instance?.ToggleRecording(this);
    }

    public void PlayVoice()
    {
        if (Data == null || string.IsNullOrWhiteSpace(Data.voiceFilePath)) return;

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        VoiceNoteManager.Instance?.Play(Data.voiceFilePath, audioSource);
    }

    public void SaveAndRefresh()
    {
        if (Data == null) return;

        Data.worldPosition = AnchorRoot != null ? AnchorRoot.transform.position : transform.position;
        Data.worldRotation = AnchorRoot != null ? AnchorRoot.transform.eulerAngles : transform.eulerAngles;
        NoteManager.Instance?.UpdateNote(Data);
        RefreshFromData();
    }

    private void CacheComponents()
    {
        if (styleManager == null)
            styleManager = GetComponentInChildren<NoteStyleManager>(true);

        if (titleText == null)
            titleText = FindText("TitleText-NeedtoEdite") ?? FindText("TitleText");

        if (contentText == null)
            contentText = FindText("ContentText-NeedtoEdite") ?? FindText("ContentText");

        if (noteCanvases == null || noteCanvases.Length == 0)
            noteCanvases = GetComponentsInChildren<Canvas>(true);
    }

    private void WireButtons()
    {
        WireButton("CheckButton", ToggleCompleted);
        WireButton("EditButton", OpenEditor);
        WireButton("DeleteButton", DeleteNote);
        WireButton("VoiceButton", ToggleVoiceRecording);
    }

    private void WireButton(string childName, UnityEngine.Events.UnityAction action)
    {
        Transform child = FindDeepChild(transform, childName);
        if (child == null) return;

        Button button = child.GetComponent<Button>();
        if (button == null) return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child == null ? null : child.GetComponent<TMP_Text>();
    }

    private void SetCanvasesVisible(bool visible)
    {
        if (noteCanvases == null) return;

        foreach (Canvas canvas in noteCanvases)
        {
            if (canvas != null)
                canvas.gameObject.SetActive(visible);
        }
    }

    private Transform FindDeepChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindDeepChild(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }
}
