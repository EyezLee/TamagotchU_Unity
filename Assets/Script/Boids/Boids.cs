using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EasyButtons;
using System.Runtime.InteropServices;

public struct BoidAgent
{
    Vector2 pos;
    Vector2 vel;
    Vector2 acc;
    float spd;
    float m; // mass
};

public class Boids : Simulation
{
    [Header("General Setting")]
    [SerializeField] ComputeShader cs;
    //[SerializeField] int SimulationManager.ResolutionX = 512;
    //[SerializeField] int SimulationManager.ResolutionY = 512;
    [SerializeField] int stepPerFrame = 1;
    [SerializeField] int frameMod = 9;

    [Header("Boids Setting")]
    [SerializeField] [Range(1, 1000000)] int agentCount = 1000;
    [SerializeField] [Range(0, 1)] float diffuseFactor = 0.8f;
    [SerializeField] [Range(0, 1)] float evaporate = 0.5f;
    [SerializeField] [Range(1, 100)] int range = 10;
    [SerializeField] [Range(0, 10)] float alignMaxForce;
    [SerializeField] [Range(0, 10)] float cohesionMaxForce;
    [SerializeField] [Range(0, 10)] float separationMaxForce;
    [SerializeField] [Range(0, 1)] float normalStrength;
    [SerializeField] [Range(0, 1)] float boidsWeight;


    [Header("Sensors Setting")]
    [SerializeField] [Range(0, 2)] float sensorWeight = 0.5f;
    [SerializeField] [Range(0, 20)] int sensorOffset = 9;
    [SerializeField] [Range(0, 90)] float sensorAngle = 22.5f;
    [SerializeField] [Range(0, 90)] float rotateAngle = 45.0f;
    [SerializeField] [Range(0, 1)] float sensorSwirl = 0.2f;


    [Header("Fluid Influence")]
    [SerializeField] [Range(0, 3)] float fluidWeight;

    // compute shader related properties
    ComputeBuffer agentBuffer, agentPrevBuffer;
    int resetKernel, moveAgentKernel, write2DataKernel, diffuseTrailKernel;
    int bufferGroupThreads = 64;
    int textureGroupThreadsX = 32;
    int textureGroupThreadsY = 32;
    int bufferDispatchGroups { get { return Mathf.CeilToInt((float)agentCount / bufferGroupThreads); } }
    int textureDispatchGroupsX { get { return Mathf.CeilToInt((float)SimulationManager.ResolutionX / textureGroupThreadsX); } }
    int textureDispatchGroupsY { get { return Mathf.CeilToInt((float)SimulationManager.ResolutionY / textureGroupThreadsY); } }

    RenderTexture dataMap, trailMap;
    RenderTexture fluidMap;

    [Button]
    public override void Reset()
    {
        // bind kernels
        resetKernel = cs.FindKernel("ResetKernel");
        moveAgentKernel = cs.FindKernel("MoveAgentKernel");
        write2DataKernel = cs.FindKernel("Write2DataKernel");
        diffuseTrailKernel = cs.FindKernel("DiffuseTrailKernel");


        // allocate buffer memory
        agentBuffer?.Release();
        agentBuffer = new ComputeBuffer(agentCount, Marshal.SizeOf(new BoidAgent()));
        agentPrevBuffer?.Release();
        agentPrevBuffer = new ComputeBuffer(agentCount, Marshal.SizeOf(new BoidAgent()));

        // prepare texture pointers
        dataMap = CreateTexure(RenderTextureFormat.ARGBFloat);
        trailMap = CreateTexure(RenderTextureFormat.ARGBFloat);
        fluidMap = SimulationManager.Maps["vectorField"];


        // dispatch reset kernel    
        ResetKernel();
        Write2DataKernel();
    }

    void ResetKernel()
    {
        cs.SetInt("_AgentCount", agentCount);
        cs.SetInt("_ResX", SimulationManager.ResolutionX);
        cs.SetInt("_ResY", SimulationManager.ResolutionY);
        cs.SetFloat("_Time", Time.time);

        cs.SetBuffer(resetKernel, "_AgentBuffer", agentBuffer);
        cs.SetBuffer(resetKernel, "_AgentPrevBuffer", agentPrevBuffer);
        cs.SetTexture(resetKernel, "_DataMap", dataMap);
        cs.SetTexture(resetKernel, "_TrailMap", trailMap);
        cs.Dispatch(resetKernel, textureDispatchGroupsX, textureDispatchGroupsY, 1);
        this.GetComponent<Renderer>().material.SetTexture("_TrailMap", dataMap);
    }

