using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIPauseMenu : MonoBehaviour
{
    public GameFlowManager gameFlowManager;

    public GameObject gameplayPanel;
    public Text statusInfo;
    public Button buttonResume;
    public Button buttonRestart;
    public Button buttonExit;

    private void Awake()
    {
        gameFlowManager = GameFlowManager.Instance;
    }

    private void Start()
    {
        gameObject.SetActive(false);
        
        buttonResume.onClick.AddListener(gameFlowManager.ResumeGame);
        buttonRestart.onClick.AddListener(gameFlowManager.RestartGame);
        buttonExit.onClick.AddListener(gameFlowManager.ExitGame);
    }

    private void OnEnable()
    {
        if (gameFlowManager.IsGameOver)
        {
            statusInfo.text = "GAME OVER!";
            statusInfo.color = Color.red;
            buttonResume.gameObject.SetActive(false);
        }
        else
        {
            statusInfo.text = "GAME PAUSED";
            statusInfo.color =Color.white;
        }
        
        gameplayPanel.SetActive(false);
    }

    private void OnDisable()
    {
        gameplayPanel.SetActive(true);
    }
    
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
