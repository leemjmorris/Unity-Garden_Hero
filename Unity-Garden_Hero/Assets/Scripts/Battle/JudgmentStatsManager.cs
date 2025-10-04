using UnityEngine;

/// <summary>
/// 스테이지별 판정 통계를 추적하는 매니저
/// </summary>
public class JudgmentStatsManager : MonoBehaviour
{
    public static JudgmentStatsManager Instance { get; private set; }

    // 현재 스테이지 통계
    private int perfectCount = 0;
    private int goodCount = 0;
    private int missCount = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 판정 결과 기록
    /// </summary>
    public void RecordJudgment(JudgmentResult result)
    {
        switch (result)
        {
            case JudgmentResult.Perfect:
                perfectCount++;
                break;
            case JudgmentResult.Good:
                goodCount++;
                break;
            case JudgmentResult.Miss:
                missCount++;
                break;
        }
    }

    /// <summary>
    /// 현재 스테이지 통계 초기화 (새 스테이지 시작 시)
    /// </summary>
    public void ResetStats()
    {
        perfectCount = 0;
        goodCount = 0;
        missCount = 0;
    }

    // Getter methods
    public int GetPerfectCount() => perfectCount;
    public int GetGoodCount() => goodCount;
    public int GetMissCount() => missCount;
    public int GetTotalNotes() => perfectCount + goodCount + missCount;

    /// <summary>
    /// 통계 정보 로그 출력 (디버그용)
    /// </summary>
    public void LogStats()
    {
        Debug.Log($"[JudgmentStats] Perfect: {perfectCount}, Good: {goodCount}, Miss: {missCount}, Total: {GetTotalNotes()}");
    }
}
