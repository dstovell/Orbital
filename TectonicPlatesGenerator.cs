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

		public float MinPlateExtrusion = 0.0f;
		public float MaxPlateExtrusion = 0.6f;

		public Dictionary<int, Tile> HexplanetTiles;

		public HexplanetTectonicPlate[] Plates;

		public override bool GenerateTerrain(MeshFilter terrainMesh)
		{
			//Vector3 center = Vector3.zero;

			if (terrainMesh == null)
			{
				return false;
			}

			return true;
		}

		public override bool GenerateTerrain(Hexsphere hexPlanet)
		{
			if (hexPlanet == null)
			{
				return false;
			}

			//Hack...
			float scale = (hexPlanet.detailLevel == 4) ? 2.0f : 2.8f;
			hexPlanet.setWorldScale(scale);

			this.Plates = this.BuildPlates();

			return true;
		}

		public HexplanetTectonicPlate[] BuildPlates()
		{
			HexplanetTectonicPlate[] plates = new HexplanetTectonicPlate[this.PlateCount];

			this.HexplanetTiles = new Dictionary<int, Tile>();

			List<Tile> tiles = this.HexPlanet.GetTiles();

			for (int i=0; i<plates.Length; i++)
			{
				int randomIndex = Random.Range(0, tiles.Count);
				bool isWater = (Random.value < this.ChanceOfWaterPlate);
				float topLayer = 0.05f;
				float extrusion = isWater ? Random.Range(this.MinPlateExtrusion, this.SeaLevel-topLayer) : Random.Range(this.SeaLevel+topLayer, this.MaxPlateExtrusion);
				TerrainElevation elevation = this.GetElevation(extrusion);
				Color color = (elevation != null) ? elevation.TerrainColor : Color.black;
				plates[i] = new HexplanetTectonicPlate(this.HexPlanet, tiles[randomIndex], isWater, extrusion*this.ExtrusionMultiplier, color, this.HexplanetTiles);
			}

			int maxLoop = 100;
			for (int i=0; i<maxLoop; i++)
			{
				bool addedAny = false;
				for (int j=0; j<plates.Length; j++)
				{
					bool added = plates[j].Fill(this.HexplanetTiles, this.ChanceOfFillRequeue);
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

			Debug.Log("Stopped HexplanetUsedTiles " + this.HexplanetTiles.Count + " / " + tiles.Count);

			return plates;
		}

		void Start() 
		{
		}
	}

	public class HexplanetTectonicPlate 
	{
		public Hexsphere HexPlanet;

		public Tile Center;

		public List<Tile> Tiles;

		public List<Tile> Queue;

		public bool isWater = false;
		public Color plateColor;
		public float extrusion;

		public HexplanetTectonicPlate(Hexsphere hexPlanet, Tile center, bool isWater, float _extrusion, Color color, Dictionary<int, Tile> usedTiles = null)
		{
			this.HexPlanet = hexPlanet;
			this.Center = center;
			this.Tiles = new List<Tile>();
			this.Queue = new List<Tile>();

			this.isWater = (Random.value > 0.5f);
			this.extrusion = _extrusion;

			//this.plateColor = Random.ColorHSV();
			this.plateColor = color;
		
			this.AddTile(center, usedTiles);

			//center.setColor(Color.red);
		}

		private bool AddTile(Tile t, Dictionary<int, Tile> usedTiles = null)
		{
			if (usedTiles != null)
			{
				if (usedTiles.ContainsKey(t.id))
				{
					return false;
				}

				usedTiles[t.id] = t;
			}

			this.Tiles.Add(t);

			t.Extrude(this.extrusion);
			t.setColor(this.plateColor);

			for (int j=0; j<t.neighborTiles.Count; j++)
			{
				this.AddTileQueue(t.neighborTiles[j], usedTiles);
			}

			return true;
		}

		private bool AddTileQueue(Tile t, Dictionary<int, Tile> usedTiles = null)
		{
			if (usedTiles != null)
			{
				if (usedTiles.ContainsKey(t.id))
				{
					return false;
				}
			}

			this.Queue.Add(t);
			return true;
		}

		public bool Fill(Dictionary<int, Tile> usedTiles, float chanceOfFillRequeue)
		{
			List<Tile> q = this.Queue;
			this.Queue = new List<Tile>();

			bool tilesAdded = false;
			Tile[] currentTiles = this.Tiles.ToArray();
			for (int i=0; i<q.Count; i++)
			{
				Tile t = q[i];
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

