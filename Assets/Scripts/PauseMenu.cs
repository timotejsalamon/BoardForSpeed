using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.PostProcessing;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;
    public GameObject pauseMenuUI;
    public string sceneName;
    public GameObject pauseBtn;

    public Camera camera;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        PostProcessVolume ppVolume = camera.gameObject.GetComponent<PostProcessVolume>();
        ppVolume.enabled = false;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        pauseBtn.SetActive(true);
    }

    void Pause()
    {
        PostProcessVolume ppVolume = camera.gameObject.GetComponent<PostProcessVolume>();
        ppVolume.enabled = true;
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        pauseBtn.SetActive(false);
    }

    public void Back()
    {
        SceneManager.LoadScene(sceneName);
    }
}
