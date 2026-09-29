using UnityEngine;
using UnityEngine.InputSystem;

public class SpawnObject : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public GameObject startObject;
    [SerializeField] public static GameObject currentObject;
    void Start()
    {
        currentObject = startObject;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            GameObject clone = Instantiate(currentObject);
            Vector3 mousePos = Mouse.current.position.ReadValue();
            mousePos.z = 10f; // distance from camera to spawn plane
            clone.transform.position = Camera.main.ScreenToWorldPoint(mousePos);


        }
    }
}
