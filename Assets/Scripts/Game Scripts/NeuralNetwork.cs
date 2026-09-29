
using NUnit.Framework.Internal;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
[System.Serializable]
public class Connection
{
    public float weight;
    public int destIndex;


    public Connection(float weight, int destIndex)
    {
        this.weight = weight;
        this.destIndex = destIndex;
    }
}



[System.Serializable]
public class Neuron
{

    public enum Type
    {
        Normal,
        Input,
        Output
    }
    public Module module;
    public Type type;
    public float activation;
    public float nextActivation;
    public int refactoryPeriod;
    public int refactory;
    public float x = 0;
    public float y = 0;
    public float thresholdMin = 0;
    public float thresholdMax = 1;
    public List<Connection> connections = new List<Connection>();
    public Neuron()
    {
        module = null;
        type = Type.Normal;
        this.x = 0;
        this.y = 0;
        this.activation = 0;
        this.nextActivation = 0;
        this.refactoryPeriod = Random.Range(2,6);
        this.thresholdMin = Random.Range(0f, .3f);
        this.thresholdMax = Random.Range(.75f, 1.0f);
        this.refactory = refactoryPeriod;
    }



    public void Update(NeuralNetwork network)
    {
     
        if (refactory > 0)
        {
            refactory -= 1;
        }
        else
        {
        
            Propagate(network);
        }





    }

    public void Propagate(NeuralNetwork network)
    {


        if (refactory <= 0 && activation > 0 && type != Neuron.Type.Output)
        {
            if (type == Neuron.Type.Input || activation >= thresholdMin && activation <= thresholdMax)
            {

                foreach (var connection in connections)
                {
                    
                    network.neurons[connection.destIndex].nextActivation += activation * connection.weight;
                    if (network.neurons[connection.destIndex].nextActivation > 1.0f) { network.neurons[connection.destIndex].nextActivation = 1.0f; }
                    if (network.neurons[connection.destIndex].nextActivation < 0.0f) { network.neurons[connection.destIndex].nextActivation = 0.0f; }
                    /*
                    network.neurons[connection.destIndex].activation += activation * connection.weight;
                    if (network.neurons[connection.destIndex].activation > 1.0f) { network.neurons[connection.destIndex].activation = 1.0f; }
                    if (network.neurons[connection.destIndex].activation < 0.0f) { network.neurons[connection.destIndex].activation = 0.0f; }
                    */

                }
                if (type != Neuron.Type.Input)
                {
                    activation = 0;
                    nextActivation = 0;
                }

                refactory = refactoryPeriod;
            }
        }


    }
}
[System.Serializable]
public class NeuralNetwork : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [System.NonSerialized]
    public List<Neuron> neurons = new List<Neuron>();
    public GameObject neuronClone;
    private List<GameObject> neuronCircles = new List<GameObject>();
    public int inputNeurons = 0;
    public int outputNeurons = 0;
    public int normalNeurons = 0;
    public int connectionCount = 0;
    void Start()
    {



    }

    public void GenerateNetwork()
    {
        for (int i = 0; i < inputNeurons; ++i)
        {
            Neuron n = new Neuron();
            n.type = Neuron.Type.Input;
            neurons.Add(n);
            n.thresholdMin = 0;
            n.x = Random.Range(-700f, -400f);
            n.y = Random.Range(-400f, 400f);
            n.thresholdMax = 1;
        }
        for (int i = 0; i < outputNeurons; ++i)
        {
            Neuron n = new Neuron();
            n.type = Neuron.Type.Output;
            neurons.Add(n);
            n.thresholdMin = 0;
            
            n.x = Random.Range(400f, 700f);
            n.y = Random.Range(-400f, 400f);
            n.thresholdMax = 1;
        }
        for (int i = 0; i < normalNeurons; ++i)
        {
            Neuron n = new Neuron();
            n.type = Neuron.Type.Normal;
            neurons.Add(n);
            n.x = Random.Range(-300f, 300f);
            n.y = Random.Range(-400f, 400f);
            n.thresholdMin = 0;

        }

        for (int i = 0; i < connectionCount; ++i)
        {
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
                //  Debug.Log(source);
                // Debug.Log(dest);*/
              Connection c = new Connection(Random.Range(-1.0f, 1.0f), dest);
               neurons[source].connections.Add(c);
          /*  int dest = i+4;
            int source = i;
            Connection c = new Connection(Random.Range(1.0f, 1.0f), dest);
              neurons[source].connections.Add(c);*/
        }

        foreach (Neuron n in neurons)
        {
            //  GameObject clone = Instantiate(neuronClone);
            // clone.transform.position = new Vector3(Random.Range(-10,10), Random.Range(-10, 10),0);
            // Debug.Log(clone);
            // neuronCircles.Add(clone);

        }
    }

    // Update is called once per frame
    void Update()
    {
      
        int i = 0;
        foreach (GameObject n in neuronCircles)
        {
            if (n != null)
            {
                n.GetComponent<SpriteRenderer>().color = new Color(neurons[i].activation * 255, neurons[i].activation * 255, neurons[i].activation * 255);
                ++i;
            }

        }
        foreach (Neuron neuron in neurons)
        {


            neuron.Update(this);
        }

        foreach (Neuron neuron in neurons)
        {
           
            neuron.activation = neuron.nextActivation;


        }
        foreach (Neuron neuron in neurons)
        {
            
                   neuron.nextActivation *= .98f;
                   if (neuron.nextActivation < 0.001f) neuron.nextActivation = 0;
                   neuron.activation = neuron.nextActivation;
               

          //  neuron.activation *= .98f;
        //    if (neuron.activation < 0.001f) neuron.activation = 0;
        }
    }


}