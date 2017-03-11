using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum EStates
{
	ES_Idle = 0,
	ES_Flee,
	ES_Patrol,
	ES_Watch,
	ES_Chase,
	ES_Dead,
	ES_Rescue,
	ES_Wander,
	ES_Count
}

public class States : ScriptableObject {

	protected AI_Agent m_owner;
	public AI_Agent Owner
	{
		get { return m_owner; }
		set { m_owner = value; }
	}

	protected EStates m_stateId;
	public EStates StateID
	{
		get { return m_stateId; }
		set { m_stateId = value; }
	}

	virtual public void Init() { }
	virtual public void Shutdown() { }
	virtual public void Update() { }
}


public class IdleState : States                                             // IDLE
{
	override public void Init()
	{
		base.Init();
		StateID = EStates.ES_Idle;
		//Debug.Log(Owner.gameObject.name + " Entered Idle");
	}

	override public void Update()
	{
		if (CheckThreats() && TestTransition())
		{
			GameObject newThreat = Owner.Vision.Threats[0];
			if (newThreat)
				Owner.Squad.Notify(newThreat);

			if (Owner.m_personality == AI_Agent.PersonalityType.PT_Aggressive)
				TransitionToChase();

			else if (Owner.m_personality == AI_Agent.PersonalityType.PT_Coward)
				TransitionToFlee();

			else if (Owner.m_personality == AI_Agent.PersonalityType.PT_Careful)
				TransitionToWatch();
		}
	}

	bool CheckThreats()
	{
		bool result = false;
		if (Owner.Vision.Threats.Count > 0)
		{
			return true;
		}
		return result;
	}

	public bool TestTransition()
	{
		if (Owner.Vision.Threats.Count > 0)
		{
			Rigidbody rb = Owner.Vision.Threats[0].GetComponent<Rigidbody>();
			return rb.velocity.sqrMagnitude > 0.0005f;
		}
		return false;
	}

	public void TransitionToChase()
	{
		ChaseState chaseState = CreateInstance<ChaseState>();
		chaseState.Owner = Owner;
		Owner.FSM.ChangeState(chaseState);
	}

	public void TransitionToFlee()
	{
		FleeState fleeState = CreateInstance<FleeState>();
		fleeState.Owner = Owner;
		Owner.FSM.ChangeState(fleeState);
	}

	public void TransitionToWatch()
	{
		WatchState watchState = CreateInstance<WatchState>();
		watchState.Owner = Owner;
		Owner.FSM.ChangeState(watchState);
	}
}
/// end IDLE state


public class WatchState : States                                                // WATCH
{
	Vector3 TargetLastPosition = Vector3.zero;

	public override void Init()
	{
		base.Init();
		StateID = EStates.ES_Watch;
		Owner.SearchLight.gameObject.SetActive(true);
		//Debug.Log(Owner.gameObject.name + " Entered Watch");
	}

	public override void Update()
	{
		base.Update();

		if (Owner.Vision.HasVisualOnTarget())
		{
			if (!Owner.SearchLight.gameObject.activeInHierarchy)
				Owner.SearchLight.gameObject.SetActive(true);

			foreach (GameObject obj in Owner.Vision.Threats)
			{
				/// Aim Heat Ray
				Vector3 ThreatPosition = obj.transform.position;
				Vector3 LocalPosition = Owner.SearchLight.position;
				Owner.SearchLight.GetComponent<LineRenderer>().SetPosition(0, LocalPosition);
				Owner.SearchLight.GetComponent<LineRenderer>().SetPosition(1, ThreatPosition);

				/// Face to Target
				Vector3 FaceDirection = (ThreatPosition - Owner.transform.position).normalized;
				Vector3 LookDirection = Vector3.Slerp(Owner.transform.forward, FaceDirection, Time.deltaTime * 0.77f);
				Owner.transform.rotation = Quaternion.LookRotation(LookDirection);
				Rigidbody rb = Owner.GetComponent<Rigidbody>();
				if (rb)
				{
					rb.angularVelocity *= 0.99f;
				}
			}

			TargetLastPosition = Owner.Vision.Threats[0].transform.position;
		}
		else/// target is beyond range and angle
		{
			Owner.SearchLight.gameObject.SetActive(false);
			VisuallyInvestigate(TargetLastPosition);
		}
	}

