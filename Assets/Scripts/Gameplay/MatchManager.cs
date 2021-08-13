using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class MatchManager : MonoBehaviour
{
    public static MatchManager instance;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		if (!PhotonNetwork.IsConnected)
			SceneManager.LoadScene(0);
	}
}
