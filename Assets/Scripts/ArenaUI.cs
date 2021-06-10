using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ArenaUI : MonoBehaviour
{
    [SerializeField] private Arena _arena;
    [SerializeField] private TMPro.TextMeshProUGUI _label;
    [SerializeField] private TMPro.TextMeshProUGUI _hint;

    private void OnEnable()
    {
        HideLabel();
        switch (_arena.Status)
        {
            case ArenaStatus.NextRoundPrepared:
                ShowStartNextRoundLabel();
                break;
            case ArenaStatus.BattleEnded:
                ShowRestartLabel();
                break;
        }

        _arena.NextRoundPrepared += ShowStartNextRoundLabel;
        _arena.BattleEnded += ShowRestartLabel;
    }

    private void OnDisable()
    {
        _arena.NextRoundPrepared -= ShowStartNextRoundLabel;
        _arena.BattleEnded -= ShowRestartLabel;
    }

    private void ShowStartNextRoundLabel()
    {
        if (_arena.Round == 0)
        {
            ShowActionLabel("����� �����", "������� ������, ����� ������", () =>
            {
                _arena.StartNextRound();
            });
        }
        else
        {
            ShowActionLabel($"����� {_arena.Round} �������", "������� ������, ����� ����������", () =>
            {
                _arena.StartNextRound();
            });
        }
    }

    private void ShowRestartLabel()
    {
        ShowActionLabel("����� ��������", "������� ������, ����� �������������", () =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        });
    }

    private void ShowActionLabel(string text, string hint, Action action)
    {
        _label.gameObject.SetActive(true);
        _hint.gameObject.SetActive(true);
        _label.text = text;
        _hint.text = hint;
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
        _label.gameObject.SetActive(false);
        _hint.gameObject.SetActive(false);
    }
}
