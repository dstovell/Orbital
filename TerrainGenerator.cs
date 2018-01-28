using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public abstract class TerrainGenerator : MonoBehaviour 
	{
		public bool GenerateNow = false;
		public int RandomSeed = 1337;

		public MeshFilter TerrainMesh;
		public Hexsphere HexPlanet;

		public float SeaLevel = 0.1f;
		public float ExtrusionMultiplier = 0.1f;

		public TerrainElevation [] Elevations;

		public abstract bool GenerateTerrain(MeshFilter terrainMesh);

		public abstract bool GenerateTerrain(Hexsphere hexPlanet);

		public bool GenerateTerrain()
		{
			if ((this.Elevations == null) || (this.Elevations.Length == 0))
			{
				this.Elevations = this.GetComponents<TerrainElevation>();
			}

			Random.seed = this.RandomSeed;
			if (this.HexPlanet != null)
			{
				return this.GenerateTerrain(this.HexPlanet);
			}
			else if (this.TerrainMesh != null)
			{
				return this.GenerateTerrain(this.TerrainMesh);
			}

			return false;
		}

		public TerrainElevation GetElevation(float extrusion)
		{
			for (int i=0; i<this.Elevations.Length; i++)
			{
				TerrainElevation e = this.Elevations[i];
				if (e.IsExtrusionMatch(extrusion))
				{
					return e;
				}
			}
			return null;
		}

		void Update()
		{
			if (this.GenerateNow)
			{
				this.GenerateTerrain();
				this.GenerateNow = false;
			}
		}
	}
}
