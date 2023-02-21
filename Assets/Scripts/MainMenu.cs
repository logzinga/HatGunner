using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MainMenu : MonoBehaviour
{
    public void HostGame () {
        SceneManager.LoadScene("Level01");
    }
    public void QuitGame () {
        Debug.Log("Exiting Game...");
        Application.Quit();
    }
}
