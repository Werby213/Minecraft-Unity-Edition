﻿using System.Collections.Generic;
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
                var chunkPos = GetChunkPosition(world, new Vector3Int(x, 0, z));
                chunkPositionsToCreate.Add(chunkPos);
                
                // Add the chunks directly around and below the player so they can dig
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

    public static List<Vector3Int> GetPositionsToCreate(World.WorldData worldData, List<Vector3Int> allChunkPositionsNeeded, Vector3Int playerPos)
    {
        return allChunkPositionsNeeded
            .Where(pos => !worldData.chunkDict.ContainsKey(pos))
            .OrderBy(pos => Vector3.Distance(playerPos, pos))
            .ToList();
    }

    public static List<Vector3Int> GetDataPositionsToCreate(World.WorldData worldData, List<Vector3Int> allChunkDataPositionsNeeded, Vector3Int playerPos)
    {
        return allChunkDataPositionsNeeded
            .Where(pos => !worldData.chunkDataDict.ContainsKey(pos))
            .OrderBy(pos => Vector3.Distance(playerPos, pos))
            .ToList();
    }
}