	public void VisuallyInvestigate(Vector3 InterestPoint)
	{
		Debug.Log(Owner.gameObject.name + " investigating lost target");
		Vector3 FaceDirection = (InterestPoint - Owner.transform.position).normalized;
		Vector3 LookDirection = Vector3.Slerp(Owner.transform.forward, FaceDirection, Time.deltaTime * 0.77f);
		Owner.transform.rotation = Quaternion.LookRotation(LookDirection);

		if (Owner.transform.rotation == Quaternion.Euler(LookDirection))
		{
			if (Owner.Vision.Threats.Count == 0)
			{
				IdleState idleState = CreateInstance<IdleState>();
				idleState.Owner = Owner;
				Owner.FSM.ChangeState(idleState);
				Debug.Log(Owner.gameObject.name + " went to Idle");
			}
		}
	}

	public override void Shutdown()
	{
		Owner.SearchLight.gameObject.SetActive(false);
	}
}
/// end WATCH state


public class FleeState : States                                             // FLEE
{
	Rigidbody rb;
	GameObject lights;
	Navigator navigator;
	GameObject RetreatObj;
	GameObject P_Terror;
	Vector3 RetreatPosition = Vector3.zero;
	float DistanceToThreat = 0;

	public override void Init()
	{
		base.Init();
		StateID = EStates.ES_Flee;
		RetreatObj = Owner.NavPoint;
		P_Terror = Owner.TerrorEffect;
		P_Terror.SetActive(true);
		lights = Owner.EngineLight;
		lights.SetActive(true);

		rb = Owner.GetComponent<Rigidbody>();
		rb.mass = 25;
		navigator = Owner.GetComponent<Navigator>();
		//Debug.Log(Owner.gameObject.name + " Entered Flee");
	}

	public override void Update()
	{
		base.Update();

		Light tempLight = lights.GetComponent<Light>();
		tempLight.intensity = Random.Range(7, 8);

		Vector3 LocalPosition = Owner.transform.position;

		if (Owner.Vision.Threats.Count > 0)
		{
			/// Set Retreat Vector
			Vector3 ThreatPosition = Owner.Vision.Threats[0].transform.position;
			Vector3 AwayFromThreat = (LocalPosition - ThreatPosition).normalized;
			if (RetreatPosition == Vector3.zero)
				RetreatPosition = AwayFromThreat * 800;

			float DistToRetreat = Vector3.Distance(LocalPosition, RetreatPosition);
			DistanceToThreat = Vector3.Distance(ThreatPosition, LocalPosition);

			/// Generate Retreat Destination
			if (RetreatObj.transform.position == Vector3.zero)
			{
				RetreatObj = (GameObject)Instantiate(RetreatObj, RetreatPosition, Quaternion.identity);
				RetreatObj.transform.position = RetreatPosition;
			}

			/// Navigate to Destination
			if (DistToRetreat > Owner.ApproachRange * 1.5f)
			{
				navigator.NavigateTo(RetreatObj.transform);
				Debug.DrawLine(LocalPosition, RetreatObj.transform.position, Color.yellow);
			}
			else
			{
				Owner.Vision.Threats.Clear();
				if (Owner.Vision.HasVisualOnTarget() == false)// && DistanceToThreat > Owner.Vision.VisionRange)
				{
					if (CheckTransitionToWander())
					{
						TransitionToWander();
					}
				}
			}
		}
		else if (DistanceToThreat > Owner.Vision.VisionRange)
		{
			Debug.Log("/////");
			if (CheckTransitionToWander())
			{
				TransitionToWander();
			}
		}
	}

	bool CheckTransitionToWander()
	{
		bool result = false;
		if (Owner.Vision.HasVisualOnTarget() == false)
			result = true;

		return result;
	}

	void TransitionToWander()
	{
		WanderState wanderState = CreateInstance<WanderState>();
		wanderState.Owner = Owner;
		Owner.FSM.ChangeState(wanderState);
	}

	public override void Shutdown()
	{
		rb.mass = 30;
		lights.SetActive(false);
		P_Terror.SetActive(false);
	}

}
/// end FLEE state


public class ChaseState : States												// CHASE
{
	public Rigidbody rb;
	public Navigator navi;
	public float DragReset;
	Transform ChaseTransform;

