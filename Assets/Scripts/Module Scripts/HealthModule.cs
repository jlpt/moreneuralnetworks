using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class HealthModule : Module
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    public override void AddSockets()
    {
        outputSockets = 0;
        inputSockets = 1;
    }
    

    // Update is called once per frame
    public override void UpdateModule()
    {

     if (organism != null)
        {
            inputNeurons[0].activation = 1;// - (organism.health / organism.maxHealth);
            inputNeurons[0].nextActivation = 1;// - (organism.health / organism.maxHealth);
        }
                   
          
        
    }



}
