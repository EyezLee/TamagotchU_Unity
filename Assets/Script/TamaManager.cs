using AfterimageSample;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics.Geometry;
using Unity.MLAgents.Integrations.Match3;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.VFX;

public enum EmoTag
{
    Sadness,
    Happiness,
    Surprise,
    Fear,
    Disgust,
    Anger,
    Neutral
}

public struct EmotionStatus
{
    public string currEmoTag;
    public float currEmoVal;
    public Vector4 emotionDimension;

    public EmotionStatus(string currEmoTag, float currEmoVal, Vector4 overallEmo)
    {
        this.currEmoTag = currEmoTag;
        this.currEmoVal = currEmoVal;
        this.emotionDimension = overallEmo;
    }
}

public struct TransformData
{
    public Vector3 position;
    public Vector3 forward;

    public TransformData(Vector3 pos, Vector3 fwd)
    {
        position = pos;
        forward = fwd;
    }
}

public class TamaManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] public string tamapagerIP = "127.0.0.1";
    [SerializeField] public SocketReceiver socketReceiver;
    [SerializeField] public FrameRequester frameRequester;
    [SerializeField] GameObject bubble;
    [SerializeField] GameObject tamaBg;
    [SerializeField] SkinnedMeshRenderer tamaRenderer;
    [SerializeField] GameObject[] alarms;

    [Header("Movement Settings")]
    [SerializeField] float radiansPerSecond = 1f;
    [SerializeField] Transform cameraOrigin;
    [SerializeField] public Bounds swimBounds = new Bounds(Vector3.zero, new Vector3(10f, 3f, 10f));
    [SerializeField] float swimSpeed = 1f;
    [SerializeField] float turnSpeed = 30f;
    [SerializeField] Transform cave; 

    [Header("Audio Source")]
    [SerializeField] List<AudioSource> sfxList = new List<AudioSource>();

    [Header("Emotion Emulator")]
    [SerializeField] bool debugMode = false;
    [SerializeField][Range(0, 1)] float hypeDebug;
    [SerializeField][Range(0, 1)] float posDebug;
    [SerializeField][Range(0, 1)] float negDebug;
    [SerializeField][Range(0, 1)] float alarmingDebug;
    [SerializeField] KeyCode testKey;

    /*    [Header("Spin Motion")]
        // Axis around which the mesh spins
        //public Vector3 spinAxis = Vector3.up;
        // Rotation speed in degrees per second
        public float spinSpeed = 90f;

        [Header("Bounce Motion")]
        public Vector3 sphereCenter = Vector3.zero;
        public float sphereRadius = 5f;
        public Vector3 velocity = new Vector3(1, 2, 1.5f);
        public float damping = 0.98f; // slows it down a bit each bounce
        public float accelerationStrength = 0f; // set >0 for gravity, e.g., 9.8f*/

    float faceCamDist = float.MaxValue;
    private Vector3 swimDirection;
    Vector3 velocity;
    float damping = 0.98f; // slows it down a bit each bounce

    private List<TimedEntry> cachedMetaDataList = new List<TimedEntry>();
    private EmotionStatus tamaEmo = new EmotionStatus(EmoTag.Neutral.ToString(), 0, new Vector4(0, 0, 0, 0));
    Vector3 boundsMin;
    Vector3 boundsMax;

    private float puffBlendTimer = 0f;
    private bool isPuffBlend = false;

    private Coroutine mouthCoroutine;
    private Coroutine puffCoroutine;
    int mouthShapekeyIndex = 0;
    int bodyShapekeyIndex = 1;

    float GetAnimateValue(float val)
    {
        return Mathf.Sin(Time.fixedTime * Mathf.Rad2Deg * val) * 0.5f + 0.5f; // 0-1
    }

    private void Start()
    {
        // Start swimming horizontally in object's forward direction
        swimDirection = -transform.forward;
        swimDirection.y = 0;
        swimDirection.Normalize();

        boundsMin = swimBounds.min + swimBounds.center;
        boundsMax = swimBounds.max + swimBounds.center;
        // sound
        /*        foreach(var a in sfxList)
                {
                    a.loop = true;
                    a.Play();
                }*/
    }
    private void Update()
    {
        ProcessTamaData();

        InstantEmotionFeedback();
        ContiniousEmotionFeedback();
        //FixToBound();

        /*        float hypeVal = debugMode ? hypeDebug : tamaEmo.hyped;
                float calmVal = 1 - hypeVal;
                float posVal = debugMode ? posDebug : tamaEmo.lovey;
                float alarmVal = debugMode ? alarmingDebug : tamaEmo.alarming;
                float negVal = debugMode ? negDebug : tamaEmo.annoyned;

                float bodyLow = 0, bodyHigh = 100, bodyLerp = negVal;
                float mouthLow = 0, mouthHigh = 100, mouthLerp = (posVal + negVal) /2.0f;

                // calm <-----> hype
                TransformData bounceTrans = BounceMotion(transform);
                TransformData SpinTrans = SpinMotion(transform, sphereCenter);
                transform.position = Vector3.Lerp(bounceTrans.position, SpinTrans.position, hypeVal);
                transform.forward = Vector3.Lerp(bounceTrans.forward, SpinTrans.forward, hypeVal);
                if (bubble)
                {
                    float scale = Mathf.Lerp(0.15f, 1.0f, calmVal);
                    bubble.transform.localScale = new Vector3(scale, scale, scale);
                    bubble.transform.position = transform.position + new Vector3(0, 0, -0.5f * hypeVal);
                }
                if(GetComponent<AfterimageRenderer>() != null)
                {
                    GetComponent<AfterimageRenderer>().Duration = (int)Mathf.Lerp(1, 125, hypeVal);
                }
                calmAudio.volume = calmVal;
                hypeAudio.volume = hypeVal * 0.85f;
                hypeAudio.pitch = hypeVal * 1;

                if (!debugMode && faceCamDist <=20) frameRequester.HumanBorn(transform.position, tamapagerIP); // spawn human fish unless debug mode
                //Debug.Log(faceCamDist);

                // happy
                if (posVal > 0.56)
                {
                    mouthHigh = 100 * posVal;
                    mouthLow = 0;
                }
                posAudio.volume = Mathf.Pow(posVal, 4);
                posAudio.pitch = Mathf.Pow(posVal * 2, 2);

                if(Input.GetKeyDown(testKey))
                {
                    frameRequester.HumanBorn(transform.position, tamapagerIP);
                }

                // alarm
                for (int i = 0; i < alarms.Length; i++)
                {
                    if (alarms[i])
                    {
                        // alarm material
                        float alarmEmi = (Mathf.Sin(Mathf.Rad2Deg * Time.fixedTime) + 1) * 10 * alarmVal;
                        alarms[i].GetComponent<MeshRenderer>().material.SetFloat("_Emission", Mathf.Lerp(1, alarmEmi, alarmVal));
                    }
                }
                Material skyboxMat = RenderSettings.skybox;
                if (skyboxMat)
                {
                    skyboxMat.SetFloat("_Speed", Mathf.Lerp(-0.1f, 0.45f, alarmVal));
                    skyboxMat.SetFloat("_LCDScale", Mathf.Lerp(65.0f, 1.0f, alarmVal));
                    skyboxMat.SetFloat("_LEDScale", Mathf.Lerp(5.0f, 95.0f, alarmVal));
                    skyboxMat.SetFloat("_VoronoiScale", Mathf.Lerp(7.0f, 0.0f, alarmVal));
                }
                alarmAudio.volume = alarmVal;
                alarmAudio.pitch = alarmVal * 2;

                // neg: shapekeys, material
                mouthLow = -50 * negVal;
                bodyLow = 100 * negVal;
                negAudio.volume = negVal;

                // shapekeys
                bodyLerp = negVal;
                tamaRenderer.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(bodyShapekeyIndex, Mathf.Lerp(bodyLow, bodyHigh, bodyLerp));
                tamaBg.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(bodyShapekeyIndex, Mathf.Lerp(bodyLow, bodyHigh, bodyLerp));
                mouthLerp = GetAnimateValue(posVal + negVal);
                tamaRenderer.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(mouthShapekeyIndex, Mathf.Lerp(mouthLow, mouthHigh, mouthLerp));
                tamaBg.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(mouthShapekeyIndex, Mathf.Lerp(mouthLow, mouthHigh, posVal - negVal));
        */
        //Debug.Log(DebugEmo());
    }

    private void FixToBound()
    {
        throw new NotImplementedException();
    }

    private void ContiniousEmotionFeedback()
    {
        // positive: shading
        // negative: shading + bubble
        // neutral: shading

        // distance based behavior
        // swim closer to camera, more bubbles
        if (!debugMode && faceCamDist <= 20) frameRequester.HumanBorn(transform.position, tamapagerIP); // spawn human fish unless debug mode
    }

    private void InstantEmotionFeedback()
    {
        // happy: aiioui swirl
        if (tamaEmo.currEmoTag == EmoTag.Happiness.ToString())
        {
            PositiveSpin();
            // sound
            PlayCurrSFX(0, tamaEmo.currEmoVal * 2, 1);
        }

        // neutral: swim & bubble
        if (tamaEmo.currEmoTag == EmoTag.Neutral.ToString())
        {
            NeutralSwim();
            PlayCurrSFX(1, 1, 1);
        }

        // angry & disguist: blow-up
        float currPuffKey = tamaRenderer.GetComponent<SkinnedMeshRenderer>().GetBlendShapeWeight(bodyShapekeyIndex);
        if (tamaEmo.currEmoTag == EmoTag.Anger.ToString() || tamaEmo.currEmoTag == EmoTag.Disgust.ToString())
        {
            AnimateBlendShape(puffCoroutine, bodyShapekeyIndex, currPuffKey, 100, 1);
            PlayCurrSFX(2, 1, 1);
        }
        else AnimateBlendShape(puffCoroutine, bodyShapekeyIndex, currPuffKey, 0, 1);

        // surprise: open mouth
        float currMouthKey = tamaRenderer.GetComponent<SkinnedMeshRenderer>().GetBlendShapeWeight(mouthShapekeyIndex);
        if (tamaEmo.currEmoTag == EmoTag.Surprise.ToString())
        {
            MoveAndRotateTowards(cameraOrigin.position, transform.position - cameraOrigin.position);
            AnimateBlendShape(mouthCoroutine, mouthShapekeyIndex, currMouthKey, 125, 1);
            PlayCurrSFX(4, 1, 1);
        }
        else AnimateBlendShape(mouthCoroutine, mouthShapekeyIndex, currMouthKey, -10, 1);

        // fear & sad: hide in cave
        if (tamaEmo.currEmoTag == EmoTag.Sadness.ToString() || tamaEmo.currEmoTag == EmoTag.Fear.ToString())
        {
            MoveAndRotateTowards(cave.position, transform.position - cameraOrigin.position);
            PlayCurrSFX(3, 1, 1);
        }
    }

    void PlayCurrSFX(int playID, float pitch, float volume)
    {
        for(int i = 0; i < sfxList.Count; i++)
        {
            AudioSource currAudio = sfxList[i];

            if (i == playID)
            {
                currAudio.pitch = pitch;
                currAudio.volume = volume;
                // If not playing, start playing with loop disabled (let clip finish)
                if (!currAudio.isPlaying)
                {
                    currAudio.Play();
                }
            }
            else 
            {
                currAudio.Stop();
            }
        }
    }

    void ProcessTamaData()
    {
        socketReceiver.GetClientDataList(tamapagerIP, cachedMetaDataList);

        int count = cachedMetaDataList.Count;
        tamaEmo.emotionDimension = Vector4.zero;

        for (int i = 0; i < count; i++)
        {
            string tag = cachedMetaDataList[i].tag;
            float val = cachedMetaDataList[i].value;
            if (tag == EmoTag.Neutral.ToString()) tamaEmo.emotionDimension.x += val; // positive 
            else if (tag == EmoTag.Happiness.ToString() || tag == EmoTag.Surprise.ToString()) tamaEmo.emotionDimension.y += val;
            else tamaEmo.emotionDimension.z += val;
        }

        tamaEmo.emotionDimension /= count; // normalize

        if (count > 0)
        {
            var latestData = cachedMetaDataList.Last();
            faceCamDist = latestData.dist;
            tamaEmo.currEmoTag = latestData.tag;
            tamaEmo.currEmoVal = latestData.value;
        }
        Debug.Log(DebugEmo());
    }

    private void NeutralSwim()
    {
        Vector3 acceleration = Vector3.zero;
        // Uncomment either gravity or center-seeking acceleration as needed
        // acceleration = Vector3.down * accelerationStrength;  // Downward gravity
        // acceleration = (swimBounds.center - transform.position).normalized * accelerationStrength;  // Toward center

        Vector3 pos = transform.position;

        // Apply acceleration to velocity
        velocity += acceleration * Time.deltaTime;

        // Update position by velocity
        pos += velocity * Time.deltaTime;
        bool bounced = false;

        if (pos.x < boundsMin.x)
        {
            pos.x = boundsMin.x;
            velocity.x = Mathf.Abs(velocity.x);  // Bounce right
            bounced = true;
        }
        else if (pos.x > boundsMax.x)
        {
            pos.x = boundsMax.x;
            velocity.x = -Mathf.Abs(velocity.x); // Bounce left
            bounced = true;
        }

        if (pos.y < boundsMin.y)
        {
            pos.y = boundsMin.y;
            velocity.y = Mathf.Abs(velocity.y);  // Bounce up
            bounced = true;
        }
        else if (pos.y > boundsMax.y)
        {
            pos.y = boundsMax.y;
            velocity.y = -Mathf.Abs(velocity.y); // Bounce down
            bounced = true;
        }

        if (pos.z < boundsMin.z)
        {
            pos.z = boundsMin.z;
            velocity.z = Mathf.Abs(velocity.z);  // Bounce forward
            bounced = true;
        }
        else if (pos.z > boundsMax.z)
        {
            pos.z = boundsMax.z;
            velocity.z = -Mathf.Abs(velocity.z); // Bounce backward
            bounced = true;
        }

        // Apply damping on bounce to simulate energy loss
        if (bounced)
        {
            velocity *= damping;

            // Add some random noise to velocity for more natural movement
            velocity += UnityEngine.Random.insideUnitSphere * 0.5f;
        }

        transform.position = pos;

        // Align forward direction opposite to velocity (fish points opposite direction to swim vector)
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.forward = -velocity.normalized;
        }
    }

    void PositiveSpin()
    {
        Vector3 normalizedAxis = transform.up;
        float deltaRadians = radiansPerSecond * Time.deltaTime;
        Quaternion deltaRotation = Quaternion.AngleAxis(deltaRadians * Mathf.Rad2Deg, normalizedAxis);
        transform.rotation = deltaRotation * transform.rotation;
        // Calculate angular velocity vector
        Vector3 angularVelocityVector = normalizedAxis * radiansPerSecond;
        if (GetComponent<Rigidbody>())
        {
            GetComponent<Rigidbody>().angularVelocity = angularVelocityVector;
        }

    }

    void MoveAndRotateTowards(Vector3 target, Vector3 directionToFace)
    {
        float slowingRadius = 1f;
        float maxSpeed = 6f;
        float maxForce = 10f;
        float rotationSpeed = 7f;

        // Move towards target
        Vector3 desired = target - transform.position;
        float distance = desired.magnitude;

        if (distance > 0.1f)
        {
            desired.Normalize();

            if (distance < slowingRadius)
                desired *= maxSpeed * (distance / slowingRadius);
            else
                desired *= maxSpeed;

            Vector3 steer = desired - velocity;
            steer = Vector3.ClampMagnitude(steer, maxForce);

            velocity = Vector3.ClampMagnitude(velocity + steer * Time.deltaTime, maxSpeed);
            Vector3 newPos = transform.position + velocity * Time.deltaTime;

            // Clamp position to within bounds
            newPos.x = Mathf.Clamp(newPos.x, boundsMin.x, boundsMax.x);
            newPos.y = Mathf.Clamp(newPos.y, boundsMin.y, boundsMax.y);
            newPos.z = Mathf.Clamp(newPos.z, boundsMin.z, boundsMax.z);

            transform.position = newPos;
        }
        else
        {
            velocity = Vector3.zero;
        }

        // Rotate towards given direction
        if (directionToFace.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToFace.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void AnimateBlendShape(Coroutine bs, int shapekeyIndex, float startValue, float endValue, float duration)
    {
        if (bs != null)
        {
            StopCoroutine(bs);
        }
        bs = StartCoroutine(BlendShapeCoroutine(shapekeyIndex, startValue, endValue, duration));
    }

    private IEnumerator BlendShapeCoroutine(int shapekeyIndex, float startValue, float endValue, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Interpolate the blend shape value
            float currentValue = Mathf.Lerp(startValue, endValue, t);
            tamaRenderer.GetComponent<SkinnedMeshRenderer>().SetBlendShapeWeight(shapekeyIndex, currentValue);

            yield return null;
        }
    }

    public string DebugEmo()
    {
        // return $"Calm: {tamaEmo.neutral}, Hyped: {tamaEmo.hyped}, Lovey: {tamaEmo.lovey}, Alarming: {tamaEmo.alarming}, Annoyned: {tamaEmo.annoyned}";
        return $"currEmoTag: {tamaEmo.currEmoTag}, value: {tamaEmo.currEmoVal}, dimension: {tamaEmo.emotionDimension}";
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(swimBounds.center, swimBounds.size);
    }
}
