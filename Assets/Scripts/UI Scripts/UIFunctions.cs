using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFunctions : MonoBehaviour
{
    [SerializeField] private Transform contentArea;
    [SerializeField] private GameObject moduleButtonPrefab;
    [SerializeField] private Transform neuralNetworkArea;

    private NeuralNetwork activeNetwork;
    private List<UINeuron> uiNeurons = new List<UINeuron>();


    void Start()
    {
        UIRemoteEvents.OnShowModulesClicked += ShowModules;
        UIRemoteEvents.OnDeleteClicked += DeleteObject;
        UIRemoteEvents.OnShowNetworkClicked += ShowNetwork;
    }

    void Update()
    {
        UpdateNeuronUI();
    }


    // ------------------------------------------------------------
    // INTERNAL NEURON UI CLASS
    // ------------------------------------------------------------
    private class UINeuron
    {
        public RectTransform rect;
        public Image image;
        public TMP_Text label;
        public Neuron neuronRef;
        public Neuron.Type type;
    }


    // ------------------------------------------------------------
    // SHOW NEURAL NETWORK + BUILD UI
    // ------------------------------------------------------------
    private void ShowNetwork()
    {
        if (SelectObject.currentlySelectedObject == null)
            return;

        // Clear previous neurons
        foreach (Transform child in neuralNetworkArea)
            Destroy(child.gameObject);
        uiNeurons.Clear();
        activeNetwork = null;

        var networks = SelectObject.currentlySelectedObject.GetComponentsInChildren<NeuralNetwork>();
        if (networks == null || networks.Length == 0)
            return;

        activeNetwork = networks[0];

        foreach (var neuron in activeNetwork.neurons)
            CreateNeuronUI(neuron);
    }


    // ------------------------------------------------------------
    // CREATE A NEURON UI ELEMENT
    // ------------------------------------------------------------
    private void CreateNeuronUI(Neuron neuron)
    {
        // Neuron circle
        GameObject neuronObj = new GameObject("NeuronUI", typeof(RectTransform));
        neuronObj.transform.SetParent(neuralNetworkArea, false);

        RectTransform rt = neuronObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(40, 40);

        Image img = neuronObj.AddComponent<Image>();
        img.type = Image.Type.Simple;
        img.preserveAspect = true;

        // Activation label
        GameObject textObj = new GameObject("ActivationText", typeof(RectTransform));
        textObj.transform.SetParent(neuronObj.transform, false);

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.5f, 0f);
        textRT.anchorMax = new Vector2(0.5f, 0f);
        textRT.anchoredPosition = new Vector2(0, -20);

        TMP_Text label = textObj.AddComponent<TextMeshProUGUI>();
        label.fontSize = 14;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        Neuron.Type neuronType = neuron.type;
        // Store references in tracking list
        uiNeurons.Add(new UINeuron
        {
            rect = rt,
            image = img,
            label = label,
            neuronRef = neuron,
            type = neuronType
        });
    }


    // ------------------------------------------------------------
    // UPDATE NEURON UI EVERY FRAME
    // ------------------------------------------------------------
    private void UpdateNeuronUI()
    {
        if (activeNetwork == null)
            return;

        foreach (var ui in uiNeurons)
        {
            // Update position
            ui.rect.anchoredPosition = new Vector2(ui.neuronRef.x, ui.neuronRef.y);

            // Activation → Color
            float a = Mathf.Clamp01(ui.neuronRef.activation);
            if (ui.type == Neuron.Type.Normal)
            {
                ui.image.color = Color.Lerp(Color.red, Color.green, a);
            } else if (ui.type == Neuron.Type.Input)
            {
                ui.image.color = Color.Lerp(Color.black, Color.white, a);
            }
            else if (ui.type == Neuron.Type.Output)
            {
                ui.image.color = Color.Lerp(Color.cyan, Color.yellow, a);
            }


            // Activation text
            ui.label.text = ui.neuronRef.activation.ToString("F2");
        }
    }


    // ------------------------------------------------------------
    // DELETE object
    // ------------------------------------------------------------
    private void DeleteObject()
    {
        if (SelectObject.currentlySelectedObject != null)
            Destroy(SelectObject.currentlySelectedObject);
    }


    // ------------------------------------------------------------
    // MODULE LIST UI
    // ------------------------------------------------------------
    private void ClearUI()
    {
        foreach (Transform child in contentArea)
            Destroy(child.gameObject);
    }

    private void CreateModuleButton(Module module)
    {
        GameObject buttonObj = Instantiate(moduleButtonPrefab, contentArea);
        TMP_Text label = buttonObj.GetComponentInChildren<TMP_Text>();
        label.text = module.name;
    }

    private void ShowModules()
    {
        if (SelectObject.currentlySelectedObject == null)
            return;

        ClearUI();

        Module[] modules = SelectObject.currentlySelectedObject.GetComponentsInChildren<Module>();
        foreach (var module in modules)
            CreateModuleButton(module);
    }
}
