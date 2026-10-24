using System.Collections.Generic;
using UnityEngine;

public class BugSpawner : MonoBehaviour
{
    public Bug bugPrefab;
    public float firstSpawnDelay = 8f;
    public float startInterval = 7f;
    public float minInterval = 2.5f;
    public float intervalDecrease = 0.3f;
    public int maxBugsAlive = 6;
    public float spawnRadius = 0.3f;

    readonly List<Bug> bugs = new List<Bug>();
    Garden garden;
    bool running;
    float timer;
    float interval;

    public void Begin(Garden target)
    {
        garden = target;
        running = true;
        timer = firstSpawnDelay;
        interval = startInterval;
    }

    public void Stop()
    {
        running = false;
    }

    public void ClearAll()
    {
        foreach (var b in bugs)
            if (b != null) Destroy(b.gameObject);
        bugs.Clear();
    }

    void Update()
    {
        if (!running || garden == null) return;
        var gm = GameManager.Instance;
        if (gm == null || gm.State != GameState.Playing) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        bugs.RemoveAll(b => b == null);
        if (bugs.Count < maxBugsAlive) Spawn();

        interval = Mathf.Max(minInterval, interval - intervalDecrease);
        timer = interval;
    }

    void Spawn()
    {
        Plant target = garden.GetRandomTargetPlant();
        if (target == null) return;

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 localPos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spawnRadius;
        localPos.y = garden.SurfaceHeightAt(localPos);

        Bug bug = bugPrefab
            ? Instantiate(bugPrefab, garden.transform)
            : new GameObject("Bug").AddComponent<Bug>();

        bug.transform.SetParent(garden.transform, false);
        bug.transform.localPosition = localPos;
        bug.transform.localRotation = Quaternion.identity;
        bug.Init(garden, target);
        bugs.Add(bug);
    }
}
