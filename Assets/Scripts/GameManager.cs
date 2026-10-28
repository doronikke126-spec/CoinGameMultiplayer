using System.Collections.Generic;
using Fusion;
using UnityEngine;

public enum GameState { Waiting, Playing, Finished }

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public NetworkObject CoinPrefab;
    public float MatchSeconds = 60f;
    public float ResultSeconds = 6f;
    public int MaxCoins = 8;
    public float ArenaLimit = 13f;

    [Networked] public GameState State { get; set; }
    [Networked] private TickTimer Timer { get; set; }

    public float TimeLeft => Timer.RemainingTime(Runner) ?? 0f;

    public override void Spawned()
    {
        Instance = this;
        if (Object.HasStateAuthority)
            State = GameState.Waiting;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this) Instance = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        switch (State)
        {
            case GameState.Waiting:
                if (Player.All.Count >= 2) StartMatch();
                break;

            case GameState.Playing:
                if (Player.All.Count < 2)
                {
                    ClearCoins();
                    State = GameState.Waiting;
                    break;
                }
                SpawnCoins();
                if (Timer.Expired(Runner))
                {
                    ClearCoins();
                    State = GameState.Finished;
                    Timer = TickTimer.CreateFromSeconds(Runner, ResultSeconds);
                }
                break;

            case GameState.Finished:
                if (Timer.Expired(Runner))
                {
                    if (Player.All.Count >= 2) StartMatch();
                    else State = GameState.Waiting;
                }
                break;
        }
    }

    void StartMatch()
    {
        foreach (var p in Player.All)
        {
            p.Score = 0;
            p.StunTimer = default;
        }
        State = GameState.Playing;
        Timer = TickTimer.CreateFromSeconds(Runner, MatchSeconds);
    }

    void SpawnCoins()
    {
        int toSpawn = MaxCoins - Coin.All.Count;
        for (int i = 0; i < toSpawn; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-ArenaLimit, ArenaLimit), 0f,
                Random.Range(-ArenaLimit, ArenaLimit));
            Runner.Spawn(CoinPrefab, pos, Quaternion.identity);
        }
    }

    void ClearCoins()
    {
        foreach (var c in new List<Coin>(Coin.All))
        {
            if (c != null && !c.Taken)
            {
                c.Taken = true;
                Runner.Despawn(c.Object);
            }
        }
    }
}
