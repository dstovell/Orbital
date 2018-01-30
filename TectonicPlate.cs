using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Orbital
{
	public class TectonicPlate : MonoBehaviour
	{
		public Hexsphere HexPlanet;

		public Tile Center;

		public List<Tile> Tiles;
		public Dictionary<int, Tile> TileMap;
		public List<Tile> EdgeTiles;

		private List<Tile> Queue;

		public bool isWater = false;
		public Color plateColor;
		public float extrusion;

		public Vector2 PressureDir;

		public void PlateSetup(Hexsphere hexPlanet, Tile center, bool isWater, float _extrusion, Color color, Dictionary<int, Tile> usedTiles = null)
		{			
			this.HexPlanet = hexPlanet;
			this.Center = center;
			this.Tiles = new List<Tile>();
			this.Queue = new List<Tile>();
			this.TileMap = new Dictionary<int, Tile>();
			this.EdgeTiles = new List<Tile>();

			this.isWater = (Random.value > 0.5f);
			this.extrusion = _extrusion;

			this.plateColor = Random.ColorHSV();
			//this.plateColor = color;
		
			this.AddTile(center, usedTiles);

			//center.setColor(Color.red);
			this.PressureDir = Random.insideUnitCircle;
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
			this.TileMap[t.id] = t;

			t.Extrude(this.extrusion);
			t.setColor(this.plateColor);

			t.transform.SetParent(this.transform);

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

		public void SetupTileInfo()
		{
			for (int i=0; i<this.Tiles.Count; i++)
			{
				Tile t = this.Tiles[i];
				int edgeCount = this.GetTileEdgeCount(t);
				if (edgeCount > 0)
				{
					this.EdgeTiles.Add(t);

					if (this.IsPressureEdgeTile(t))
					{
						//Color c = this.plateColor;
						//c.r = 1f;
						//t.setColor(c);
						t.gameObject.name = "PressureEdge";
					}
				}
			}
		}

		public bool ContainsTile(Tile t)
		{
			if ((t == null) || (this.TileMap == null))
			{
				return false;
			}

			return this.TileMap.ContainsKey(t.id);
		}

		public bool IsEdgeTile(Tile t)
		{
			return (this.GetTileEdgeCount(t) > 0);
		}

		public int GetTileEdgeCount(Tile t)
		{
			int count = 0;
			if (!this.ContainsTile(t))
			{
				return count;
			}

			for (int j=0; j<t.neighborTiles.Count; j++)
			{
				if (!this.ContainsTile(t.neighborTiles[j]))
				{
					count++;
				}
			}

			return count;
		}

		public bool IsPressureEdgeTile(Tile t)
		{
			Vector3 directionFromCenter = t.center - this.Center.center;

			Vector3 pressureVector = t.transform.forward*this.PressureDir.x + t.transform.right*this.PressureDir.y;

			float angle = Vector3.Angle(directionFromCenter, pressureVector);

			return (angle < 45);
		}
	}

}
