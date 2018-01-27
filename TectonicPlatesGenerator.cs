using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class TectonicPlatesGenerator : TerrainGenerator 
	{
		//public float PlateCount = 1.0f;
		public int PlateCount = 10;

		public Dictionary<int, HexasphereGrid.Tile> UsedTiles;

		public override bool GenerateTerrain(MeshFilter terrainMesh)
		{
			//Vector3 center = Vector3.zero;

			if (terrainMesh == null)
			{
				return false;
			}

			return true;
		}

		public override bool GenerateTerrain(HexasphereGrid.Hexasphere hexSphere)
		{
			if (hexSphere == null)
			{
				return false;
			}

			HexasphereGrid.Tile[] tiles = this.HexSphere.tiles;

			TectonicPlate[] plates = this.BuildPlates(hexSphere);

			return true;
		}

		public TectonicPlate[] BuildPlates(HexasphereGrid.Hexasphere hexSphere)
		{
			TectonicPlate[] plates = new TectonicPlate[this.PlateCount];

			this.UsedTiles = new Dictionary<int, HexasphereGrid.Tile>();

			HexasphereGrid.Tile[] tiles = this.HexSphere.tiles;
			for (int i=0; i<plates.Length; i++)
			{
				int randomIndex = Random.Range(0, tiles.Length);
				plates[i] = new TectonicPlate(this.HexSphere, tiles[randomIndex], this.UsedTiles);
			}

			int maxLoop = 50;
			for (int i=0; i<maxLoop; i++)
			{
				bool addedAny = false;
				for (int j=0; j<plates.Length; j++)
				{
					bool added = plates[j].Fill(this.UsedTiles);
					if (added)
					{
						addedAny = true;
					}
				}

				if (!addedAny)
				{
					Debug.Log("Stopped adding at i=" + i);
					break;
				}
			}

			for (int i=0; i<plates.Length; i++)
			{
				TectonicPlate p = plates[i];
				bool isWater = (p.plateColor == Color.blue);
				float extrudeAmount = isWater ? Random.Range(0.0f, this.SeaLevel) : Random.Range(this.SeaLevel, 1.0f);
				this.HexSphere.SetTileExtrudeAmount(p.Tiles, extrudeAmount);
			}

			Debug.Log("Stopped UsedTiles " + this.UsedTiles.Count + " / " + tiles.Length);

			return plates;
		}

		void Start () 
		{
			//GenerateTerrain();
		}
	}

	public class TectonicPlate 
	{
		public HexasphereGrid.Hexasphere HexSphere;

		public HexasphereGrid.Tile Center;

		public List<HexasphereGrid.Tile> Tiles;

		public List<HexasphereGrid.Tile> Queue;

		public Color plateColor;

		public TectonicPlate(HexasphereGrid.Hexasphere hexSphere, HexasphereGrid.Tile center, Dictionary<int, HexasphereGrid.Tile> usedTiles = null)
		{
			this.HexSphere = hexSphere;
			this.Center = center;
			this.Tiles = new List<HexasphereGrid.Tile>();
			this.Queue = new List<HexasphereGrid.Tile>();

			//this.plateColor = Random.ColorHSV();
			this.plateColor = (Random.value > 0.5f) ? Color.green : Color.blue;
		
			this.AddTile(center, usedTiles);

			hexSphere.SetTileColor(center.index, Color.red);
		}

		private bool AddTile(HexasphereGrid.Tile t, Dictionary<int, HexasphereGrid.Tile> usedTiles = null)
		{
			if (usedTiles != null)
			{
				if (usedTiles.ContainsKey(t.index))
				{
					return false;
				}

				usedTiles[t.index] = t;
			}

			//Debug.Log("Added Tile index=" + t.index);

			this.Tiles.Add(t);

			this.HexSphere.SetTileColor(t.index, this.plateColor);

			for (int j=0; j<t.neighbours.Length; j++)
			{
				this.AddTileQueue(t.neighbours[j], usedTiles);
			}

			return true;
		}

		private bool AddTileQueue(HexasphereGrid.Tile t, Dictionary<int, HexasphereGrid.Tile> usedTiles = null)
		{
			if (usedTiles != null)
			{
				if (usedTiles.ContainsKey(t.index))
				{
					return false;
				}
			}

			this.Queue.Add(t);
			return true;
		}

		public bool Fill(Dictionary<int, HexasphereGrid.Tile> usedTiles)
		{
			List<HexasphereGrid.Tile> q = this.Queue;
			this.Queue = new List<HexasphereGrid.Tile>();

			bool tilesAdded = false;
			HexasphereGrid.Tile[] currentTiles = this.Tiles.ToArray();
			for (int i=0; i<q.Count; i++)
			{
				bool added = this.AddTile(q[i], usedTiles);
				if (added)
				{
					tilesAdded = true;
				}
			}

			q.Clear();

			return tilesAdded;
		}
	}
}

