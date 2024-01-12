using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CarController : MonoBehaviour
{
    public enum ControlMode
    {
        Keyboard,
        BalanceBoard
    };

    public enum Axel
    {
        Front,
        Rear
    }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;
        public Axel axel;
    }

    public ControlMode control;

    public float maxAcceleration = 20.0f;

    private float speedMax = 320.0f;
    public float brakeAcceleration = 60.0f;

    private float turnSensitivity = 0.4f;
    public float maxSteerAngle = 30.0f;

    public Vector3 _centerOfMass;

    public List<Wheel> wheels;

    float moveInput;
    float steerInput;

    private Rigidbody carRb;
    private static int whichRemote = 0;
    private Vector4 minus = new Vector4(0.0f,0.0f,0.0f,0.0f);

    public Image up;
    public Image right;
    public Image down;
    public Image left;

    private bool stoja = false; 

    private float vizMul;

    private float previousWeight = 9999f;
    private float jumpTreshold = 2f;
    private bool inRise = false;
    private float detectionWindow = 0.2f;
    private float weightAtRise;
    private float jumpForce = 5000.0f;

    private float minSpeed = 0.0f;
    private float maxSpeed = 30.0f;
    private float currentSpeed;

    private float minPitch = 0.5f;
    private float maxPitch = 1f;
    private float pitchFromCar;
    private AudioSource carAudio;
    public AudioClip powerUp;
    public AudioClip powerDown;

    void Start()
    {
        carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerOfMass;

        Wii.OnDiscoveryFailed     += OnDiscoveryFailed;
		Wii.OnWiimoteDiscovered   += OnWiimoteDiscovered;
		Wii.OnWiimoteDisconnected += OnWiimoteDisconnected;

        BoardMinus BoardMinusInstance = BoardMinus.Instance;
        minus = BoardMinusInstance.minus;
        stoja = !BoardMinusInstance.sedenje;
        Debug.Log(stoja);

        if (stoja) {
            vizMul = 0.2f;
            jumpTreshold = 5f;
            turnSensitivity = 0.2f;
            speedMax = 300f;
        }
        else {
            vizMul = 0.2f;
        }

        carAudio = GetComponent<AudioSource>();
    }

    void Update()
    {
        GetInputs();
        AnimateWheels();
        EngineSound();
    }

    void LateUpdate()
    {
        Move();
        Steer();
        Brake();
    }

    public void MoveInput(float input)
    {
        moveInput = input;
    }

    public void SteerInput(float input)
    {
        steerInput = input;
    }

    void GetInputs()
    {
        if(control == ControlMode.Keyboard)
        {
            moveInput = Input.GetAxis("Vertical");
            steerInput = Input.GetAxis("Horizontal");
        }
        else if(Wii.IsActive(0))
		{			
		    if(Wii.GetExpType(whichRemote)==3) // balance board detected
			{
				Vector4 theBalanceBoard = Wii.GetBalanceBoard(whichRemote);
        
				Vector4 balanceNew;
				balanceNew = theBalanceBoard-minus;

                float valueFront = (balanceNew[0] + balanceNew[1]) / 2.0f;
                float valueBack = (balanceNew[2] + balanceNew[3]) / 2.0f;   

                moveInput = valueFront - valueBack;

                float valueLeft = (balanceNew[1] + balanceNew[3]) / 2.0f;
                float valueRight = (balanceNew[0] + balanceNew[2]) / 2.0f;

                float steerInputOg = valueRight - valueLeft;

                float squaredSteerInput = steerInputOg * steerInputOg;
                squaredSteerInput *= Mathf.Sign(steerInputOg);

                steerInput = Mathf.Clamp(squaredSteerInput, -1f, 1f);

                // vizualizacija
                float upVal = valueFront * vizMul;
                float rightVal = valueRight * vizMul;
                float downVal = valueBack * vizMul;
                float leftVal = valueLeft * vizMul;
                
                up.color = new Color(up.color.r, up.color.g, up.color.b, upVal);
                right.color = new Color(right.color.r, right.color.g, right.color.b, rightVal);
                down.color = new Color(down.color.r, down.color.g, down.color.b, downVal);
                left.color = new Color(left.color.r, left.color.g, left.color.b, leftVal);

                // skok
                float weight = Wii.GetTotalWeight(0);
                float weightChange = weight - previousWeight;

                if (weightChange > jumpTreshold)
                {
                    weightAtRise = weight;
                    inRise = true;
                    detectionWindow = 0.2f;
                }

                if (inRise)
                {
                    if ((weightAtRise - weight) > jumpTreshold && IsWheelTouchingFloor())
                    {
                        Jump();
                        inRise = false;
                    }

                    detectionWindow -= Time.deltaTime;
                    if (detectionWindow <= 0)
                    {
                        inRise = false;
                    }
                }
                previousWeight = weight;

                // rotacija v zraku
                if (!IsWheelTouchingFloor())
                {
                    Vector3 rot = new Vector3(0, steerInput * 0.2f, 0);
                    carRb.MoveRotation(carRb.rotation * Quaternion.Euler(rot));
                }
			}
		}
    }

    void Move()
    {
        foreach(var wheel in wheels)
        {
            float speed = moveInput * speedMax * maxAcceleration * Time.deltaTime;
            if (speed > 400)
            {
                speed = 400;
            }
            else if (speed < -400)
            {
                speed = -400;
            }
            wheel.wheelCollider.motorTorque = speed;
        }
    }

    void Steer()
    {
        foreach(var wheel in wheels)
        {
            if (Mathf.Abs(steerInput) > 0.2f)
            {
                if (wheel.axel == Axel.Front)
                {
                    var _steerAngle = steerInput * turnSensitivity * maxSteerAngle;
                    wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _steerAngle, 0.6f);
                }
            }
        }
    }

    void Brake()
    {
        if (Input.GetKey(KeyCode.Space) || (moveInput >=-0.5 && moveInput <= 0.5))
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.brakeTorque = 300 * brakeAcceleration * Time.deltaTime;
            }
        }
        else
        {
            foreach (var wheel in wheels)
            {
                wheel.wheelCollider.brakeTorque = 0;
            }
        }
    }

    void AnimateWheels()
    {
        foreach(var wheel in wheels)
        {
            Quaternion rot;
            Vector3 pos;
            wheel.wheelCollider.GetWorldPose(out pos, out rot);
            wheel.wheelModel.transform.position = pos;
            wheel.wheelModel.transform.rotation = rot;
        }
    }

    public void Jump()
    {
        carRb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boost"))
        {
            SpeedBoost();
            AudioSource.PlayClipAtPoint(powerUp, transform.position);
            other.gameObject.SetActive(false);
        }

        if (other.CompareTag("Slow"))
        {
            AudioSource.PlayClipAtPoint(powerDown, transform.position);
            StartCoroutine(SpeedSlow());
            other.gameObject.SetActive(false);
        }

        if (other.CompareTag("start"))
        {   
            Timer timerController = FindObjectOfType<Timer>();
            if (timerController != null)
            {
                timerController.StartTimer();
            }
        }
    }

    bool IsWheelTouchingFloor()
    {
        WheelHit wheelHit;
        if (wheels[0].wheelCollider.GetGroundHit(out wheelHit))
        {
            if (wheelHit.collider.tag == "Floor")
            {
                return true;
            }
        }
        return false;
    }

    void SpeedBoost()
    {
        carRb.AddForce(transform.TransformDirection(Vector3.forward * 5000f ), ForceMode.Impulse);
    }

    IEnumerator SpeedSlow()
{
    float time = 0;
    while (time < 1f)
    {
        Vector3 brakingDirection = -transform.forward;
        carRb.AddForce(brakingDirection * 1000f, ForceMode.Force);
        time += Time.deltaTime;
        yield return null;
    }
}

    void EngineSound()
    {
        currentSpeed = carRb.velocity.magnitude;
        pitchFromCar = carRb.velocity.magnitude / 50f;

        if (currentSpeed < minSpeed)
        {
            carAudio.pitch = minPitch;
        }

        if (currentSpeed > minSpeed && currentSpeed < maxSpeed)
        {
            carAudio.pitch = minPitch + pitchFromCar;
        }

        if (currentSpeed > maxSpeed)
        {
            carAudio.pitch = maxPitch;
        }
    }

    public void OnDiscoveryFailed(int i) {
		//searching = false;
	}
	
	public void OnWiimoteDiscovered (int thisRemote) {
		Debug.Log("found this one: "+thisRemote);
		if(!Wii.IsActive(whichRemote))
			whichRemote = 0;
	}
	
	public void OnWiimoteDisconnected (int whichRemote) {
		Debug.Log("lost this one: "+ whichRemote);	
	}
}