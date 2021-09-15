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
		UpdateStats
	}

	private int index;

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		if (!PhotonNetwork.IsConnected)
			SceneManager.LoadScene(0);
		else
			NewPlayerSend(PhotonNetwork.NickName);
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
		object[] package = new object[allPlayers.Count];

		for (int i = 0; i < allPlayers.Count; i++)
		{
			object[] piece = new object[4];

			piece[0] = allPlayers[i].name;
			piece[1] = allPlayers[i].actor;
			piece[2] = allPlayers[i].kills;
			piece[3] = allPlayers[i].deaths;

			package[i] = piece;
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

		for (int i = 0; i < dataReceived.Length; i++)
		{
			object[] piece = (object[])dataReceived[i];

			PlayerInfo player = new PlayerInfo((string)piece[0], (int)piece[1], (int)piece[2], (int)piece[3]);

			allPlayers.Add(player);

			if (PhotonNetwork.LocalPlayer.ActorNumber == player.actor)
			{
				index = i;
			}
		}
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

				break;
			}
		}
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
