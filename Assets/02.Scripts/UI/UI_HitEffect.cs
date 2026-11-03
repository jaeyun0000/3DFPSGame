using System.Collections;
using UnityEngine.UI;
using UnityEngine;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.1f;
    [SerializeField] private float _fadeTime = 0.3f;

    private Image _image;
    private WaitForSeconds _flashWait;   // 맞을 때마다 new로 만들지 않도록 재사용
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false;
        _flashWait = new WaitForSeconds(_flashTime);
    }

    private void Start()
    {
        Player player = GameObject.FindAnyObjectByType<Player>();

        if (player != null)
        {
            player.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Fade(0.6f);
    }

    public void Flash(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;

        StartCoroutine(Flash_Coroutine());
    }

    private IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;

        yield return _flashWait;

        _image.enabled = false;
    }

    public void Fade(float alpha)
    {
        // 1. 0.4로 보이게한다.
        // 2. 0.4에서 서서히 0으로 가서 사라지게 한다.
        SetAlpha(alpha);

        // 연속으로 맞으면 이전 페이드를 멈춘다. (여러 코루틴이 동시에 알파값을 덮어쓰지 않도록)
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(Fade_Coroutine());
    }

    private IEnumerator Fade_Coroutine()
    {
        _image.enabled = true;

        float elpasedTime = 0f;

        float startAlpha = _image.color.a;

        while (elpasedTime < _fadeTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0f, elpasedTime / _fadeTime);

            SetAlpha(alpha);

            elpasedTime += Time.deltaTime;
            yield return null;  // 한 프레임씩 쉰다
        }

        _fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}
