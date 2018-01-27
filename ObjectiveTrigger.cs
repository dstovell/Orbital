using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Orbital
{
	public class ObjectiveTrigger : MonoBehaviour
	{
		public static ObjectiveTrigger Create(Vector3 position, float radius)
		{
			GameObject go = new GameObject("ObjectiveTrigger");
			go.transform.position = position;

			SphereCollider sc = go.AddComponent<SphereCollider>();
			sc.radius = radius;
			sc.isTrigger = true;

			ObjectiveTrigger ot = go.AddComponent<ObjectiveTrigger>();
			return ot;
		}

		public List<Orbiter> Triggered;

		void Awake()
		{
			this.Triggered = new List<Orbiter>();
		}

		public bool HasTriggered(Orbiter ob )
		{
			return this.Triggered.Contains(ob);
		}

		void OnTriggerEnter(Collider other)
		{
			Orbiter ob = other.gameObject.GetComponent<Orbiter>();
			if (ob != null)
			{
				if (!HasTriggered(ob))
				{
					this.Triggered.Add(ob);
				}
			}
		}
	}
}

