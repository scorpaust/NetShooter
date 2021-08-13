﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerSpawner : MonoBehaviour
{
    public static PlayerSpawner instance;

	private GameObject player;

	public GameObject playerPrefab;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		if (PhotonNetwork.IsConnected)
		{
			SpawnPlayer();
		}
	}

	public void SpawnPlayer()
	{
		Transform spawnPoint = SpawnManager.instance.GetSpawnPoint();

		player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPoint.position, spawnPoint.rotation);
	}
}
