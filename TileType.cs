using UnityEngine;
using System.Collections;

namespace Orbital
{
	public class TileType : MonoBehaviour
	{
		public enum Zones
		{
			None,
			Arctic,
			Temperate,
			Tropical,

			All
		}

		public enum TerrainTypes
		{
			None,

			//Water
			Ocean,
			Littoral,

			//Navigable Land
			Forest,
			Grassland,
			Desert,

			//Non-Navigable Land
			Mountain
		}

		public TerrainTypes Type = TerrainTypes.None;
		public Zones Zone = Zones.None;

		public GameObject HexReplacement;
	}
}

