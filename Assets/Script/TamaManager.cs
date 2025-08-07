using System.Collections.Generic;
using Unity.Mathematics.Geometry;
using Unity.MLAgents.Integrations.Match3;
using Unity.VisualScripting;
using UnityEngine;

struct TamaEmo
{
    public float calm;
    public float hyped;
    public float lovey;
    public float alarming;
    public float annoyned;
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
    [SerializeField] SocketReceiver socketReceiver;
    [SerializeField] FrameRequester frameRequester;
    [Header("Emotion Emulator")]
    [SerializeField][Range(0, 1)] float calmDebug;
    [SerializeField][Range(0, 1)] float posDebug;
    [SerializeField][Range(0, 1)] float negDebug;
    [SerializeField][Range(0, 1)] float alarmingDebug;

    TamaEmo tamaEmo;
    private void Update()
    {
        ProcessTamaEmo();

        TransformData bounceTrans = BounceMotion(transform);
        TransformData SpinTrans = SpinMotion(transform, sphereCenter);
        transform.position = Vector3.Lerp(bounceTrans.position, SpinTrans.position, calmDebug);
        transform.forward = Vector3.Lerp(bounceTrans.forward, SpinTrans.forward, calmDebug);

        //Debug.Log(DebugEmo());
    }

    void ProcessTamaEmo()
    {
        int playerEmoCnt = socketReceiver.PlayerEmoEntries.Count;

        float happyWeightedSum = 0, negWeightedSum = 0;
        int totalPosWeight = 1, totalNegWeight = 1;
        List<string> emoTagList = new List<string>();

        // Weights: oldest = 1, newest = count (simple linear scale)
        for (int i = 0; i < playerEmoCnt; i++)
        {
            string emoTag = socketReceiver.PlayerEmoEntries[i].message;
            float emoVal = socketReceiver.PlayerEmoEntries[i].value;
            if (!emoTagList.Contains(emoTag))
                emoTagList.Add(emoTag);

            int weight = i + 1;
            if (emoTag == "Happiness")
            {
                happyWeightedSum += emoVal * weight;
                totalPosWeight += weight;
            }
            else if (emoTag == "Sadness" || emoTag == "Fear" | emoTag == "Disgust" | emoTag == "Anger")
            {
                negWeightedSum += emoVal * weight;
                totalNegWeight += weight;
            }
        }

        // normalize weighted average
        happyWeightedSum /= totalPosWeight;
        negWeightedSum /= totalNegWeight;
        tamaEmo.lovey = happyWeightedSum;
        tamaEmo.annoyned = negWeightedSum;
        tamaEmo.alarming = emoTagList.Count / 7.0f; // 7 types of emotion in total
        tamaEmo.hyped = Mathf.Clamp01(playerEmoCnt / 30.0f);
        tamaEmo.calm = 1.0f - tamaEmo.hyped;
    }


    public Vector3 sphereCenter = Vector3.zero;
    public float sphereRadius = 5f;
    public Vector3 velocity = new Vector3(1, 2, 1.5f);
    public float damping = 0.98f; // slows it down a bit each bounce
    public float accelerationStrength = 0f; // set >0 for gravity, e.g., 9.8f
    private TransformData BounceMotion(Transform trans)
    {
        Vector3 acceleration = Vector3.zero;
        // Uncomment the next line for downward gravity:
        // acceleration = Vector3.down * accelerationStrength;
        // Or center-seeking gravity:
        // acceleration = (sphereCenter - transform.position).normalized * accelerationStrength;
        TransformData t = new TransformData(trans.position, trans.forward);
        Vector3 pos = trans.position;
        velocity += acceleration * Time.deltaTime;
        pos += velocity * Time.deltaTime;

        Vector3 toCenter = pos - sphereCenter;
        if (toCenter.sqrMagnitude > sphereRadius * sphereRadius)
        {
            toCenter = toCenter.normalized * sphereRadius;
            pos = sphereCenter + toCenter;

            Vector3 normal = toCenter.normalized;
            velocity = Vector3.Reflect(velocity, normal);
            velocity *= damping;
            velocity += Random.insideUnitSphere * 0.2f;
        }
        t.position = pos;
        // Align mesh forward direction to velocity if velocity is non-zero
        if (velocity.sqrMagnitude > 0.0001f)
        {
            t.forward = -velocity.normalized;
        }

        return t;
    }


    // Axis around which the mesh spins
    public Vector3 spinAxis = Vector3.up;
    // Rotation speed in degrees per second
    public float spinSpeed = 90f;
    TransformData SpinMotion(Transform trans, Vector3 center)
    {
        float angle = spinSpeed * Time.deltaTime;
        Vector3 axis = spinAxis.normalized;
        // Get vector from center of rotation to current position
        Vector3 offset = trans.position - center;
        // Create a quaternion representing the rotation around the axis by the angle
        Quaternion rotation = Quaternion.AngleAxis(angle, axis);
        // Rotate the offset
        Vector3 rotatedOffset = rotation * offset;
        Vector3 newPosition = center + rotatedOffset;
        TransformData t = new TransformData(trans.position, trans.forward);
        t.position = newPosition;
        t.forward = Vector3.forward;
        return t;
    }


    public string DebugEmo()
    {
        return $"Calm: {tamaEmo.calm}, Hyped: {tamaEmo.hyped}, Lovey: {tamaEmo.lovey}, Alarming: {tamaEmo.alarming}, Annoyned: {tamaEmo.annoyned}";
    }
}
