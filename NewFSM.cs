using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NewFSM : ScriptableObject {

	public Stack<States> m_states;
	
	public void Init ()
	{
		m_states = new Stack<States>();
	}

	public States GetCurrentState()
	{
		return m_states.Peek();
	}

	public void ChangeState(States newState)
	{
		if (m_states.Count > 0)
		{
			m_states.Peek().Shutdown();
			m_states.Pop();
		}

		newState.Init();
		m_states.Push(newState);
	}
	
	public void Update ()
	{
		if (m_states.Count >= 0)
		{
			m_states.Peek().Update();
		}
	}
}
