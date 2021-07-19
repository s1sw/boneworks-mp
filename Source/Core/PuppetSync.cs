using System.Collections.Generic;
using MultiplayerMod.Structs;
using PuppetMasta;
using StressLevelZero.Interaction;
using UnityEngine;

public static class PuppetSync
{
	public static readonly Dictionary<PuppetMaster, CachedPuppet> cachedPuppets = new Dictionary<PuppetMaster, CachedPuppet>();

	/// <summary>
	/// Clears cache of puppets
	/// </summary>
	public static void ClearCache() => cachedPuppets.Clear();
	
	/// <summary>
	/// Gets new puppet dat
	/// </summary>
	/// <param name="puppet"></param>
	/// <param name="rigidbodies"></param>
	/// <param name="grips"></param>
	public static void GetPuppetData(PuppetMaster puppet, out Rigidbody[] rigidbodies, out Grip[] grips)
	{
		//Get Existing
		if (cachedPuppets.TryGetValue(puppet, out CachedPuppet cache))
		{
			rigidbodies = cache.rigidbodies;
			grips = cache.grips;
			return;
		}
		//Create New
		CachedPuppet newCache = new CachedPuppet(puppet);
		rigidbodies = newCache.rigidbodies;
		grips = newCache.grips;
		//Add to Dict
		cachedPuppets.Add(puppet, newCache);
	}

	/// <summary>
	/// Gets rigidbodies from puppet
	/// </summary>
	/// <param name="puppet"></param>
	/// <param name="rigidbodies"></param>
	public static void GetPuppetData(PuppetMaster puppet, out Rigidbody[] rigidbodies) => GetPuppetData(puppet, out rigidbodies, out var grips);
}
