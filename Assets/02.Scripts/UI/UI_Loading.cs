using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로딩 씬: 다음 씬을 비동기로 불러오면서 진행률을 슬라이더와 글자로 보여준다.
public class UI_Loading : MonoBehaviour
{
    [SerializeField] private Slider _progressSlider;
    [SerializeField] private TextMeshProUGUI _progressText;

    [Header("로딩")]
    [SerializeField] private string _defaultSceneName = SceneNames.Game;  // 로딩 씬을 바로 실행해서 SceneLoader를 거치지 않았을 때 불러올 씬
    [SerializeField] private float _fillSpeed = 1.2f;   // 표시 진행률이 실제 진행률을 따라가는 속도 (초당)

    private void Start()
    {
        Refresh(0f);
        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        // 비동기 씬 로드: 불러오는 동안에도 이 씬의 Update와 UI가 계속 돌아간다.
        string sceneName = string.IsNullOrEmpty(SceneLoader.NextSceneName) ? _defaultSceneName : SceneLoader.NextSceneName;
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // Build Settings에 없는 씬 이름이면 null이 돌아온다.
        if (operation == null)
        {
            Debug.LogError($"씬을 불러올 수 없습니다: {sceneName} (Build Settings에 등록되어 있는지 확인)");
            yield break;
        }

        operation.allowSceneActivation = false;  // 표시가 100%에 닿을 때까지 씬 전환을 붙잡아 둔다.

        float displayed = 0f;
        while (!operation.isDone)
        {
            // progress는 불러오기가 끝나도 0.9에서 멈추므로 0~1로 다시 맞춘다.
            float actual = Mathf.Clamp01(operation.progress / 0.9f);

            // 실제 진행률로 바로 바꾸면 순식간에 차 버리므로, 일정한 속도로 따라가게 한다.
            displayed = Mathf.MoveTowards(displayed, actual, _fillSpeed * Time.unscaledDeltaTime);
            Refresh(displayed);

            if (displayed >= 1f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void Refresh(float progress)
    {
        _progressSlider.value = progress;
        _progressText.text = $"불러오는 중 {Mathf.RoundToInt(progress * 100f)}%";
    }
}
