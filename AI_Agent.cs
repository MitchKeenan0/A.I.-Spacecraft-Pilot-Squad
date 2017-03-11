using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AI_Agent : MonoBehaviour {

	public enum PersonalityType
	{
		PT_Coward = 1,
		PT_Aggressive = 1 << 1,
		PT_Careful = 1 << 2,
		PT_ALL = PT_Coward | PT_Aggressive | PT_Careful
	};

	public PersonalityType m_personality;
	public Vector3 botMaxSpeed;
	public List<IPerceptions> m_perceptions;
	public float RescueRange = 200;
	public float ApproachRange = 80;
	public float WanderRange = 800;
	public float SafeZoneSize = 350;
	public float ShipDrag = 0.5f;
	public float threatRadius;
	public float minThreatRadius;
	public int m_ignoreMask;
	public SquadManager Squad;
	public Transform PatrolCentre;
	public GameObject NavPoint;
	public Transform SearchLight;
	public GameObject EngineLight;
	public GameObject CollisionEffect;
	public GameObject TerrorEffect;

	private int m_groundLayerMask = 1 << 8;
	private int m_botLayerMask = 1 << 9;
	private Vector3 desiredVelocityRatio = Vector3.zero;

	private Navigator navigator;
	public Navigator Navi
	{
		get { return navigator; }
		set { navigator = value; }
	}

	private LOS_Perception m_vision;
	public LOS_Perception Vision
	{
		get { return m_vision; }
		set { m_vision = value; }
	}

	private NewFSM m_fsm;
	public NewFSM FSM
	{
		get { return m_fsm; }
		set { m_fsm = value; }
	}

	public States GetCurrentState()
	{
		return m_fsm.m_states.Peek();
	}

	void Start()
	{
		m_ignoreMask = ~(m_groundLayerMask | m_botLayerMask);
		m_vision = GetComponent<LOS_Perception>();
		m_vision.Owner = gameObject;
		m_vision.IgnoreMask = m_ignoreMask;

		m_perceptions = new List<IPerceptions>();
		m_perceptions.Add(m_vision);

		m_fsm = ScriptableObject.CreateInstance<NewFSM>();
		m_fsm.Init();
		InitFirstState();

		navigator = GetComponent<Navigator>();
		navigator.Init();

		SearchLight.gameObject.SetActive(false);
	}

	void Update()
	{
		m_fsm.Update();

		/// Percieve only if not dead
		if (m_fsm.GetCurrentState().StateID != EStates.ES_Dead)
		{
			m_vision.UpdatePerception();
		}
	}

	public void InitFirstState()
	{
		PatrolState patrolState = ScriptableObject.CreateInstance<PatrolState>();
		patrolState.Owner = this;
		m_fsm.ChangeState(patrolState);
	}

	public void Die()
	{
		DeadState deadState = ScriptableObject.CreateInstance<DeadState>();
		deadState.Owner = this;
		m_fsm.ChangeState(deadState);
	}

	void OnCollisionEnter(Collision col)
	{
		Vector3 contact = col.contacts[0].point;
		Instantiate(CollisionEffect, contact, Quaternion.identity);
		Debug.Log("*** Collision!  " + Time.time + " ***");

		if (col.transform.tag == "Player")
		{
			GetComponent<Health>().TakeDamageAtPoint(100, transform.position);
		}
	}
}

