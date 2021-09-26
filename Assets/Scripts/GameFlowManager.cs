using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms.Impl;

public class GameFlowManager : MonoBehaviour
{
    #region Singleton

    private static GameFlowManager _instance;

    public static GameFlowManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameFlowManager>();

                if (_instance == null)
                {
                    Debug.LogError("GameFlowManager Not Found!");
                }
            }
            return _instance;
        }
    }

    [Header("UI")]
    public UIGameOver gameOverUI;
    public UIPauseMenu pauseMenuUI;

    #endregion

    private bool _isGameOver;
    
    public bool IsGameOver => _isGameOver;

    private void Start()
    {
        _isGameOver = false;
    }
    public void PauseGame()
    {
        Time.timeScale = 0f;
        pauseMenuUI.Show();
    }
    
    public void ResumeGame()
    {
        Time.timeScale = 1f;
        pauseMenuUI.Hide();
    }

    public void RestartGame()
    {
        _isGameOver = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    
    public void ExitGame()
    {
        Application.Quit();
    }

    public void GameOver()
    {
        _isGameOver = true;
        if (ScoreManager.Instance.CurrentScore > ScoreManager.Instance.HighScore)
        {
            ScoreManager.Instance.SetHighScore();
            gameOverUI.newHighScore.text = $"You've Set a New High Score!\n{ScoreManager.Instance.HighScore}";
            gameOverUI.newHighScore.gameObject.SetActive(true);
        }
        
        gameOverUI.Show();
    }
}
