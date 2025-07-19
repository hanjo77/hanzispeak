using System.Collections;
using UnityEngine;
using TMPro;

public class CalloutSpawner : MonoBehaviour
{
    public OVRControllerHelper controllerHelper;
    public GameObject calloutUIPrefab;
    public Vector3 calloutOffset;
    public OVRInput.Button quitButton = OVRInput.Button.Two; // B

    private GameObject currentCallout;

    private void Start()
    {
        StartCoroutine(AttachCalloutWhenReady());
    }

    private IEnumerator AttachCalloutWhenReady()
    {
        yield return new WaitForSeconds(0.5f); // Wait for model activation

        Transform activeControllerModel = null;

        foreach (Transform child in controllerHelper.transform)
        {
            if (child.gameObject.activeInHierarchy)
            {
                activeControllerModel = child;
                break;
            }
        }

        if (activeControllerModel != null)
        {
            var bButton = activeControllerModel.Find("b_button_b");
            if (bButton != null && calloutUIPrefab != null)
            {
                currentCallout = Instantiate(calloutUIPrefab, bButton.position + calloutOffset, Quaternion.identity);
                currentCallout.transform.SetParent(bButton, worldPositionStays: true);

                var tmp = currentCallout.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = "Quit";
            }
            else
            {
                Debug.LogWarning("b_button not found on model: " + activeControllerModel.name);
            }
        }
        else
        {
            Debug.LogWarning("No active controller model found!");
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(quitButton, OVRInput.Controller.RTouch))
        {
            AppManager.Instance.StartView();
        }
    }
}
