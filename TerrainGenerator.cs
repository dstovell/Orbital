using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public abstract class TerrainGenerator : MonoBehaviour 
	{
		public bool GenerateNow = false;

		public MeshFilter TerrainMesh;
		public HexasphereGrid.Hexasphere HexSphere;

		public float SeaLevel = 0.1f;
		public float ExtrusionMultiplier = 0.1f;

		public abstract bool GenerateTerrain(MeshFilter terrainMesh);

		public abstract bool GenerateTerrain(HexasphereGrid.Hexasphere hexSphere);

		public bool GenerateTerrain()
		{
			if (this.HexSphere != null)
			{
				this.HexSphere.extruded = true;
				this.HexSphere.extrudeMultiplier = this.ExtrusionMultiplier;
				return this.GenerateTerrain(this.HexSphere);
			}
			else if (this.TerrainMesh != null)
			{
				return this.GenerateTerrain(this.TerrainMesh);
			}

			return false;
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
