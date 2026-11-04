using UnityEngine.SceneManagement;

public static class SceneLoader
{
    // 로딩 씬(UI_Loading)이 이어서 불러올 씬 이름
    public static string NextSceneName { get; private set; }

    public static void Load(string sceneName)
    {
        NextSceneName = sceneName;
        SceneManager.LoadScene(SceneNames.Loading);
    }
}
