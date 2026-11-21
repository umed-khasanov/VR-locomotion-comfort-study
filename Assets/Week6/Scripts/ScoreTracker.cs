using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Lab Part 4: счёт = сколько маркеров маршрута игрок уже посетил.
// Один счётчик обновляет сразу оба экрана:
//  - недиегетический: HUD, прикреплённый к камере;
//  - диегетический: табло (scoreboard) в сцене.
public class ScoreTracker : MonoBehaviour
{
    public List<TMP_Text> displays = new List<TMP_Text>();
    public List<Transform> targets = new List<Transform>();
    public float radius = 1f;     // м по горизонтали до центра маркера

    Transform head;
    readonly HashSet<Transform> reached = new HashSet<Transform>();

    void Start()
    {
        FindHead();
        Refresh();
    }

    void FindHead()
    {
        Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (cam != null) head = cam.transform;
    }

    void Update()
    {
        if (head == null) { FindHead(); return; }
        for (int i = 0; i < targets.Count; i++)
        {
            Transform t = targets[i];
            if (t == null || reached.Contains(t)) continue;
            Vector3 d = t.position - head.position;
            d.y = 0f;
            if (d.magnitude <= radius)
            {
                reached.Add(t);
                Refresh();
                Debug.Log("[Lab] Score " + reached.Count + "/" + targets.Count + " (" + t.name + ")");
            }
        }
    }

    public void ResetScore()
    {
        reached.Clear();
        Refresh();
    }

    void Refresh()
    {
        string s = "Score: " + reached.Count + " / " + targets.Count;
        for (int i = 0; i < displays.Count; i++)
            if (displays[i] != null) displays[i].text = s;
    }
}
