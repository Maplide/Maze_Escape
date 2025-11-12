using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsUI : MonoBehaviour
{
    public string menuScene = "MainMenu";

    public void OnClickVolver()
    {
        SceneManager.LoadScene(menuScene);
    }
}
