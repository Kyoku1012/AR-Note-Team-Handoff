using UnityEngine;
using UnityEngine.UI;

public class NoteStyleManager : MonoBehaviour
{
    [Header("Color Panels")]
    public GameObject yellowPanel;
    public GameObject pinkPanel;
    public GameObject bluePanel;
    public GameObject greenPanel;

    [Header("Icon")]
    public Image iconImage;
    public Sprite starIcon;
    public Sprite finishIcon;
    public Sprite inprocessIcon;
    public Sprite reminderIcon;
    public Sprite workIcon;
    public Sprite studyIcon;
    public Sprite shoppingIcon;

    [Header("Priority")]
    public Image priorityImage;
    public Sprite priorityHighSprite;
    public Sprite priorityMediumSprite;
    public Sprite priorityLowSprite;

    public void ApplyStyle(NoteData data)
    {
        if (data == null)
        {
            Debug.LogWarning("NoteStyleManager.ApplyStyle received null NoteData.");
            return;
        }

        string color = Normalize(data.colorName);
        string icon = Normalize(data.iconId);
        string priority = Normalize(data.priorityId);

        // Color Panels
        SetActiveSafe(yellowPanel, false);
        SetActiveSafe(pinkPanel, false);
        SetActiveSafe(bluePanel, false);
        SetActiveSafe(greenPanel, false);

        switch (color)
        {
            case "pink":
                SetActiveSafe(pinkPanel, true);
                break;
            case "blue":
                SetActiveSafe(bluePanel, true);
                break;
            case "green":
                SetActiveSafe(greenPanel, true);
                break;
            case "yellow":
            case "":
            case "null":
            default:
                SetActiveSafe(yellowPanel, true);
                break;
        }

        // Icon
        if (iconImage != null)
        {
            Sprite selectedIcon = null;

            switch (icon)
            {
                case "star":
                    selectedIcon = starIcon;
                    break;
                case "finish":
                    selectedIcon = finishIcon;
                    break;
                case "inprocess":
                    selectedIcon = inprocessIcon;
                    break;
                case "reminder":
                    selectedIcon = reminderIcon;
                    break;
                case "work":
                    selectedIcon = workIcon;
                    break;
                case "study":
                    selectedIcon = studyIcon;
                    break;
                case "shopping":
                    selectedIcon = shoppingIcon;
                    break;
                default:
                    selectedIcon = null;
                    break;
            }

            iconImage.sprite = selectedIcon;
            iconImage.gameObject.SetActive(selectedIcon != null);
        }

        // Priority Sprite
        if (priorityImage != null)
        {
            Sprite selectedPrioritySprite = null;

            switch (priority)
            {
                case "high":
                    selectedPrioritySprite = priorityHighSprite;
                    break;
                case "medium":
                    selectedPrioritySprite = priorityMediumSprite;
                    break;
                case "low":
                    selectedPrioritySprite = priorityLowSprite;
                    break;
                default:
                    selectedPrioritySprite = null;
                    break;
            }

            priorityImage.sprite = selectedPrioritySprite;
            priorityImage.gameObject.SetActive(selectedPrioritySprite != null);
        }
    }

    private string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
    }

    private void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
    }
}
