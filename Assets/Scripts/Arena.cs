using System;
using UnityEngine;
using UnityEngine.Profiling;

public class Arena : MonoBehaviour
{
    private readonly ArenaHierarchy _hierarchy = new ArenaHierarchy();

    private int _cubesCountInRound;

    public int Round { get; private set; }

    public ArenaState State { get; private set; }

    private event Action _roundStarted;
    public event Action RoundStarted
    {
        add => _roundStarted += value;
        remove => _roundStarted -= value;
    }

    private event Action _roundEnded;
    public event Action RoundEnded
    {
        add => _roundEnded += value;
        remove => _roundEnded -= value;
    }

    private event Action _nextRoundPrepared;
    public event Action NextRoundPrepared
    {
        add => _nextRoundPrepared += value;
        remove => _nextRoundPrepared -= value;
    }

    private event Action _battleEnded;
    public event Action BattleEnded
    {
        add => _battleEnded += value;
        remove => _battleEnded -= value;
    }

    private void Start()
    {
        if (!IsPowerOfTwo(transform.childCount))
        {
            Debug.LogError("Cube count must be power of 2");
            State = ArenaState.NotInitialized;
            return;
        }

        CollectCubes();
        PrepareNextRound();
    }

    private bool IsPowerOfTwo(int x)
    {
        return x > 0 && (x & (x - 1)) == 0;
    }

    private void CollectCubes()
    {
        Profiler.BeginSample("Adding cubes in hierarchy");
        Transform transform = GetComponent<Transform>();
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent(out Cube cube))
            {
                _hierarchy.Add(cube);
                Attach(cube);
            }
        }
        Profiler.EndSample();
    }

    private void Attach(Cube cube)
    {
        cube.Won += ReturnInHierarchy;
        cube.Died += Cleanup;
    }

    private void Detach(Cube cube)
    {
        cube.Won -= ReturnInHierarchy;
        cube.Died -= Cleanup;
    }

    private void EndRound()
    {
        State = ArenaState.RoundEnded;
        _roundEnded?.Invoke();
        TryEndBattle();
        if (State != ArenaState.BattleEnded)
        {
            PrepareNextRound();
        }
    }

    private void TryEndBattle()
    {
        if (_hierarchy.Count != 1)
        {
            return;
        }

        _hierarchy.Foreach(Detach);
        _hierarchy.Clear();
        State = ArenaState.BattleEnded;
        _battleEnded?.Invoke();
    }

    private void PrepareNextRound()
    {
        Profiler.BeginSample("Linking cubes");
        _hierarchy.ForeachPair((firstCube, secondCube) =>
        {
            firstCube.SetEnemy(secondCube);
            secondCube.SetEnemy(firstCube);
        });
        Profiler.EndSample();

        _cubesCountInRound = _hierarchy.Count;
        State = ArenaState.NextRoundPrepared;
        _nextRoundPrepared?.Invoke();
    }

    public void StartNextRound()
    {
        if (State != ArenaState.NextRoundPrepared)
        {
            return;
        }

        Round++;
        _hierarchy.Foreach(cube => cube.AttackEnemy());
        _hierarchy.Clear();
        State = ArenaState.RoundStarted;
        _roundStarted?.Invoke();
    }

    private void ReturnInHierarchy(Cube cube)
    {
        _hierarchy.Add(cube);
        RemoveCubeFromRound();
    }

    private void Cleanup(Cube cube)
    {
        Detach(cube);
        RemoveCubeFromRound();
    }

    private void RemoveCubeFromRound()
    {
        _cubesCountInRound--;
        if (_cubesCountInRound == 0)
        {
            EndRound();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        _hierarchy.DebugDraw();
    }
#endif
}
