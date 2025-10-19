using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float frameRate = 0.1f;
    public bool loop = true;

    SpriteRenderer sr;
    int index; float t; bool playing = true;

    public bool IsPlaying => playing;

    void Awake() => sr = GetComponent<SpriteRenderer>();

    void Update()
    {
        if (!playing || frames == null || frames.Length == 0) return;

        t += Time.deltaTime;
        if (t >= frameRate)
        {
            t -= frameRate;
            index++;
            if (index >= frames.Length)
            {
                if (loop) index = 0;
                else { index = frames.Length - 1; playing = false; }
            }
            sr.sprite = frames[index];
        }
    }

    public void Play(Sprite[] newFrames, float newRate, bool looping = true)
    {
        frames = newFrames; frameRate = newRate; loop = looping;
        index = 0; t = 0f; playing = true;
        if (frames != null && frames.Length > 0) sr.sprite = frames[0];
    }

    public void StopOnFirst()
    {
        if (frames == null || frames.Length == 0) return;
        index = 0; t = 0; playing = false; sr.sprite = frames[0];
    }
}
