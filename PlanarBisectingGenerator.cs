using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class PlanarBisectingGenerator : TerrainGenerator 
	{
		public float DefaultExtrusion = 0.3f;
		public float RaiseAmount = 0.1f;
		public int Iterations = 10;

		public override bool GenerateTerrain(MeshFilter terrainMesh)
		{
			Vector3 center = Vector3.zero;

			if (terrainMesh == null)
			{
				return false;
			}

			Mesh mesh = terrainMesh.mesh;
			center = mesh.bounds.center;
			Vector3[] verts = mesh.vertices;

			float[] extrusions = this.GeneratePlanarExtrusions(center, verts, this.Iterations);

			for (int v=0; v<verts.Length; v++)
			{
				Vector3 vert = verts[v];
				Vector3 dir = (vert - center).normalized;
				verts[v] = vert + this.ExtrusionMultiplier*extrusions[v]*dir;
			}

			mesh.vertices = verts;
			mesh.RecalculateNormals();

			return true;
		}

		public override bool GenerateTerrain(HexasphereGrid.Hexasphere hexSphere)
		{
			if (hexSphere == null)
			{
				return false;
			}

			Vector3 center = Vector3.zero;
			HexasphereGrid.Tile[] tiles = hexSphere.tiles;

			Vector3[] positions = new Vector3[tiles.Length];

			for (int t=0; t<tiles.Length; t++)
			{
				positions[t] = tiles[t].center;
			}

			float[] extrusions = this.GeneratePlanarExtrusions(center, positions, this.Iterations);

			for (int t=0; t<tiles.Length; t++)
			{
				float extrusion = extrusions[t];
				hexSphere.SetTileExtrudeAmount(tiles[t].index, extrusion);
				tiles[t].heightMapValue = extrusion;

				if (extrusion > this.SeaLevel)
				{
					hexSphere.SetTileColor(tiles[t].index, Color.green);
				}
				else {
					hexSphere.SetTileColor(tiles[t].index, Color.blue);
				}
				//Debug.Log("SetTileExtrudeAmount index=" + tiles[t].index + " extrusion=" + extrusions[t]);
			}

			return true;
		}

		public float[] GeneratePlanarExtrusions(Vector3 center, Vector3[] positions, int interations)
		{
			float maxExtrusion = 1.0f;
			float minExtrusion = 0.0f;

			float [] extrusions = new float[positions.Length];
			for (int p=0; p<positions.Length; p++)
			{
				extrusions[p] = this.DefaultExtrusion;
			}

			Vector3 randomVector = new Vector3();

			for (int i=0; i<interations; i++)
			{
				int raised = 0;
				int lowered = 0;
				float minAngle = 180.0f;
				float maxAngle = 0.0f;

				//randomVector.Set(Random.Range(-1, 1), Random.Range(-1, 1), Random.Range(-1, 1));
				randomVector = Random.insideUnitSphere;

				for (int p=0; p<positions.Length; p++)
				{
					Vector3 dir = positions[p] - center;

					if (Vector3.Angle(dir, randomVector) > 90)
					{
						raised++;
						extrusions[p] += this.RaiseAmount;
					}
					else
					{
						lowered++;
						extrusions[p] -= this.RaiseAmount;
					}

					extrusions[p] = Mathf.Clamp01(extrusions[p]);
				}

				Debug.Log("lowered=" + lowered + " raised=" + raised);
			}

			return extrusions;
		}

		void Start () 
		{
			//GenerateTerrain();
		}
	}
}
