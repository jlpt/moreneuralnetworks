using System.Collections.Generic;
using UnityEngine;

public class SpawnFood : MonoBehaviour
{
    [Header("Settings")]
    public GameObject entity;
    public GameObject parent;
    public int amount;
    public int spawnLimit;

    // Frames/Ticks between spawns
    public float spawnTime;
    public float time;

    [Header("Spawn Area (Relative to this Object)")]
    // These are now LOCAL offsets. 
    // e.g., (-8, -4) means 8 units left and 4 units down FROM THE SPAWNER.
    public Vector2 min = new Vector2(-8f, -4f);
    public Vector2 max = new Vector2(8f, 4f);

    private const int maxAttempts = 20;

    private List<GameObject> trackedEntities = new List<GameObject>();

    void Start()
    {
        for (int i = 0; i < amount; i++)
        {
            TrySpawnEntity();
        }
    }

    void Update()
    {
        time += 1;

        // Cleanup list
        for (int i = trackedEntities.Count - 1; i >= 0; i--)
        {
            if (trackedEntities[i] == null) trackedEntities.RemoveAt(i);
        }

        if (time >= spawnTime && trackedEntities.Count < spawnLimit)
        {
            TrySpawnEntity();
            time = 0;
        }
    }

    private void TrySpawnEntity()
    {
        Vector3 spawnPos = GetValidSpawnPosition();
        if (spawnPos != Vector3.negativeInfinity)
        {
            SpawnEntity(spawnPos);
        }
    }

    private void SpawnEntity(Vector3 pos)
    {
        GameObject clone = Instantiate(entity);

        if (parent != null)
            clone.transform.parent = parent.transform;

        clone.transform.position = pos;
        trackedEntities.Add(clone);
    }

    private Vector3 GetValidSpawnPosition()
    {
        Collider2D prefabCol = entity.GetComponent<Collider2D>();
        Vector2 size = prefabCol != null ? prefabCol.bounds.size : new Vector2(0.5f, 0.5f);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Calculate random offset
            float randomX = Random.Range(min.x, max.x);
            float randomY = Random.Range(min.y, max.y);

            // Add the offset to the Spawner's current position
            Vector2 pos = (Vector2)transform.position + new Vector2(randomX, randomY);

            // Overlap check
            Collider2D hit = Physics2D.OverlapBox(pos, size, 0f);

            if (hit == null)
            {
                return new Vector3(pos.x, pos.y, 0);
            }
        }
        return Vector3.negativeInfinity;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        // Calculate size of the box
        float width = max.x - min.x;
        float height = max.y - min.y;

        // Calculate center offset relative to the local min/max
        Vector3 localCenterOffset = new Vector3(min.x + width / 2, min.y + height / 2, 0);

        // Draw the cube at Spawner Position + Offset
        Gizmos.DrawWireCube(transform.position + localCenterOffset, new Vector3(width, height, 0));
    }
#endif
}