using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{

	public class LinearAccelerator : MonoBehaviour 
	{
		public bool DoLaunch = false;
		public float ForceAmount = 600.0f;
		public float ForceAngle = 0.0f;
		public float ForceRotation = 0.0f;

		public GameObject [] LaunchableObjects;

		public void Launch(GameObject launchable)
		{
			Rigidbody rb = launchable.GetComponent<Rigidbody>();
			if (rb != null)
			{
				launchable.SetActive(true);
				rb.isKinematic = false;
			}
		}

		public void LaunchNext()
		{
			GameObject l = null;
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
				this.Launch(l);
			}

			Orbital.CameraSwitcher.Instance.LoadPlanetView();
		}


		void OnTriggerStay(Collider other)
    	{
			Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
			if (rb != null)
			{
				Vector3 force = -1.0f * this.ForceAmount * this.transform.forward;
				rb.isKinematic = false;
				rb.AddForce(force, ForceMode.Force);
				Debug.Log("OnTriggerStay applying force");
			}
    	}

		void OnTriggerExit(Collider other)
    	{
			Rigidbody rb = other.gameObject.GetComponent<Rigidbody>();
			if (rb != null)
			{
				//other.gameObject.transform.parent = null;
			}
    	}

		void Update () 
		{
			Vector3 angles = this.transform.localRotation.eulerAngles;
			angles.x = this.ForceAngle;
			this.transform.localRotation = Quaternion.Euler(angles);

			//Vector3 baseAngles = this.transform.parent.localRotation.eulerAngles;
			//baseAngles.y = this.ForceRotation;
			//this.transform.parent.localRotation = Quaternion.Euler(baseAngles);
		}

		void OnGUI() 
		{
			GUILayout.BeginArea(new Rect(25, 10, 225, 200));

			this.ForceAmount = GUILayout.HorizontalSlider(this.ForceAmount, 50.0F, 200.0F);
			GUILayout.Box("ForceAmount: " + this.ForceAmount);

			this.ForceAngle = GUILayout.HorizontalSlider(this.ForceAngle, 0.0f, 45.0f);
			GUILayout.Box("ForceAngle: " + this.ForceAngle);

			//this.ForceRotation = GUILayout.HorizontalSlider(this.ForceRotation, 0.0f, 45.0f);
			//GUILayout.Box("ForceRotation: " + this.ForceRotation);

			if (GUILayout.Button("Launch"))
			{
				this.LaunchNext();
			}

			GUILayout.EndArea();
		}
	}
}
