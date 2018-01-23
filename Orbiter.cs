using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class Orbiter : MonoBehaviour 
	{
		public Planet GravitySource;

		public static List<Orbiter> Orbiters = new List<Orbiter>();

		public float OrbitalDistance = 0;

		public static float GetLargestOrbitalDistance()
		{
			float largestDistance = 0;
			for (int i=0; i<Orbiters.Count; i++)
			{
				Orbiter o = Orbiters[i];
				if (o.isActiveAndEnabled && (o.GravitySource != null))
				{
					if (o.OrbitalDistance > largestDistance)
					{
						largestDistance = o.OrbitalDistance;
					}
				}
			}
			return largestDistance;
		}

		void OnEnable() 
		{
			if (this.transform.parent != null)
			{
				this.GravitySource = this.transform.parent.gameObject.GetComponent<Planet>();
			}
			Orbiter.Orbiters.Add(this);
		}

		void OnDisable() 
		{
			Orbiter.Orbiters.Remove(this);
		}

		void Update() 
		{
			if (this.GravitySource != null)
			{
				this.OrbitalDistance = Vector3.Distance(this.transform.position, this.GravitySource.transform.position);
			}
		}
	}
}
