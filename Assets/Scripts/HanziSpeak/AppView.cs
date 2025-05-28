using System;
using UnityEngine;

public class AppView : MonoBehaviour
{
    [SerializeField] private GameObject viewPanel;
    
    public virtual void ShowView()
    {
        UnityEngine.Debug.Log($"Showing {viewPanel.name}");
        viewPanel.SetActive(true);
    }
    
    public virtual void HideView()
    {
        UnityEngine.Debug.Log($"Hiding {viewPanel.name}");
        viewPanel.SetActive(false);
    }
}