using UnityEngine;
using System.Collections;

public class PlayerMotion : MonoBehaviour {

	public Vector3 playerMaxSpeed;
	public float ClimbAngle = 15;
	public GameObject MainEngineFX;
	public GameObject ReverseFX;
	public GameObject LaserPlow;
	public float PlowRange;
	public float PlowStrength;
	public float PlowDamage;

	private Rigidbody rb;
	private Rigidbody otherRb;
	private Health otherHealth;

	void Start()
	{
		rb = GetComponent<Rigidbody>();
		MainEngineFX.SetActive(false);
		ReverseFX.SetActive(false);
		LaserPlow.SetActive(false);
	}

	void Update()
	{
		PlayerMove();
		//Debug.DrawRay(transform.position, transform.forward * PlowRange, Color.grey);

		if (Input.GetButton("Fire1"))
		{
			PlowBeam();
			if (!LaserPlow.activeInHierarchy)
				LaserPlow.SetActive(true);
		}
		else if (LaserPlow.activeInHierarchy)
			LaserPlow.SetActive(false);
	}


	void PlowBeam()
	{
		Vector3 PlowForward = LaserPlow.transform.forward;
		Vector3 PlowPosition = LaserPlow.transform.position;

		RaycastHit hit;
		if (Physics.Linecast(PlowPosition, (PlowPosition + PlowForward * PlowRange), out hit, -1))
		{
			GameObject obj = hit.transform.gameObject;

			/// Physics Push
			if (obj.GetComponent<Rigidbody>())
			{
				otherRb = obj.GetComponent<Rigidbody>();
				otherRb.AddForce(PlowForward * PlowStrength, ForceMode.Impulse);
			}

			/// Damage
			if (obj.GetComponent<Health>())
			{
				otherHealth = obj.GetComponent<Health>();
				otherHealth.TakeDamageAtPoint(PlowDamage, hit.point);
			}
		}
	}


	void PlayerMove()
	{
		/// Thrust and Steering
		float x = Input.GetAxis("Horizontal");
		float z = Input.GetAxis("Vertical");
		Vector3 LocalSpeed = playerMaxSpeed;
		LocalSpeed.x = 0.0f;
		LocalSpeed.y = 0.0f;
		if (z > 0)
		{
			LocalSpeed.z *= playerMaxSpeed.z;
			MainEngineFX.SetActive(true);
		}
		else if (z < 0)
		{
			LocalSpeed.z *= playerMaxSpeed.z;
			ReverseFX.SetActive(true);
		}
		else
		{
			if (MainEngineFX.activeInHierarchy)
				MainEngineFX.SetActive(false);
			if (ReverseFX.activeInHierarchy)
				ReverseFX.SetActive(false);
		}

		rb.AddTorque(x * transform.up * (rb.mass * playerMaxSpeed.x));
		rb.AddForce(z * transform.forward * LocalSpeed.z);

		/// Vertical Control
		if (Input.GetButton("Jump"))
		{
			rb.AddForce(Vector3.up * playerMaxSpeed.y);
			Quaternion UpAngle = Quaternion.Euler(-ClimbAngle, transform.rotation.eulerAngles.y, 0);
			transform.rotation = Quaternion.Lerp(transform.rotation, UpAngle, Time.deltaTime);
		}
		else if (Input.GetButton("Crouch"))
		{
			rb.AddForce(Vector3.up * -playerMaxSpeed.y);
			Quaternion DownAngle = Quaternion.Euler(ClimbAngle, transform.rotation.eulerAngles.y, 0);
			transform.rotation = Quaternion.Lerp(transform.rotation, DownAngle, Time.deltaTime);
		}
		else
		{
			Quaternion Natural = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
			transform.rotation = Quaternion.Lerp(transform.rotation, Natural, Time.deltaTime);
		}
	}

}
