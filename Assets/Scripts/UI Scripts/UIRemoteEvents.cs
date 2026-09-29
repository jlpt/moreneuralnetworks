using UnityEngine;
using System;

public class UIRemoteEvents : MonoBehaviour
{
    public static event Action OnShowModulesClicked;
    public static event Action OnShowNetworkClicked;
    public static event Action OnDeleteClicked;

    public void ShowModules()
    {
        OnShowModulesClicked?.Invoke();
    }

    public void ShowNetwork()
    {
        OnShowNetworkClicked?.Invoke();
    }

    public void DeleteObject()
    {
        OnDeleteClicked?.Invoke();
    }
 


}