	public override void Init()
	{
		base.Init();
		StateID = EStates.ES_Chase;
		rb = Owner.GetComponent<Rigidbody>();
		navi = Owner.GetComponent<Navigator>();
		DragReset = Owner.ShipDrag;
		//Debug.Log(Owner.gameObject.name + " Entered Chase");
	}

	public override void Update()
	{
		base.Update();

		if (Owner.Vision.Threats.Count > 0)
		{
			ChaseTransform = Owner.Vision.Threats[0].transform;
			float DistanceToTarget = Vector3.Distance(Owner.transform.position, ChaseTransform.position);
			if (DistanceToTarget > Owner.ApproachRange)
			{
				navi.NavigateTo(ChaseTransform);
				rb.drag = Owner.ShipDrag;
				rb.angularVelocity *= 0.99f;
			}
			else
			{
				rb.drag *= 1.0001f;
				///Debug.Log("drag: " + rb.drag);
			}
		}
		else { InvestigateAt(ChaseTransform.position); }
	}

	public void InvestigateAt(Vector3 CheckPosition)
	{
		float DistanceToTarget = Vector3.Distance(Owner.transform.position, CheckPosition);
		if (DistanceToTarget > Owner.ApproachRange)
		{
			navi.NavigateTo(ChaseTransform);
			rb.drag = Owner.ShipDrag;
			rb.angularVelocity *= 0.99f;
		}
		else
		{
			TransitionToIdle();
		}
	}

	public void TransitionToIdle()
	{
		Shutdown();
		IdleState idleState = CreateInstance<IdleState>();
		idleState.Owner = Owner;
		Owner.FSM.ChangeState(idleState);
	}

	public override void Shutdown()
	{
		rb.drag = Owner.ShipDrag;
	}
}
/// end CHASE state


public class PatrolState : States                                               // PATROL
{
	int NumPatrolPoints = 4;
	int m_patrolIndex = 0;
	float PatrolRadius;
	Vector3 PatrolOrigin;
	List<Transform> patrolPath;
	Transform CurrentDestination;
	Navigator navigator;
	GameObject NavPointPrefab;

	override public void Init()
	{
		base.Init();
		StateID = EStates.ES_Patrol;
		PatrolOrigin = Owner.PatrolCentre.position;

		navigator = Owner.GetComponent<Navigator>();
		NavPointPrefab = Owner.NavPoint;
		patrolPath = new List<Transform>();
		CreatePatrolPath();

		//Debug.Log(Owner.gameObject.name + " Entered Patrol at range " + PatrolRadius);
	}

	public override void Update()
	{
		base.Update();

		/// Visualise patrol plan
		foreach (Transform point in patrolPath)
			Debug.DrawLine(PatrolOrigin, point.position, Color.green);

		/// Patrol
		if (CheckDestination())
			OnReachDestination();
		else
			GoToDestination();

		/// Transitions
		if (CheckTransitionToWander())
			TransitionToWander();

		if (CheckTransitionToFlee())
			TransitionToFlee();
	}

	void CreatePatrolPath()
	{
		PatrolRadius = Owner.WanderRange;
		float AnglePerPoint = 360.0f / NumPatrolPoints;
		Vector3 Radius = PatrolOrigin + Vector3.forward * PatrolRadius;

		for (int i = 0; i < NumPatrolPoints; i++)
		{
			Radius = Quaternion.Euler(0, AnglePerPoint, 0) * Radius;

			GameObject newOBJ = Instantiate(NavPointPrefab, Radius, Quaternion.identity) as GameObject;
			newOBJ.transform.position = Radius;
			Transform newT = newOBJ.transform;
			patrolPath.Add(newT);
		}
	}

	bool CheckDestination()
	{
		bool result = false;
		CurrentDestination = patrolPath.ElementAt(m_patrolIndex);

		float dist = Vector3.Distance(Owner.transform.position, CurrentDestination.position);
		if (dist <= Owner.ApproachRange)
			result = true;

		return result;
	}

	void OnReachDestination()
	{
		m_patrolIndex++;
		m_patrolIndex = m_patrolIndex >= patrolPath.Count ? 0 : m_patrolIndex;
	}

