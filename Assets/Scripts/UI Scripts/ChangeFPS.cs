using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class ChangeFPS : MonoBehaviour
{
    [Header("Frame Rate Control")]
    public int frameRate = 60;

    private int lastFrameRate;

    float deltaTime = 0f;
    public TextMeshProUGUI FPS;
    public Slider FPSSlider;
    void Start()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync
        lastFrameRate = frameRate;
        Application.targetFrameRate = frameRate;
    }

    void Update()
    {
        if (frameRate != lastFrameRate)
        {
            Application.targetFrameRate = frameRate;
            lastFrameRate = frameRate;
           
        }

        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        float fps = 1f / Time.unscaledDeltaTime;
        float speedMultiplier = fps / 60f;
        Time.timeScale = speedMultiplier;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 32;
        style.normal.textColor = Color.white;

        int fps = Mathf.CeilToInt(1f / deltaTime);

        FPS.text = "Simulation Speed: x"+Time.timeScale.ToString("F2");


        // --- SLIDER ---
       // Debug.Log((int)FPSSlider.GetComponent<Slider>().value);
        //(int)FPSSlider.value;//
        frameRate = (int)FPSSlider.value;//(int)GUI.HorizontalSlider(new Rect(10, 130, 300, 25), frameRate, 60, 3000);
    }
}
