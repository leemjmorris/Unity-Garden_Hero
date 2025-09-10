using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TimelineSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public AudioManager audioManager;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        if (audioManager != null)
            audioManager.OnSliderPointerDown();
    }
    
    public void OnPointerUp(PointerEventData eventData)
    {
        if (audioManager != null)
            audioManager.OnSliderPointerUp();
    }
}