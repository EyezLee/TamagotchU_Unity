using System.Collections.Generic;
using Unity.MLAgents.Integrations.Match3;
using UnityEngine;

struct TamaEmo
{
    public float calm;
    public float hyped;
    public float lovey;
    public float alarming;
    public float annoyned;
}

public class TamaManager : MonoBehaviour
{
    [SerializeField] SocketReceiver socketReceiver;
    [SerializeField] FrameRequester frameRequester;

    TamaEmo tamaEmo;
    private void Update()
    {
        ProcessTamaEmo();
        Debug.Log(DebugEmo());
    }

    void ProcessTamaEmo()
    {
        int playerEmoCnt = socketReceiver.PlayerEmoEntries.Count;

        float happyWeightedSum = 0, negWeightedSum = 0;
        int totalPosWeight = 0, totalNegWeight = 0;
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
        tamaEmo.hyped = Mathf.Clamp01(socketReceiver.PlayerEmoEntries.Count / 30.0f);
        tamaEmo.calm = 1.0f - tamaEmo.hyped;
    }

    public string DebugEmo()
    {
        return $"Calm: {tamaEmo.calm}, Hyped: {tamaEmo.hyped}, Lovey: {tamaEmo.lovey}, Alarming: {tamaEmo.alarming}, Annoyned: {tamaEmo.annoyned}";
    }
}
