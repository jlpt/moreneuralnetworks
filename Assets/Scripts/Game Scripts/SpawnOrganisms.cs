

using UnityEngine;

public class SpawnOrganisms : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject entity;
    public int amount;
    public float spawnTime;
    public float time;
    public int spawnLimit;
    void Start()
    {
        for (int i = 0; i < amount; i++)
        {
            GameObject clone = Instantiate(entity);
            clone.transform.position = new Vector3(Random.Range(-8f, 8f), Random.Range(-4f, 4f), 0);
            //clone.GetComponent<SpriteRenderer>().color = new Color(Random.Range(0f,1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        }
    }

    // Update is called once per frame
    void Update()
    {
        time += 1;
        Organism[] entities = FindObjectsByType<Organism>(FindObjectsSortMode.None);
        
        if (time >= spawnTime && entities.Length < spawnLimit)
        {
            GameObject clone = Instantiate(entity);

            clone.transform.position = new Vector3(Random.Range(-8f, 8f), Random.Range(-4f, 4f), 0);
          //  clone.GetComponent<SpriteRenderer>().color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            time = 0;
        }
    }
}
