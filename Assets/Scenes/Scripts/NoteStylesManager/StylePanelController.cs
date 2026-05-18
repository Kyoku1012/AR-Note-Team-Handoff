using UnityEngine;

public class StylePanelController : MonoBehaviour
{
    public NoteStyleManager selectedNote;

    private NoteView selectedView;

    private void OnEnable()
    {
        if (NoteManager.Instance != null)
            NoteManager.Instance.NoteSelected += SetSelectedNote;
    }

    private void OnDisable()
    {
        if (NoteManager.Instance != null)
            NoteManager.Instance.NoteSelected -= SetSelectedNote;
    }

    private void Start()
    {
        if (NoteManager.Instance != null && NoteManager.Instance.SelectedNote != null)
            SetSelectedNote(NoteManager.Instance.SelectedNote);
    }

    public void SetSelectedNote(NoteStyleManager note)
    {
        selectedNote = note;
        selectedView = note == null ? null : note.GetComponentInParent<NoteView>();
    }

    public void SetSelectedNote(NoteView noteView)
    {
        selectedView = noteView;
        selectedNote = noteView == null ? null : noteView.GetComponentInChildren<NoteStyleManager>(true);
    }

    public void SetYellow() => SetColor("yellow");
    public void SetPink() => SetColor("pink");
    public void SetBlue() => SetColor("blue");
    public void SetGreen() => SetColor("green");

    public void SetStarIcon() => SetIcon("star");
    public void SetFinishIcon() => SetIcon("finish");
    public void SetInProcessIcon() => SetIcon("inprocess");
    public void SetReminderIcon() => SetIcon("reminder");
    public void SetWorkIcon() => SetIcon("work");
    public void SetStudyIcon() => SetIcon("study");
    public void SetShoppingIcon() => SetIcon("shopping");
    public void HideIcon() => SetIcon("");

    public void SetHighPriority() => SetPriority("high");
    public void SetMediumPriority() => SetPriority("medium");
    public void SetLowPriority() => SetPriority("low");
    public void HidePriority() => SetPriority("");

    private void SetColor(string colorName)
    {
        NoteData data = GetSelectedData();
        if (data == null) return;

        data.colorName = colorName;
        data.colorLabel = colorName;
        SaveSelected();
    }

    private void SetIcon(string iconId)
    {
        NoteData data = GetSelectedData();
        if (data == null) return;

        data.iconId = iconId;
        SaveSelected();
    }

    private void SetPriority(string priorityId)
    {
        NoteData data = GetSelectedData();
        if (data == null) return;

        data.priorityId = priorityId;
        SaveSelected();
    }

    private NoteData GetSelectedData()
    {
        if (selectedView == null && NoteManager.Instance != null)
            selectedView = NoteManager.Instance.SelectedNote;

        if (selectedView == null)
        {
            Debug.LogWarning("StylePanelController: select or place a note before styling.");
            return null;
        }

        return selectedView.Data;
    }

    private void SaveSelected()
    {
        if (selectedView != null)
            selectedView.SaveAndRefresh();
        else if (selectedNote != null)
            selectedNote.ApplyStyle(GetSelectedData());
    }
}
