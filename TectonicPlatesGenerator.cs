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

		public TectonicPlate[] Plates;

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

		public TectonicPlate[] BuildPlates()
		{
			TectonicPlate[] plates = new TectonicPlate[this.PlateCount];

			this.HexplanetTiles = new Dictionary<int, Tile>();

			List<Tile> tiles = this.HexPlanet.GetTiles();

			for (int i=0; i<plates.Length; i++)
			{
				int randomIndex = Random.Range(0, tiles.Count);
				Tile randomTile = tiles[randomIndex];

				GameObject go = new GameObject("Plate"+i);
				go.transform.position = randomTile.center;
				go.transform.rotation = randomTile.transform.rotation;
				go.transform.SetParent(this.HexPlanet.transform);
				plates[i] = go.AddComponent<TectonicPlate>();

				bool isWater = (Random.value < this.ChanceOfWaterPlate);
				float topLayer = 0.05f;
				float extrusion = isWater ? Random.Range(this.MinPlateExtrusion, this.SeaLevel-topLayer) : Random.Range(this.SeaLevel+topLayer, this.MaxPlateExtrusion);
				TerrainElevation elevation = this.GetElevation(extrusion);
				Color color = (elevation != null) ? elevation.TerrainColor : Color.black;
				plates[i].PlateSetup(this.HexPlanet, tiles[randomIndex], isWater, extrusion*this.ExtrusionMultiplier, color, this.HexplanetTiles);
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

			for (int j=0; j<plates.Length; j++)
			{
				plates[j].SetupTileInfo();
			}

			Debug.Log("Stopped HexplanetUsedTiles " + this.HexplanetTiles.Count + " / " + tiles.Count);

			return plates;
		}

		void Start() 
		{
		}
	}
}

