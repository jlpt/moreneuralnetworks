using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovementModule : Module
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    
    private ConstantForce2D constantForce;
    public override void AddSockets()
    {
        outputSockets = 4;
        inputSockets = 0;
    }
    void Awake()
    {


        base.Awake();

        
        if (organism.GetComponent<ConstantForce2D>() == null)
        {
            organism.AddComponent<ConstantForce2D>();
            
        }
        constantForce = organism.GetComponent<ConstantForce2D>();
       
    }

    // Update is called once per frame
    public override void UpdateModule()
    {

        if (outputNeurons.Count >= 4)
        {
            // Calculate direction based on neuron pairs
            // Pair 1: X-Axis (Right vs Left)
            float xDir = outputNeurons[0].activation - outputNeurons[1].activation;

            // Pair 2: Y-Axis (Up vs Down)
            float yDir = outputNeurons[2].activation - outputNeurons[3].activation;

            // Update the position directly
            // We use Time.deltaTime to make movement independent of frame rate
          //  organism.transform.position += new Vector3(xDir, yDir, 0) * 0.1f;
          constantForce.force = new Vector2(xDir, yDir) * 25f;
        }

    }

    
}
