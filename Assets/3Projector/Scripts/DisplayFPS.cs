using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.Mathematics;

public class DisplayFPS : MonoBehaviour
{

    private float fps = 0;
    public TextMeshProUGUI  txt;
    private float vel = 0;

    // Start is called before the first frame update
    void Start()
    {


    }

    // Update is called once per frame
    void Update()
    {
        fps = Mathf.SmoothDamp(fps, 1 / Time.deltaTime, ref vel, 1.0f);
        txt.text = Mathf.Round(fps).ToString() ;

    }
}
