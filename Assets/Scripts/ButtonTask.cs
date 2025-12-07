// ButtonTask.cs
public class ButtonTask
{
    public int buttonID;
    public float time; // timp relativ în rundă (secunde)
    public int round;  // runda în care a fost creat (owner ghost index)
    public bool isActive; // true = taskul e permis (nu a fost furat)
    public int lastExecutedCycle = -1; // ultima rundă în care a fost executat

    public ButtonTask(int id, float t, int r, bool active)
    {
        buttonID = id;
        time = t;
        round = r;
        isActive = active;
        lastExecutedCycle = -1;
    }
}
