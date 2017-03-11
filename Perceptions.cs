using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface IPerceptions
{
	void UpdatePerception();
	List<GameObject> GetThreats();
}
