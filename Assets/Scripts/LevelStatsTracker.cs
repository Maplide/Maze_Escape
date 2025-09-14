using UnityEngine;

public class LevelStatsTracker : MonoBehaviour
{
    public static LevelStatsTracker Instance;
    float startTime;
    int hits;

    void Awake(){
        Instance = this;
        startTime = Time.time;
        hits = 0;
    }

    public void RegisterHit(){ hits++; }
    public float ElapsedTime() => Time.time - startTime;
    public int Hits() => hits;

    public string Rank(){
        float t = ElapsedTime();
        int h = hits;
        if (t < 45f && h == 0) return "S";
        if (t < 75f && h <= 2) return "A";
        if (t < 120f && h <= 4) return "B";
        return "C";
    }
}
