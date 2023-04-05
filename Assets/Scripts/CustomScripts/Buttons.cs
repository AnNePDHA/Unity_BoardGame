using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Buttons : MonoBehaviour
{
    public void Easy()
    {
        NetworkManager.diff = GameManager.Difficulty.Easy;
        SceneManager.LoadScene("Main");
    }

    public void Medium()
    {
        NetworkManager.diff = GameManager.Difficulty.Medium;
        SceneManager.LoadScene("Main");
    }

    public void Hard()
    {
        NetworkManager.diff = GameManager.Difficulty.Hard;
        SceneManager.LoadScene("Main");
    }
}
