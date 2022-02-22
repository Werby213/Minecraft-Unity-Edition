﻿using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

public class World : MonoBehaviour
{
    public int renderDistance = 6;
    public int chunkSize = 16; // 16x16 chunks
    public int chunkHeight = 100; // 100 blocks high
    public GameObject chunkPrefab;
    
    public TerrainGenerator terrainGenerator;
    public Vector2Int mapOffset;

    public UnityEvent OnWorldCreated;
    public UnityEvent OnNewChunksGenerated;

    public WorldData worldData { get; private set; }
    public bool IsWorldCreated { get; private set; }
    
    private Stopwatch stopwatch = new Stopwatch();


    private void OnValidate()
    {
        worldData = new WorldData
        {
            chunkHeight = this.chunkHeight,
            chunkSize = this.chunkSize,
            chunkDataDict = new Dictionary<Vector3Int, ChunkData>(),
            chunkDict = new Dictionary<Vector3Int, ChunkRenderer>()
        };
    }

    public async void GenerateWorld()
    {
        await GenerateWorld(Vector3Int.zero);
    }

    private async Task GenerateWorld(Vector3Int position)
    {
        if (!Application.isPlaying)
        {
            stopwatch.Start();
        }
        WorldGenerationData worldGenerationData = await Task.Run(() => GetPositionsInRenderDistance(position));

        foreach (var pos in worldGenerationData.chunkPositionsToRemove)
        {
            WorldDataHelper.RemoveChunk(this, pos);
        }
        
        foreach (var pos in worldGenerationData.chunkDataToRemove)
        {
            WorldDataHelper.RemoveChunkData(this, pos);
        }

        // Generate data chunks
        ConcurrentDictionary<Vector3Int, ChunkData> dataDict = 
            await CalculateWorldChunkData(worldGenerationData.chunkDataPositionsToCreate);

        foreach (var calculatedData in dataDict)
        {
            worldData.chunkDataDict.Add(calculatedData.Key, calculatedData.Value);
        }

        // Generate visual chunks
        // ConcurrentDictionary<Vector3Int, MeshData> meshDataDict = 
        //     await CalculateChunkMeshData(worldGenerationData.chunkPositionsToCreate);
        ConcurrentDictionary<Vector3Int, MeshData> meshDataDict;
        List<ChunkData> dataToRender = worldData.chunkDataDict
            .Where(x => worldGenerationData.chunkPositionsToCreate.Contains(x.Key))
            .Select(x => x.Value)
            .ToList();
        
        meshDataDict = await CreateMeshDataAsync(dataToRender);

        StartCoroutine(ChunkCreationCoroutine(meshDataDict, position));
    }

    private Task<ConcurrentDictionary<Vector3Int, MeshData>> CreateMeshDataAsync(List<ChunkData> dataToRender)
    {
        return Task.Run(() =>
        {
            var dict = new ConcurrentDictionary<Vector3Int, MeshData>();
            Parallel.ForEach(dataToRender, data =>
            {
                var meshData = Chunk.GetChunkMeshData(data);
                dict.TryAdd(data.worldPos, meshData);
            });

            return dict;
        });
    }

    // private Task<ConcurrentDictionary<Vector3Int, MeshData>> CalculateChunkMeshData(List<Vector3Int> chunkPositionsToCreate)
    // {
    //     return Task.Run(() =>
    //     {
    //         var meshDataDict = new ConcurrentDictionary<Vector3Int, MeshData>();
    //         
    //         Parallel.ForEach(chunkPositionsToCreate, pos =>
    //         {
    //             var data = worldData.chunkDataDict[pos];
    //             var meshData = Chunk.GetChunkMeshData(data);
    //             meshDataDict.TryAdd(pos, meshData);
    //         });
    //         
    //         return meshDataDict;
    //     });
    // }

