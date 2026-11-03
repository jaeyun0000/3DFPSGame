using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    Pause,
    GameOver
}

public class GameManager : SingletonBehaviour<GameManager>
{
    private GameState _state = GameState.Ready;
    public GameState State => _state;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Animator _playerAnimator;      // 게임 오버 때 군인을 서 있는 동작으로 돌린다.
    [SerializeField] private UI_OptionPopup _optionPopup;   // ESC를 누르면 뜨는 일시정지 팝업
    [SerializeField] private UI_GameOverPopup _gameOverPopup;  // 플레이어가 죽으면 뜨는 게임 오버 팝업

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_state == GameState.Playing)
            {
                Pause();
            }
            else if (_state == GameState.Pause)
            {
                Continue();
            }
        }
    }

    public void Pause()
    {
        _state = GameState.Pause;
        Time.timeScale = 0; // 유니티에서 흐르는 게임 시간;

        _optionPopup.Show();
        CursorManager.Instance.UnlockCursor();  // 팝업 버튼을 누를 수 있게 커서를 푼다.
    }

    public void Continue()
    {
        _state = GameState.Playing;
        Time.timeScale = 1;

        _optionPopup.Hide();
        CursorManager.Instance.LockCursor();
    }

    public void Restart()
    {
        // timeScale은 씬을 다시 불러와도 유지되므로 먼저 되돌린다.
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";
        yield return new WaitForSeconds(1.5f);
        _stateText.text = "시작";
        yield return new WaitForSeconds(0.5f);
        _stateText.gameObject.SetActive(false);

        _state = GameState.Playing;
    }

    public void GameOver()
    {
        _state = GameState.GameOver;

        _gameOverPopup.Show();
        CursorManager.Instance.UnlockCursor();  // 팝업 버튼을 누를 수 있게 커서를 푼다.

        _playerAnimator.SetFloat("MoveMotion", 0f);  // 달리던 중이었어도 서 있는 동작으로 돌린다.
    }
}