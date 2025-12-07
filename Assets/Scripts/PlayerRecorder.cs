using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerFrame
{
    public Vector3 position;
    public float time; // timp relativ în rundă

    public PlayerFrame(Vector3 pos, float t)
    {
        position = pos;
        time = t;
    }
}

public class PlayerRecorder : MonoBehaviour
{
    public List<PlayerFrame> recordedFrames = new List<PlayerFrame>();
    private float timer = 0f;
    private bool isRecording = false;
    public float recordInterval = 0.02f; // ajustează în Inspector

    public void StartRecording()
    {
        recordedFrames.Clear();
        timer = 0f;
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
    }

    public bool IsRecording() => isRecording;

    public List<PlayerFrame> GetRecordedFrames() => new List<PlayerFrame>(recordedFrames);

    void Update()
    {
        if (!isRecording) return;

        timer += Time.deltaTime;
        if (timer >= recordInterval)
        {
            float relTime = Time.time - GameManager.CurrentRoundStartTime;
            recordedFrames.Add(new PlayerFrame(transform.position, relTime));
            timer = 0f;
        }
    }
}