    private Task<ConcurrentDictionary<Vector3Int, ChunkData>> CalculateWorldChunkData(List<Vector3Int> chunkDataPositionsToCreate)
    {

        return Task.Run(() =>
        {
            var dataDict = new ConcurrentDictionary<Vector3Int, ChunkData>();

            Parallel.ForEach(chunkDataPositionsToCreate, pos =>
            {
                var data = new ChunkData(chunkSize, chunkHeight, this, pos);
                var newData = terrainGenerator.GenerateChunkData(data, mapOffset);
                dataDict.TryAdd(pos, newData);

            });

            return dataDict;
        });
    }

    IEnumerator ChunkCreationCoroutine(ConcurrentDictionary<Vector3Int, MeshData> meshDataConDict, Vector3Int playerPos)
    {
        var meshDataDict = meshDataConDict.OrderBy(pair => Vector3Int.Distance(playerPos, pair.Key));
        foreach (var item in meshDataDict)
        {
            CreateChunk(worldData, item.Key, item.Value);
            if (Application.isPlaying)
            {
                yield return new WaitForEndOfFrame();
            }
        }

        if (!IsWorldCreated && Application.isPlaying)
        {
            IsWorldCreated = true;
            OnWorldCreated?.Invoke();
        }

        if (!Application.isPlaying)
        {
            stopwatch.Stop();
            Debug.Log($"World created in {stopwatch.ElapsedMilliseconds}ms");
            stopwatch.Reset();
        }
    }

    private void CreateChunk(WorldData worldData, Vector3Int pos, MeshData meshData)
    {
        var chunkObj = Instantiate(chunkPrefab, pos, Quaternion.identity);
        var chunkRenderer = chunkObj.GetComponent<ChunkRenderer>();
        if (worldData.chunkDict.TryAdd(pos, chunkRenderer) && worldData.chunkDataDict.TryGetValue(pos, out var data))
        {
            chunkRenderer.Initialize(data);
            chunkRenderer.UpdateChunk(meshData);
        }
    }
    

    private WorldGenerationData GetPositionsInRenderDistance(Vector3Int playerPos)
    {
        var allChunkPositionsNeeded = WorldDataHelper.GetChunkPositionsInRenderDistance(this, playerPos);
        var allChunkDataPositionsNeeded = WorldDataHelper.GetDataPositionsInRenderDistance(this, playerPos);
        
        var chunkPositionsToCreate = WorldDataHelper.GetPositionsToCreate(worldData, allChunkPositionsNeeded,playerPos);
        var chunkDataPositionsToCreate = WorldDataHelper.GetDataPositionsToCreate(worldData, allChunkDataPositionsNeeded,playerPos);
        
        var chunkPositionsToRemove = WorldDataHelper.GetUnneededChunkPositions(worldData, allChunkPositionsNeeded);
        var chunkDataToRemove = WorldDataHelper.GetUnneededDataPositions(worldData, allChunkDataPositionsNeeded);
        

        var data = new WorldGenerationData
        {
            chunkPositionsToCreate = chunkPositionsToCreate,
            chunkDataPositionsToCreate = chunkDataPositionsToCreate,
            chunkPositionsToRemove = chunkPositionsToRemove,
            chunkDataToRemove = chunkDataToRemove
        };
        
        return data;
    }
    
    public bool SetBlock(RaycastHit hit, BlockType blockType)
    {
        var chunk = hit.collider.GetComponent<ChunkRenderer>();
        if (chunk == null)
        {
            return false;
        }
        
        var blockPos = GetBlockPos(hit);

        // // Dig out the blocks around the block
        // var blocksToDig = new List<Vector3Int>();
        // blocksToDig.Add(blockPos);
        // blocksToDig.Add(blockPos + Vector3Int.up);
        // blocksToDig.Add(blockPos + Vector3Int.down);
        // blocksToDig.Add(blockPos + Vector3Int.left);
        // blocksToDig.Add(blockPos + Vector3Int.right);
        // blocksToDig.Add(blockPos + Vector3Int.forward);
        // blocksToDig.Add(blockPos + Vector3Int.back);
        //
        // foreach (var pos in blocksToDig)
        // {
        //     SetBlock(chunk, pos, blockType);
        // }

        SetBlock(chunk, blockPos, blockType);
        return true;
    }

