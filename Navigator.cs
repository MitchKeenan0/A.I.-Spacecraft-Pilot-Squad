using UnityEngine;
using System.Collections;

public class Navigator : MonoBehaviour {

	public float ReactionTime = 0.5f;
	public float TurnSpeed = 0.77f;
	public float DetourSize = 300f;
	public float SafeDistance = 100f;
	public float SafetyForceScalar = 1.0f;
	public GameObject EngineFX;
	float MoveSpeed = 0;
	float ResponseTimer = 0;
	AI_Agent agent;
	Rigidbody rb;
	Vector3 LocalPosition = Vector3.zero;
	Vector3 DestPosition = Vector3.zero;

	public void Init()
	{
		agent = GetComponent<AI_Agent>();
		rb = GetComponent<Rigidbody>();
		MoveSpeed = agent.botMaxSpeed.z;
		ResponseTimer = 0;
		LocalPosition = transform.position;
	}

	
	public void NavigateTo(Transform Destination)
	{
		
		LocalPosition = transform.position;
		DestPosition = Destination.position;
		RaycastHit hit;

		/// Main Line to destination
		if (Physics.Linecast(LocalPosition, DestPosition, out hit))
		{
			if (hit.transform == Destination)
				{ MoveTo(Destination.position); }
			else
			{
				if (hit.transform.tag != "Navigation")
				{
					Vector3 Detour = hit.normal * DetourSize;
					Debug.DrawRay(hit.point, Detour, Color.blue);

					Vector3 DetourActual = Detour + hit.point;
					MoveTo(DetourActual);
				}
				else
				{
					MoveTo(Destination.position);
				}
			}
		}

		/// Safety Forces - checks sphere for nearby obstacles
		ResponseTimer += Time.deltaTime;
		if (ResponseTimer >= ReactionTime)
		{
			Collider[] surroundings = Physics.OverlapSphere(LocalPosition, DetourSize / 2);
			foreach (Collider c in surroundings)
			{
				if (c.transform != transform && c.transform.tag != "Navigation")
				{
					Vector3 HitPosition = c.ClosestPointOnBounds(LocalPosition);
					float ScalarAcutal = MoveSpeed * SafetyForceScalar;

					/// Decelerate if too close to obj
					float DistToObj = Vector3.Distance(LocalPosition, c.transform.position);
					if (DistToObj <= SafeDistance)
						rb.velocity *= 0.985f;

					Vector3 SafetyVector = (LocalPosition - HitPosition).normalized;
					rb.AddForce(SafetyVector * ScalarAcutal, ForceMode.Impulse);

					///Debug.DrawLine(LocalPosition, HitPosition, Color.white);
				}
			}

			ResponseTimer = 0.0f;
		}
	}

	public void MoveTo(Vector3 endPoint)
	{
		/// Rotation
		Vector3 ToPoint = (endPoint - LocalPosition).normalized;
		Vector3 LookDirection = Vector3.Slerp(transform.forward, ToPoint, Time.deltaTime * TurnSpeed);
		transform.rotation = Quaternion.LookRotation(LookDirection, Vector3.up);

		/// Thrust
		rb.AddForce(transform.forward * MoveSpeed);

		EngineFX.SetActive(true);
	}
}
