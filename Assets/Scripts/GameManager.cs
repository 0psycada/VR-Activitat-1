using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public enum GameState { Instructions, Scanning, Placing, Playing, Won, Lost, Unsupported }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int bloomsToWin = 3;
    public int deathsToLose = 3;
    public int pointsPerBloom = 100;
    public int pointsPerBug = 10;
    public int timeBonusSeconds = 180;

    public ARPlacementController placement;
    public BugSpawner bugSpawner;

    public GameState State { get; private set; } = GameState.Instructions;
    public Garden CurrentGarden { get; private set; }
    public int Blooms { get; private set; }
    public int Deaths { get; private set; }
    public int Score { get; private set; }
    public float ElapsedTime { get; private set; }

    public event Action<GameState> StateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Application.targetFrameRate = 60;
    }

    IEnumerator Start()
    {
        SetState(GameState.Instructions);

#if !UNITY_EDITOR
        if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            yield return ARSession.CheckAvailability();

        if (ARSession.state == ARSessionState.Unsupported)
        {
            SetState(GameState.Unsupported);
        }
#endif
        yield break;
    }

    void Update()
    {
        if (State == GameState.Playing) ElapsedTime += Time.deltaTime;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void CloseInstructions()
    {
        if (State != GameState.Instructions) return;
        StartScanning();
    }

    public void StartScanning()
    {
        ResetRound();
        placement.BeginScanning();
        SetState(GameState.Scanning);
    }

    public void ConfirmPlacement()
    {
        if (State != GameState.Placing || CurrentGarden == null) return;
        bugSpawner.Begin(CurrentGarden);
        SetState(GameState.Playing);
        placement.LockPlacement();
    }

    public void ResetGame() => StartScanning();

    public void OnGardenPlaced(Garden garden)
    {
        CurrentGarden = garden;
        SetState(GameState.Placing);
    }

    public void RegisterBloom()
    {
        if (State != GameState.Playing) return;
        Blooms++;
        Score += pointsPerBloom;
        if (Blooms >= bloomsToWin) EndGame(true);
    }

    public void RegisterDeath()
    {
        if (State != GameState.Playing) return;
        Deaths++;
        if (Deaths >= deathsToLose) EndGame(false);
    }

    public void RegisterBugSquashed()
    {
        if (State != GameState.Playing) return;
        Score += pointsPerBug;
    }

    void EndGame(bool won)
    {
        if (won) Score += Mathf.Max(0, timeBonusSeconds - Mathf.FloorToInt(ElapsedTime));
        bugSpawner.Stop();
        SetState(won ? GameState.Won : GameState.Lost);
    }

    void ResetRound()
    {
        Blooms = 0;
        Deaths = 0;
        Score = 0;
        ElapsedTime = 0f;
        bugSpawner.Stop();
        bugSpawner.ClearAll();
        placement.ResetPlacement();
        CurrentGarden = null;
    }

    void SetState(GameState newState)
    {
        State = newState;
        StateChanged?.Invoke(newState);
    }
}
