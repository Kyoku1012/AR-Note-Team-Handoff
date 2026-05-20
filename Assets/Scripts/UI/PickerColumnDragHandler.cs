using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

public class PickerColumnDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float StepThreshold = 18f;

    private UnityAction upAction;
    private UnityAction downAction;
    private float accumulatedDrag;

    public void Configure(UnityAction onSwipeDown, UnityAction onSwipeUp)
    {
        upAction = onSwipeDown;
        downAction = onSwipeUp;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        accumulatedDrag = 0f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        accumulatedDrag += eventData.delta.y;

        while (accumulatedDrag >= StepThreshold)
        {
            accumulatedDrag -= StepThreshold;
            upAction?.Invoke();
        }

        while (accumulatedDrag <= -StepThreshold)
        {
            accumulatedDrag += StepThreshold;
            downAction?.Invoke();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        accumulatedDrag = 0f;
    }
}
