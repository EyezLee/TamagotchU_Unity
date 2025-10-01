using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FramerateLimiter : MonoBehaviour
{
    public float targetFramerate = 60;
    public bool setFramerateByVsync = false;
    public bool setFramerateLimit = false;
    public vSync vSyncCount = vSync.off;


    public enum vSync {off,vSync1,vSync2,vSync3,vSync4};
    
    // Start is called before the first frame update
    void Start()
    {

        if (setFramerateByVsync)
            vSyncCount = (vSync)(Screen.currentResolution.refreshRateRatio.value / targetFramerate);
        else if (setFramerateLimit)
            Application.targetFrameRate = (int) targetFramerate;
        
        QualitySettings.vSyncCount = (int) vSyncCount;      
            
        
    }

    private void OnValidate()
    {
        Start();
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
