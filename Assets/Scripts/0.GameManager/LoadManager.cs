using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadManager : MonoBehaviour
{
    public void Tutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }
    public void GameStart()
    {
        SceneManager.LoadScene("Lee Test");
    }

    public void StartScene()
    {
        SceneManager.LoadScene("StartScene");
    }
    public void SettingScene()
    {
        SceneManager.LoadScene("SettingScene");
    }
    public void Exit()
    {
        Application.Quit();
    }
    public void restart()
    {
        Time.timeScale = 1f;
    }
}
