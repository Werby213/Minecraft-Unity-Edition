﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class WorldDataHelper
{
    public static Vector3Int GetChunkPosition(World world, Vector3Int worldSpacePos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldSpacePos.x / (float)world.chunkSize) * world.chunkSize,
            Mathf.FloorToInt(worldSpacePos.y / (float)world.chunkHeight) * world.chunkHeight,
            Mathf.FloorToInt(worldSpacePos.z / (float)world.chunkSize) * world.chunkSize
        );
    }

    public static List<Vector3Int> GetChunkPositionsInRenderDistance(World world, Vector3Int playerPos)
    {
        var startX = playerPos.x - world.renderDistance * world.chunkSize;
        var startZ = playerPos.z - world.renderDistance * world.chunkSize;
        var endX = playerPos.x + world.renderDistance * world.chunkSize;
        var endZ = playerPos.z + world.renderDistance * world.chunkSize;
        
        return GetPositionsInRenderDistance(world,playerPos, startX, startZ, endX, endZ);
    }

    public static List<Vector3Int> GetDataPositionsInRenderDistance(World world, Vector3Int playerPos)
    {
        var startX = playerPos.x - (world.renderDistance+1) * world.chunkSize;
        var startZ = playerPos.z - (world.renderDistance+1) * world.chunkSize;
        var endX = playerPos.x + (world.renderDistance+1) * world.chunkSize;
        var endZ = playerPos.z + (world.renderDistance+1) * world.chunkSize;
        
        return GetPositionsInRenderDistance(world,playerPos, startX, startZ, endX, endZ);
    }

    private static List<Vector3Int> GetPositionsInRenderDistance(World world, Vector3Int playerPos, int startX,
        int startZ, int endX, int endZ)
    {
        var chunkPositionsToCreate = new List<Vector3Int>();
        for (var x = startX; x <= endX; x += world.chunkSize)
        {
            for (var z = startZ; z <= endZ; z += world.chunkSize)
            {
                for (var y = 0; y < world.worldHeight; y+= world.chunkSize)
                {
                    var chunkPos = GetChunkPosition(world, new Vector3Int(x, y, z));
                    chunkPositionsToCreate.Add(chunkPos);
                }
                
                // // Add the chunks directly around and below the player so they can dig
                // if(x >= playerPos.x - world.chunkSize && x <= playerPos.x + world.chunkSize)
                // {
                //     if(z >= playerPos.z - world.chunkSize && z <= playerPos.z + world.chunkSize)
                //     {
                //         for (var y = -world.chunkHeight; y >= playerPos.y - world.chunkHeight*2; y-=world.chunkHeight)
                //         {
                //             chunkPos = GetChunkPosition(world, new Vector3Int(x, y, z));
                //             chunkPositionsToCreate.Add(chunkPos);
                //         }
                //     }
                // }
            }
        }
        
        return chunkPositionsToCreate;
    }

    public static List<Vector3Int> GetPositionsToCreate(WorldData worldData, List<Vector3Int> allChunkPositionsNeeded, Vector3Int playerPos)
    {
        return allChunkPositionsNeeded
            .Where(pos => !worldData.chunkDict.ContainsKey(pos))
            .OrderBy(pos => Vector3.Distance(playerPos, pos))
            .ToList();
    }

    public static List<Vector3Int> GetDataPositionsToCreate(WorldData worldData, List<Vector3Int> allChunkDataPositionsNeeded, Vector3Int playerPos)
    {
        return allChunkDataPositionsNeeded
            .Where(pos => !worldData.chunkDataDict.ContainsKey(pos))
            .OrderBy(pos => Vector3.Distance(playerPos, pos))
            .ToList();
    }

    public static List<Vector3Int> GetUnneededChunkPositions(WorldData worldData, List<Vector3Int> allChunkPositionsNeeded)
    {
        var positionsToRemove = new List<Vector3Int>();
        foreach (var pos in worldData.chunkDict.Keys.Where(pos => !allChunkPositionsNeeded.Contains(pos)))
        {
            positionsToRemove.Add(pos);
        }
        
        return positionsToRemove;
    }

    public static List<Vector3Int> GetUnneededDataPositions(WorldData worldData, List<Vector3Int> allChunkDataPositionsNeeded)
    {
        return worldData.chunkDataDict.Keys.Where(pos => !allChunkDataPositionsNeeded.Contains(pos) && !worldData.chunkDataDict[pos].modifiedByPlayer).ToList();
    }

    public static void RemoveChunk(World world, Vector3Int pos)
    {
        ChunkRenderer chunk = null;
        if(world.worldData.chunkDict.TryGetValue(pos, out chunk))
        {
            world.worldRenderer.RemoveChunk(chunk);
            world.worldData.chunkDict.Remove(pos);
        }
        else
        { 
            throw new Exception("Could not find chunk to remove");
        }
    }

    public static void RemoveChunkData(World world, Vector3Int pos)
    {
        world.worldData.chunkDataDict.Remove(pos);
    }

    public static void SetBlock(World world, Vector3Int worldBlockPos, BlockType blockType)
    {
        var chunkData = GetChunkData(world, worldBlockPos);
        if (chunkData != null)
        {
            Vector3Int localPos = chunkData.GetLocalBlockCoords(worldBlockPos);
            chunkData.SetBlock(localPos, blockType);
        }
    }

    public static ChunkRenderer GetChunk(World world, Vector3Int worldPos)
    {
        if (world.worldData.chunkDict.ContainsKey(worldPos))
        {
            return world.worldData.chunkDict[worldPos];
        }

        return null;
    }
    
    public static ChunkData GetChunkData(World world, Vector3Int blockPos)
    {
        var chunkPos = GetChunkPosition(world, blockPos);

        world.worldData.chunkDataDict.TryGetValue(chunkPos, out var containerChunk);
        
        return containerChunk;
    }
}
