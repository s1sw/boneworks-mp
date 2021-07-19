using UnityEngine;
using StressLevelZero.Interaction;
using PuppetMasta;

namespace MultiplayerMod.Structs
{
	public struct CachedPuppet
	{
		public Rigidbody[] rigidbodies { get; set; }

		public Grip[] grips { get; set; }

		public CachedPuppet(PuppetMaster puppet)
		{
			rigidbodies = puppet.GetComponentsInChildren<Rigidbody>(true);
			grips = puppet.GetComponentsInChildren<Grip>(true);
		}
	}
}