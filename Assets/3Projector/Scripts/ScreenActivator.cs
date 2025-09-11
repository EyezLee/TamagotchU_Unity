using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenActivator : MonoBehaviour
{

	public Camera camera1;
	public Camera camera2;
	public Camera camera3;
	
	[Tooltip("Number of displays to skip. Set to 0 to start at first display, 1 to skip to the second, etc")] public int skipFirst = 0;

	public bool hideMouse = true;
	[Tooltip("set to 0 to disable, otherwise this will resize the full screen after x seconds to ensure the screen size is correct")] public float delayedReszize = 0f;

	// Use this for initialization
	void Start()
	{
		SetDisplays();


		if (hideMouse)
		{
			Cursor.visible = false;
		}
		/*
		for (int i = 0; i < Display.displays.Length; i++)
		{
			Display.displays[i].Activate(,);
			//Display.displays[i].SetRenderingResolution(Display.displays[i].systemWidth, Display.displays[i].systemHeight);
		}
		*/
		if (delayedReszize > 0)
		{
			Screen.fullScreen = false;
			StartCoroutine(delayedScreenActivation(delayedReszize));
		}
		else
		{
			ActivateDisplays();
		}
			
	}

	IEnumerator delayedScreenActivation(float delay)
	{
		yield return new WaitForSeconds(delay);

		Vector2Int resolution = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
		Screen.fullScreen = true;

		ActivateDisplays();

		yield return new WaitForSeconds(0.1f);
		Screen.SetResolution(resolution.x, resolution.y, true);
		Debug.Log("resize to " + resolution.x + "," + resolution.y);

	}

	void Update()
	{
		

		/*
		if (Input.GetKeyDown(KeyCode.LeftBracket))
		{
			
			Camera temp = camera1;
			camera1 = camera2;
			camera2 = camera3;
			camera3 = temp;
			

			SetDisplays();
		}
		else if (Input.GetKeyDown(KeyCode.RightBracket))
		{
			
			Camera temp = camera3;
			camera3 = camera2;
			camera2 = camera1;
			camera1 = temp;
			

			SetDisplays();
		}
		*/
	}

	void SetDisplays()
	{
		if(camera1)
			camera1.enabled = false;

		if(camera2)
			camera2.enabled = false;

		if(camera3)
			camera3.enabled = false;

		if (Display.displays.Length > skipFirst)
		{
			//set and activate up to 3 displays and set resolution to windows system resolution
			if (Display.displays.Length >= skipFirst + 1 && camera1 != null)
			{
				camera1.targetDisplay = skipFirst;
				camera1.enabled = true;
				//Display.displays[1].Activate();
				//Display.displays[1].SetRenderingResolution(Display.displays[1].systemWidth, Display.displays[1].systemHeight);
			}

			if (Display.displays.Length >= skipFirst + 2 && camera2 != null)
			{
				camera2.targetDisplay = skipFirst + 1;
				camera2.enabled = true;
				//Display.displays[2].Activate();
				//Display.displays[2].SetRenderingResolution(Display.displays[2].systemWidth, Display.displays[2].systemHeight);
			}

			if (Display.displays.Length >= skipFirst + 3 && camera3 != null)
			{
				camera3.targetDisplay = skipFirst + 2;
				camera3.enabled = true;
				//Display.displays[3].Activate();
				//Display.displays[3].SetRenderingResolution(Display.displays[3].systemWidth, Display.displays[3].systemHeight);
			}


		}
	}

	void ActivateDisplays()
	{
		for(int i = skipFirst; i< Display.displays.Length; i++)
		{
			Display.displays[i].Activate();
		}
	}
}

