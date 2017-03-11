using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LOS_Perception : MonoBehaviour, IPerceptions {

	public bool bDrawVisionRays = true;
	public float FieldOfView	= 30f;
	public float VisionRange	= 20f;
	public float ReactionTime	= 0.25f;
	public List<GameObject> Threats;
	public PlayerMotion m_Player;

	private bool bVisualOnTarget = false;
	public bool HasVisualOnTarget() { return bVisualOnTarget; }

	private float Timer = 0;

	private GameObject m_owner;
	public GameObject Owner
	{
		get { return m_owner; }
		set { m_owner = value; }
	}

	public int m_ignoreMask;
	public int IgnoreMask
	{
		get { return m_ignoreMask; }
		set { m_ignoreMask = value; }
	}
	
	void Start ()
	{
		Threats = new List<GameObject>();
		m_Player = FindObjectOfType<PlayerMotion>();
	}
	
	public void UpdatePerception()
	{
		bVisualOnTarget = false;
		Vector3 PlayerPosition = m_Player.transform.position;

		float DistToPlayer = Vector3.Distance(transform.position, PlayerPosition);
		if (DistToPlayer <= VisionRange)
		{
			Vector3 RightPeriphery = Quaternion.Euler(0, FieldOfView, 0) * transform.forward;
			Vector3 LeftPeriphery = Quaternion.Euler(0, -FieldOfView, 0) * transform.forward;

			if (bDrawVisionRays)
			{
				Debug.DrawRay(transform.position, RightPeriphery * VisionRange, Color.red);
				Debug.DrawRay(transform.position, LeftPeriphery * VisionRange, Color.red);
			}
			
			Vector3 LocalForward = transform.forward.normalized;
			Vector3 ToPlayer = (PlayerPosition - transform.position).normalized;
			float AngleToPlayer = Vector3.Angle(LocalForward, ToPlayer);
			if (AngleToPlayer <= FieldOfView)
			{
				Timer += Time.deltaTime;
				if (Timer >= ReactionTime)
				{
					/// Try to get direct Visual
					RaycastHit hit;
					if (Physics.Raycast(transform.position, ToPlayer * DistToPlayer, out hit))
					{
						GameObject hitObject = hit.transform.gameObject;
						if (hitObject == m_Player.gameObject)
						{
							bVisualOnTarget = true;
							Debug.DrawLine(transform.position, hit.transform.position, Color.white);
							
							/// Log target to Threats
							if (Threats.Count == 0)
							{
								Threats.Add(hitObject);
							}
							else foreach (GameObject obj in Threats)
							{
								if (hitObject != obj)
								{
									Threats.Add(hitObject);
									break;
								}
							}
						}
					}
					///end raycast
					
					Timer = 0f;
				}
			}///out of angle
			
		}///out of distance
	

		if (Threats.Count > 0 && DistToPlayer > VisionRange * 5) //m_threats.Count > 0 && bVisualOnTarget == false && DistToPlayer > VisionRange
		{
			Debug.Log("Cleared Threats");
			Threats.Clear();
		}
	}

	public List<GameObject> GetThreats()
	{
		return Threats;
	}

	
}
