using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class TerrainElevation : MonoBehaviour 
	{
		public float MinExtrusion = 0.0f;
		public float MaxExtrusion = 0.0f;
		public Color TerrainColor = Color.blue;
		public Material TerrainMat;

		public bool IsExtrusionMatch(float e)
		{
			if ((e >= this.MinExtrusion) && (e < this.MaxExtrusion))
			{
				return true;
			}

			return false;
		}
	}
}
