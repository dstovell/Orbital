using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Vectrosity;

namespace Orbital
{
	public class Objective : MonoBehaviour
	{
		public enum ActiveState
		{
			None,
			Active,
			Completed
		}
		public ActiveState State;

		public static List<Objective> Objectives = new List<Objective>();

		public static List<Objective> GetObjectivesOfState(ActiveState s)
		{
			List<Objective> ol = new List<Objective>();

			for (int i=0; i<Objectives.Count; i++)
			{
				if (Objectives[i].State == s)
				{
					ol.Add(Objectives[i]);
				}
			}
			return ol;
		}

		public static int GetObjectivesCountOfState(ActiveState s)
		{
			int count = 0;

			for (int i=0; i<Objectives.Count; i++)
			{
				if (Objectives[i].State == s)
				{
					count++;
				}
			}
			return count;
		}

		void OnEnable() 
		{
			Objective.Objectives.Add(this);
			OnBegin();
		}

		void OnDisable() 
		{
			Objective.Objectives.Remove(this);
		}

		public virtual bool IsCompleted()
		{
			return false;
		}

		public virtual void OnBegin()
		{
		}

		public virtual void OnComplete()
		{
		}

		void Update()
		{
			if (this.State == ActiveState.Active)
			{
				if (this.IsCompleted())
				{
					this.OnComplete();
					this.State = ActiveState.Completed;
				}
			}
		}
	}

	public class OrbitObjective : Orbital.Objective
	{
		public int OrbitCollidersCount = 10;
		public float InnerBoundry = 12;
		public float OuterBoundry = 25;
		public ObjectiveTrigger [] Triggers;

		public override bool IsCompleted()
		{
			if ((this.Triggers == null) || (this.Triggers.Length == 0))
			{
				return false;
			}

			ObjectiveTrigger firstTrigger = this.Triggers[0];
			for (int i=0; i<firstTrigger.Triggered.Count; i++)
			{
				Orbiter ob = firstTrigger.Triggered[i];
				for (int j=0; j<this.Triggers.Length; j++)
				{
					if (!this.Triggers[j].HasTriggered(ob))
					{
						return false;
					}
				}
			}
			return true;
		}

		public override void OnBegin()
		{
			this.Triggers = new ObjectiveTrigger[this.OrbitCollidersCount];
			float orbitRadius = Mathf.Lerp(this.InnerBoundry, this.OuterBoundry, 0.5f);
			float triggerRadius = this.OuterBoundry - this.InnerBoundry;
			Vector3 OrbitCenter = Vector3.zero;
			Debug.Log("orbitRadius="+ orbitRadius + " triggerRadius=" + triggerRadius);
			for (int i=0; i<this.OrbitCollidersCount; i++)
			{
				float radians = 2.0f * Mathf.PI * ((float)i/(float)this.OrbitCollidersCount);
				float x = orbitRadius * Mathf.Cos(radians);
				float z = orbitRadius * Mathf.Sin(radians);
				Debug.Log("i=" + i + " radians="+ radians + " x=" + x + " z=" + z + " (float)(i/this.OrbitCollidersCount)=" + ((float)i/(float)this.OrbitCollidersCount));

				ObjectiveTrigger ot = ObjectiveTrigger.Create(OrbitCenter + new Vector3(x, 0.0f, z), triggerRadius);
				this.Triggers[i] = ot;
			}
		}

		public override void OnComplete()
		{
		}
	}
}
