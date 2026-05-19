using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Vuforia;

public class PlaceNote : MonoBehaviour
{
    public GameObject notePrefab;
    public float offset = 0.01f;
    public float noteTapRayDistance = 20f;
    [Header("Create Note Placement")]
    public float wallCreateDistance = 1.2f;
    public float horizontalCreateDistance = 1.0f;
    public float horizontalCreateDrop = 0.45f;
    [Range(-1f, 0f)]
    public float lookDownThreshold = -0.45f;

    private PlaneFinderBehaviour planeFinder;

    private void Start()
    {
        planeFinder = GetComponent<PlaneFinderBehaviour>();
        StartCoroutine(RestoreSavedNotes());
    }

    private void Update()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPosition = Input.GetTouch(0).position;

            if (TryOpenExistingNoteFromUi(touchPosition))
                return;

            if (HandleBlockingUiTap(touchPosition))
            {
                Debug.Log("Touch is over blocking UI, skip plane hit test.");
                return;
            }

            if (TryOpenExistingNote(touchPosition))
                return;

            planeFinder?.PerformHitTest(touchPosition);
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;

            if (TryOpenExistingNoteFromUi(mousePosition))
                return;

            if (HandleBlockingUiTap(mousePosition))
            {
                Debug.Log("Mouse is over blocking UI, skip plane hit test.");
                return;
            }

            if (TryOpenExistingNote(mousePosition))
                return;

            planeFinder?.PerformHitTest(mousePosition);
        }
