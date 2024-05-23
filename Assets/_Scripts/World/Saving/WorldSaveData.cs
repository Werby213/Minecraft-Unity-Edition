using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldSaveData
{
    public string worldName;
    public Vector3Int seedOffset;
    public byte[] compressedChunksData;
    public ChunkSaveData[] chunks; // Define the chunks property here

    [NonSerialized]
    public Dictionary<Vector3Int, ChunkSaveData> chunksDic = new();

    public void CompressAndStoreChunks()
    {
        var chunkList = new List<ChunkSaveData>(chunksDic.Values);
        var serializedData = SerializationHelper.SerializeToBinary(chunkList);
        compressedChunksData = SerializationHelper.Compress(serializedData);
    }

    public void DecompressAndLoadChunks()
    {
        if (compressedChunksData == null)
            return;

        var decompressedData = SerializationHelper.Decompress(compressedChunksData);
        var chunkList = SerializationHelper.DeserializeFromBinary(decompressedData) as List<ChunkSaveData>;
        chunksDic = new Dictionary<Vector3Int, ChunkSaveData>();

        if (chunkList != null)
        {
            foreach (var chunk in chunkList)
            {
                chunksDic[chunk.position] = chunk;
            }
        }
    }

    public ChunkSaveData RequestChunkData(Vector3Int chunkPos)
    {
        if (chunksDic.ContainsKey(chunkPos))
        {
            return chunksDic[chunkPos];
        }
        else
        {
            return null;
        }
    }
    public WorldSaveData(ChunkSaveData[] chunkSaveDatas)
    {
        // Initialize chunks array here if needed
        chunks = chunkSaveDatas;
    }

}
