using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SelectObject : MonoBehaviour
{
    [Header("Dependencies")]
    private Camera mainCamera;

    [Header("Selection Settings")]
    public Material outlineMaterial;   // Material for Hover
    public Material selectionMaterial; // Material for Click

    public static GameObject currentlySelectedObject = null;

    private GameObject currentlyOutlinedObject = null;

    private Dictionary<Renderer, Material> outlineOriginalMaterials = new Dictionary<Renderer, Material>();
    private Dictionary<Renderer, Material> selectionOriginalMaterials = new Dictionary<Renderer, Material>();

    public static event Action<GameObject> OnSelectionChanged;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            HandleSelection();
        }

        HandleHover();
    }

    private void HandleSelection()
    {
        GameObject hitObject = GetObjectUnderMouse();

        if (hitObject != currentlySelectedObject)
        {
            if (currentlySelectedObject != null)
            {
                RestoreMaterials(currentlySelectedObject, selectionOriginalMaterials);
                selectionOriginalMaterials.Clear();
            }

            currentlySelectedObject = hitObject;
            OnSelectionChanged?.Invoke(currentlySelectedObject);

            if (currentlySelectedObject != null)
            {
                if (currentlyOutlinedObject == currentlySelectedObject)
                {
                    RestoreMaterials(currentlyOutlinedObject, outlineOriginalMaterials);
                    outlineOriginalMaterials.Clear();
                    currentlyOutlinedObject = null;
                }

                ApplyMaterial(currentlySelectedObject, selectionMaterial, selectionOriginalMaterials);
            }
        }
    }

    private void HandleHover()
    {
        GameObject hitObject = GetObjectUnderMouse();

        if (hitObject != currentlyOutlinedObject)
        {
            if (currentlyOutlinedObject != null)
            {
                if (currentlyOutlinedObject != currentlySelectedObject)
                {
                    RestoreMaterials(currentlyOutlinedObject, outlineOriginalMaterials);
                }
                outlineOriginalMaterials.Clear();
            }

            currentlyOutlinedObject = hitObject;

            if (currentlyOutlinedObject != null)
            {
                if (currentlyOutlinedObject != currentlySelectedObject)
                {
                    ApplyMaterial(currentlyOutlinedObject, outlineMaterial, outlineOriginalMaterials);
                }
            }
        }
    }

    private GameObject GetObjectUnderMouse()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, Mathf.Abs(mainCamera.transform.position.z))
        );

        Collider2D hit = Physics2D.OverlapPoint(worldPos);

        if (hit != null)
            return hit.transform.gameObject; // return EXACT object hit

        return null;
    }

    private void ApplyMaterial(GameObject target, Material newMat, Dictionary<Renderer, Material> dictToSaveTo)
    {
        Renderer[] childRenderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in childRenderers)
        {
            if (!dictToSaveTo.ContainsKey(rend))
                dictToSaveTo[rend] = rend.material;

            rend.material = newMat;
        }
    }

    private void RestoreMaterials(GameObject target, Dictionary<Renderer, Material> dictToReadFrom)
    {
        foreach (var kvp in dictToReadFrom)
        {
            if (kvp.Key != null)
                kvp.Key.material = kvp.Value;
        }
    }
}
