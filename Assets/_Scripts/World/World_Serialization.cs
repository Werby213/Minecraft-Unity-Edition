﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public partial class World
{
    [HideInInspector]
    public bool isSaving = false;

    public async void SaveWorld()
    {
        if (World.Instance.worldName == "")
        {
            Debug.Log("World name is empty - not saving");
            return;
        }

        if (isSaving)
        {
            Debug.Log("Already saving");
            return;
        }

        isSaving = true;
        var stopwatch = Stopwatch.StartNew();
        var saveWatch = Stopwatch.StartNew();

        // Create an array of ChunkSaveData from modified chunks
        var chunkSaveDatas = worldData.chunkDataDict.Values
            .Where(data => data.modifiedAfterSave)
            .Select(ChunkSaveData.Serialize)
            .ToArray();

        // Create object with data to save and pass chunkSaveDatas to the constructor
        var worldSaveData = new WorldSaveData(chunkSaveDatas)
        {
            worldName = worldName,
            seedOffset = mapSeedOffset
        };
        saveWatch.Stop();
        Debug.Log($"Creating local save object took {saveWatch.ElapsedMilliseconds}ms");

        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + worldSaveData.worldName + "/chunks"));

        // Save the chunks
        await Task.Run(() =>
        {
            Parallel.ForEach(worldSaveData.chunks, new ParallelOptions() { MaxDegreeOfParallelism = 8 }, chunk =>
            {
                var path = GetChunkPath(worldName, chunk.position);
                string json;
                if (binaryCompressSaves)
                {
                    json = Compress(JsonUtility.ToJson(chunk));
                }
                else
                {
                    json = JsonUtility.ToJson(chunk);
                }
                File.WriteAllText(path, json);
            });
        });

        // Save the world data
        var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + worldSaveData.worldName + "/world.json");

        string worldJson;

        if (binaryCompressSaves)
        {
            worldJson = Compress(JsonUtility.ToJson(worldSaveData));
        }
        else
        {
            worldJson = JsonUtility.ToJson(worldSaveData);
        }

        await File.WriteAllTextAsync(savePath, worldJson);

        // Save all the player data

        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + worldSaveData.worldName + "/playerdata"));

        if (GameManager.Instance != null)
        {
            foreach (var player in GameManager.Instance.players)
            {
                SavePlayer(player.SavePlayer());
            }
        }

        Debug.Log("Saved world in " + stopwatch.ElapsedMilliseconds + "ms");
        isSaving = false;
    }


    public void SavePlayer(WorldServer.SavePlayerMessage message)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + worldName + $"/playerdata/{message.steamId}.json");
        File.WriteAllText(path, JsonUtility.ToJson(message));
    }

    public UniTask<ConcurrentDictionary<Vector3Int, ChunkData>> LoadChunksAsync(IEnumerable<Vector3Int> chunks, string worldName)
    {
        return UniTask.RunOnThreadPool(() =>
        {
            // TODO: for some reason when you use more than 1 thread it crashes because of type mismatch in chunkSection deserialize
            // this seems to be a bug in unity
            var dict = new ConcurrentDictionary<Vector3Int, ChunkData>();
            // Parallel.ForEach(chunks, chunkPos =>
            // {
            foreach (var chunkPos in chunks)
            {
                if(taskTokenSource.IsCancellationRequested)
                {
                    taskTokenSource.Token.ThrowIfCancellationRequested();
                }
                
                var path = GetChunkPath(worldName, chunkPos);
        
                if (!File.Exists(path))
                {
                    Debug.LogWarning("Chunk file not found at: " + chunkPos);
                    continue;
                }
                string json;

                if (binaryCompressSaves)
                {
                    json = Decompress(File.ReadAllText(path));
                }
                else
                {
                    json = File.ReadAllText(path);
                }
                
                var chunkSaveData = JsonUtility.FromJson<ChunkSaveData>(json);
                dict.TryAdd(chunkPos, ChunkData.Deserialize(chunkSaveData));
            }

            // });
            return dict;
        }, cancellationToken: taskTokenSource.Token);
    }

    public static string GetChunkPath(string worldName, Vector3Int chunkPos)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves/"+worldName+"/chunks/"+chunkPos.x+"_"+chunkPos.y+"_"+chunkPos.z+".json");
    }

    public static string Compress(string s)
    {
        var bytes = Encoding.Unicode.GetBytes(s);
        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(mso, CompressionMode.Compress))
        {
            msi.CopyTo(gs);
        }
        return Convert.ToBase64String(mso.ToArray());
    }
 
    public static string Decompress(string s)
    {
        var bytes = Convert.FromBase64String(s);
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return Encoding.Unicode.GetString(mso.ToArray());
        }
    }
}

