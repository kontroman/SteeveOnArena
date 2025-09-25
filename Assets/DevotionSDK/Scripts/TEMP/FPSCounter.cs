using Devotion.SDK.Controllers;
using TMPro;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    public TMP_Text fpsText;
    private float deltaTime;

    private void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            GameRoot.Instance.PlayerProgress.InventoryProgress.AddResource();

        if(Input.GetKeyDown(KeyCode.P))
            Debug.LogError(GameRoot.Instance.PlayerProgress.InventoryProgress.DebugOnlyResourcesInInventory);

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = Mathf.Ceil(fps).ToString() + " FPS";
    }
}
