﻿
using UnityEngine;

public static class Blocks
{
    public static readonly Block NOTHING = new Block(BlockType.Nothing);
    public static Block WATER => new WaterBlock();
    public static Block AIR => new AirBlock();
}