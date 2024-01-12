using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;

public class Board : MonoBehaviour {
	private static int whichRemote = 0;
    public Vector4 minus = new Vector4(0.0f,0.0f,0.0f,0.0f);
	private bool balanceZero = false;
	public TMP_Text statusText;
	private float timer; 

	public Image klicaj1;
	public Image klicaj2;
	
	public Image gor;
	public Image dol;
	public Image levo;
	public Image desno;
	private string inputDisplay = "";

	public bool sitting = true;
	public Toggle btnSitting;
	public Toggle btnStanding;
	public Sprite selectedBackground;
    public Sprite deselectedBackground;
	private bool isUpdatingToggles = false;

	void Start()
	{
		btnSitting.onValueChanged.AddListener(delegate { ToggleChanged(btnSitting, true); });
        btnStanding.onValueChanged.AddListener(delegate { ToggleChanged(btnStanding, false); });
        UpdateBackground(btnSitting, btnSitting.isOn);
	}

	void OnEnable() {
		
		Wii.OnDiscoveryFailed     += OnDiscoveryFailed;
		Wii.OnWiimoteDiscovered   += OnWiimoteDiscovered;
		Wii.OnWiimoteDisconnected += OnWiimoteDisconnected;
	}

	void OnDisable()
	{
		Wii.OnDiscoveryFailed     -= OnDiscoveryFailed;
		Wii.OnWiimoteDiscovered   -= OnWiimoteDiscovered;
		Wii.OnWiimoteDisconnected -= OnWiimoteDisconnected;
	}
	
	void Update ()
    {
		if (Wii.IsActive(0))
		{		
			if(Wii.GetExpType(whichRemote)==3)//balance board is in
			{
				Vector4 theBalanceBoard = Wii.GetBalanceBoard(whichRemote); 
				Vector2 theCenter = Wii.GetCenterOfBalance(whichRemote);
		        if (balanceZero)
		        {
		            minus = new Vector4(theBalanceBoard.x, theBalanceBoard.y, theBalanceBoard.z, theBalanceBoard.w);
					Debug.Log(minus);
		        }
				Vector4 balanceNew;
				balanceNew = theBalanceBoard-minus;
				balanceZero = false;		
				klicaj1.gameObject.SetActive(false);		
				// vizualizacija
				float valueFront = (balanceNew[0] + balanceNew[1]) / 2.0f;
		        float valueBack = (balanceNew[2] + balanceNew[3]) / 2.0f;   
		        float valueLeft = (balanceNew[1] + balanceNew[3]) / 2.0f;
		        float valueRight = (balanceNew[0] + balanceNew[2]) / 2.0f;		
				Vector4 vrednosti = new Vector4(valueFront, valueRight, valueBack, valueLeft);
				if (vrednosti.x < 0.2f) vrednosti.x = 0f;
				if (vrednosti.y < 0.2f) vrednosti.y = 0f;
				if (vrednosti.z < 0.2f) vrednosti.z = 0f;
				if (vrednosti.w < 0.2f) vrednosti.w = 0f;		
				vrednosti.x = Mathf.Clamp(vrednosti.x, -0.5f, 0.5f);				
				vrednosti.y = Mathf.Clamp(vrednosti.y, -0.5f, 0.5f);
				vrednosti.z = Mathf.Clamp(vrednosti.z, -0.5f, 0.5f);
				vrednosti.w = Mathf.Clamp(vrednosti.w, -0.5f, 0.5f);		
				gor.transform.localScale = new Vector3(vrednosti.x,vrednosti.x,1);
				desno.transform.localScale = new Vector3(vrednosti.y,vrednosti.y,1);
				dol.transform.localScale = new Vector3(vrednosti.z,vrednosti.z,1);
				levo.transform.localScale = new Vector3(vrednosti.w,vrednosti.w,1);		
			}
			else {
				inputDisplay = inputDisplay + "\nBalance Board not detected.\nDetected controller of type: " + Wii.GetExpType(whichRemote).ToString();
			}
			statusText.text = inputDisplay;
		}
	}

    public void SetZero()
    {
        balanceZero = true;
		klicaj2.gameObject.SetActive(false);

    }

	private void ToggleChanged(Toggle changedToggle, bool isToggle1)
    {
        if (isUpdatingToggles) return;

        isUpdatingToggles = true;

        if (isToggle1)
        {
            btnStanding.isOn = !changedToggle.isOn;
			sitting = true;
        }
        else
        {
            btnSitting.isOn = !changedToggle.isOn;
			sitting = false;
        }

        UpdateBackground(btnSitting, btnSitting.isOn);
        UpdateBackground(btnStanding, btnStanding.isOn);

        isUpdatingToggles = false;
    }

    private void UpdateBackground(Toggle toggle, bool isOn)
    {
        Transform backgroundTransform = toggle.transform.Find("Background");
        if (backgroundTransform != null)
        {
            Image backgroundImage = backgroundTransform.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.sprite = isOn ? selectedBackground : deselectedBackground;
            }
        }
    }

	public void CancelSearch()
	{
		Wii.StopSearch();
	}

	public void BeginSearch()
	{
		Wii.StartSearch();
		Time.timeScale = 1.0f;
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