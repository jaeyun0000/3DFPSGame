using System;
using UnityEngine;

// 게임 전체의 좀비 수를 관리한다.
// 모든 스포너는 스폰하기 전에 최대 좀비 수에 도달했는지 이 매니저에게 확인한다.
public class ZombieSpawnManager : MonoBehaviour
{
    [SerializeField] private int _maxZombieCount = 10;

    private int _aliveCount;

    public int AliveCount => _aliveCount;
    public int MaxZombieCount => _maxZombieCount;
    public bool CanSpawn => _aliveCount < _maxZombieCount;

    // 좀비 수(현재, 최대)가 바뀔 때 알린다. (옵저버 패턴)
    public event Action<int, int> OnZombieCountChanged;

    public void AddZombie()
    {
        _aliveCount++;
        OnZombieCountChanged?.Invoke(_aliveCount, _maxZombieCount);
    }

    public void RemoveZombie()
    {
        _aliveCount--;
        OnZombieCountChanged?.Invoke(_aliveCount, _maxZombieCount);
    }
}
