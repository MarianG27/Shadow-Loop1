using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGhost : MonoBehaviour
{
    public int ghostIndex;
    public float defaultPlaybackInterval = 0.02f; // fallback

    private Coroutine playbackCoroutine;

    // Start playback with frames (frames contain relative times)
    public void StartPlayback(List<PlayerFrame> frames)
    {
        StopPlayback();
        playbackCoroutine = StartCoroutine(PlaybackInterpolated(frames));
    }

    public void StopPlayback()
    {
        if (playbackCoroutine != null)
        {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }
    }

    private IEnumerator PlaybackInterpolated(List<PlayerFrame> frames)
    {
        if (frames == null || frames.Count == 0)
            yield break;

        // folosește timpul absolut de start al playbacks
        float playbackStart = Time.time;

        int idx = 0;
        // poziția inițială
        transform.position = frames[0].position;

        while (true)
        {
            float elapsed = Time.time - playbackStart;

            // dacă s-a terminat lista, poziționează la ultimul frame și termină
            if (elapsed >= frames[frames.Count - 1].time)
            {
                transform.position = frames[frames.Count - 1].position;
                break;
            }

            // găsește segmentul [idx, idx+1] în care se află elapsed
            while (idx < frames.Count - 2 && frames[idx + 1].time <= elapsed)
                idx++;

            PlayerFrame a = frames[idx];
            PlayerFrame b = frames[idx + 1];

            float segmentDuration = b.time - a.time;
            float t = (segmentDuration > 0f) ? Mathf.Clamp01((elapsed - a.time) / segmentDuration) : 0f;

            // interpolează liniar poziția
            transform.position = Vector3.Lerp(a.position, b.position, t);

            // small wait pentru a nu bloca CPU; folosim un wait mic
            yield return null;
        }

        playbackCoroutine = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
    }
}
