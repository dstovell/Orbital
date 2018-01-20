using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{

	public class Launcher : MonoBehaviour 
	{
		public bool DoLaunch = false;
		public float ForceAmount = 600.0f;
		public float ForceAngle = 0.0f;

		public Launchable [] LaunchableObjects;

		public void Launch(Launchable l, Vector3 direction, float forceAmount)
		{
			if (l != null)
			{
				l.Launch(direction, forceAmount);
			}
		}

		public void LaunchNext(Vector3 direction, float forceAmount)
		{
			Launchable l = null;
			for (int i=0; i<this.LaunchableObjects.Length; i++)
			{
				if (this.LaunchableObjects[i] != null)
				{
					l = this.LaunchableObjects[i];
					this.LaunchableObjects[i] = null;
					break;
				}
			}

			if (l != null)
			{
				this.Launch(l, direction, forceAmount);
			}
		}

		void Start () 
		{
			
		}
		
		void Update () 
		{
			if (this.DoLaunch)
			{
				this.LaunchNext(Vector3.right, 600);
				this.DoLaunch = false;
			}
		}

		void OnGUI() 
		{
			GUILayout.BeginArea(new Rect(10, 10, 200, 200));

			this.ForceAmount = GUILayout.HorizontalSlider(this.ForceAmount, 1.0F, 1000.0F);
			GUILayout.Box("ForceAmount: " + this.ForceAmount);

			this.ForceAngle = GUILayout.HorizontalSlider(this.ForceAngle, 0.0f, 45.0f);
			GUILayout.Box("ForceAngle: " + this.ForceAngle);

			if (GUILayout.Button("Launch"))
			{
				Vector3 launchDir = Vector3.RotateTowards(Vector3.right, Vector3.up, this.ForceAngle*0.0174533f, 1);

				this.LaunchNext(launchDir, this.ForceAmount);
			}

			GUILayout.EndArea();
		}
	}
}
