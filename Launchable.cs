using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class Launchable : MonoBehaviour 
	{
		public Rigidbody RB;

		public void Launch(Vector3 direction, float forceAmount)
		{
			this.gameObject.SetActive(true);
			if (this.RB != null)
			{
				Vector3 force = forceAmount * direction;
				this.RB.isKinematic = false;
				this.RB.AddForce(force, ForceMode.Force);
			}
		}

		void Awake() 
		{
			this.RB = this.GetComponent<Rigidbody>();
		}
		
		void Update () 
		{
			
		}
	}
}
