using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CanvasSelect : MonoBehaviour
{

    public TMP_Dropdown dropdown;
    public List<Camera> cameras;
    public Canvas canvas;
    public int selectedCamera = 0;
    // Start is called before the first frame update
    void Start()
    {

        LoadCanvasSettings();

        canvas.worldCamera = cameras[selectedCamera];
        dropdown.value = selectedCamera;
    }


    void LoadCanvasSettings()
    {
        if (PlayerPrefs.HasKey("TitleScreen"))
        {
            selectedCamera = PlayerPrefs.GetInt("TitleScreen");
        }
    }

    void SaveCanvasSettings()
    {

        PlayerPrefs.SetInt("TitleScreen", selectedCamera);
        PlayerPrefs.Save();
    }

    public void SetCameraNumber(int n)
    {

        selectedCamera = n;

        if(cameras.Count>n)
            canvas.worldCamera = cameras[n];

        SaveCanvasSettings();
    }

}
