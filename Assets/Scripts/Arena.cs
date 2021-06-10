using System;
using UnityEngine;

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

    private event Action<Cube> _battleEnded;
    public event Action<Cube> BattleEnded
    {
        add => _battleEnded += value;
        remove => _battleEnded -= value;
    }

    private void Start()
    {
        Transform transform = GetComponent<Transform>();
        int childCount = transform.childCount % 2 == 0 ? transform.childCount : transform.childCount - 1;
        for (int i = 0; i < childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent(out Cube cube))
            {
                _hierarchy.Add(cube);
                cube.Won += ReturnInHierarchy;
                cube.Died += Cleanup;
            }
        }

        if (childCount == 0)
        {
            return;
        }

        PrepareNextRound();
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

        Cube winner = null;
        _hierarchy.Foreach(cube =>
        {
            cube.Won -= ReturnInHierarchy;
            cube.Died -= Cleanup;
            winner = cube;
        });
        _hierarchy.Clear();
        Status = ArenaStatus.BattleEnded;
        _battleEnded?.Invoke(winner);
    }

    private void PrepareNextRound()
    {
        _hierarchy.ForeachPair((firstCube, secondCube) =>
        {
            firstCube.SetEnemy(secondCube);
            secondCube.SetEnemy(firstCube);
        });

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
        cube.Died -= Cleanup;
        cube.Won -= ReturnInHierarchy;
        RemoveCubeFromRound();
    }
}
