using TMPro;
using UnityEngine;

// 현재 좀비 수 / 최대 좀비 수를 보여준다.
public class UI_ZombieCount : MonoBehaviour
{
    [SerializeField] private ZombieSpawnManager _spawnManager;
    [SerializeField] private TextMeshProUGUI _countText;

    private void Start()
    {
        // 좀비 수가 바뀔 때만 텍스트를 갱신한다. (옵저버 패턴)
        _spawnManager.OnZombieCountChanged += RefreshCount;
        RefreshCount(_spawnManager.AliveCount, _spawnManager.MaxZombieCount);
    }

    private void OnDestroy()
    {
        if (_spawnManager != null)
        {
            _spawnManager.OnZombieCountChanged -= RefreshCount;
        }
    }

    private void RefreshCount(int aliveCount, int maxCount)
    {
        _countText.text = $"좀비 {aliveCount} / {maxCount}";
    }
}