#endif
    }

    public void AnchorCreated(HitTestResult result)
    {
        if (result == null || notePrefab == null) return;

        Quaternion finalRotation = CalculateReadableRotation(result);
        CreateNoteAt("NoteAnchor", result.Position, finalRotation, true, "New Note", "Tap Edit to add details");
    }

    public NoteView CreateCenterScreenNote()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Center-screen notes can only be created in Play Mode.");
            return null;
        }

        if (notePrefab == null)
        {
            Debug.LogWarning("PlaceNote cannot create a center-screen note because notePrefab is not assigned.");
            return null;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (TryOpenExistingNoteFromUi(center) || TryOpenExistingNote(center))
            {
                Debug.Log("Center screen is already occupied by a note; opened the existing note instead of creating another.");
                return null;
            }
        }

        NoteView noteView = CreateNoteInFrontOfCamera("CenterScreenNoteAnchor", "New Note", "Tap Edit to add details");
        if (noteView != null)
            Debug.Log("Created center-screen note without ground-plane detection.");

        return noteView;
    }

    private NoteView CreateNoteInFrontOfCamera(string anchorName, string title, string content)
    {
        if (notePrefab == null)
            return null;

        CalculateCreateNotePose(out Vector3 position, out Quaternion rotation, out string placementMode);
        Debug.Log("Create Note placement mode: " + placementMode);

        return CreateNoteAt(anchorName, position, rotation, true, title, content);
    }

    private void CalculateCreateNotePose(out Vector3 position, out Quaternion rotation, out string placementMode)
    {
        Transform cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        Vector3 cameraForward = cameraTransform.forward.sqrMagnitude > 0.01f
            ? cameraTransform.forward.normalized
            : Vector3.forward;

        bool lookingAtHorizontalSurface = cameraForward.y < lookDownThreshold;
        Vector3 flatForward = FlattenToGround(cameraForward, cameraTransform);

        if (lookingAtHorizontalSurface)
        {
            placementMode = "horizontal surface";
            position = cameraTransform.position
                + flatForward * horizontalCreateDistance
                + Vector3.down * horizontalCreateDrop;
            rotation = Quaternion.LookRotation(flatForward, Vector3.up);
            return;
        }

        placementMode = "wall/front surface";
        position = cameraTransform.position + cameraForward * wallCreateDistance;
        rotation = Quaternion.LookRotation(flatForward, Vector3.up);
    }

    private Vector3 FlattenToGround(Vector3 direction, Transform fallbackTransform)
    {
        Vector3 flatForward = direction;
        flatForward.y = 0f;

        if (flatForward.sqrMagnitude < 0.01f)
        {
            flatForward = fallbackTransform.forward;
            flatForward.y = 0f;
        }

        if (flatForward.sqrMagnitude < 0.01f)
        {
            flatForward = fallbackTransform.up;
            flatForward.y = 0f;
        }

        if (flatForward.sqrMagnitude < 0.01f)
            flatForward = Vector3.forward;

        return flatForward.normalized;
    }

    private NoteView CreateNoteAt(string anchorName, Vector3 position, Quaternion rotation, bool selectOnCreate, string title, string content)
    {
        GameObject anchorObj = CreateAnchor(anchorName, position, rotation);
        GameObject instantiatedNote = Instantiate(notePrefab, anchorObj.transform);
        instantiatedNote.transform.localPosition = new Vector3(0, offset, 0);
        instantiatedNote.transform.localRotation = Quaternion.identity;

        NoteData data = new NoteData
        {
            title = title,
            content = content,
            annotation = "",
            isVisible = true,
            colorName = "yellow",
            iconId = "",
            priorityId = "",
            worldPosition = anchorObj.transform.position,
            worldRotation = anchorObj.transform.eulerAngles
        };
        data.ApplyDefaults();

        NoteView noteView = instantiatedNote.GetComponent<NoteView>() ?? instantiatedNote.AddComponent<NoteView>();
        noteView.Initialize(data, anchorObj, selectOnCreate);

        NoteManager.Instance?.AddNote(noteView.Data);

        StylePanelController stylePanel = FindObjectOfType<StylePanelController>();
        if (stylePanel != null)
            stylePanel.SetSelectedNote(noteView);

        return noteView;
    }

    private IEnumerator RestoreSavedNotes()
    {
        yield return null;

        if (notePrefab == null || NoteManager.Instance == null)
            yield break;

        foreach (NoteData note in NoteManager.Instance.GetAllNotes())
        {
            if (note == null || string.IsNullOrWhiteSpace(note.id))
                continue;

            if (NoteManager.Instance.GetView(note.id) != null)
                continue;

            GameObject anchorObj = CreateAnchor("NoteAnchor_" + note.id, note.worldPosition, Quaternion.Euler(note.worldRotation));
            GameObject instantiatedNote = Instantiate(notePrefab, anchorObj.transform);
            instantiatedNote.transform.localPosition = new Vector3(0, offset, 0);
            instantiatedNote.transform.localRotation = Quaternion.identity;

            NoteView noteView = instantiatedNote.GetComponent<NoteView>() ?? instantiatedNote.AddComponent<NoteView>();
            noteView.Initialize(note, anchorObj, false);
        }

        AlarmManager.Instance?.ScheduleAll(NoteManager.Instance.GetAllNotes());
    }

    private Quaternion CalculateReadableRotation(HitTestResult result)
    {
        Vector3 surfaceNormal = result.Rotation * Vector3.up;

        if (Mathf.Abs(surfaceNormal.y) < 0.5f)
            return Quaternion.LookRotation(surfaceNormal, Vector3.up);

        Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.01f && Camera.main != null)
            camForward = Camera.main.transform.up;

        return Quaternion.LookRotation(camForward, Vector3.up);
    }

    private GameObject CreateAnchor(string anchorName, Vector3 position, Quaternion rotation)
    {
        GameObject anchorObj = new GameObject(anchorName);
        anchorObj.transform.position = position;
        anchorObj.transform.rotation = rotation;
        anchorObj.AddComponent<AnchorBehaviour>();
        return anchorObj;
    }

    private bool TryOpenExistingNote(Vector2 screenPosition)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return false;

        Ray ray = camera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, noteTapRayDistance))
            return false;

        NoteView noteView = hit.collider.GetComponentInParent<NoteView>();
        if (noteView == null)
            return false;

        noteView.OpenEditor();
        Debug.Log("Existing note tapped; opening editor instead of creating a new note.");
        return true;
    }

    private bool TryOpenExistingNoteFromUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == null)
                continue;

            NoteView noteView = result.gameObject.GetComponentInParent<NoteView>();
            if (noteView == null)
                continue;

            Button noteButton = result.gameObject.GetComponentInParent<Button>();
            if (noteButton != null)
            {
                if (noteButton.IsActive() && noteButton.IsInteractable())
                    noteButton.onClick.Invoke();

                Debug.Log("Existing note button tapped; handled note UI button.");
                return true;
            }
        }

        return false;
    }

    private bool HandleBlockingUiTap(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            GameObject hitObject = result.gameObject;
            if (hitObject == null)
                continue;

            if (hitObject.GetComponentInParent<NoteView>() != null)
                continue;

            if (TryHandleInteractiveUi(hitObject))
                return true;
        }

        foreach (RaycastResult result in results)
        {
            GameObject hitObject = result.gameObject;
            if (hitObject == null)
                continue;

            // Note UI should be handled by TryOpenExistingNoteFromUi, not treated as a blocker.
            if (hitObject.GetComponentInParent<NoteView>() != null)
                continue;

            if (IsBusinessUiBlocker(hitObject))
            {
                Debug.Log("Touch hit non-interactive business UI: " + hitObject.name);
                return true;
            }
        }

        return false;
    }

    private bool TryHandleInteractiveUi(GameObject hitObject)
    {
        RuntimeButtonActionRelay relay = hitObject.GetComponentInParent<RuntimeButtonActionRelay>();
        if (relay != null)
        {
            relay.Invoke();
            Debug.Log("Manually invoked runtime UI relay: " + relay.name);
            return true;
        }

        Button button = hitObject.GetComponentInParent<Button>();
        if (button != null)
        {
            if (button.IsActive() && button.IsInteractable())
            {
                button.onClick.Invoke();
                Debug.Log("Manually invoked UI button: " + button.name);
            }
            return true;
        }

        Toggle toggle = hitObject.GetComponentInParent<Toggle>();
        if (toggle != null)
        {
            if (toggle.IsActive() && toggle.IsInteractable())
            {
                toggle.isOn = !toggle.isOn;
                Debug.Log("Manually toggled UI control: " + toggle.name);
            }
            return true;
        }

        TMP_InputField tmpInput = hitObject.GetComponentInParent<TMP_InputField>();
        if (tmpInput != null)
        {
            tmpInput.ActivateInputField();
            Debug.Log("Activated TMP input field: " + tmpInput.name);
            return true;
        }

        InputField input = hitObject.GetComponentInParent<InputField>();
        if (input != null)
        {
            input.ActivateInputField();
            Debug.Log("Activated input field: " + input.name);
            return true;
        }

        return false;
    }

    private bool IsBusinessUiBlocker(GameObject hitObject)
    {
        return hitObject.GetComponentInParent<NoteEditPanel>() != null
            || hitObject.GetComponentInParent<StylePanelController>() != null
            || hitObject.GetComponentInParent<Selectable>() != null;
    }
}
