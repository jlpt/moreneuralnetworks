
using TMPro;
using UnityEngine;

public class HighestGeneration : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    int highestGen = 0;
    public TextMeshProUGUI FPS;
    // Update is called once per frame
    void Update()
    {
        
        Organism[] entities = FindObjectsByType<Organism>(FindObjectsSortMode.None);
        
        foreach (Organism organism in entities)
        {
            highestGen = Mathf.Max(organism.generation, highestGen);
        }

     

  
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 32;
        style.normal.textColor = Color.white;
        FPS.text = "Highest Gen: " + highestGen;
      //  GUI.Label(new Rect(200, 10, 300, 40), "Highest Gen: " + highestGen, style);
    }
}
