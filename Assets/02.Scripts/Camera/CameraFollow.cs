using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _target;

    private void Awake()
    {
        if (_target == null)
        {
            Debug.LogWarning("[CameraFollow] Camera Target null");
        }
    }

    private void LateUpdate()
    {
        transform.position = _target.position;
    }
}
