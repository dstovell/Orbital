using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{

	public class Sun : MonoBehaviour 
	{
		public float RotationSpeed = 10f;

		void Start () 
		{
			
		}

		void Update () 
		{
			Vector3 rotation = this.transform.rotation.eulerAngles;
			rotation.y += this.RotationSpeed*Time.deltaTime;
			this.transform.rotation = Quaternion.Euler(rotation);
		}
	}
}
