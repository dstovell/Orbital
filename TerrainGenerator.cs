using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class TerrainGenerator : MonoBehaviour 
	{
		public MeshFilter Terrain;
		public GameObject Rotator;
		public BoxCollider Raise;
		public BoxCollider Lower;

		public float RaiseAmount = 0.1f;
		public int Iterations = 10;

		public void Generate(int interations)
		{
			if ((this.Terrain == null) || (this.Rotator == null) || (this.Raise == null) || (this.Lower == null))
			{
				return;
			}

			Mesh mesh = this.Terrain.mesh;

			//Rotator.transform.rotation = Quaternion.AngleAxis(Random.value*360, this.transform.up);

			for (int i=0; i<this.Iterations; i++)
			{			
				
				Vector3[] verts = mesh.vertices;

				int raised = 0;
				int lowered = 0;

				//Rotator.transform.rotation = Random.rotation;

				//Rotator.transform.rotation = Quaternion.AngleAxis(Random.value*360, this.transform.up);

				for (int v=0; v<verts.Length; v++)
				{
					Vector3 vert = verts[v];
					Vector3 dir = vert.normalized;
					Vector3 worldPos = vert + this.transform.position;
					if (this.Raise.bounds.Contains(worldPos))
					{
						raised++;
						verts[v] = vert + this.RaiseAmount*dir;
					}
					else if (this.Lower.bounds.Contains(worldPos))
					{
						lowered++;
						verts[v] = vert - this.RaiseAmount*dir;
					}
				}

				Debug.Log("lowered=" + lowered + " raised=" + raised);

				mesh.vertices = verts;
				mesh.RecalculateNormals();
			}
		}

		void Start () 
		{
			Generate(10);
		}
		
		// Update is called once per frame
		void Update () 
		{
			
		}
	}
}
