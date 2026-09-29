using UnityEngine;

public class MovementSensorModule : Module
{
    public float speedScale = .2f; // optional scaling factor for neuron activation

    public override void AddSockets()
    {
        outputSockets = 0;
        inputSockets = 5; // 0: Right, 1: Left, 2: Up, 3: Down, 4: Stationary
    }

    public override void UpdateModule()
    {
        if (inputNeurons.Count < 5) return;

        Rigidbody2D rb = organism.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        Vector2 velocity = rb.linearVelocity;
        
        // Reset all neuron activations
        for (int i = 0; i < 5; i++)
        {

            inputNeurons[i].activation = 0;
            inputNeurons[i].nextActivation = 0;
        }

        // Right/Left
        if (velocity.x > 0)
        {
            inputNeurons[0].activation = Mathf.Clamp01(velocity.x * speedScale);
            inputNeurons[0].nextActivation = inputNeurons[0].activation;
        }
        else if (velocity.x < 0)
        {
            inputNeurons[1].activation = Mathf.Clamp01(-velocity.x * speedScale);
            inputNeurons[1].nextActivation = inputNeurons[1].activation;
        }

        // Up/Down
        if (velocity.y > 0)
        {
            inputNeurons[2].activation = Mathf.Clamp01(velocity.y * speedScale);
            inputNeurons[2].nextActivation = inputNeurons[2].activation;
        }
        else if (velocity.y < 0)
        {
            inputNeurons[3].activation = Mathf.Clamp01(-velocity.y * speedScale);
            inputNeurons[3].nextActivation = inputNeurons[3].activation;
        }
      
        // Stationary
        if (velocity.magnitude < 0.01f)
        {
            inputNeurons[4].activation = 1;
            inputNeurons[4].nextActivation = 1;
        }
    }
}