	void GoToDestination()
	{
		Transform CurrentTransform = patrolPath[m_patrolIndex].transform;
		navigator.NavigateTo(CurrentTransform);
		///Debug.DrawLine(Owner.transform.position, CurrentTransform.position, Color.white);
	}

	bool CheckTransitionToWander()
	{
		bool result = false;
		if (Input.GetButtonDown("Submit"))
			result = true;

		return result;
	}

	bool CheckTransitionToFlee()
	{
		bool result = false;
		if (Owner.Vision.Threats.Count > 0 || Owner.Vision.HasVisualOnTarget())
		{
			GameObject newThreat = Owner.Vision.Threats[0];
			if (newThreat && Owner.Squad)
				Owner.Squad.Notify(newThreat);
			result = true;
		}
		return result;
	}

	void TransitionToWander()
	{
		WanderState wanderState = CreateInstance<WanderState>();
		wanderState.Owner = Owner;
		Owner.FSM.ChangeState(wanderState);
	}

	void TransitionToFlee()
	{
		FleeState fleeState = CreateInstance<FleeState>();
		fleeState.Owner = Owner;
		Owner.FSM.ChangeState(fleeState);
	}
}
/// end PATROL state


public class DeadState : States {												// DEAD

	bool bCallSent = false;
	GameObject EngineFX;

	public override void Init()
	{
		EngineFX = Owner.GetComponent<Navigator>().EngineFX;
		if (EngineFX)
			EngineFX.SetActive(false);
		else
			Debug.Log("No engine");
		base.Init();
		StateID = EStates.ES_Dead;
		//Debug.Log(Owner.gameObject.name + " entered Death");
	}

	public override void Update()
	{
		base.Update();

		if (!bCallSent)
			DistressCall();
	}

	public void DistressCall()
	{
		Vector3 LocalPosition = Owner.transform.position;
		Collider[] surrounding = Physics.OverlapSphere(LocalPosition, 5000, -1);
		foreach (Collider col in surrounding)
		{
			if (col.GetComponent<AI_Agent>())
			{
				RaycastHit hit;
				if (Physics.Linecast(LocalPosition, col.transform.position, out hit))
				{
					AI_Agent Rescuer = col.GetComponent<AI_Agent>();
					if (Rescuer.m_personality == AI_Agent.PersonalityType.PT_Aggressive)
					{
						if (Rescuer.GetCurrentState().StateID != EStates.ES_Dead)
						{
							RescueState rescueState = CreateInstance<RescueState>();
							rescueState.Owner = Rescuer;
							rescueState.RescueTransform = Owner.transform;
							Rescuer.FSM.ChangeState(rescueState);
							Debug.Log(Owner.transform.name + " Sent Distress Call");

							if (Rescuer.FSM.GetCurrentState().StateID == EStates.ES_Rescue)
								bCallSent = true;
							break;
						}
					}
				}
			}
		}
	}///end DistressCall()

	public override void Shutdown()
	{
		//
	}
}
/// end DEAD state


public class RescueState : States                                                // RESCUE
{
	public Transform RescueTransform;
	Rigidbody rb;
	Navigator navigator;

	public override void Init()
	{
		base.Init();
		StateID = EStates.ES_Rescue;
		rb = Owner.GetComponent<Rigidbody>();
		rb.drag = Owner.ShipDrag;
		navigator = Owner.GetComponent<Navigator>();
		//Debug.Log(Owner.gameObject.name + " entered Rescue");
	}

	public override void Update()
	{
		base.Update();

		if (CheckTransitionToFlee())
			TransitionToFlee();

		Vector3 RescuePostion = RescueTransform.position;
		float DistToTarget = Vector3.Distance(Owner.transform.position, RescuePostion);

		if (DistToTarget > Owner.RescueRange)
			navigator.NavigateTo(RescueTransform);//Approach(RescuePostion);
		else
			Revive(RescuePostion);
	}

