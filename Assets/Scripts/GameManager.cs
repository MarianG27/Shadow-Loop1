// GameManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player & Ghost")]
    public GameObject player;
    public GameObject ghostPrefab;
    public Transform spawnPoint;

    [Header("Cycle Settings")]
    public float cycleTime = 10f;
    public float freezeDuration = 2f;
    public float preStartDelay = 1f;
    public int maxGhosts = 5;

    public static int CurrentCycleIndex = 0;
    public static float CurrentRoundStartTime = 0f;

    // GLOBAL list of button tasks
    public static List<ButtonTask> allButtonTasks = new List<ButtonTask>();

    private PlayerRecorder recorder;
    private PlayerController controller;
    private float timer;

    private readonly List<GameObject> ghosts = new List<GameObject>();
    private readonly List<List<PlayerFrame>> allGhostRecordings = new List<List<PlayerFrame>>();

    void Start()
    {
        recorder = player.GetComponent<PlayerRecorder>();
        controller = player.GetComponent<PlayerController>();

        CurrentRoundStartTime = Time.time;
        recorder.StartRecording();
        timer = 0f;
        CurrentCycleIndex = 0;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= cycleTime && recorder.IsRecording())
        {
            recorder.StopRecording();
            StartCoroutine(CycleTransition());
        }
    }

    IEnumerator CycleTransition()
    {
        controller.SetCanMove(false);

        // salvăm înregistrarea curentă
        List<PlayerFrame> frames = recorder.GetRecordedFrames();
        allGhostRecordings.Add(frames);

        // ascundem fantomele existente
        foreach (var g in ghosts) g.SetActive(false);

        yield return new WaitForSeconds(freezeDuration);

        // reset player la spawn
        player.transform.position = spawnPoint.position;

        // reset vizual butoane (doar vizual)
        ButtonTrigger.ResetAllButtons();

        yield return new WaitForSeconds(preStartDelay);

        // dacă sunt prea multe fantome, deletem cea mai veche
        if (allGhostRecordings.Count > maxGhosts)
        {
            Destroy(ghosts[0]);
            ghosts.RemoveAt(0);
            allGhostRecordings.RemoveAt(0);
        }

        // spawn / reactiva fantome si porneste playback (index = 0..count-1)
        for (int i = 0; i < allGhostRecordings.Count; i++)
        {
            if (i < ghosts.Count)
            {
                ghosts[i].transform.position = spawnPoint.position;
                ghosts[i].SetActive(true);
                ghosts[i].GetComponent<PlayerGhost>().ghostIndex = i;
                ghosts[i].GetComponent<PlayerGhost>().StartPlayback(allGhostRecordings[i]);
            }
            else
            {
                GameObject ghost = Instantiate(ghostPrefab, spawnPoint.position, Quaternion.identity);
                ghost.tag = "Ghost";
                var gs = ghost.GetComponent<PlayerGhost>();
                gs.ghostIndex = i;
                gs.StartPlayback(allGhostRecordings[i]);
                ghosts.Add(ghost);
            }
        }

        // Avansăm indexul de rundă (următoarea rundă va fi CurrentCycleIndex)
        CurrentCycleIndex = allGhostRecordings.Count;

        // Setăm timpul de start pentru următoarea rundă și pornim înregistrarea
        CurrentRoundStartTime = Time.time;
        recorder.StartRecording();

        // Programăm activările pentru task-urile care aparțin fantomelor spawnate
        ScheduleAllPendingTasksForSpawnedGhosts();

        // restart timer & reactive player
        timer = 0f;
        controller.SetCanMove(true);
    }

    // Register a button press (player pressed in this round)
    public static void RegisterButtonPress(int buttonID, float relativeTime, int round)
    {
        // "FURT": dezactivăm (isActive = false) task-urile active anterioare pentru acest buton
        for (int i = 0; i < allButtonTasks.Count; i++)
        {
            if (allButtonTasks[i].buttonID == buttonID && allButtonTasks[i].isActive)
            {
                allButtonTasks[i].isActive = false;
            }
        }

        // adăugăm noul task, activ pentru fantoma round (va rămâne activ până e furat)
        allButtonTasks.Add(new ButtonTask(buttonID, relativeTime, round, true));
        Debug.Log($"[GameManager] Registered ButtonTask id={buttonID}, relTime={relativeTime:F3}, round={round}");
    }

    // Nu schimbăm semantica: isActive înseamnă că taskul poate fi executat de fantoma sa (owner)
    // Dar nu îl dezactivăm după execuție — îl păstrăm pentru execuții viitoare (aceeași fantomă),
    // doar updatezăm lastExecutedCycle pentru a nu activa de două ori într-o singură rundă.

    public static List<ButtonTask> GetTasksForGhost(int ghostIndex)
    {
        var res = new List<ButtonTask>();
        foreach (var t in allButtonTasks)
            if (t.round == ghostIndex) res.Add(t);
        return res;
    }

    private void ScheduleAllPendingTasksForSpawnedGhosts()
    {
        // pentru fiecare ghostIndex existent programăm task-urile sale (dacă isActive)
        for (int gi = 0; gi < allGhostRecordings.Count; gi++)
        {
            List<ButtonTask> tasks = GetTasksForGhost(gi);
            foreach (var task in tasks)
            {
                if (task.isActive)
                {
                    StartCoroutine(ActivateButtonAtRelativeTime(task, Time.time));
                }
            }
        }
    }

    private IEnumerator ActivateButtonAtRelativeTime(ButtonTask task, float playbackStartTime)
    {
        float targetTime = playbackStartTime + task.time;
        float wait = targetTime - Time.time;
        if (wait > 0f) yield return new WaitForSeconds(wait);
        else yield return null;

        // daca task a fost furat intre timp -> nu activam
        if (!task.isActive) yield break;

        // daca task a fost deja executat in aceasta rundă (lastExecutedCycle == CurrentCycleIndex), skip
        if (task.lastExecutedCycle == CurrentCycleIndex) yield break;

        if (ButtonTrigger.TryGetButton(task.buttonID, out var btn))
        {
            btn.ActivateButton(task.round);
            // in loc sa dezactivam task-ul, marcam doar ca a fost executat in aceasta rundă
            task.lastExecutedCycle = CurrentCycleIndex;
            Debug.Log($"[GameManager] Task executed (scheduled): id={task.buttonID}, ownerRound={task.round}, execCycle={CurrentCycleIndex}");
        }
        else
        {
            Debug.LogWarning($"[GameManager] Could not find ButtonTrigger id {task.buttonID} when executing scheduled task.");
        }
    }

    // OPTIONAL: prune/cleanup (dacă vrei): elimina task-urile foarte vechi sau inactive
    public void PruneOldTasksIfNeeded()
    {
        // exemplu: sterge task-urile inactive mai vechi de maxGhosts runde
        int minRoundAllowed = Mathf.Max(0, CurrentCycleIndex - maxGhosts - 2);
        allButtonTasks.RemoveAll(t => !t.isActive && t.round < minRoundAllowed);
    }
}
