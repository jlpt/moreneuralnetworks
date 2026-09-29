using UnityEngine;
using UnityEngine.UI;

public class SelectObjectButton : MonoBehaviour
{
    public GameObject prefabToSelect;   // Assign in the Inspector

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        SpawnObject.currentObject = prefabToSelect;
        BuildModeDisplayObject.currentObject = prefabToSelect;
    }
}
