using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class TectonicPlatesGenerator : TerrainGenerator 
	{
		public int PlateCount = 10;
		public float ChanceOfFillRequeue = 0.8f;
		public float ChanceOfWaterPlate = 0.5f;

		public Dictionary<int, HexasphereGrid.Tile> HexasphereUsedTiles;

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

		public override bool GenerateTerrain(Hexsphere hexPlanet)
		{
			hexPlanet.setWorldScale(2.8f);

			Tile[] tiles = hexPlanet.gameObject.GetComponentsInChildren<Tile>();

			Debug.Log("HexPlanet found " + tiles.Length);

			return false;
		}

		public TectonicPlate[] BuildPlates(HexasphereGrid.Hexasphere hexSphere)
		{
			TectonicPlate[] plates = new TectonicPlate[this.PlateCount];

			this.HexasphereUsedTiles = new Dictionary<int, HexasphereGrid.Tile>();

			HexasphereGrid.Tile[] tiles = this.HexSphere.tiles;
			for (int i=0; i<plates.Length; i++)
			{
				int randomIndex = Random.Range(0, tiles.Length);
				bool isWater = (Random.value < this.ChanceOfWaterPlate);
				float extrusion = isWater ? Random.Range(0.0f, this.SeaLevel) : Random.Range(this.SeaLevel, 1.0f);
				TerrainElevation elevation = this.GetElevation(extrusion);
				Color color = (elevation != null) ? elevation.TerrainColor : Color.black;
				plates[i] = new TectonicPlate(this.HexSphere, tiles[randomIndex], isWater, extrusion, color, this.HexasphereUsedTiles);
			}

			int maxLoop = 100;
			for (int i=0; i<maxLoop; i++)
			{
				bool addedAny = false;
				for (int j=0; j<plates.Length; j++)
				{
					bool added = plates[j].Fill(this.HexasphereUsedTiles, this.ChanceOfFillRequeue);
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
				this.HexSphere.SetTileExtrudeAmount(p.Tiles, p.extrusion);
			}

			Debug.Log("Stopped UsedTiles " + this.HexasphereUsedTiles.Count + " / " + tiles.Length);

			return plates;
		}

		void Start() 
		{
			if (this.HexSphere != null)
			{
				this.HexSphere.numDivisions = this.HexDivsions;
			}
		}
	}

	public class TectonicPlate 
	{
		public HexasphereGrid.Hexasphere HexSphere;

		public HexasphereGrid.Tile Center;

		public List<HexasphereGrid.Tile> Tiles;

		public List<HexasphereGrid.Tile> Queue;

		public bool isWater = false;
		public Color plateColor;
		public float extrusion;

		public TectonicPlate(HexasphereGrid.Hexasphere hexSphere, HexasphereGrid.Tile center, bool isWater, float _extrusion, Color color, Dictionary<int, HexasphereGrid.Tile> usedTiles = null)
		{
			this.HexSphere = hexSphere;
			this.Center = center;
			this.Tiles = new List<HexasphereGrid.Tile>();
			this.Queue = new List<HexasphereGrid.Tile>();

			this.isWater = (Random.value > 0.5f);
			this.extrusion = _extrusion;

			//this.plateColor = Random.ColorHSV();
			this.plateColor = color;
		
			this.AddTile(center, usedTiles);

			//hexSphere.SetTileColor(center.index, Color.red);
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

		public bool Fill(Dictionary<int, HexasphereGrid.Tile> usedTiles, float chanceOfFillRequeue)
		{
			List<HexasphereGrid.Tile> q = this.Queue;
			this.Queue = new List<HexasphereGrid.Tile>();

			bool tilesAdded = false;
			HexasphereGrid.Tile[] currentTiles = this.Tiles.ToArray();
			for (int i=0; i<q.Count; i++)
			{
				HexasphereGrid.Tile t = q[i];
				bool requeue = (Random.value < chanceOfFillRequeue);
				if (requeue)
				{
					tilesAdded = true;
					this.AddTileQueue(t, usedTiles);
					continue;
				}

				bool added = this.AddTile(t, usedTiles);
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

