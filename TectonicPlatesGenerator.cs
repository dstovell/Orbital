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

		public Dictionary<int, Tile> HexplanetUsedTiles;

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

			HexasphereTectonicPlate[] plates = this.BuildPlates(hexSphere);

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

			HexplanetTectonicPlate[] plates = this.BuildPlates(hexPlanet);

			return true;
		}

		public HexasphereTectonicPlate[] BuildPlates(HexasphereGrid.Hexasphere hexSphere)
		{
			HexasphereTectonicPlate[] plates = new HexasphereTectonicPlate[this.PlateCount];

			this.HexasphereUsedTiles = new Dictionary<int, HexasphereGrid.Tile>();

			HexasphereGrid.Tile[] tiles = this.HexSphere.tiles;
			for (int i=0; i<plates.Length; i++)
			{
				int randomIndex = Random.Range(0, tiles.Length);
				bool isWater = (Random.value < this.ChanceOfWaterPlate);
				float extrusion = isWater ? Random.Range(0.0f, this.SeaLevel) : Random.Range(this.SeaLevel, 1.0f);
				TerrainElevation elevation = this.GetElevation(extrusion);
				Color color = (elevation != null) ? elevation.TerrainColor : Color.black;
				plates[i] = new HexasphereTectonicPlate(this.HexSphere, tiles[randomIndex], isWater, extrusion, color, this.HexasphereUsedTiles);
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
				HexasphereTectonicPlate p = plates[i];
				this.HexSphere.SetTileExtrudeAmount(p.Tiles, p.extrusion);
			}

			Debug.Log("Stopped HexasphereUsedTiles " + this.HexasphereUsedTiles.Count + " / " + tiles.Length);

			return plates;
		}

		public HexplanetTectonicPlate[] BuildPlates(Hexsphere hexPlanet)
		{
			HexplanetTectonicPlate[] plates = new HexplanetTectonicPlate[this.PlateCount];

			this.HexplanetUsedTiles = new Dictionary<int, Tile>();

			Tile[] tiles = hexPlanet.gameObject.GetComponentsInChildren<Tile>();
			Debug.Log("HexPlanet found " + tiles.Length);

			for (int i=0; i<plates.Length; i++)
			{
				int randomIndex = Random.Range(0, tiles.Length);
				bool isWater = (Random.value < this.ChanceOfWaterPlate);
				float topLayer = 0.05f;
				float extrusion = isWater ? Random.Range(0.0f, this.SeaLevel-topLayer) : Random.Range(this.SeaLevel+topLayer, 1.0f);
				TerrainElevation elevation = this.GetElevation(extrusion);
				Color color = (elevation != null) ? elevation.TerrainColor : Color.black;
				plates[i] = new HexplanetTectonicPlate(hexPlanet, tiles[randomIndex], isWater, extrusion*this.ExtrusionMultiplier, color, this.HexplanetUsedTiles);
			}

			int maxLoop = 100;
			for (int i=0; i<maxLoop; i++)
			{
				bool addedAny = false;
				for (int j=0; j<plates.Length; j++)
				{
					bool added = plates[j].Fill(this.HexplanetUsedTiles, this.ChanceOfFillRequeue);
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

			Debug.Log("Stopped HexplanetUsedTiles " + this.HexplanetUsedTiles.Count + " / " + tiles.Length);

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

	public class HexasphereTectonicPlate 
	{
		public HexasphereGrid.Hexasphere HexSphere;

		public HexasphereGrid.Tile Center;

		public List<HexasphereGrid.Tile> Tiles;

		public List<HexasphereGrid.Tile> Queue;

		public bool isWater = false;
		public Color plateColor;
		public float extrusion;

		public HexasphereTectonicPlate(HexasphereGrid.Hexasphere hexSphere, HexasphereGrid.Tile center, bool isWater, float _extrusion, Color color, Dictionary<int, HexasphereGrid.Tile> usedTiles = null)
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

