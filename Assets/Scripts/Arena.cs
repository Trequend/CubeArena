using System;
using UnityEngine;
using UnityEngine.Profiling;

public class Arena : MonoBehaviour
{
    private readonly ArenaBHV _hierarchy = new ArenaBHV();

    private int _cubesCountInRound;

    public int Round { get; private set; }

    public ArenaStatus Status { get; private set; }

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
        Transform transform = GetComponent<Transform>();
        int childCount = transform.childCount;
        if (!IsPowerOfTwo(childCount))
        {
            Debug.LogError("Cube count must be power of 2");
            Status = ArenaStatus.NotInitialized;
            return;
        }

        Profiler.BeginSample("Adding cubes in hierarchy");
        for (int i = 0; i < childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent(out Cube cube))
            {
                _hierarchy.Add(cube);
                Attach(cube);
            }
        }
        Profiler.EndSample();

        PrepareNextRound();
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

    private bool IsPowerOfTwo(int x)
    {
        return x > 0 && (x & (x - 1)) == 0;
    }

    private void RemoveCubeFromRound()
    {
        _cubesCountInRound--;
        if (_cubesCountInRound == 0)
        {
            EndRound();
        }
    }

    private void EndRound()
    {
        Status = ArenaStatus.RoundEnded;
        _roundEnded?.Invoke();
        TryEndBattle();
        if (Status != ArenaStatus.BattleEnded)
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

        _hierarchy.Foreach(cube =>
        {
            cube.Won -= ReturnInHierarchy;
            cube.Died -= Cleanup;
        });
        _hierarchy.Clear();
        Status = ArenaStatus.BattleEnded;
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
        Status = ArenaStatus.NextRoundPrepared;
        _nextRoundPrepared?.Invoke();
    }

    public void StartNextRound()
    {
        if (Status != ArenaStatus.NextRoundPrepared)
        {
            return;
        }

        Round++;
        _hierarchy.Foreach(cube => cube.AttackEnemy());
        _hierarchy.Clear();
        Status = ArenaStatus.RoundStarted;
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

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        _hierarchy.DebugDraw();
    }
#endif
}
