using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FOVSlider : MonoBehaviour
{
    public Camera leftCamera;
    public Camera centreCamera;
    public Camera rightCamera;
    public Slider fovSlider;
    public TextMeshProUGUI fovValue;
    public Slider stretchSlider;
    public TextMeshProUGUI stretchValue;

    public float centreFocalLength = 8;
    private float fov;
    private float fovStretch = 0;
    // Start is called before the first frame update
    void Start()
    {
        LoadFoVSettings();
        UpdateFoV(fov);
    }

    // Update is called once per frame
    
    public void UpdateAndSaveFoV(float n)
    {
        UpdateFoV(n);
        SaveFoVSettings(Mathf.RoundToInt(n));
    }
    
    public void ResetLoadedFoV()
    {
        UpdateFoV(fov);
    }

    void UpdateFoV(float n)
    {
        fov = n;
        fovValue.text = fov.ToString();


        centreCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(fov, centreCamera.aspect);
        leftCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(fov, leftCamera.aspect);
        rightCamera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(fov, rightCamera.aspect);


        leftCamera.transform.localEulerAngles = - Vector3.up * (fov - fovStretch);
        rightCamera.transform.localEulerAngles = Vector3.up * (fov - fovStretch);

        
    }

    public void UpdateAndSaveStretch(float stretch)
    {
        SetStretchedFOV(stretch);
        SaveStretchSetting(Mathf.RoundToInt(stretch));
        UpdateFoV(fov);
    }

    void SetStretchedFOV(float stretch)
    {
        stretchValue.text = stretch.ToString();        
        centreCamera.sensorSize = new Vector2(stretch, centreCamera.sensorSize.y);
        leftCamera.sensorSize = new Vector2(stretch, leftCamera.sensorSize.y);
        rightCamera.sensorSize = new Vector2(stretch, rightCamera.sensorSize.y);

        fovStretch = fov - Camera.VerticalToHorizontalFieldOfView(centreCamera.fieldOfView, centreCamera.sensorSize.x / centreCamera.sensorSize.y);
        
        Debug.Log(fovStretch);
    }

    void LoadFoVSettings()
    {
        if (PlayerPrefs.HasKey("FoV"))
        {
            float n = PlayerPrefs.GetInt("FoV");
            fov = n;
            fovSlider.value = n;
            fovValue.text = n.ToString();
        }

        if (PlayerPrefs.HasKey("FoV_Stretch"))
        {
            float s = PlayerPrefs.GetInt("FoV_Stretch");
            //fovStretch = s;
            SetStretchedFOV(s);
            stretchSlider.value = s;
            stretchValue.text = s.ToString();
        }
    }

    void SaveStretchSetting(int s)
    {

        PlayerPrefs.SetInt("FoV_Stretch", s);
        PlayerPrefs.Save();
    }

    void SaveFoVSettings(int n)
    {

        PlayerPrefs.SetInt("FoV",n);
        PlayerPrefs.Save();
    }
}
  