// ============================================
// ORGANISM.CS - FIXED REPRODUCE METHOD
// ============================================
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Organism : MonoBehaviour
{
    public NeuralNetwork network;
    public GameObject parent;
    public int freeInput;
    public int freeOutput;
    public List<Module> modules = new List<Module>();
    public bool isClone = false;
    public int foodEaten;
    public float health;
    public float maxHealth;
    private Rigidbody2D rb;
    public int generation;

    void SetupOrganism()
    {
        network.GenerateNetwork();
        foreach (var neuralnetwork in GetComponentsInChildren<NeuralNetwork>())
        {
            network = neuralnetwork;
        }
        foreach (var module in parent.GetComponentsInChildren<Module>())
        {
            module.AddSockets();
            module.AddToOrganism(this);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (!isClone)
        {
         
            SetupOrganism();
            GetComponent<SpriteRenderer>().color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
            for (int i = 0; i < 10; i++)
            {
            //    Reproduce();
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < modules.Count; i++)
        {
            modules[i].UpdateModule();
        }

        if (Time.frameCount % 10 == 0)
        {
            freeOutput = CountFreeOutputNeurons();
            freeInput = CountFreeInputNeurons();
        }

        if (foodEaten >= 2)
        {
            foodEaten = 0;
            Reproduce();
        }

        health -= 1;// Time.deltaTime;
        if (health <= 0)
        {
            Destroy(gameObject.transform.root.gameObject);
        }
    }

    private void FixedUpdate()
    {
        // 1. CAP VELOCITY
        if (rb != null && rb.linearVelocity.magnitude > 2f)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * 2f;
        }
/*
        // 2. BOUNDARY CHECK (New Feature)
        // This forces the organism to stay inside X[-10, 10] and Y[-6, 6]
        float clampedX = Mathf.Clamp(transform.position.x, -10f, 10f);
        float clampedY = Mathf.Clamp(transform.position.y, -5f, 5f);

        if (transform.position.x != clampedX || transform.position.y != clampedY)
        {
            transform.position = new Vector3(clampedX, clampedY, transform.position.z);
            // Optional: Kill velocity if hitting a wall so they don't stick
            // rb.linearVelocity = Vector2.zero; 
        }*/
    }

    private void OnDestroy()
    {
        // CRITICAL: Properly clean up to avoid memory leaks
        if (modules != null)
        {
            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] != null)
                {
                    modules[i].inputNeurons?.Clear();
                    modules[i].outputNeurons?.Clear();
                }
            }
            modules.Clear();
        }

        if (network != null && network.neurons != null)
        {
            for (int i = 0; i < network.neurons.Count; i++)
            {
                if (network.neurons[i] != null)
                {
                    network.neurons[i].module = null;
                    network.neurons[i].connections?.Clear();
                }
            }
            network.neurons.Clear();
        }
    }

    private int CountFreeInputNeurons()
    {
        if (network == null || network.neurons == null) return 0;

        int count = 0;
        for (int i = 0; i < network.neurons.Count; i++)
        {
            if (network.neurons[i].type == Neuron.Type.Input && network.neurons[i].module == null)
            {
                count++;
            }
        }
        return count;
    }

    private int CountFreeOutputNeurons()
    {
        if (network == null || network.neurons == null) return 0;

        int count = 0;
        for (int i = 0; i < network.neurons.Count; i++)
        {
            if (network.neurons[i].type == Neuron.Type.Output && network.neurons[i].module == null)
            {
                count++;
            }
        }
        return count;
    }

    public List<Neuron> GetFreeInputNeurons()
    {
        List<Neuron> inputNeurons = new List<Neuron>();
        if (network == null || network.neurons == null) return inputNeurons;

        for (int i = 0; i < network.neurons.Count; i++)
        {
            if (network.neurons[i].type == Neuron.Type.Input && network.neurons[i].module == null)
            {
                inputNeurons.Add(network.neurons[i]);
            }
        }
        return inputNeurons;
    }

    public List<Neuron> GetFreeOutputNeurons()
    {
        List<Neuron> outputNeurons = new List<Neuron>();
        if (network == null || network.neurons == null) return outputNeurons;

        for (int i = 0; i < network.neurons.Count; i++)
        {
            if (network.neurons[i].type == Neuron.Type.Output && network.neurons[i].module == null)
            {
                outputNeurons.Add(network.neurons[i]);
            }
        }
        return outputNeurons;
    }


    public void Mutate(int power)
    {
        if (network == null || network.neurons == null) return;

        List<Neuron> neurons = network.neurons;
        Neuron neuron = neurons[Random.Range(0, neurons.Count)];
        neuron.refactoryPeriod += Random.Range(-2, 3);
          neuron.refactoryPeriod = Mathf.Clamp(neuron.refactoryPeriod, 1, 60);
        // 1. EXISTING WEIGHT MUTATION
        for (int i = 0; i < power; i++)
        {
            if (neurons.Count == 0) break;

            neuron = neurons[Random.Range(0, neurons.Count)];
            if (neuron.connections.Count > 0)
            {
                Connection conn = neuron.connections[Random.Range(0, neuron.connections.Count)];
                conn.weight += Random.Range(-.1f, .1f);
                conn.weight = Mathf.Clamp(conn.weight, -1f, 1f);
            }

         
        }
        
        // 2. STRUCTURAL MUTATION: CONNECTIONS (1% Chance)
        // Random.value returns 0.0 to 1.0, so <= 0.01 is 1%
        if (Random.value <= 0.01f)
        {
            // 50% chance to ADD, 50% chance to REMOVE
            if (Random.value <= 0.67f)
            {
                // --- ADD WEIGHT (CONNECTION) ---
                if (neurons.Count > 0)
                {
                    // Pick random source and destination indices
                    int source = Random.Range(0, neurons.Count);
                    int dest = Random.Range(0, neurons.Count);

                    while (neurons[source].type == Neuron.Type.Output)
                    {
                    

                        source = Random.Range(0, neurons.Count);
                       

                    }

                    while (neurons[dest].type == Neuron.Type.Input)
                    {
                        dest = Random.Range(0, neurons.Count);
                    }

                    while (neurons[source].type == Neuron.Type.Input && neurons[dest].type == Neuron.Type.Input ||
                    neurons[source].type == Neuron.Type.Input && neurons[dest].type == Neuron.Type.Output)
                    {
                        dest = Random.Range(0, neurons.Count);
                    }
                    if (source == dest)
                    {
                        dest = (dest + 1) % neurons.Count;
                    }
            
                    Connection c = new Connection(Random.Range(-1.0f, 1.0f), dest);
                    neurons[source].connections.Add(c);
                }
            }
            else
            {
                // --- REMOVE WEIGHT (CONNECTION) ---
                // Find neurons that actually have connections to remove
                List<Neuron> candidates = new List<Neuron>();
                foreach (var n in neurons) { if (n.connections.Count > 0) candidates.Add(n); }

                if (candidates.Count > 0)
                {
                    Neuron target = candidates[Random.Range(0, candidates.Count)];
                    target.connections.RemoveAt(Random.Range(0, target.connections.Count));
                }
            }
        }
        /*
        // 3. STRUCTURAL MUTATION: NEURONS (0.01% Chance)
        // <= 0.0001 is 0.01%
        if (Random.value <= 0.0001f)
        {
            if (Random.value <= 0.5f)
            {
                // --- ADD NEURON ---
                Neuron n = new Neuron();
                // Constructor sets Type to Normal by default
                neurons.Add(n);
            }
            else
            {
                // --- REMOVE NEURON ---
                // We must strictly remove only 'Normal' neurons. 
                // Removing Input/Output neurons breaks the Modules.
                List<int> candidateIndices = new List<int>();
                for (int i = 0; i < neurons.Count; i++)
                {
                    if (neurons[i].type == Neuron.Type.Normal) candidateIndices.Add(i);
                }

                if (candidateIndices.Count > 0)
                {
                    // 1. Pick a random normal neuron to remove
                    int removeIndex = candidateIndices[Random.Range(0, candidateIndices.Count)];

                    // 2. Remove it from the list
                    neurons.RemoveAt(removeIndex);

                    // 3. CRITICAL: Fix indices in all other connections
                    // Since the list shifted, any connection pointing to an index > removeIndex
                    // needs to be decremented by 1. Connections pointing TO the removed neuron must be deleted.
                    foreach (Neuron n in neurons)
                    {
                        // Loop backwards to safely remove items
                        for (int i = n.connections.Count - 1; i >= 0; i--)
                        {
                            Connection c = n.connections[i];
                            if (c.destIndex == removeIndex)
                            {
                                // Remove connection pointing to the dead neuron
                                n.connections.RemoveAt(i);
                            }
                            else if (c.destIndex > removeIndex)
                            {
                                // Shift index down to match new list position
                                c.destIndex--;
                            }
                        }
                    }
                }
            }
        }*/
    }

    public void Collide(EntitySO entity, GameObject gameObject)
    {
    //    ++foodEaten;
      //  health = maxHealth;

    }
    public void Reproduce()
    {
        if (parent == null)
        {
            Debug.LogError("Parent is null! Cannot reproduce.");
            return;
        }

        // CRITICAL FIX: Clone the parent GameObject
        GameObject cloneObj = Instantiate(parent);

        Organism clonedOrganism = cloneObj.GetComponentInChildren<Organism>();
        if (clonedOrganism == null)
        {
            Debug.LogError("No Organism component found in clone!");
            Destroy(cloneObj);
            return;
        }

        // Set basic properties
        clonedOrganism.isClone = true;
        clonedOrganism.foodEaten = 0;
        clonedOrganism.generation = generation + 1;
        clonedOrganism.health = clonedOrganism.maxHealth;
        cloneObj.name = "Organism Gen " + clonedOrganism.generation;

        // Apply color mutation
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        SpriteRenderer cloneSr = clonedOrganism.GetComponent<SpriteRenderer>();

        if (sr != null && cloneSr != null)
        {
            // Convert parent color to HSV
            Color.RGBToHSV(sr.color, out float h, out float s, out float v);

            // Mutate hue a little
            h = (h + Random.Range(-0.02f, 0.02f) + 1f) % 1f;

            // Mutate saturation and value slightly but prevent drift into white
            s = Mathf.Clamp01(s + Random.Range(-0.01f, 0.01f));
            v = Mathf.Clamp01(v + Random.Range(-0.01f, 0.01f));

            // Convert back to RGB
            Color newColor = Color.HSVToRGB(h, s, v);

            // KEEP ORIGINAL ALPHA (avoid alpha drift)
            newColor.a = sr.color.a;

            cloneSr.color = newColor;
        }

        // CRITICAL FIX: The clone already has its own neural network from Instantiate
        // We need to PROPERLY COPY the network structure without memory leaks

        if (clonedOrganism.network != null && network != null)
        {
            // Clear the cloned network's existing neurons (from Instantiate)
            clonedOrganism.network.neurons.Clear();

            clonedOrganism.modules.Clear();
            Module[] modules = cloneObj.GetComponentsInChildren<Module>();
            for (int i = 0; i < modules.Length; i++)
            {
                modules[i].inputNeurons.Clear();
                modules[i].outputNeurons.Clear();
                modules[i].organism = null;
                modules[i].network = null;


            }

            
            // Create NEW neurons for the clone (deep copy structure)
            for (int i = 0; i < network.neurons.Count; i++)
            {
                Neuron originalNeuron = network.neurons[i];
                Neuron newNeuron = new Neuron();
                newNeuron.type = originalNeuron.type;
                newNeuron.refactoryPeriod = originalNeuron.refactoryPeriod;
                newNeuron.refactory = 0;//1/newNeuron.refactoryPeriod;
                newNeuron.x = originalNeuron.x;
                newNeuron.y = originalNeuron.y;
                newNeuron.activation = 0;
                newNeuron.nextActivation = 0;
                newNeuron.module = null;
                newNeuron.thresholdMax = originalNeuron.thresholdMax;
                newNeuron.thresholdMin = originalNeuron.thresholdMin;

                // Copy connections with NEW Connection objects
                  
                for (int j = 0; j < originalNeuron.connections.Count; j++)
                {
                    Connection originalConn = originalNeuron.connections[j];
                    Connection newConn = new Connection(originalConn.weight, originalConn.destIndex);
                    newNeuron.connections.Add(newConn);
                }

                clonedOrganism.network.neurons.Add(newNeuron);
            }
        }

        // Mutate the cloned network
        clonedOrganism.Mutate(80);

  

        Module[] moduleArray = cloneObj.GetComponentsInChildren<Module>();
        for (int i = 0; i < moduleArray.Length; i++)
        {
            moduleArray[i].organism = clonedOrganism;
            moduleArray[i].network = clonedOrganism.network;
     
            moduleArray[i].AddSockets();
            moduleArray[i].AddToOrganism(clonedOrganism);
        }
    }
}