    void Write2DataKernel()
    {
        cs.SetBuffer(write2DataKernel, "_AgentBuffer", agentBuffer);
        cs.SetTexture(write2DataKernel, "_DataMap", dataMap);
        cs.Dispatch(write2DataKernel, bufferDispatchGroups, 1, 1);
    }

    void MoveAgentKernel()
    {
        cs.SetInt("_Range", range);
        cs.SetFloat("_AlignMaxForce", alignMaxForce);
        cs.SetFloat("_CohesionMaxForce", cohesionMaxForce);
        cs.SetFloat("_SeparationMaxForce", separationMaxForce);
        cs.SetFloat("_BoidsWeight", boidsWeight);

        cs.SetTexture(moveAgentKernel, "_FluidMap", fluidMap);
        cs.SetFloat("_FluidWeight", fluidWeight);

        cs.SetInt("_SO", sensorOffset);
        cs.SetFloat("_SensorWeight", sensorWeight);
        cs.SetFloat("_RA", rotateAngle);
        cs.SetFloat("_SA", sensorAngle);
        cs.SetFloat("_Swirl", sensorSwirl);
        cs.SetBuffer(moveAgentKernel, "_AgentBuffer", agentBuffer);
        cs.SetBuffer(moveAgentKernel, "_AgentPrevBuffer", agentPrevBuffer);
        cs.SetTexture(moveAgentKernel, "_DataMap", dataMap);
        cs.SetTexture(moveAgentKernel, "_TrailMap", trailMap);
        cs.Dispatch(moveAgentKernel, bufferDispatchGroups, 1, 1);
    }

    void DiffuseTrailKernel()
    {
        cs.SetFloat("_DiffuseFactor", diffuseFactor);
        cs.SetFloat("_Evaporate", evaporate);
        cs.SetTexture(diffuseTrailKernel, "_DataMap", dataMap);
        cs.SetTexture(diffuseTrailKernel, "_TrailMap", trailMap);
       // cs.SetTexture(diffuseTrailKernel, "_FluidMap", fluidMap);

        cs.Dispatch(diffuseTrailKernel, textureDispatchGroupsX, textureDispatchGroupsY, 1);
    }
    
    [Button]
    public override void Step()
    {
        MoveAgentKernel();
        Write2DataKernel();
        SwapBuffer();
        DiffuseTrailKernel();
        GetComponent<Renderer>().material.SetTexture("_TrailMap", trailMap);
        GetComponent<Renderer>().material.SetTexture("_FluidMap", fluidMap);
        GetComponent<Renderer>().material.SetInt("_ResX", SimulationManager.ResolutionX);
        GetComponent<Renderer>().material.SetInt("_ResY", SimulationManager.ResolutionY);
        GetComponent<Renderer>().material.SetFloat("_NormalStrength", normalStrength);
    }

    void SwapBuffer()
    {
        // copy agent buffer to buffer copy
        (agentPrevBuffer, agentBuffer) = (agentBuffer, agentPrevBuffer);
    }

    //private void Start()
    //{
    //    Reset();
    //}

    //private void Update()
    //{
    //    if (Time.frameCount % frameMod == 0)
    //    {
    //        for (int i = 0; i < stepPerFrame; i++)
    //        {
    //            Step();
    //        }
    //    }
    //}

    // ----------------------------utility functions------------------------------
    private RenderTexture CreateTexure(RenderTextureFormat format)
    {
        RenderTexture tex = new RenderTexture(SimulationManager.ResolutionX, SimulationManager.ResolutionY, 1, format);
        tex.enableRandomWrite = true;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Point;
        tex.useMipMap = false;
        tex.Create();

        return tex;
    }

    // release compute buffer
    private void OnDisable()
    {
        agentBuffer?.Release();
    }

    private void OnDestroy()
    {
        agentBuffer?.Release();
    }

    private void OnEnable()
    {
        agentBuffer?.Release();
    }
}
