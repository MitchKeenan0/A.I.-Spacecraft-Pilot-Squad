using UnityEngine;
using System.Collections.Generic;

public class SquadManager : MonoBehaviour {

	// Also handles Game Over

	public List<AI_Agent> m_agentsList;
	public List<AI_Agent> m_squadList;
	int DeadAgentCounter;

	void Start ()
	{
		m_agentsList = new List<AI_Agent>();
		m_squadList = new List<AI_Agent>();

		/// List of ALL agents
		foreach (AI_Agent obj in GameObject.FindObjectsOfType<AI_Agent>())
		{
			m_agentsList.Add(obj.GetComponent<AI_Agent>());
		}

		/// List of SQUAD agents
		foreach(GameObject obj in GameObject.FindGameObjectsWithTag("Squad"))
		{
			m_squadList.Add(obj.GetComponent<AI_Agent>());
		}
	}

	void Update()
	{
		/// Track agents for KIAs
		foreach(AI_Agent agent in m_agentsList)
		{
			float AgentLife = agent.GetComponent<Health>().GetHealth();
			if (AgentLife <= 0.0f)
			{
				DeadAgentCounter++;
			}
			else { DeadAgentCounter = 0; }
		}

		/// All agents killed -> GameOver!
		if (DeadAgentCounter >= m_agentsList.Count)
		{
			Application.LoadLevel("EndScreen");
		}
	}
	
	public void Notify(GameObject threat)
	{
		foreach (AI_Agent agent in m_squadList)
		{
			bool doNotify = true;
			if (agent.Vision.Threats.Count == 0)
			{
				doNotify = true;
			}
			else foreach(GameObject obj in agent.Vision.Threats)
			{
				if (threat == obj)
				{
					doNotify = false;
				}
			}

			if (doNotify)
			{
				agent.Vision.Threats.Add(threat);
			}
		}
	}

	public void StandDown()
	{
		foreach (AI_Agent localAgent in m_squadList)
		{
			localAgent.Vision.Threats.Clear();
		}
	}
}
