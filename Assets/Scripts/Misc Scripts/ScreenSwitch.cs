using UnityEngine;
using UnityEngine.SceneManagement;
public class ScreenSwitch : MonoBehaviour
{

   
    public void EditorScene()
    {
        SceneManager.LoadScene("Editor");
    }

    public void GameScene()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
