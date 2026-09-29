using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SensorModule : Module
{
    public EntitySO entityType;
    public float detectionRadius = 5f;
    public List<GameObject> gameObjects;

    // NEW PUBLIC VARIABLE
    public int ignoreAmount = 0;

    public override void AddSockets()
    {
        outputSockets = 0;
        inputSockets = 5;
    }

    public override void UpdateModule()
    {

        
    
        int i = 0;
        foreach (GameObject gameObject in gameObjects)
        {
            gameObject.GetComponent<SpriteRenderer>().color =
                new Color(inputNeurons[i].activation, inputNeurons[i].activation, inputNeurons[i].activation);
            ++i;
        }

        // RESET INPUT NEURONS
        if (inputNeurons.Count > 0)
        {
            for (int j = 0; j < 5; j++)
            {
                inputNeurons[j].activation = 0;
                inputNeurons[j].nextActivation = 0;
            }

          

            if (entityType == null) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);
        
            // Filter by Entity type
            List<GameObject> matching = new List<GameObject>();
            foreach (Collider2D collider in hits)
            {

                Entity e = collider.gameObject.GetComponent<Entity>();
              
                if (e != null && e.data.id == entityType.id)
                {
                  
                    matching.Add(collider.gameObject);
                }
            }

            if (matching.Count > 0)
            {
                // SORT BY DISTANCE
                matching = matching.OrderBy(obj =>
                    Vector2.Distance(obj.transform.position, organism.transform.position)).ToList();

                // APPLY ignoreAmount
                if (ignoreAmount >= matching.Count)
                    return; // nothing to sense

                GameObject target = matching[ignoreAmount]; // <-- the selected object

                Vector2 organismPos = organism.transform.position;
                Vector2 targetPos = target.transform.position;

                float xDist = Mathf.Abs(targetPos.x - organismPos.x);
                float yDist = Mathf.Abs(targetPos.y - organismPos.y);

                // Left/Right
                if (organismPos.x - targetPos.x < 0)
                {
                    inputNeurons[0].activation = (detectionRadius - Mathf.Clamp(xDist, 0f, detectionRadius)) / detectionRadius;
                    inputNeurons[0].nextActivation = inputNeurons[0].activation;
                }
                else
                {
                    inputNeurons[1].activation = (detectionRadius - Mathf.Clamp(xDist, 0f, detectionRadius)) / detectionRadius;
                    inputNeurons[1].nextActivation = inputNeurons[1].activation;
                }

                // Up/Down
                if (organismPos.y - targetPos.y < 0)
                {
                    inputNeurons[2].activation = (detectionRadius - Mathf.Clamp(yDist, 0f, detectionRadius)) / detectionRadius;
                    inputNeurons[2].nextActivation = inputNeurons[2].activation;
                }
                else
                {
                    inputNeurons[3].activation = (detectionRadius - Mathf.Clamp(yDist, 0f, detectionRadius)) / detectionRadius;
                    inputNeurons[3].nextActivation = inputNeurons[3].activation;
                }

                // No directional activation → "nothing detected"
                
            }

           
        }
        inputNeurons[4].nextActivation = 1;
       
  
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        Entity ent = collision.gameObject.GetComponent<Entity>();
        if (ent != null)
        {
            if (ent.data.id == 1)
            {
                ++organism.foodEaten;
                organism.health = organism.maxHealth;
                Destroy(collision.gameObject);
            }
        }

        ent = collision.gameObject.GetComponent<Entity>();
        if (ent != null)
        {
            if (ent.data.id == 2)
            {
               
                organism.health = 0;
                Destroy(collision.gameObject);
            }
        }
    }
}
