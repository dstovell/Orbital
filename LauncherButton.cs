using UnityEngine;
using System.Collections;

public class LauncherButton : MonoBehaviour
{

	public Orbital.Launcher Launcher;

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
	
	}

	void OnMouseDown()
    {
		if (this.Launcher != null)
		{
			this.Launcher.LaunchNext(Vector3.right, 590);
		}
    }
}

