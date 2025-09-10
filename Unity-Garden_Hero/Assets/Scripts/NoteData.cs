using System;
using UnityEngine;

[Serializable]
public class NoteData
{
    public enum NoteType
    {
        Normal,
        Long
    }
    
    public enum NoteDirection
    {
        TopToBottom,    // ↓
        LeftToRight,    // →
        RightToLeft     // ←
    }
    
    public float timing;        // 노트가 나타날 시간 (초)
    public float duration;      // 롱노트 지속시간 (일반노트는 0)
    public NoteType type;
    public NoteDirection direction;
    public Vector3 position;    // 3D 위치
    
    public NoteData(float timing, NoteDirection direction, NoteType type = NoteType.Normal, float duration = 0f)
    {
        this.timing = timing;
        this.direction = direction;
        this.type = type;
        this.duration = duration;
        this.position = Vector3.zero;
    }
}