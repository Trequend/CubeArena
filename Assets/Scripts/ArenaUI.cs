using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ArenaUI : MonoBehaviour
{
    [SerializeField] private Arena _arena;
    [SerializeField] private GameObject _labelPanel;
    [SerializeField] private TMPro.TextMeshProUGUI _label;
    [SerializeField] private TMPro.TextMeshProUGUI _hint;

    private void OnEnable()
    {
        HideLabel();
        switch (_arena.State)
        {
            case ArenaState.NextRoundPrepared:
                ShowStartNextRoundLabel();
                break;
            case ArenaState.BattleEnded:
                ShowRestartLabel();
                break;
        }

        _arena.NextRoundPrepared += ShowStartNextRoundLabel;
        _arena.BattleEnded += ShowRestartLabel;
    }

    private void OnDisable()
    {
        HideLabel();
        _arena.NextRoundPrepared -= ShowStartNextRoundLabel;
        _arena.BattleEnded -= ShowRestartLabel;
    }

    private void ShowStartNextRoundLabel()
    {
        if (_arena.Round == 0)
        {
            ShowActionLabel("Cube arena", "Press space to start", () =>
            {
                _arena.StartNextRound();
            });
        }
        else
        {
            ShowActionLabel($"Round {_arena.Round} is over", "Press space to continue", () =>
            {
                _arena.StartNextRound();
            });
        }
    }

    private void ShowRestartLabel()
    {
        ShowActionLabel("The battle is over", "Press space to restart", () =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

    private void ShowActionLabel(string text, string hint, Action action)
    {
        _labelPanel.SetActive(true);
        _label.text = text;
        _hint.text = hint;
        StopAllCoroutines();
        StartCoroutine(WaitSpaceKey(action));
    }

    private IEnumerator WaitSpaceKey(Action action)
    {
        while (!Input.GetKey(KeyCode.Space))
        {
            yield return null;
        }

        HideLabel();
        action();
    }

    private void HideLabel()
    {
        _labelPanel.SetActive(false);
    }
}
