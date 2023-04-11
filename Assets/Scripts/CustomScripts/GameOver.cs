using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
 * Game Management Script - handles user input once game is over.
 * Contains functions to restart and quit the game when corresponding button called.
 */
public class GameOver : MonoBehaviour
{
    public GameObject gameOverPanel;
    public void RestartGame()
    {
        SceneManager.LoadScene("Main");
        gameOverPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
