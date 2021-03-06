﻿using UnityEngine;

// we don't want Block to be MonoBehavior because that would add a lot of additional stuff limiting performance
public class ChunkMonoBehavior : MonoBehaviour
{
    // how far a block can fall until it disappear - it prevents from infinite drop
    private const int MaxDropValue = 100;
    Chunk _owner;
    public ChunkMonoBehavior() { }
    public void SetOwner(Chunk o)
    {
        _owner = o;
        InvokeRepeating("SaveProgress", 10, 100); // repeats every 100 seconds
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="startingBlock"></param>
    /// <param name="bt"></param>
    /// <param name="strength">Health value of the block - how far we let the water flow out</param>
    /// <param name="maxsize">Restrict recursive algorithm to grow to big</param>
    /// <returns></returns>
    //public IEnumerator Flow(Block startingBlock, TerrainGenerator.BlockType bt, int strength, int maxsize)
    //{
    //	// reduce the strength of the fluid block
    //	// with each new block created
    //	if (maxsize <= 0) yield break;
    //	if (startingBlock == null) yield break; // block would be null if the neighboring location does not exist
    //	if (strength <= 0) yield break;
    //	if (startingBlock.Type != TerrainGenerator.BlockType.Air) yield break; // water only spread trough the air
    //	startingBlock.Type = bt; // BUG: This should also change the parent's game object to apply transparency
    //	startingBlock.CurrentHealth = strength;
    //	startingBlock.Owner.DestroyMeshAndCollider();
    //	startingBlock.Owner.CreateMeshAndCollider();
    //	yield return new WaitForSeconds(1); // water spread one block per second

    //	int x = (int)startingBlock.LocalPosition.x;
    //	int y = (int)startingBlock.LocalPosition.y;
    //	int z = (int)startingBlock.LocalPosition.z;

    //	// flow down if air block beneath
    //	Block below = startingBlock.GetBlock(x, y - 1, z);
    //	if (below != null && below.Type == TerrainGenerator.BlockType.Air)
    //	{
    //		StartCoroutine(Flow(startingBlock.GetBlock(x, y - 1, z), bt, strength, --maxsize));
    //	}
    //	else // flow outward
    //	{
    //		--strength;
    //		--maxsize;
    //		// flow left
    //		World.Queue.Run(Flow(startingBlock.GetBlock(x - 1, y, z), bt, strength, maxsize));
    //		yield return new WaitForSeconds(1);

    //		// flow right
    //		World.Queue.Run(Flow(startingBlock.GetBlock(x + 1, y, z), bt, strength, maxsize));
    //		yield return new WaitForSeconds(1);

    //		// flow forward
    //		World.Queue.Run(Flow(startingBlock.GetBlock(x, y, z + 1), bt, strength, maxsize));
    //		yield return new WaitForSeconds(1);

    //		// flow back
    //		World.Queue.Run(Flow(startingBlock.GetBlock(x, y, z - 1), bt, strength, maxsize));
    //		yield return new WaitForSeconds(1);
    //	}
    //}

    //public IEnumerator Drop(Block thisBlock, TerrainGenerator.BlockType type)
    //{
    //	Block prevBlock = null;
    //	for (int i = 0; i < MaxDropValue; i++)
    //	{
    //		var previousType = thisBlock.Type;
    //		if(previousType != type)
    //			thisBlock.Type = type;
    //		if (prevBlock != null)
    //			prevBlock.Type = previousType;

    //		prevBlock = thisBlock;
    //		thisBlock.Owner.DestroyMeshAndCollider();
    //		thisBlock.Owner.CreateMeshAndCollider();

    //		yield return new WaitForSeconds(0.2f);
    //		Vector3 pos = thisBlock.LocalPosition;

    //		thisBlock = thisBlock.GetBlock((int)pos.x, (int)pos.y - 1, (int)pos.z);
    //		if (thisBlock.Type != TerrainGenerator.BlockType.Air && thisBlock.Type != TerrainGenerator.BlockType.Water)
    //		{
    //			yield break;
    //		}
    //	}
    //}

    //// I have been hit please heal me after 3 seconds
    //public IEnumerator HealBlock(Vector3 bpos)
    //{
    //	yield return new WaitForSeconds(3);
    //	int x = (int)bpos.x;
    //	int y = (int)bpos.y;
    //	int z = (int)bpos.z;

    //          // if it hasn't been already destroy reset it
    //          var block = _owner.Blocks[x, y, z];
    //          if (block.Type != TerrainGenerator.BlockType.Air)
    //              block.Reset();
    //}
    
}