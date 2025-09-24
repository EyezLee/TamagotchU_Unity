using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EasyButtons;
using System;

public class SimulationManager : MonoBehaviour
{
    static SimulationManager SM;

    [SerializeField] int resolutionX;
    public static int ResolutionX => SM.resolutionX;

    [SerializeField] int resolutionY;
    public static int ResolutionY => SM.resolutionY;

    List<Simulation> simulations = new List<Simulation>();
    public static List<Simulation> Simulations => SM.simulations;

    Dictionary<string, RenderTexture> maps = new Dictionary<string, RenderTexture>();
    public static Dictionary<string, RenderTexture> Maps => SM.maps;

    [SerializeField] List<TextureProperties> texProperties = new List<TextureProperties>(); 

    private void Awake()
    {
        SM = this;

        simulations = FindObjectsOfType<Simulation>().ToList();
    }

    private void Start()
    {
        Reset();
    }

    private void Update()
    {
        Step();
    }

    [Button]
    private void Reset()
    {
        CreateTextures();
        foreach (var simu in simulations) simu.Reset();
    }

    [Button]
    void Step()
    {
        foreach (var simu in simulations) simu.Step();
    }

    void CreateTextures()
    {
        foreach (var tex in texProperties)
        {
            var rt = CreateTexure(tex.Format, tex.Filter);
            maps[tex.Name] = rt;
        }
    }

    private RenderTexture CreateTexure(RenderTextureFormat format, FilterMode filter)
    {
        RenderTexture tex = new RenderTexture(SimulationManager.ResolutionX, SimulationManager.ResolutionY, 1, format);
        tex.enableRandomWrite = true;
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = filter;
        tex.useMipMap = false;
        tex.Create();

        return tex;
    }
}

[Serializable]
public struct TextureProperties
{
    public string Name;
    public RenderTextureFormat Format;
    public FilterMode Filter;
}
