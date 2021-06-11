using System;
using System.Collections;
using UnityEngine;

public class Cube : MonoBehaviour
{
    [SerializeField] [Min(2.0f)] private float _minHealth = 2.0f;
    [SerializeField] [Min(0.0f)] private float _healthVariation = 0.0f;

    [SerializeField] [Min(0.5f)] private float _minDamage = 0.5f;
    [SerializeField] [Min(0.0f)] private float _damageVariation = 0.0f;

    [SerializeField] [Min(0.1f)] private float _attackDistance = 1.5f;
    [SerializeField] [Min(0.1f)] private float _attackCooldown = 0.1f;

    [SerializeField] [Min(3.0f)] private float _speed = 3.0f;

    private float _health;

    private Cube _enemy;
    private Coroutine _attackCoroutine;

    private event Action<Cube> _died;
    public event Action<Cube> Died
    {
        add => _died += value;
        remove => _died -= value;
    }

    private event Action<Cube> _won;
    public event Action<Cube> Won
    {
        add => _won += value;
        remove => _won -= value;
    }

    private void Start()
    {
        _health = _minHealth + UnityEngine.Random.Range(0.0f, _healthVariation);
    }

    public void SetEnemy(Cube enemy)
    {
        if (_attackCoroutine != null)
        {
            StopCoroutine(_attackCoroutine);
        }

        _enemy = enemy;
    }

    public Cube GetEnemy()
    {
        return _enemy;
    }

    public void AttackEnemy()
    {
        if (_enemy == null)
        {
            return;
        }

        if (_attackCoroutine != null)
        {
            return;
        }

        _attackCoroutine = StartCoroutine(Attack());
    }

    private IEnumerator Attack()
    {
        float timer = _attackCooldown;
        transform.LookAt(_enemy.transform);
        while (_enemy != null)
        {
            float distance = Vector3.Distance(transform.position, _enemy.transform.position);
            if (distance <= _attackDistance)
            {
                timer -= Time.deltaTime;
                if (timer <= 0.0f)
                {
                    timer = _attackCooldown;
                    Damage damage = Damage.FromVariation(_minDamage, _damageVariation);
                    _enemy.TakeDamage(damage);
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    _enemy.transform.position,
                    _speed * Time.deltaTime
                );
            }

            yield return null;
        }

        transform.rotation = Quaternion.identity;
        _attackCoroutine = null;
        SetEnemy(null);
        _won?.Invoke(this);
    }

    public void TakeDamage(in Damage damage)
    {
        _health -= damage.Value;
        if (_health <= 0.0f)
        {
            Die();
        }
    }

    private void Die()
    {
        SetEnemy(null);
        Destroy(gameObject);
        _died?.Invoke(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (_enemy == null)
        {
            return;
        }

        Color temp = Gizmos.color;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, _enemy.transform.position);
        Gizmos.color = temp;
    }
#endif
}