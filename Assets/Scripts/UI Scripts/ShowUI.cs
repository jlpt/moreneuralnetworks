using UnityEngine;
using UnityEngine.UI;

public class ShowUI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private Button[] buttons;
    void Start()
    {
        InputHandler.OnRightClick += OpenMenu;
       
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Button button in buttons)
        {
            button.interactable = false;
        }
        if (SelectObject.currentlySelectedObject != null)
        {
           
            buttons[1].interactable = true;

            if (SelectObject.currentlySelectedObject.GetComponentInChildren<NeuralNetwork>() != null)
            {
                
                buttons[2].interactable = true;



            }

        }
        
    }

 
    private void OpenMenu()
    {
        if (!menuRoot.activeSelf)
        {
            menuRoot.transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
            menuRoot.SetActive(true);
        }
        else
        {
            menuRoot.SetActive(false);
        }
    }
}
