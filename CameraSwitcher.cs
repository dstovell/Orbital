using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbital
{
	public class CameraSwitcher : MonoBehaviour 
	{
		public static Orbital.CameraSwitcher Instance = null;

		public KGFOrbitCamSettings PlanetView;
		public KGFOrbitCamSettings LauncherView;

		void Awake() 
		{
			Orbital.CameraSwitcher.Instance = this;
		}

		void Start () 
		{
			this.LoadLauncherView();
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
			
		}
	}
}
