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

			this.BuildPlates();

			this.AdjustTerrainHeights();

			this.AssignTerrainTypes();

			return true;
		}

		public void BuildPlates()
		{
			this.Plates = new TectonicPlate[this.PlateCount];

			this.HexplanetTiles = new Dictionary<int, Tile>();

			List<Tile> tiles = this.HexPlanet.GetTiles();

			for (int i=0; i<this.Plates.Length; i++)
			{
				int randomIndex = Random.Range(0, tiles.Count);
				Tile randomTile = tiles[randomIndex];

				GameObject go = new GameObject("Plate"+i);
				go.transform.position = randomTile.center;
				go.transform.rotation = randomTile.transform.rotation;
				go.transform.SetParent(this.HexPlanet.transform);
				this.Plates[i] = go.AddComponent<TectonicPlate>();

				bool isWater = TerrainGenerator.EvalPercentChance(this.ChanceOfWaterPlate);

				float extrusion = isWater ? (this.SeaLevel / 2.0f) : Random.Range(this.GetMinLandExtude(), this.MaxPlateExtrusion);
				TerrainElevation elevation = this.GetElevation(extrusion);
				Color color = (elevation != null) ? elevation.TerrainColor : Color.black;
				this.Plates[i].PlateSetup(this.HexPlanet, tiles[randomIndex], isWater, extrusion*this.ExtrusionMultiplier, color, this.HexplanetTiles);
			}

			int maxLoop = 100;
			for (int i=0; i<maxLoop; i++)
			{
				bool addedAny = false;
				for (int j=0; j<this.Plates.Length; j++)
				{
					bool added = this.Plates[j].Fill(this.HexplanetTiles, this.ChanceOfFillRequeue);
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

			for (int j=0; j<this.Plates.Length; j++)
			{
				this.Plates[j].SetupTileInfo();
			}

			Debug.Log("Stopped HexplanetUsedTiles " + this.HexplanetTiles.Count + " / " + tiles.Count);
		}

		public void AdjustTerrainHeights()
		{
			for (int i=0; i<this.Plates.Length; i++)
			{
				TectonicPlate plate = this.Plates[i];
				float extrusion = plate.extrusion;
				bool isWater = plate.isWater;
				for (int j=0; j<plate.PressureEdgeTiles.Count; j++)
				{
					Tile tile = plate.PressureEdgeTiles[j];
					float tileExtrusion = extrusion;

					for (int k=0; k<tile.neighborTiles.Count; k++)
					{
						Tile otherTile = tile.neighborTiles[k];
						TectonicPlate otherPlate = TectonicPlate.GetTilePlate(otherTile);

						if ((otherTile != null) && (otherPlate != null))
						{
							bool isOtherWater = otherPlate.isWater;
							bool isOtherPressureTile = otherPlate.IsPressureEdgeTile(otherTile);
							float otherExtrusion = otherPlate.extrusion;

							if (isWater)
							{
								if (isOtherWater)
								{
									if (isOtherPressureTile)
									{
										float chanceOfOceanFaultIsland = 0.2f;
										if (TerrainGenerator.EvalPercentChance(chanceOfOceanFaultIsland))
										{
											tileExtrusion = this.GetMinLandExtude(); //Mathf.Max(this.SeaLevel + tileExtrusion, this.GetMinLandExtude());
										}
									}
								}
								else
								{
								}
							}
							else 
							{

							}
						}
					}

					this.ExtrudeTile(tileExtrusion, tile);
				}


				if (isWater)
				{
					for (int j=0; j<plate.EdgeTiles.Count; j++)
					{
						Tile edgeTile = plate.EdgeTiles[j];
						if (plate.IsShorelineTile(edgeTile, this.SeaLevel))
						{
							this.ExtrudeTile(this.GetMaxWaterExtude(), edgeTile, plate);
						}
					}
				}
			}
		}

		public void AssignTerrainTypes()
		{
			for (int i=0; i<this.Plates.Length; i++)
			{
				TectonicPlate plate = this.Plates[i];
				for (int j=0; j<plate.Tiles.Count; j++)
				{
					Tile t = plate.Tiles[j];
					TerrainElevation elevation = this.GetElevation(t.amountExtruded);
					Color color = (elevation != null) ? elevation.TerrainColor : Color.black;
					t.setColor(color);
				}
			}
		}

		private float GetMaxWaterExtude()
		{
			float topLayer = 0.05f;
			return (this.SeaLevel - topLayer);
		}

		private float GetMinLandExtude()
		{
			float topLayer = 0.05f;
			return (this.SeaLevel + topLayer);
		}

		private void ExtrudeTile(float amount, Tile t, TectonicPlate parentPlate = null)
		{
			float clampedAmount = Mathf.Clamp(amount, 0.0f, 1.0f);
			if (parentPlate) { 
				clampedAmount = parentPlate.isWater ? Mathf.Clamp(amount, 0.0f, this.GetMaxWaterExtude()) : Mathf.Clamp(amount, this.GetMinLandExtude(), 1.0f);
			}

			if (clampedAmount > 0.0f)
			{
				t.ExtrudeAbsolute(clampedAmount);
			}
		}

		void Start() 
		{
		}
	}
}

