using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vectrosity;

namespace Orbital
{
	public class LaunchPath : MonoBehaviour
	{
		//Line Stuff
		public bool LineOn = false;
		public Texture LineTexture;
		public Color LineColor = Color.white;
		public int LinePointCount = 10;
		public float LineMaxLength = 10;
		public bool continuousUpdate = true;


		private VectorLine PathLine;
		public Vector3 [] PathPoints;

		public Vector3 LineStart;
		public Vector3 LineDirection;
		public float ForceAmount = 600.0f;
		public float ForceAmountMin = 80.0f;
		public float ForceAmountMax = 120.0f;	

		public void Enable()
		{
			this.LineOn = true;
		}

		public void Disable()
		{
			this.LineOn = false;
		}

		// Use this for initialization
		void Start ()
		{
			this.PathLine = new VectorLine("LaunchPathLine", new List<Vector3>(), this.LineTexture, 12.0f, LineType.Continuous);
			this.PathLine.color = this.LineColor;
			this.PathLine.textureScale = 1.0f;

			this.PathPoints = new Vector3[this.LinePointCount];

			for (int i=0; i<this.LinePointCount; i++)
			{
				this.PathLine.points3.Add(Vector3.forward);
			}
		}
		
		// Update is called once per frame
		void Update ()
		{
			if (this.LineOn)
			{
				this.PathLine.active = true;

				float t = Mathf.InverseLerp(this.ForceAmountMin, this.ForceAmountMax, this.ForceAmount);
				float segmentSize = (this.LineMaxLength * t) / this.PathPoints.Length;

				Vector3 delta = Vector3.zero;
				for (int i=0; i<this.PathPoints.Length; i++)
				{
					delta = -1.0f * segmentSize * (float)i * this.LineDirection;
					//this.PathLine.points3[i].Set(delta.x+pos.x, delta.y+pos.y, delta.z+pos.z);
					this.PathPoints[i] = this.LineStart + delta;
					this.PathLine.points3[i] = this.PathPoints[i];
				}

				this.PathLine.color = this.LineColor;
				//this.PathLine.Draw();
				this.PathLine.Draw3D();
			}
			else
			{
				this.PathLine.active = false;
			}
		}
	}
}
