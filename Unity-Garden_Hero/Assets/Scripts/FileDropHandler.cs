using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System;
using Unity.VisualScripting;
#pragma warning disable

public class FileDropHandler : MonoBehaviour
{
    [Header("References")]
    public AudioFileLoader audioLoader;
    public Button loadFileButton;
    public TextMeshProUGUI fileStatusText;

    void Start()
    {
        if (loadFileButton != null)
        {
            loadFileButton.onClick.AddListener(OpenFileDialog);
        }

        UpdateFileStatus("음악 파일을 로드해주세요");
    }

    void Update()
    {
        // 간단한 파일 드롭 체크 (실제 구현은 플러그인 필요)
        if (Input.inputString.Length > 0)
        {
            foreach (char c in Input.inputString)
            {
                if (c == '\n' || c == '\r') // Enter 키 감지 시 파일 선택 창
                {
                    OpenFileDialog();
                }
            }
        }
    }

    public void OpenFileDialog()
    {
        // 실제 구현에서는 파일 선택 창을 열어야 함
        // 테스트용으로 StreamingAssets 폴더의 파일 검색
        string streamingAssetsPath = Application.streamingAssetsPath;

        if (Directory.Exists(streamingAssetsPath))
        {
            string[] audioFiles = Directory.GetFiles(streamingAssetsPath, "*.*")
                .Where(file => IsAudioFile(file)).ToArray();

            if (audioFiles.Length > 0)
            {
                LoadAudioFile(audioFiles[0]); // 첫 번째 오디오 파일 로드
            }
            else
            {
                UpdateFileStatus("StreamingAssets 폴더에 음악 파일이 없습니다");
            }
        }
        else
        {
            UpdateFileStatus("StreamingAssets 폴더를 만들고 음악 파일을 넣어주세요");
        }
    }

    public void LoadAudioFile(string filePath)
    {
        if (IsAudioFile(filePath) && audioLoader != null)
        {
            audioLoader.LoadAudioFile(filePath);
            UpdateFileStatus($"로드됨: {Path.GetFileName(filePath)}");
        }
        else
        {
            UpdateFileStatus("지원하지 않는 파일 형식입니다");
        }
    }

    bool IsAudioFile(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();
        return ext == ".wav" || ext == ".mp3" || ext == ".ogg" || ext == ".aiff";
    }

    void UpdateFileStatus(string message)
    {
        if (fileStatusText != null)
        {
            fileStatusText.text = message;
        }
        Debug.Log(message);
    }
    // public int BinarySearch(int targetValue, int[] originArray)
    // {
    //     int left = 0;
    //     int right = originArray.Length - 1;

    //     while (left <= right)
    //     {
    //         int mid = left + (right - left) / 2;

    //         if (originArray[mid] == targetValue)
    //         {
    //             return originArray[mid];
    //         }
    //         else if (originArray[mid] < targetValue)
    //         {
    //             left = mid + 1; //오른쪽 탐색
    //         }
    //         else
    //         {
    //             right = mid - 1; //왼쪽 탐색
    //         }
    //     }
    //     return -1;
    // }
}