	void Revive(Vector3 TargetPositon)
	{
		Debug.Log(Owner.gameObject.name + " reviving");

		/// Decelerate to heal
		if (rb)
		{
			rb.velocity *= 0.975f;
		}

		/// Heal
		Vector3 LocalPosition = Owner.transform.position;
		Vector3 ToTarget = TargetPositon - LocalPosition;
		Debug.DrawRay(LocalPosition, ToTarget, Color.green);
		RaycastHit hit;
		if (Physics.Raycast(LocalPosition, ToTarget, out hit))
		{
			if (hit.transform.GetComponent<Health>())
			{
				Health otherHealth = hit.transform.GetComponent<Health>();
				if (otherHealth.GetHealth() < otherHealth.MaxHealth)
				{
					otherHealth.Heal(0.01f);
					//Debug.Log(hit.transform.name + " health: " + otherHealth.GetHealth());
				}
				else/// heal complete
				{
					if (otherHealth.GetComponent<AI_Agent>().GetCurrentState().StateID != EStates.ES_Idle)
					{
						WanderState wanderState = CreateInstance<WanderState>();
						wanderState.Owner = otherHealth.GetComponent<AI_Agent>();
						otherHealth.GetComponent<AI_Agent>().FSM.ChangeState(wanderState);

						WanderState wander = CreateInstance<WanderState>();
						wander.Owner = Owner;
						Owner.FSM.ChangeState(wander);
					}
				}
			}
		}
	}///end Res()

	bool CheckTransitionToFlee()
	{
		bool result = false;
		if (Owner.Vision.Threats.Count > 0)
		{
			result = true;
		}
		return result;
	}

	void TransitionToFlee()
	{
		FleeState fleeState = CreateInstance<FleeState>();
		fleeState.Owner = Owner;
		Owner.FSM.ChangeState(fleeState);
	}

	public override void Shutdown()
	{
		base.Shutdown();
	}
}
/// end RESCUE state


public class WanderState : States
{
	Rigidbody rb;
	Navigator navi;
	GameObject CurrentTarget = null;
	GameObject NavPointPrefab;
	float MaxRange;
	bool isMoving;

	public override void Init()
	{
		base.Init();
		StateID		= EStates.ES_Rescue;
		rb			= Owner.GetComponent<Rigidbody>();
		rb.drag		= Owner.ShipDrag;
		MaxRange	= Owner.WanderRange;
		navi		= Owner.GetComponent<Navigator>();
		NavPointPrefab = Owner.NavPoint;
		CurrentTarget = GeneratePoint(MaxRange);

		//Debug.Log(Owner.gameObject.name + " entered Wander");
	}

	public override void Update()
	{
		base.Update();

		if (CheckTransitionToFlee())
		{
			GameObject newThreat = Owner.Vision.Threats[0];
			if (newThreat && Owner.Squad)
				Owner.Squad.Notify(newThreat);
			TransitionToFlee();
		}

		Vector3 LocalPosition = Owner.transform.position;

		if (CurrentTarget)
		{
			Vector3 TargetPosition = CurrentTarget.transform.position;
			///Debug.DrawLine(LocalPosition, TargetPosition, Color.white);

			float DistToTarget = Vector3.Distance(LocalPosition, TargetPosition);
			if (DistToTarget >= Owner.ApproachRange)
			{
				navi.NavigateTo(CurrentTarget.transform);
				rb.drag = Owner.ShipDrag;
				rb.angularVelocity *= 0.99f;
				isMoving = true;
			}
			else if (isMoving)/// reached destination
			{
				Destroy(CurrentTarget);
				isMoving = false;
				CurrentTarget = GeneratePoint(MaxRange);
				rb.velocity *= 0.5f;
			}
		}
		else { CurrentTarget = GeneratePoint(MaxRange); }
	}

	public GameObject GeneratePoint(float range)
	{
		Vector3 PointPosition = Vector3.zero + Random.insideUnitSphere * range;
		PointPosition.y *= 0.66f;/// kern vertical pos for "gameplay" lol
		GameObject result = (GameObject)Instantiate(NavPointPrefab, PointPosition, Quaternion.identity);
		return result;
	}

	bool CheckTransitionToFlee()
	{
		bool result = false;
		if (Owner.Vision.Threats.Count > 0)
			result = true;
		return result;
	}

	void TransitionToFlee()
	{
		FleeState fleeState = CreateInstance<FleeState>();
		fleeState.Owner = Owner;
		Owner.FSM.ChangeState(fleeState);
	}

	public override void Shutdown()
	{
		base.Shutdown();
	}
}
/// end WANDER state