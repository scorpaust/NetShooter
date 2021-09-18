using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class MatchManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static MatchManager instance;

	public List<PlayerInfo> allPlayers = new List<PlayerInfo>();

	public enum EventCodes : byte
	{
		NewPlayer,
		ListPlayers,
		UpdateStats,
		NextMatch,
		TimerSync
	}

	public enum GameState
	{
		Waiting,
		Playing,
		Ending
	}

	public int killsToWin = 10;

	public Transform mapCamPoint;

	public GameState state = GameState.Waiting;

	public float waitAfterEnding;

	public bool perpetual;

	public float matchLength = 300f;

	private int index;

	private float currentMatchTime;

	private float sendTimer;

	private List<LeaderboardPlayer> lboardPlayers = new List<LeaderboardPlayer>();

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		if (!PhotonNetwork.IsConnected)
		{
			SceneManager.LoadScene(0);
		}			
		else
		{
			NewPlayerSend(PhotonNetwork.NickName);

			state = GameState.Playing;

			SetupTimer();

			if (!PhotonNetwork.IsMasterClient)
			{
				UIController.instance.timerText.gameObject.SetActive(false);
			}
		}		
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Tab) && state != GameState.Ending)
		{
			if (UIController.instance.leaderboard.activeInHierarchy)
			{
				UIController.instance.leaderboard.SetActive(false);
			}
			else
			{
				ShowLeaderboard();
			}
		}

		if (PhotonNetwork.IsMasterClient)
		{
			if (currentMatchTime > 0f && state == GameState.Playing)
			{
				currentMatchTime -= Time.deltaTime;

				if (currentMatchTime <= 0f)
				{
					currentMatchTime = 0f;

					state = GameState.Ending;

					ListPlayersSend();

					StateCheck();					
				}

				UpdateTimerDisplay();

				sendTimer -= Time.deltaTime;

				if (sendTimer <= 0f)
				{
					sendTimer += 1f;

					TimerSend();
				}
			}
		}	
	}

	public void OnEvent(EventData photonEvent)
	{
		if (photonEvent.Code < 200)
		{
			EventCodes theEvent = (EventCodes)photonEvent.Code;

			object[] data = (object[])photonEvent.CustomData;

			// Debug.Log("Received Event: " + theEvent);

			switch(theEvent)
			{
				case EventCodes.NewPlayer:
					
					NewPlayerReceive(data);
					
					break;

				case EventCodes.ListPlayers:

					ListPlayersReceive(data);

					break;

				case EventCodes.UpdateStats:

					UpdateStatsReceive(data);

					break;

				case EventCodes.NextMatch:

					NextMatchReceive();

					break;

				case EventCodes.TimerSync:

					TimerReceive(data);

					break;
			}
		}
	}

	public override void OnEnable()
	{
		PhotonNetwork.AddCallbackTarget(this);
	}

	public override void OnDisable()
	{
		PhotonNetwork.RemoveCallbackTarget(this);
	}

	public void NewPlayerSend(string username)
	{
		object[] package = new object[4];

		package[0] = username;

		package[1] = PhotonNetwork.LocalPlayer.ActorNumber;

		package[2] = 0;

		package[3] = 0;

		PhotonNetwork.RaiseEvent((byte)EventCodes.NewPlayer, package, new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
			new SendOptions
			{
				Reliability = true
			});
	}

	public void NewPlayerReceive(object[] dataReceived)
	{
		PlayerInfo player = new PlayerInfo((string)dataReceived[0], (int)dataReceived[1], (int)dataReceived[2], (int)dataReceived[3]);

		allPlayers.Add(player);

		ListPlayersSend();
	}

	public void ListPlayersSend()
	{
		object[] package = new object[allPlayers.Count + 1];

		package[0] = state;

		for (int i = 0; i < allPlayers.Count; i++)
		{
			object[] piece = new object[4];

			piece[0] = allPlayers[i].name;
			piece[1] = allPlayers[i].actor;
			piece[2] = allPlayers[i].kills;
			piece[3] = allPlayers[i].deaths;

			package[i + 1] = piece;
		}

		PhotonNetwork.RaiseEvent((byte)EventCodes.ListPlayers, package, new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions
			{
				Reliability = true
			});
	}

	public void ListPlayersReceive(object[] dataReceived)
	{
		allPlayers.Clear();

		state = (GameState)dataReceived[0];

		for (int i = 1; i < dataReceived.Length; i++)
		{
			object[] piece = (object[])dataReceived[i];

			PlayerInfo player = new PlayerInfo((string)piece[0], (int)piece[1], (int)piece[2], (int)piece[3]);

			allPlayers.Add(player);

			if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
			{
				index = i - 1;
			}
		}

		StateCheck();
	}

	public void UpdateStatsSend(int actorSending, int statToUpdate, int amountToChange)
	{
		object[] package = new object[] { actorSending, statToUpdate, amountToChange };

		PhotonNetwork.RaiseEvent((byte)EventCodes.UpdateStats, package, new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions
			{
				Reliability = true
			});
	}

	public void UpdateStatsReceive(object[] dataReceived)
	{
		int actor = (int)dataReceived[0];

		int stat = (int)dataReceived[1];

		int amountOfChange = (int)dataReceived[2];

		for (int i = 0; i < allPlayers.Count; i++)
		{
			if (allPlayers[i].actor == actor)
			{
				switch(stat)
				{
					case 0:
						allPlayers[i].kills += amountOfChange;

						break;

					case 1:
						allPlayers[i].deaths += amountOfChange;

						break;
				}

				if (i == index)
				{
					UpdateStatsDisplay();
				}

				if (UIController.instance.leaderboard.activeInHierarchy)
				{
					ShowLeaderboard();
				}

				break;
			}
		}

		ScoreCheck();
	}

	public void UpdateStatsDisplay()
	{
		if (allPlayers.Count > index)
		{
			UIController.instance.killsText.text = "Kills: " + allPlayers[index].kills;

			UIController.instance.deathsText.text = "Deaths: " + allPlayers[index].deaths;
		} 
		else
		{
			UIController.instance.killsText.text = "Kills: " + 0;

			UIController.instance.deathsText.text = "Deaths: " + 0;
		}
	}

	public override void OnLeftRoom()
	{
		base.OnLeftRoom();

		SceneManager.LoadScene(0);
	}

	public void NextMatchSend()
	{
		PhotonNetwork.RaiseEvent((byte)EventCodes.NextMatch, null, new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions
			{
				Reliability = true
			});
	}

	public void NextMatchReceive()
	{
		state = GameState.Playing;

		UIController.instance.endScreen.SetActive(false);

		UIController.instance.leaderboard.SetActive(false);

		foreach (PlayerInfo player in allPlayers)
		{
			player.kills = 0;

			player.deaths = 0;
		}

		UpdateStatsDisplay();

		PlayerSpawner.instance.SpawnPlayer();

		SetupTimer();
	}

	public void TimerSend()
	{
		object[] package = new object[] { (int)currentMatchTime, state };

		PhotonNetwork.RaiseEvent((byte)EventCodes.TimerSync, package, new RaiseEventOptions { Receivers = ReceiverGroup.All },
			new SendOptions
			{
				Reliability = true
			});
	}

	public void TimerReceive(object[] dataReceived)
	{
		currentMatchTime = (int)dataReceived[0];

		state = (GameState)dataReceived[1];

		UpdateTimerDisplay();

		UIController.instance.timerText.gameObject.SetActive(true);
	}

	public void SetupTimer()
	{
		if (matchLength > 0)
		{
			currentMatchTime = matchLength;


		}
	}

	public void UpdateTimerDisplay()
	{
		var timeToDisplay = System.TimeSpan.FromSeconds(currentMatchTime);

		UIController.instance.timerText.text = timeToDisplay.Minutes.ToString("00") + ":" + timeToDisplay.Seconds.ToString("00");
	}

	private void ShowLeaderboard()
	{
		UIController.instance.leaderboard.SetActive(true);

		foreach (LeaderboardPlayer lp in lboardPlayers)
		{
			Destroy(lp.gameObject);
		}

		lboardPlayers.Clear();

		UIController.instance.leaderboardPlayerDisplay.gameObject.SetActive(false);

		List<PlayerInfo> sorted = SortPlayers(allPlayers);

		foreach (PlayerInfo player in sorted)
		{
			LeaderboardPlayer newPlayerDisplay = Instantiate(UIController.instance.leaderboardPlayerDisplay, UIController.instance.leaderboardPlayerDisplay.transform.parent);

			newPlayerDisplay.SetDetails(player.name, player.kills, player.deaths);

			newPlayerDisplay.gameObject.SetActive(true);

			lboardPlayers.Add(newPlayerDisplay);
		}
	}

	private List<PlayerInfo> SortPlayers(List<PlayerInfo> players)
	{
		List<PlayerInfo> sorted = new List<PlayerInfo>();

		while (sorted.Count < players.Count)
		{
			int highest = -1;

			PlayerInfo selection = players[0];

			foreach (PlayerInfo player in players)
			{
				if (!sorted.Contains(player))
				{
					if (player.kills > highest)
					{
						selection = player;

						highest = player.kills;
					}
				}
			}
			sorted.Add(selection);
		}

		return sorted;
	}

	private void ScoreCheck()
	{
		bool winnerFound = false;

		foreach (PlayerInfo player in allPlayers)
		{
			if (player.kills >= killsToWin && killsToWin > 0)
			{
				winnerFound = true;

				break;
			}
		}

		if (winnerFound)
		{
			if (PhotonNetwork.IsMasterClient && state != GameState.Ending)
			{
				state = GameState.Ending;

				ListPlayersSend();
			}
		}
	}

	private void StateCheck()
	{
		if (state == GameState.Ending)
		{
			EndGame();
		}
	}

	private void EndGame()
	{
		state = GameState.Ending;

		if (PhotonNetwork.IsMasterClient)
		{
			PhotonNetwork.DestroyAll();
		}

		UIController.instance.endScreen.SetActive(true);

		ShowLeaderboard();

		Cursor.lockState = CursorLockMode.None;

		Cursor.visible = true;

		Camera.main.transform.position = mapCamPoint.position;

		Camera.main.transform.rotation = mapCamPoint.rotation;

		StartCoroutine(EndCo());
	}

	private IEnumerator EndCo()
	{
		yield return new WaitForSeconds(waitAfterEnding);

		if (!perpetual)
		{
			PhotonNetwork.AutomaticallySyncScene = false;

			PhotonNetwork.LeaveRoom();
		}
		else
		{
			if (PhotonNetwork.IsMasterClient)
			{
				if (!Launcher.instance.changeMapBetweenRounds)
				{
					NextMatchSend();
				}
				else
				{
					int newLevel = Random.Range(0, Launcher.instance.allMaps.Length);

					if (Launcher.instance.allMaps[newLevel] == SceneManager.GetActiveScene().name)
					{
						NextMatchSend();
					}
					else
					{
						PhotonNetwork.LoadLevel(Launcher.instance.allMaps[newLevel]);
					}

				}
			}
		}
	}
}

[System.Serializable]
public class PlayerInfo
{
	public string name;

	public int actor, kills, deaths;

	public PlayerInfo(
		string _name, int _actor, int _kills, int _deaths)
	{
		name = _name;

		actor = _actor;

		kills = _kills;

		deaths = _deaths;
	}
}
