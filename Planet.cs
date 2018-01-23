using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class Planet : MonoBehaviour 
	{

		[Tooltip ("Texture with elevation data. The red channel is used as the height value.")]
		public Texture2D heightMap;

		[Tooltip ("Texture with water mask data. The alpha channel determines the location of water (values < constant threshold)")]
		public Texture2D waterMask;

		public float rotationSpeed = 1.0f;

		private HexasphereGrid.Hexasphere hexa;

		void Start () 
		{
			this.hexa = this.GetComponent<HexasphereGrid.Hexasphere>();
			if ((this.hexa != null) && (this.heightMap != null) && (this.waterMask != null))
			{
				this.hexa.numDivisions = 20;
				this.hexa.extrudeMultiplier = 0.1f;
				this.hexa.ApplyHeightMap(this.heightMap, this.waterMask);	
			}
		}

		void Update () 
		{
			this.transform.RotateAround(this.transform.position, this.transform.up, this.rotationSpeed*Time.deltaTime);
		}
	}
}
