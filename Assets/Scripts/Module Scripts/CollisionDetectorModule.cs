using System.Collections.Generic;
using UnityEngine;

public class CollisionDetectorModule : Module
{
    public List<GameObject> gameObjects;
    public int addInputSockets = 4;

    // Inspector-selectable layer to detect
    public LayerMask detectionLayer;

    public override void AddSockets()
    {
        outputSockets = 0;
        inputSockets = addInputSockets;
    }

    public override void UpdateModule()
    {
        for (int i = 0; i < inputSockets; ++i)
        {
            inputNeurons[i].activation = 0;
            inputNeurons[i].nextActivation = 0;
        }

        if (inputNeurons.Count > 0)
        {
            int index = 0;

            foreach (GameObject gameObject in gameObjects)
            {
                Collider2D col = gameObject.GetComponent<Collider2D>();

                if (col != null)
                {
                    Vector2 center = col.bounds.center;
                    Vector2 size = col.bounds.size;

                    // Detect ONLY colliders on the chosen layer
                    Collider2D hit = Physics2D.OverlapBox(
                        center,
                        size,
                        0f,
                        detectionLayer     // <-- LAYER FILTER
                    );

                    bool touching = (hit != null && hit.gameObject != gameObject);

                    inputNeurons[index].activation = touching ? 1 : 0;
                    inputNeurons[index].nextActivation = touching ? 1 : 0;

                    SpriteRenderer sprite = gameObject.GetComponent<SpriteRenderer>();
                    if (sprite != null)
                    {
                        sprite.color = touching
                            ? new Color(1f, 1f, 1f, 1f)
                            : new Color(0f, 0f, 0f, 1f);
                    }
                }
                else
                {
                    inputNeurons[index].activation = 0;
                    inputNeurons[index].nextActivation = 0;
                }

                ++index;
            }
        }
    }
}
