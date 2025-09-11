using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;
using TMPro;

namespace Huckleberry
{
	public class KeyboardInput : MonoBehaviour
	{
		public bool hideMouse = true;
		public List<GameObject> uiObjects;

		[Header("Visual Controls")]
		public List<GameObject> wordObjects;
		public List<Texture2D> wordTextures;
		public List<Texture2D> wordVFXTextures;

		public CanvasSelect titleCanvas;

		[Header("Audio Controls")]
		public List<AudioSource> audioSources;
		public List<AudioClip> wordAudioClips;
		public float clipDelayTime = 1.0f;

		[Header("Keyboard Controls")]
		public KeyCode animationTriggerKey = KeyCode.Space;
		public KeyCode uiToggleKey = KeyCode.Tab;
		public float triggerDelayTime = 3.0f;

		private float triggerTime;
		private int index;
		private int wordIndex;

		void Start()
		{
			if (hideMouse)
				Cursor.visible = false;

			foreach (GameObject go in uiObjects)
				go.SetActive(false);

			wordIndex = Random.Range(0, wordTextures.Count);
		}

		void Update()
		{
			// Trigger animation and audio
			if (Input.GetKeyDown(animationTriggerKey) && wordTextures.Count > 0 && (Time.time - triggerTime > triggerDelayTime))
			{
				while (!wordObjects[index].activeInHierarchy)
					IncrementIndex(ref index, wordObjects);

				int add = Random.Range(1, wordTextures.Count);
				wordIndex = (wordIndex + add) % wordTextures.Count;

				// Set texture
				wordObjects[index].GetComponent<RawImage>().texture = wordTextures[wordIndex];

				// Set VFX texture
				if (wordVFXTextures.Count > wordIndex)
					wordObjects[index].GetComponent<VisualEffect>().SetTexture("Texture", wordVFXTextures[wordIndex]);
				else
					wordObjects[index].GetComponent<VisualEffect>().SetTexture("Texture", wordTextures[wordIndex]);

				// Play animation
				wordObjects[index].GetComponent<Animator>().Play("Word Fade", 0);

				// Play sound
				if (wordAudioClips.Count > wordIndex)
					StartCoroutine(DelayedOneShot(audioSources[index], wordAudioClips[wordIndex], clipDelayTime));

				IncrementIndex(ref index, wordObjects);
				triggerTime = Time.time;
			}

			// Toggle UI
			if (Input.GetKeyDown(uiToggleKey))
			{
				foreach (GameObject go in uiObjects)
					go.SetActive(!go.activeSelf);

				if (hideMouse)
					Cursor.visible = !Cursor.visible;
			}

			// Exit
			if (Input.GetKeyDown(KeyCode.Escape))
				Application.Quit();
		}

		private void IncrementIndex<T>(ref int i, List<T> list)
		{
			i = (i + 1) % list.Count;
		}

		IEnumerator DelayedOneShot(AudioSource source, AudioClip clip, float delay)
		{
			yield return new WaitForSeconds(delay);
			source.PlayOneShot(clip);
		}
	}
}
