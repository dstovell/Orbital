using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vectrosity;

namespace Orbital
{

	public class LinearAccelerator : MonoBehaviour 
	{
		public bool DoLaunch = false;
		public float ForceAmount = 600.0f;
		public float BoostAmount = 50.0f;
		public float ForceAngle = 0.0f;
		public float ForceRotation = 0.0f;

		public float ForceAmountMin = 80.0f;
		public float ForceAmountMax = 120.0f;

		public float ForceAngleMin = 0.0f;
		public float ForceAngleMax = 45.0f;

		public LaunchPath PathRenderer;

		public GameObject [] LaunchableObjects;

		public GameObject LastLaunched = null;

		public GameObject LaunchPoint;

		public void Launch(GameObject launchable)
		{
			Rigidbody rb = launchable.GetComponent<Rigidbody>();
			if (rb != null)
			{
				launchable.SetActive(true);
				rb.isKinematic = false;
				this.LastLaunched = launchable;
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

		public void Boost(GameObject go)
		{
			Rigidbody rb = go.GetComponent<Rigidbody>();
			if (rb != null)
			{
				Vector3 force = this.BoostAmount * rb.velocity.normalized;
				rb.isKinematic = false;
				rb.AddForce(force, ForceMode.Force);
				Debug.Log("Boost applying force");
			}
		}

		void Start()
		{
			if (this.PathRenderer != null)
			{
				this.PathRenderer.ForceAmountMin = this.ForceAmountMin;
				this.PathRenderer.ForceAmountMax = this.ForceAmountMax;
			}
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

    	private void EnableButton(string name)
    	{
			UltimateButton b = UltimateButton.GetUltimateButton(name);
			if ((b != null) && !b.isActiveAndEnabled)
			{
				b.gameObject.SetActive(true);
			}
    	}

		private void DisableButton(string name)
    	{
			UltimateButton b = UltimateButton.GetUltimateButton(name);
			if ((b != null) && b.isActiveAndEnabled)
			{
				b.gameObject.SetActive(false);
			}
    	}

		private void EnableStick(string name)
    	{
			UltimateJoystick s = UltimateJoystick.GetJoystick(name);
			if ((s != null) && !s.isActiveAndEnabled)
			{
				s.gameObject.SetActive(true);
			}
    	}

		private void DisableStick(string name)
    	{
			UltimateJoystick s = UltimateJoystick.GetJoystick(name);
			if ((s != null) && s.isActiveAndEnabled)
			{
				s.gameObject.SetActive(false);
			}
    	}

		void Update () 
		{
			Vector3 angles = this.transform.localRotation.eulerAngles;
			angles.x = this.ForceAngle;
			this.transform.localRotation = Quaternion.Euler(angles);

			if (this.LastLaunched != null)
			{
				this.EnableButton("BoostButton");

				this.DisableButton("LaunchButton");
				this.DisableStick("LaunchStick");

				if (UltimateButton.GetButtonDown("BoostButton"))
				{
					this.Boost(this.LastLaunched);
					this.LastLaunched = null;
				}
			}
			else 
			{
				this.EnableButton("LaunchButton");
				this.EnableStick("LaunchStick");

				this.DisableButton("BoostButton");

				if (UltimateButton.GetButtonDown("LaunchButton"))
				{
					this.LaunchNext();
				}
			}

			if (this.PathRenderer != null)
			{
				this.PathRenderer.ForceAmount = this.ForceAmount;
				this.PathRenderer.LineStart = (this.LaunchPoint != null) ? this.LaunchPoint.transform.position : this.transform.position;
				this.PathRenderer.LineDirection = this.transform.forward;
			}
		}

		void FixedUpdate()
		{
			Vector2 launchStick = UltimateJoystick.GetPosition("LaunchStick");
			if (launchStick.magnitude > 0)
			{
				this.ForceAmount = Mathf.Lerp(this.ForceAmountMin, this.ForceAmountMax, launchStick.magnitude);
			}

			if ((launchStick.x > 0) && (launchStick.y >= 0))
			{
				if (this.PathRenderer != null)
				{
					this.PathRenderer.Enable();
				}
				this.ForceAngle = Vector2.Angle(launchStick, Vector2.right);
				this.ForceAngle = Mathf.Clamp(this.ForceAngle, this.ForceAngleMin, this.ForceAngleMax);
			}
			else if ((launchStick.x < 0) && (launchStick.y <= 0))
			{
				if (this.PathRenderer != null)
				{
					this.PathRenderer.Enable();
				}
				this.ForceAngle = Vector2.Angle(launchStick, Vector2.left);
				this.ForceAngle = Mathf.Clamp(this.ForceAngle, 0.0f, 45.0f);
			}
			else if ((launchStick.x == 0) && (launchStick.y == 0))
			{
				if (this.PathRenderer != null)
				{
					this.PathRenderer.Disable();
				}
			}
		}

		void OnGUI() 
		{
			int left = (int)((float)Screen.width * 0.4f);

			GUILayout.BeginArea(new Rect(left, 10, 300, 200));

			GUILayout.BeginHorizontal();
			GUILayout.Box("ForceAmount: " + this.ForceAmount);
			GUILayout.Box("ForceAngle: " + this.ForceAngle);
			GUILayout.EndHorizontal();

			GUILayout.EndArea();
		}
	}
}
