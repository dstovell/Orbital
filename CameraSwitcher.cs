using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class CameraSwitcher : MonoBehaviour 
	{
		public static Orbital.CameraSwitcher Instance = null;
		public static float ZoomBaseline = 22.0f;

		public KGFOrbitCam OrbitCam;
		public KGFOrbitCamSettings PlanetView;
		public KGFOrbitCamSettings LauncherView;

		void Awake() 
		{
			this.OrbitCam = this.GetComponent<KGFOrbitCam>();
			Orbital.CameraSwitcher.Instance = this;
		}

		void Start () 
		{
			this.LoadPlanetView();
		}

		public void LoadPlanetView()
		{
			this.LoadView(this.PlanetView);
		}

		public void LoadLauncherView()
		{
			this.LoadView(this.LauncherView);
		}

		public void LoadView(KGFOrbitCamSettings view)
		{
			if (view != null)
			{
				view.Apply();
			}
		}
		
		void Update () 
		{
			if (this.OrbitCam != null)
			{
				float maxOrbitalDistance = Orbiter.GetLargestOrbitalDistance();

				float zoom = maxOrbitalDistance + ZoomBaseline;
				this.OrbitCam.SetZoom(zoom);
			}
		}
	}
}
