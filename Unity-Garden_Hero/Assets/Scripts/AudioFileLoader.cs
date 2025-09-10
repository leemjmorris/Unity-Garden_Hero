using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;

public class AudioFileLoader : MonoBehaviour
{
    public AudioManager audioManager;
    
    public void LoadAudioFile(string filePath)
    {
        StartCoroutine(LoadAudioCoroutine(filePath));
    }
    
    IEnumerator LoadAudioCoroutine(string filePath)
    {
        string url = "file://" + filePath;
        
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, GetAudioType(filePath)))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                audioManager.LoadAudioClip(clip);
                Debug.Log("오디오 파일 로드 성공: " + Path.GetFileName(filePath));
            }
            else
            {
                Debug.LogError("오디오 파일 로드 실패: " + www.error);
            }
        }
    }
    
    AudioType GetAudioType(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLower();
        
        switch (extension)
        {
            case ".wav": return AudioType.WAV;
            case ".mp3": return AudioType.MPEG;
            case ".ogg": return AudioType.OGGVORBIS;
            case ".aiff": return AudioType.AIFF;
            default: return AudioType.UNKNOWN;
        }
    }
}