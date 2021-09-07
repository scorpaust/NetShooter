using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public static UIController instance;

	private void Awake()
	{
		instance = this;
	}

	public TMP_Text overheatedMessage;

	public Slider weaponTempSlider;

	public GameObject deathScreen;

	public TMP_Text deathText;

}
