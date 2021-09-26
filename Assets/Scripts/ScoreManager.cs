using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private static int _highScore;
    
    #region Singleton

    private static ScoreManager _instance;

    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ScoreManager>();

                if (_instance == null)
                {
                    Debug.LogError("ScoreManager Not Found!");
                }
            }
            return _instance;
        }
    }

    #endregion

    public int tileRatio;
    public int comboRatio;

    private int _currentScore;
    
    public int HighScore => _highScore;
    public int CurrentScore => _currentScore;

    private void Start()
    {
        ResetScore();
    }

    public void ResetScore()
    {
        _currentScore = 0;
    }
    
    public void IncrementCurentScore(int tileCount, int comboCount)
    {
        _currentScore += (tileCount * tileRatio) * (comboCount * comboRatio);
        SoundManager.Instance.PlayScore(comboCount > 1);
    }
    
    public void SetHighScore()
    {
        _highScore = _currentScore;
    }
}
