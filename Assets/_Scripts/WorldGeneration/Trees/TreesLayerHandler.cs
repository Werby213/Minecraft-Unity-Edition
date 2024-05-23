using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TreesLayerHandler : BlockLayerHandler
{
    public int terrainHeightLimit = 25;

    public Vector3Int[] directions = {
        new(0, 0, 1),
        new(0, 0, -1),
        new(1, 0, 0),
        new(-1, 0, 0),
        new(1, 0, 1),
        new(-1, 0, -1),
        new(1, 0, -1),
        new(-1, 0, 1)
    };

    protected override bool TryHandling(ChunkData chunk, Vector3Int worldPos, Vector3Int localPos, int surfaceHeightNoise, Vector3Int mapSeedOffset)
    {
        if (chunk.worldPos.y < 0)
        {
            return false;
        }

        if (surfaceHeightNoise < terrainHeightLimit && chunk.treeData.treePositions.Contains(new Vector2Int(worldPos.x, worldPos.z)))
        {
            var blockCoords = new Vector3Int(localPos.x, surfaceHeightNoise - chunk.worldPos.y, localPos.z);
            if (blockCoords.y < 0)
            {
                return false;
            }
            var type = chunk.GetBlock(blockCoords).type;

            if (type is BlockType.Grass or BlockType.Dirt)
            {
                chunk.SetBlock(blockCoords, BlockType.Dirt);

                System.Random rng = new System.Random(worldPos.x * 31 + worldPos.z * 17 + mapSeedOffset.GetHashCode());

                bool isHugeTree = rng.NextDouble() < 0.3;
                var treeHeight = isHugeTree ? Mathf.RoundToInt(Mathf.Lerp(8, 12, MyNoise.OctavePerlin(worldPos.x, worldPos.z, chunk.treeData.treeNoiseSettings)))
                                            : Mathf.RoundToInt(Mathf.Lerp(5, 7, MyNoise.OctavePerlin(worldPos.x, worldPos.z, chunk.treeData.treeNoiseSettings)));

                for (var i = 1; i < treeHeight; i++)
                {
                    blockCoords.y = surfaceHeightNoise - chunk.worldPos.y + i;
                    chunk.SetBlock(blockCoords, BlockType.Log);
                }

                if (isHugeTree)
                {
                    GenerateBranches(chunk, worldPos, blockCoords, treeHeight, rng);
                }

                var leavePositions = GenerateLeaves(localPos, blockCoords, isHugeTree, rng);

                foreach (var l in leavePositions)
                {
                    if (chunk.GetBlock(l).type is not BlockType.Air and not BlockType.Leaves and not BlockType.Log and not BlockType.Nothing)
                    {
                        RemoveTree(treeHeight);
                        return false;
                    }
                }

                foreach (var leavePos in leavePositions)
                {
                    if (chunk.GetBlock(leavePos).type != BlockType.Log)
                    {
                        chunk.SetBlock(leavePos, BlockType.Leaves);
                    }
                }

                void RemoveTree(int height)
                {
                    blockCoords = new Vector3Int(localPos.x, surfaceHeightNoise, localPos.z);
                    chunk.SetBlock(blockCoords, type);
                    for (var i = 1; i < height; i++)
                    {
                        blockCoords.y = surfaceHeightNoise - chunk.worldPos.y + i;
                        chunk.SetBlock(blockCoords, BlockType.Air);
                    }
                }
            }
        }

        return false;
    }

    private void GenerateBranches(ChunkData chunk, Vector3Int worldPos, Vector3Int blockCoords, int treeHeight, System.Random rng)
    {
        int numberOfBranches = rng.Next(3, 7); // Ensure at least 3 branches for realism
        for (int i = 0; i < numberOfBranches; i++)
        {
            int branchStartHeight = rng.Next(treeHeight / 2, treeHeight - 2);
            Vector3Int branchStart = new Vector3Int(blockCoords.x, blockCoords.y - treeHeight + branchStartHeight, blockCoords.z);
            Vector3Int direction = directions[rng.Next(0, directions.Length)];
            int branchLength = rng.Next(2, 5); // Shorten branches for realism

            for (int j = 0; j < branchLength; j++)
            {
                Vector3Int branchCoords = branchStart + direction * j;
                if (chunk.GetBlock(branchCoords).type is BlockType.Air or BlockType.Leaves or BlockType.Nothing)
                {
                    chunk.SetBlock(branchCoords, BlockType.Log);
                }
                else
                {
                    break;
                }
            }
        }
    }

    private List<Vector3Int> GenerateLeaves(Vector3Int localPos, Vector3Int blockCoords, bool isHugeTree, System.Random rng)
    {
        var leavePositions = new List<Vector3Int>();

        int leafRadius = isHugeTree ? 4 : 2;
        for (int y = -2; y <= 2; y++)
        {
            for (int x = -leafRadius; x <= leafRadius; x++)
            {
                for (int z = -leafRadius; z <= leafRadius; z++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(z) + Mathf.Abs(y) < (isHugeTree ? 5 : 4) && rng.NextDouble() < 0.8)
                    {
                        leavePositions.Add(new Vector3Int(localPos.x + x, blockCoords.y + y, localPos.z + z));
                    }
                }
            }
        }

        if (isHugeTree)
        {
            for (int y = 3; y <= 4; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    for (int z = -2; z <= 2; z++)
                    {
                        if (Mathf.Abs(x) + Mathf.Abs(z) + y < 6 && rng.NextDouble() < 0.6)
                        {
                            leavePositions.Add(new Vector3Int(localPos.x + x, blockCoords.y + y, localPos.z + z));
                        }
                    }
                }
            }
        }

        return leavePositions;
    }
}
