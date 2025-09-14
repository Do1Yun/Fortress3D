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
        SceneManager.LoadScene("MainScene");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
