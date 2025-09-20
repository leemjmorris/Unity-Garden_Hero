using UnityEngine;
using UnityEngine.UI;

public class LongNoteVisual : MonoBehaviour
{
    [Header("Long Note Parts")]
    private GameObject startCap;
    private GameObject body;
    private GameObject endCap;
    
    [Header("Visual Settings")]
    private float noteWidth = 50f;
    private float capHeight = 20f;
    
    private RhythmNote parentNote;
    private string direction;
    private float duration;
    private float speed;
    
    public void Initialize(RhythmNote note, float dur, float spd)
    {
        parentNote = note;
        direction = note.direction;
        duration = dur;
        speed = spd;
        
        CreateLongNoteVisual();
    }
    
    void CreateLongNoteVisual()
    {
        // LMJ: Create start cap
        startCap = CreateNotePart("StartCap", capHeight, noteWidth);
        startCap.GetComponent<Image>().color = new Color(1f, 0.9f, 0f, 1f); // LMJ: Bright yellow
        
        // LMJ: Create body
        float bodyLength = duration * speed; // LMJ: Length based on duration
        body = CreateNotePart("Body", bodyLength, noteWidth);
        body.GetComponent<Image>().color = new Color(0.9f, 0.8f, 0f, 0.8f); // LMJ: Semi-transparent yellow
        
        // LMJ: Create end cap
        endCap = CreateNotePart("EndCap", capHeight, noteWidth);
        endCap.GetComponent<Image>().color = new Color(1f, 0.9f, 0f, 1f); // LMJ: Bright yellow
        
        // LMJ: Position parts relative to each other
        PositionParts();
    }
    
    GameObject CreateNotePart(string partName, float width, float height)
    {
        GameObject part = new GameObject(partName);
        part.transform.SetParent(transform, false);
        
        RectTransform rect = part.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height); // LMJ: Width x Height
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        Image img = part.AddComponent<Image>();
        img.sprite = null; // LMJ: Using default white square
        
        return part;
    }    
    void PositionParts()
    {
        float bodyLength = duration * speed;
        
        // LMJ: Position parts horizontally in a line
        if (direction == "Right")
        {
            // LMJ: Right lane - parts arranged right to left
            startCap.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            body.GetComponent<RectTransform>().anchoredPosition = new Vector2(-bodyLength/2 - capHeight/2, 0);
            endCap.GetComponent<RectTransform>().anchoredPosition = new Vector2(-bodyLength - capHeight, 0);
        }
        else // Left and Up lanes
        {
            // LMJ: Left/Up lanes - parts arranged left to right
            startCap.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            body.GetComponent<RectTransform>().anchoredPosition = new Vector2(bodyLength/2 + capHeight/2, 0);
            endCap.GetComponent<RectTransform>().anchoredPosition = new Vector2(bodyLength + capHeight, 0);
        }
    }
    
    public void UpdateHoldState(bool isHolding, float progress)
    {
        if (isHolding)
        {
            // LMJ: Brighten colors when holding
            startCap.GetComponent<Image>().color = new Color(1f, 1f, 0.3f, 1f);
            body.GetComponent<Image>().color = new Color(1f, 0.95f, 0.2f, 0.9f);
            
            // LMJ: Shrink body horizontally based on progress
            float newWidth = (duration * speed) * (1f - progress);
            body.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, noteWidth);
            
            // LMJ: Move body to keep it aligned with start cap
            if (direction == "Right")
            {
                body.GetComponent<RectTransform>().anchoredPosition = new Vector2(-newWidth/2 + (duration * speed)/2, 0);
            }
            else // Left and Up
            {
                body.GetComponent<RectTransform>().anchoredPosition = new Vector2(newWidth/2 - (duration * speed)/2, 0);
            }
        }
    }
    
    public void HideStartCap()
    {
        if (startCap != null)
            startCap.SetActive(false);
    }
}