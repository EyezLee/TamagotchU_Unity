using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Events;
using TMPro;

public class DisplaySelector : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public int screenNumber = 0;
    public UnityEvent OnScreenActive;
    public UnityEvent OnScreenInactive;
    public List<Camera> cameras;
    public List<GameObject> canvases;
    public int selectedCamera = 0;

    private int oldCamera;

    // Start is called before the first frame update
    void Start()
    {
        Display.onDisplaysUpdated += DisplaysUpdated;

        LoadCameraSettings();

        if (canvases == null)
            canvases = new List<GameObject>();

        oldCamera = selectedCamera;

        //if (screenNumber == 0)
        //  selectedCamera = 0;

        dropdown.value = selectedCamera;

        DisplaysUpdated();
    }

    void DisplaysUpdated()
    {
        if (Display.displays.Length > screenNumber)
        {
            if (selectedCamera < cameras.Count)
            {
                if (cameras[selectedCamera] != null)
                {
                    //turn off the old camera if we don't need it anymore
                    if(oldCamera != selectedCamera)
                    {
                        if(oldCamera < cameras.Count)
                        {
                            cameras[oldCamera].enabled = false;

                            if (canvases.Count > 0)
                            {
                                if (canvases[oldCamera] != null)
                                    canvases[oldCamera].SetActive(false);
                            }
                        }
                        else
                        {
                            cameras[cameras.Count - 2].enabled = false;
                            cameras[cameras.Count - 1].enabled = false;
                            cameras[cameras.Count - 2].rect = new Rect(0, 0, 1f, 1f);
                            cameras[cameras.Count- 1].rect = new Rect(0, 0, 1f, 1f);
                        }

                    }
                        

                    oldCamera = selectedCamera;

                    //activate the camera and set the target display.
                    cameras[selectedCamera].enabled = true;

                    if(canvases.Count>0)
                    {
                        if (canvases[oldCamera] != null)
                            canvases[oldCamera].SetActive(true);
                    }                    
                    //if (!videoPlayers[selectedCamera].isPlaying)
                    //{
                    //Debug.Log("was not playing, now playing");
                    //videoPlayers[selectedCamera].Play();
                    //}


                    cameras[selectedCamera].targetDisplay = screenNumber;                    
                    Display.displays[screenNumber].Activate();  
                    Display.displays[screenNumber].SetRenderingResolution(Display.displays[screenNumber].systemWidth, Display.displays[screenNumber].systemHeight);
                    OnScreenActive.Invoke();
                    Debug.Log("Camera " + selectedCamera + " output set to Screen #" + screenNumber);
                }
            }
            else if(selectedCamera == cameras.Count || selectedCamera == cameras.Count+1)
            {

                cameras[cameras.Count-2].targetDisplay = screenNumber;
                cameras[cameras.Count-1].targetDisplay = screenNumber;

                if(selectedCamera == cameras.Count)
                {
                    cameras[cameras.Count - 2].rect = new Rect(0, 0, 0.5f, 1);
                    cameras[cameras.Count-  1].rect = new Rect(0.5f, 0, 0.5f, 1);
                }
                else
                {
                    cameras[cameras.Count - 1].rect = new Rect(0, 0, 0.5f, 1);
                    cameras[cameras.Count - 2].rect = new Rect(0.5f, 0, 0.5f, 1);
                }

                cameras[cameras.Count - 1].enabled = true;
                cameras[cameras.Count - 2].enabled = true;

                Display.displays[screenNumber].Activate();
                Display.displays[screenNumber].SetRenderingResolution(Display.displays[screenNumber].systemWidth, Display.displays[screenNumber].systemHeight);
                OnScreenActive.Invoke();
            }

        }
        else
        {
            OnScreenInactive.Invoke();
        }

    }

    void OnDestroy()
    {

        Display.onDisplaysUpdated -= DisplaysUpdated;
    }

    void LoadCameraSettings()
    {
        if (PlayerPrefs.HasKey("Screen" + screenNumber))
        {
            selectedCamera = PlayerPrefs.GetInt("Screen" + screenNumber);
        }
    }

    void SaveCameraSettings()
    {

        PlayerPrefs.SetInt("Screen" + screenNumber, selectedCamera);
        PlayerPrefs.Save();
    }

    public void SetScreenNumber(int n)
    {

        selectedCamera = n;

        DisplaysUpdated();

        SaveCameraSettings();
    }

}
