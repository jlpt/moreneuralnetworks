using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildModeDisplayObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject startObject;
    [SerializeField] public static GameObject currentObject;
    public Camera mainCamera;

    void Start()
    {
        currentObject = startObject;
    }

    // Update is called once per frame
    void Update()
    {
   
        GetComponent<SpriteRenderer>().sprite = currentObject.GetComponent<SpriteRenderer>().sprite;
        transform.position = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,10));

        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject clone = Instantiate(currentObject);
            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = 10f; // distance from camera to spawn plane
            clone.transform.position = Camera.main.ScreenToWorldPoint(mousePos);


        }
    }
}
