using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour
{
	[SerializeField]
	private Transform myCameraTransform;
	[SerializeField]
	private bool myUseInitialSettings;
	[SerializeField]
	private Vector3 myInitialPosition;
	[SerializeField]
	private Vector3 myInitialRotation;
	[SerializeField]
	private float myInitialZoom;

	private float myCameraDistance = 5.0f;
	private float myTargetCameraDistance = 5.0f;
	private Vector3 myCameraRotation = Vector3.zero;
	private Vector3 myTargetCameraRotation = Vector3.zero;
	private Vector3 myTargetCameraPosition = Vector3.zero;

	private bool myCanMove = true;
	public bool CanMove { get { return myCanMove; } set { myCanMove = value; } }
	
	void Awake()
	{
		if (myUseInitialSettings == true)
		{
			SetOrigin(myInitialPosition, true);
			SetRotation(myInitialRotation, true);
			SetZoom(myInitialZoom, true);
		}
	}

	void Update()
	{
		bool inputAllowed = myCanMove;
		if (EventSystem.current != null)
		{
			inputAllowed = (EventSystem.current.IsPointerOverGameObject() == false) && myCanMove;
		}
		Rotate(inputAllowed);
		Zoom(inputAllowed);
		Move(inputAllowed);
	}

	private void Rotate(bool aInputAllowed)
	{
		if (Input.GetMouseButton(1) == true && aInputAllowed == true)
		{
			float mouseX = Input.GetAxisRaw("Mouse X");
			float mouseY = Input.GetAxisRaw("Mouse Y");
			myTargetCameraRotation.x -= mouseY * 4.0f;
			myTargetCameraRotation.y += mouseX * 4.0f;
			myTargetCameraRotation.x = Mathf.Clamp(myTargetCameraRotation.x, -90.0f, 90.0f);
		}

		myCameraRotation.x = Mathf.MoveTowards(myCameraRotation.x, myTargetCameraRotation.x, Mathf.Abs(myTargetCameraRotation.x - myCameraRotation.x) * Time.unscaledDeltaTime * 15.0f);
		myCameraRotation.y = Mathf.MoveTowards(myCameraRotation.y, myTargetCameraRotation.y, Mathf.Abs(myTargetCameraRotation.y - myCameraRotation.y) * Time.unscaledDeltaTime * 15.0f);

		transform.eulerAngles = myCameraRotation;
	}

	private void Zoom(bool aInputAllowed)
	{
		float scrollValue = (Input.GetKey(KeyCode.LeftControl) == true || Input.GetKey(KeyCode.RightControl) == true) ? 0.0f : Input.GetAxisRaw("Mouse ScrollWheel");
		if (scrollValue > 0.0f && aInputAllowed == true)
		{
			myTargetCameraDistance *= 1.0f / 1.2f;
		}
		if (scrollValue < 0.0f && aInputAllowed == true)
		{
			myTargetCameraDistance *= 1.2f;
		}

		myCameraDistance = Mathf.MoveTowards(myCameraDistance, myTargetCameraDistance, Mathf.Abs(myTargetCameraDistance - myCameraDistance) * Time.unscaledDeltaTime * 5.0f);

		Vector3 cameraPosition = myCameraTransform.localPosition;
		cameraPosition.z = -myCameraDistance;
		myCameraTransform.localPosition = cameraPosition;
	}

	private void Move(bool aInputAllowed)
	{
		if (aInputAllowed == true)
		{
			float speed = Mathf.Abs(myCameraTransform.localPosition.z);
			float horizontal = Input.GetAxisRaw("Horizontal");
			float vertical = Input.GetAxisRaw("Vertical");
			myTargetCameraPosition += transform.right * horizontal * Time.unscaledDeltaTime * speed;
			myTargetCameraPosition += (Quaternion.Euler(0.0f, -90.0f, 0.0f) * transform.right) * vertical * Time.unscaledDeltaTime * speed;
			myTargetCameraPosition.y = 0.0f;
		}

		Vector3 cameraPosition = transform.position;
		cameraPosition = Vector3.MoveTowards(cameraPosition, myTargetCameraPosition, (myTargetCameraPosition - cameraPosition).magnitude * Time.unscaledDeltaTime * 4.0f);
		cameraPosition.y = 0.0f;
		transform.position = cameraPosition;
	}

	public void SetOrigin(Vector3 aOrigin, bool aImmediet)
	{
		myTargetCameraPosition = aOrigin;
		if (aImmediet == true)
		{
			transform.position = myTargetCameraPosition;
		}
	}

	public void SetRotation(Vector3 aEuler, bool aImmediet)
	{
		myTargetCameraRotation = aEuler;
		if (aImmediet == true)
		{
			myCameraRotation = aEuler;
			transform.eulerAngles = myCameraRotation;
		}
	}

	public void SetZoom(float aZoom, bool aImmediet)
	{
		myTargetCameraDistance = aZoom;
		if (aImmediet == true)
		{
			myCameraDistance = aZoom;
			Vector3 cameraPosition = myCameraTransform.localPosition;
			cameraPosition.z = -myCameraDistance;
			myCameraTransform.localPosition = cameraPosition;
		}
	}
}
