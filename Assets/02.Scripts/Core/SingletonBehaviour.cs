using UnityEngine;

// T를 MonoBehaviour로 제한해야 _instance != null이 Unity의 null 비교를 써서,
// 씬을 다시 불러온 뒤 파괴된 이전 인스턴스를 null로 판단한다.
public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance = null;
    public static T Instance => _instance;

    protected virtual void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
    }
}
