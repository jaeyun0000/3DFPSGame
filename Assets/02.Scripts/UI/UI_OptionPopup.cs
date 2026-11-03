using UnityEngine;

// 옵션(일시정지) 팝업: 계속하기, 다시 시작, 게임 종료 버튼을 처리한다.
public class UI_OptionPopup : MonoBehaviour
{
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ContinueButton(계속하기)의 OnClick에 연결
    public void OnClickContinue()
    {
        GameManager.Instance.Continue();
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
