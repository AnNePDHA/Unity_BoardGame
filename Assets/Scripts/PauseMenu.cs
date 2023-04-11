using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] public GameObject PauseMenuPanel;

    public void Pause(){
        PauseMenuPanel.SetActive(true);
        Time.timeScale = 0;
    }

    public void Resume(){
        PauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void Restart(){
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void Exit(){
        Time.timeScale = 1f;
        SceneManager.LoadScene("HomeScreen");
    }
}