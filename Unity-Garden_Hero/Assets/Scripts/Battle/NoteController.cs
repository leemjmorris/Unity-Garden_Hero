using UnityEngine;

public class NoteController : MonoBehaviour
{
    private Note noteData;
    [SerializeField] private float moveSpeed = 5f;
    
    public void SetNoteData(Note data)
    {
        noteData = data;
    }
    
    void Update()
    {
        // LMJ: Move forward based on spawner's rotation
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
    
    public Note GetNoteData()
    {
        return noteData;
    }
}