    public void SetBlock(ChunkRenderer chunk, Vector3Int blockPos, BlockType blockType)
    {
        WorldDataHelper.SetBlock(chunk.ChunkData.worldRef, blockPos, blockType);
        chunk.ModifiedByPlayer = true;

        if (Chunk.IsOnEdge(chunk.ChunkData, blockPos))
        {
            List<ChunkData> neighbourChunkData = Chunk.GetNeighbourChunk(chunk.ChunkData, blockPos);
            foreach (var neighChunkData in neighbourChunkData)
            {
                ChunkRenderer neighbourChunk = WorldDataHelper.GetChunk(neighChunkData.worldRef, neighChunkData.worldPos);
                if(neighbourChunk != null)
                {
                    neighbourChunk.UpdateChunk();
                }
            }
        }
        
        chunk.UpdateChunk();
    }
    
    public Vector3Int GetBlockPos(RaycastHit hit)
    {
        var pos = new Vector3(
            GetBlockPosIn(hit.point.x, hit.normal.x),
            GetBlockPosIn(hit.point.y, hit.normal.y),
            GetBlockPosIn(hit.point.z, hit.normal.z)
            );
        
        return Vector3Int.RoundToInt(pos);
    }

    private float GetBlockPosIn(float pos, float normal)
    {
        if (Mathf.Abs(pos % 1) == 0.5f)
        {
            pos -= (normal / 2);
        }
        
        return pos;
    }

    public BlockType GetBlock(ChunkData chunkData, Vector3Int pos)
    {
        var chunkPos = Chunk.ChunkPosFromBlockCoords(this, pos);

        worldData.chunkDataDict.TryGetValue(chunkPos, out var containerChunk);
        
        if (containerChunk == null)
        {
            return BlockType.Nothing;
        }

        var blockPos = Chunk.GetLocalBlockCoords(containerChunk, new Vector3Int(pos.x, pos.y, pos.z));
        return Chunk.GetBlock(containerChunk, blockPos);
    }
    
    public async void LoadAdditionalChunks(GameObject localPlayer)
    {
        // Debug.Log("Loading additional chunks");
        await GenerateWorld(Vector3Int.RoundToInt(localPlayer.transform.position));
        OnNewChunksGenerated?.Invoke();
    }
    
    public void RemoveChunk(ChunkRenderer chunk)
    {
        chunk.gameObject.SetActive(false);
    }
    
    public struct WorldGenerationData
    {
        public List<Vector3Int> chunkPositionsToCreate;
        public List<Vector3Int> chunkDataPositionsToCreate;
        public List<Vector3Int> chunkPositionsToRemove;
        public List<Vector3Int> chunkDataToRemove;
    }

    public struct WorldData
    {
        public Dictionary<Vector3Int, ChunkData> chunkDataDict;
        public Dictionary<Vector3Int, ChunkRenderer> chunkDict;
        public int chunkSize;
        public int chunkHeight;
    }



#if UNITY_EDITOR
    [CustomEditor(typeof(World))]
    public class WorldEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var world = target as World;
            if (GUILayout.Button("Generate World"))
            {
                Clear();
                world.GenerateWorld();
            }

            if (GUILayout.Button("Clear World"))
            {
                Clear();
            }
            
            base.OnInspectorGUI();
        }

        private void Clear()
        {
            var world = target as World;
            world.worldData.chunkDataDict.Clear();
            world.worldData.chunkDict.Clear();
            foreach (var chunk in FindObjectsOfType<ChunkRenderer>())
            {
                DestroyImmediate(chunk.gameObject);
            }
        }
    }
#endif
}
