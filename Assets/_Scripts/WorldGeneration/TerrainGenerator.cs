﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public BiomeGenerator biomeGenerator;
    [SerializeField] private List<Vector3Int> biomeCenters = new List<Vector3Int>();
    private List<float> temperatureNoise = new List<float>();

    [SerializeField] private NoiseSettings temperatureNoiseSettings;
    
    public DomainWarping domainWarping;
    [Tooltip("Inverse Distance Weighting")]
    public bool useIDW = true;
    
    [SerializeField]  private List<BiomeData> biomeGeneratorsData = new List<BiomeData>();

    
    
    public ChunkData GenerateChunkData(ChunkData data, Vector2Int mapSeedOffset)
    {
        BiomeGeneratorSelection biomeSelection = SelectBiomeGeneratorWeight(data.worldPos, data,false);
        // TreeData treeData = biomeGenerator.GenerateTreeData(data, mapSeedOffset);
        data.treeData = biomeSelection.biomeGenerator.GenerateTreeData(data, mapSeedOffset);
        
        for (var x = 0; x < data.chunkSize; x++)
        {
            for (var z = 0; z < data.chunkSize; z++)
            {
                biomeSelection = SelectBiomeGeneratorWeight( new Vector3Int(data.worldPos.x + x, 0, data.worldPos.z + z), data);
                data = biomeSelection.biomeGenerator.ProcessChunkColumn(data, x, z, mapSeedOffset, biomeSelection.terrainSurfaceNoise);
            }
        }

        return data;
    }

    private BiomeGeneratorSelection SelectBiomeGenerator(Vector3Int worldPos, ChunkData data, bool useDomainWarping = true)
    {
        var originalWorldPos = worldPos;
        useDomainWarping = false;
        if (useDomainWarping)
        {
            var domainOffset = Vector2Int.RoundToInt(domainWarping.GenerateDomainOffset(worldPos.x, worldPos.z));
            worldPos += new Vector3Int(domainOffset.x, 0, domainOffset.y);
        }
        
        List<BiomeSelectionHelper> biomeSelectionHelpersByDistance = GetBiomeGeneratorSelectionHelpers(worldPos);
        var generator1 = SelectBiome(biomeSelectionHelpersByDistance[0].Index);
        var generator2 = SelectBiome(biomeSelectionHelpersByDistance[1].Index);
        
        var distance = Vector3.Distance(biomeCenters[biomeSelectionHelpersByDistance[0].Index], biomeCenters[biomeSelectionHelpersByDistance[1].Index]);
        // there is something wrong with the weights distance 0 results in weight 0, what?
        var weight1 = biomeSelectionHelpersByDistance[0].Distance / distance;
        var weight2 = 1 - weight1;
        var terrainHeight1 = generator1.GetSurfaceHeightNoise(worldPos.x,worldPos.z, data.chunkHeight);
        var terrainHeight2 = generator2.GetSurfaceHeightNoise(worldPos.x,worldPos.z, data.chunkHeight);
        
        // if(Mathf.Abs(terrainHeight1 - terrainHeight2) > 15)
        // {
        //     var x = 0;
        // }

        if (worldPos.x is 123 && worldPos.z is 37 or 38)
        {
            var x = 0;
        }
        
        return new BiomeGeneratorSelection(generator1, Mathf.RoundToInt(terrainHeight1 * weight2 + terrainHeight2 * weight1));
        // return new BiomeGeneratorSelection(generator1, Mathf.RoundToInt((terrainHeight1+terrainHeight2)/2f));

    }
    
    
    // source: https://gisgeography.com/inverse-distance-weighting-idw-interpolation/
    private BiomeGeneratorSelection SelectBiomeGeneratorWeight(Vector3Int worldPos, ChunkData data, bool useDomainWarping = true)
    {
        var originalWorldPos = worldPos;
        if (useDomainWarping)
        {
            var domainOffset = Vector2Int.RoundToInt(domainWarping.GenerateDomainOffset(worldPos.x, worldPos.z));
            worldPos += new Vector3Int(domainOffset.x, 0, domainOffset.y);
        }
        
        List<BiomeSelectionHelper> biomeSelectionHelpersByDistance = GetBiomeGeneratorSelectionHelpers(worldPos);
        
        // Select the biome generators based on the temperature noise
        var generator1 = SelectBiome(biomeSelectionHelpersByDistance[0].Index);
        var generator2 = SelectBiome(biomeSelectionHelpersByDistance[1].Index);
        var generator3 = SelectBiome(biomeSelectionHelpersByDistance[2].Index);
        

        var terrainHeight1 = generator1.GetSurfaceHeightNoise(worldPos.x,worldPos.z, data.chunkHeight);
        var terrainHeight2 = generator2.GetSurfaceHeightNoise(worldPos.x,worldPos.z, data.chunkHeight);
        var terrainHeight3 = generator3.GetSurfaceHeightNoise(worldPos.x,worldPos.z, data.chunkHeight);

        if (!useIDW)
        {
            return new BiomeGeneratorSelection(generator1, terrainHeight1);
        }
        
        if(biomeSelectionHelpersByDistance[0].Distance == 0)
        {
            return new BiomeGeneratorSelection(generator1, terrainHeight1);
        }

        var distance1 = biomeSelectionHelpersByDistance[0].Distance;
        var distance2 = biomeSelectionHelpersByDistance[1].Distance;
        var distance3 = biomeSelectionHelpersByDistance[2].Distance;

        var power = 3;

        if (worldPos.x is 169 or 168 && worldPos.z is -68)
        {
            var x = 0;
        }
        
        return new BiomeGeneratorSelection(generator1, Mathf.RoundToInt(
            (
                terrainHeight1/Mathf.Pow(distance1,power) + 
                terrainHeight2/Mathf.Pow(distance2,power) +
                terrainHeight3/Mathf.Pow(distance3,power)
                )
                / 
                (1/Mathf.Pow(distance1,power) + 
                 1/Mathf.Pow(distance2,power) +
                 1/Mathf.Pow(distance3,power)
                 )
                
                ));
        
        // return new BiomeGeneratorSelection(generator1, Mathf.RoundToInt((terrainHeight1+terrainHeight2)/2f));

    }

    private BiomeGenerator SelectBiome(int index)
    {
        var temp = temperatureNoise[index];
        temp *= 4f;
        foreach (var data in biomeGeneratorsData)
        {
            if(temp >= data.temperatureStartThreshold && temp <= data.temperatureEndThreshold)
            {
                return data.biomeTerrainGenerator;
            }
        }

        Debug.LogError("No biome found for temperature: " + temp);
        return biomeGeneratorsData[0].biomeTerrainGenerator;
    }

    private List<BiomeSelectionHelper> GetBiomeGeneratorSelectionHelpers(Vector3Int pos)
    {
        pos.y = 0;
        return GetClosestBiomeIndex(pos);
    }

    private List<BiomeSelectionHelper> GetClosestBiomeIndex(Vector3Int pos)
    {
        return biomeCenters.Select((center, index) => 
        new BiomeSelectionHelper
        {
            Index = index,
            Distance = Vector3.Distance(pos, center)
        }).OrderBy(helper => helper.Distance).Take(4).ToList();
    }
    
    private struct BiomeSelectionHelper
    {
        public int Index;
        public float Distance;
    }

    public void GenerateBiomePoints(Vector3 playerPos, int renderDistance, int chunkSize, Vector2Int mapSeedOffset)
    {
        biomeCenters = new List<Vector3Int>();
        biomeCenters = BiomeCenterFinder.CalculateBiomeCenters(playerPos, renderDistance, chunkSize);

        var originamplitude = domainWarping.amplitude;
        domainWarping.amplitude.x = Mathf.RoundToInt(domainWarping.amplitude.x*3.3f);
        domainWarping.amplitude.y = Mathf.RoundToInt(domainWarping.amplitude.y*3.3f);
        for (var i = 0; i < biomeCenters.Count; i++)
        {
            var domainWarpingOffset = domainWarping.GenerateDomainOffsetInt(biomeCenters[i].x, biomeCenters[i].z);
            biomeCenters[i] += new Vector3Int(domainWarpingOffset.x, 0, domainWarpingOffset.y);
        }
        domainWarping.amplitude = originamplitude;
        
        temperatureNoise = CalculateTemperatureNoise(biomeCenters, mapSeedOffset);
    }

    private List<float> CalculateTemperatureNoise(List<Vector3Int> positions, Vector2Int mapSeedOffset)
    {
        temperatureNoiseSettings.worldSeedOffset = mapSeedOffset;
        return positions.Select(pos => MyNoise.OctavePerlin(pos.x,pos.z,temperatureNoiseSettings)).ToList();
    }

    private void OnDrawGizmos()
    {
        if (Selection.activeObject != gameObject) return;
        Gizmos.color = Color.blue;
        
        foreach (var biomeCenter in biomeCenters)
        {
            Gizmos.DrawLine(biomeCenter, biomeCenter + Vector3.up*255f);
        }
    }
}

public class BiomeGeneratorSelection
{
    public BiomeGenerator biomeGenerator = null;
    public int? terrainSurfaceNoise = null;
    
    public BiomeGeneratorSelection(BiomeGenerator biomeGenerator, int? terrainSurfaceNoise = null)
    {
        this.biomeGenerator = biomeGenerator;
        this.terrainSurfaceNoise = terrainSurfaceNoise;
    }
}

[Serializable]
public struct BiomeData
{
    [Range(0f, 4f)] public float temperatureStartThreshold, temperatureEndThreshold;
    public BiomeGenerator biomeTerrainGenerator;
}