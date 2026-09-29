using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class TabsManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private TabButton[] tabs;
    [SerializeField] private TabButton selectedTab;
    void Start()
    {
        foreach (var tab in tabs)
        {
            var button = tab.GetComponent<Button>();
            tab.content.SetActive(false);
            tab.GetComponentInChildren<TMP_Text>().color = new Color(1f, 1f, 1f, .5f);
            if (button != null)
            {
                // Capture the local variable "tab" inside the lambda
                button.onClick.AddListener(() => ClickOnTab(tab));
            }
        }
        selectedTab.content.SetActive(true);
        selectedTab.GetComponentInChildren<TMP_Text>().color = new Color(1f, 1f, 1f, 1f);
    }

    private void ClickOnTab(TabButton clickedTab)
    {
        foreach (var tab in tabs)
        {
            tab.content.SetActive(false);
            tab.GetComponentInChildren<TMP_Text>().color = new Color(1f, 1f, 1f, .5f);
        }

        clickedTab.content.SetActive(true);
        clickedTab.GetComponentInChildren<TMP_Text>().color = new Color(1f, 1f, 1f, 1f);
        selectedTab = clickedTab;


    }

 
}
