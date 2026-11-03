using UnityEngine;

// 게임 오버 팝업: 다시 시작, 게임 종료 버튼을 처리한다.
public class UI_GameOverPopup : MonoBehaviour
{
    public void Show()
    {
        gameObject.SetActive(true);
    }

    // RestartButton(다시 시작)의 OnClick에 연결
    public void OnClickRestart()
    {
        GameManager.Instance.Restart();
    }

    // QuitButton(게임 종료)의 OnClick에 연결
    public void OnClickQuit()
    {
        GameManager.Instance.Quit();
    }
}
