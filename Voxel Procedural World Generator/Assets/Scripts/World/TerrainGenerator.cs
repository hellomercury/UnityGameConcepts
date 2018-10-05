﻿using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class TerrainGenerator
{
    public struct HeightData
    {
        public float Bedrock, Stone, Dirt;
    }

    public struct HeightJob : IJobParallelFor
    {
        [ReadOnly]
        public int TotalBlockNumberX;

        // result
        public NativeArray<HeightData> Result;

        public void Execute(int i)
        {
            int x, z;
            IndexDeflattenizer2D(i, TotalBlockNumberX, out x, out z);

            Result[i] = new HeightData()
            {
                Bedrock = GenerateBedrockHeight(x, z),
                Stone = GenerateStoneHeight(x, z),
                Dirt = GenerateDirtHeight(x, z)
            };
        }
    }

    struct BlockTypeJob : IJobParallelFor
    {
        [ReadOnly]
        public int TotalBlockNumberX;
        [ReadOnly]
        public int TotalBlockNumberY;
        [ReadOnly]
        public int TotalBlockNumberZ;
        [ReadOnly]
        public NativeArray<HeightData> Heights;

        // result
        public NativeArray<BlockTypes> Result;

        public void Execute(int i)
        {
            // deflattenization - extract coords from the index
            int x, y, z;
            IndexDeflattenizer3D(i, TotalBlockNumberX, TotalBlockNumberY, out x, out y, out z);
            Result[i] = DetermineType(ref x, ref y, ref z, ref Heights);
        }
    }

    #region Constants
    // caves should be more erratic so has to be a higher number
    const float CaveProbability = 0.44f;
    const float CaveSmooth = 0.09f;
    const int CaveOctaves = 3; // reduced a bit to lower workload but not to much to maintain randomness
    const int WaterLevel = 65; // inclusive

    // shiny diamonds!
    const float DiamondProbability = 0.38f; // this is not percentage chance because we are using Perlin function
    const float DiamondSmooth = 0.06f;
    const int DiamondOctaves = 1;
    const int DiamondMaxHeight = 80;

    // red stones
    const float RedstoneProbability = 0.36f;
    const float RedstoneSmooth = 0.06f;
    const int RedstoneOctaves = 1;
    const int RedstoneMaxHeight = 50;

    // woodbase
    const float WoodbaseProbability = 0.35f;
    const float WoodbaseSmooth = 0.4f;
    const int WoodbaseOctaves = 1;
    const int TreeHeight = 7;

    const int MaxHeight = 90;
    const float Smooth = 0.01f; // bigger number increases sampling of the function
    const int Octaves = 3;
    const float Persistence = 0.5f;

    const int MaxHeightStone = 80;
    const float SmoothStone = 0.05f;
    const int OctavesStone = 2;
    const float PersistenceStone = 0.25f;

    const int MaxHeightBedrock = 15;
    const float SmoothBedrock = 0.1f;
    const int OctavesBedrock = 1;
    const float PersistenceBedrock = 0.5f;
    #endregion

    readonly int _chunkSize, _worldSizeX, _worldSizeY, _worldSizeZ, _totalBlockNumberX, _totalBlockNumberY, _totalBlockNumberZ;

    public TerrainGenerator(int chunkSize, int worldSizeX, int worldSizeY, int worldSizeZ)
    {
        _chunkSize = chunkSize;
        _worldSizeX = worldSizeX;
        _worldSizeY = worldSizeY;
        _worldSizeZ = worldSizeZ;
        _totalBlockNumberX = _worldSizeX * _chunkSize;
        _totalBlockNumberY = _worldSizeY * _chunkSize;
        _totalBlockNumberZ = _worldSizeZ * _chunkSize;
    }
    
    public static int GenerateBedrockHeight(float x, float z) =>
        (int)Map(0, MaxHeightBedrock, 0, 1,
            FractalBrownianMotion(x * SmoothBedrock, z * SmoothBedrock, OctavesBedrock, PersistenceBedrock));

    public static int GenerateStoneHeight(float x, float z) =>
        (int)Map(0, MaxHeightStone, 0, 1,
            FractalBrownianMotion(x * SmoothStone, z * SmoothStone, OctavesStone, PersistenceStone));

    public static int GenerateDirtHeight(float x, float z) =>
        (int)Map(0, MaxHeight, 0, 1,
            FractalBrownianMotion(x * Smooth, z * Smooth, Octaves, Persistence));

    public static float Map(float newmin, float newmax, float origmin, float origmax, float value) =>
        Mathf.Lerp(newmin, newmax, Mathf.InverseLerp(origmin, origmax, value));

    static BlockTypes DetermineType(ref int worldX, ref int worldY, ref int worldZ, ref NativeArray<HeightData> heights)
    {
        BlockTypes type;

        if (worldY == 0) return BlockTypes.Bedrock;

        if (worldY <= heights[IndexFlattenizer2D(worldX, worldY, worldX)].Bedrock)
            type = BlockTypes.Bedrock;
        else if (worldY <= heights[IndexFlattenizer2D(worldX, worldY, worldX)].Stone)
        {
            if (FractalFunc(worldX, worldY, worldZ, DiamondSmooth, DiamondOctaves) < DiamondProbability
                && worldY < DiamondMaxHeight)
                type = BlockTypes.Diamond;
            else if (FractalFunc(worldX, worldY, worldZ, RedstoneSmooth, RedstoneOctaves) < RedstoneProbability
                && worldY < RedstoneMaxHeight)
                type = BlockTypes.Redstone;
            else
                type = BlockTypes.Stone;
        }
        else
        {
            var height = heights[IndexFlattenizer2D(worldX, worldY, worldX)].Dirt;
            if (worldY == height)
                type = BlockTypes.Grass;
            else if (worldY < height)
                type = BlockTypes.Dirt;
            //else if (worldY <= WaterLevel)
            //    type = BlockTypes.Water;
            else
                type = BlockTypes.Air;
        }

        // BUG: caves are sometimes generated under or next to water 
        // generate caves
        if (type != BlockTypes.Water && FractalFunc(worldX, worldY, worldZ, CaveSmooth, CaveOctaves) < CaveProbability)
            type = BlockTypes.Air;

        return type;
    }

    // good noise generator
    // persistence - if < 1 each function is less powerful than the previous one, for > 1 each is more important
    // octaves - number of functions that we sum up
    static float FractalBrownianMotion(float x, float z, int oct, float pers)
    {
        float total = 0, frequency = 1, amplitude = 1, maxValue = 0;

        // Perlin function value of x is equal to its value of -x. Same for y.
        // to avoid it we need an offset, quite large one to be sure.
        const float offset = 32000f;

        for (int i = 0; i < oct; i++)
        {
            total += Mathf.PerlinNoise((x + offset) * frequency, (z + offset) * frequency) * amplitude;

            maxValue += amplitude;

            amplitude *= pers;
            frequency *= 2;
        }

        return total / maxValue;
    }

    // FractalBrownianMotion3D
    static float FractalFunc(float x, float y, int z, float smooth, int octaves)
    {
        // this is obviously more computational heavy
        float xy = FractalBrownianMotion(x * smooth, y * smooth, octaves, 0.5f);
        float yz = FractalBrownianMotion(y * smooth, z * smooth, octaves, 0.5f);
        float xz = FractalBrownianMotion(x * smooth, z * smooth, octaves, 0.5f);

        float yx = FractalBrownianMotion(y * smooth, x * smooth, octaves, 0.5f);
        float zy = FractalBrownianMotion(z * smooth, y * smooth, octaves, 0.5f);
        float zx = FractalBrownianMotion(z * smooth, x * smooth, octaves, 0.5f);

        return (xy + yz + xz + yx + zy + zx) / 6.0f;
    }

    // calculate global Heights
    public HeightData[] CalculateHeights()
    {
        // output data
        var heights = new HeightData[_totalBlockNumberX * _totalBlockNumberZ];

        var heightJob = new HeightJob()
        {
            // input
            TotalBlockNumberX = _totalBlockNumberX,

            // output
            Result = new NativeArray<HeightData>(heights, Allocator.TempJob)
        };

        var heightJobHandle = heightJob.Schedule(_totalBlockNumberX * _totalBlockNumberZ, 8);
        heightJobHandle.Complete();
        heightJob.Result.CopyTo(heights);

        // cleanup
        heightJob.Result.Dispose();

        return heights;
    }

    /// <summary>
    /// Converts coordinates to index in 2D space.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int IndexFlattenizer2D(int x, int y, int lengthX) => y * lengthX + x;

    /// <summary>
    /// Extracts coordinates from the index in 2D space.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void IndexDeflattenizer2D(int index, int lengthX, out int x, out int y)
    {
        y = index / lengthX;
        x = index - y * lengthX;
    }

    /// <summary>
    /// Converts coordinates to index in 3D space.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int IndexFlattenizer3D(int x, int y, int z, int lengthX, int lengthY) => z * lengthY * lengthX + y * lengthX + x;

    /// <summary>
    /// Extracts coordinates from the index in 3D space.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void IndexDeflattenizer3D(int index, int lengthX, int lengthY, out int x, out int y, out int z)
    {
        z = index / (lengthX * lengthY); // 10 / (3*2) = 1
        var rest = index - z * lengthX * lengthY; // 10 - 1 * 3 * 2 = 4
        y = rest / lengthX; // 4 / 3 = 1
        x = rest - y * lengthX; // 4 - 1 * 2 = 1
    }
    
    public void CalculateBlockTypes(ref Block[,,] blocks, HeightData[] heights)
    {
        var inputSize = _totalBlockNumberX * _totalBlockNumberY * _totalBlockNumberZ;

        // output data
        var types = new BlockTypes[inputSize];

        var typeJob = new BlockTypeJob()
        {
            // input
            TotalBlockNumberX = _totalBlockNumberX,
            TotalBlockNumberY = _totalBlockNumberY,
            TotalBlockNumberZ = _totalBlockNumberZ,
            Heights = new NativeArray<HeightData>(heights, Allocator.TempJob),

            // output
            Result = new NativeArray<BlockTypes>(types, Allocator.TempJob)
        };

        var typeJobHandle = typeJob.Schedule(inputSize, 8);
        typeJobHandle.Complete();
        typeJob.Result.CopyTo(types);

        // cleanup
        typeJob.Result.Dispose();
        typeJob.Heights.Dispose();

        // output deflattenization
        for (var x = 0; x < _totalBlockNumberX; x++)
            for (var y = 0; y < _totalBlockNumberY; y++)
                for (var z = 0; z < _totalBlockNumberZ; z++)
                {
                    var type = types[IndexFlattenizer3D(x, y, z, _totalBlockNumberX, _totalBlockNumberY)];
                    blocks[x, y, z].Type = type;
                    blocks[x, y, z].Hp = LookupTables.BlockHealthMax[(int)type];
                }
    }

    public void AddTrees(ref Block[,,] blocks)
    {
        for (var x = 1; x < _totalBlockNumberX - 1; x++)
            // this 50 is hard coded as for now but generally it would be nice if 
            // this loop could know in advance where is the lowest grass
            for (var y = 50; y < _totalBlockNumberY - TreeHeight; y++)
                for (var z = 1; z < _totalBlockNumberZ - 1; z++)
                {
                    if (blocks[x, y, z].Type != BlockTypes.Grass) continue;

                    if (IsThereEnoughSpaceForTree(ref blocks, x, y, z))
                    {
                        if (FractalFunc(x, y, z, WoodbaseSmooth, WoodbaseOctaves) < WoodbaseProbability)
                        {
                            BuildTree(ref blocks, x, y, z);
                            x += 2; // no trees can be that close
                        }
                    }
                }
    }

    bool IsThereEnoughSpaceForTree(ref Block[,,] blocks, int x, int y, int z)
    {
        for (int i = 2; i < TreeHeight; i++)
        {
            if (blocks[x + 1, y + i, z].Type != BlockTypes.Air
                || blocks[x - 1, y + i, z].Type != BlockTypes.Air
                || blocks[x, y + i, z + 1].Type != BlockTypes.Air
                || blocks[x, y + i, z - 1].Type != BlockTypes.Air
                || blocks[x + 1, y + i, z + 1].Type != BlockTypes.Air
                || blocks[x + 1, y + i, z - 1].Type != BlockTypes.Air
                || blocks[x - 1, y + i, z + 1].Type != BlockTypes.Air
                || blocks[x - 1, y + i, z - 1].Type != BlockTypes.Air)
                return false;
        }

        return true;
    }

    void BuildTree(ref Block[,,] blocks, int x, int y, int z)
    {
        CreateBlock(ref blocks[x, y, z], BlockTypes.Woodbase);
        CreateBlock(ref blocks[x, y + 1, z], BlockTypes.Wood);
        CreateBlock(ref blocks[x, y + 2, z], BlockTypes.Wood);

        for (int i = -1; i <= 1; i++)
            for (int j = -1; j <= 1; j++)
                for (int k = 3; k <= 4; k++)
                    CreateBlock(ref blocks[x + i, y + k, z + j], BlockTypes.Leaves);

        CreateBlock(ref blocks[x, y + 5, z], BlockTypes.Leaves);
    }

    void CreateBlock(ref Block block, BlockTypes type)
    {
        block.Type = type;
        block.Hp = LookupTables.BlockHealthMax[(int)type];
    }
}