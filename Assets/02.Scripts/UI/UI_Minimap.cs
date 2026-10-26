using UnityEngine;

// 미니맵 UI: +/- 버튼을 클릭하면 미니맵을 확대/축소한다.
public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private MinimapCamera _minimapCamera;

    // ZoomInButton(+)의 OnClick에 연결
    public void OnClickZoomIn()
    {
        _minimapCamera.ZoomIn();
    }

    // ZoomOutButton(-)의 OnClick에 연결
    public void OnClickZoomOut()
    {
        _minimapCamera.ZoomOut();
    }
}
