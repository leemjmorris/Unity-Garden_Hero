using System;

[Serializable]
public class SongData
{
    public string songName;
    public string audioFileName;
    public float bpm;
    public NoteData[] notes;
    
    public SongData()
    {
        notes = new NoteData[0];
    }
}