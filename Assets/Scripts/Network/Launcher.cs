using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

	public static bool hasSetNickname;

	public GameObject loadingScreen;

	public TMP_Text loadingText;

	public GameObject menuButtons;

	public GameObject createRoomScreen;

	public TMP_InputField roomNameInput;

	public GameObject roomScreen;

	public TMP_Text roomName, playerNameLabel;

	public GameObject errorScreen;

	public TMP_Text errorText;

	public GameObject roomBrowserScreen;

	public RoomButton roomButton;

	public GameObject nicknameInputScreen;

	public TMP_InputField nicknameInput;

	public string levelToPlay;

	public GameObject startButton;

	public GameObject roomTestButton;

	public string[] allMaps;

	public bool changeMapBetweenRounds = true;

	private List<RoomButton> allRoomButtons = new List<RoomButton>();

	private List<TMP_Text> allPlayerNames = new List<TMP_Text>();

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		CloseMenus();

		loadingScreen.SetActive(true);

		loadingText.text = "Connecting to network...";

		PhotonNetwork.ConnectUsingSettings();

#if UNITY_EDITOR
		roomTestButton.SetActive(true);
#endif
		Cursor.lockState = CursorLockMode.None;

		Cursor.visible = true;
	}

	private void ListAllPlayers()
	{
		foreach (TMP_Text player in allPlayerNames)
		{
			Destroy(player.gameObject);
		}

		allPlayerNames.Clear();

		Player[] players = PhotonNetwork.PlayerList;

		for (int i = 0; i < players.Length; i++)
		{
			TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);

			newPlayerLabel.text = players[i].NickName;

			newPlayerLabel.gameObject.SetActive(true);

			allPlayerNames.Add(newPlayerLabel);
		}
	}

	public override void OnConnectedToMaster()
	{
		base.OnConnectedToMaster();

		PhotonNetwork.JoinLobby();

		PhotonNetwork.AutomaticallySyncScene = true;

		loadingText.text = "Joining lobby...";
	}

	public override void OnJoinedLobby()
	{
		base.OnJoinedLobby();

		CloseMenus();

		menuButtons.SetActive(true);

		PhotonNetwork.NickName = Random.Range(0, 1000).ToString();

		if (!hasSetNickname)
		{
			CloseMenus();

			nicknameInputScreen.SetActive(true);

			if (PlayerPrefs.HasKey("Nickname"))
			{
				nicknameInput.text = PlayerPrefs.GetString("Nickname");
			}
		}
		else
		{
			PhotonNetwork.NickName = PlayerPrefs.GetString("Nickname");
		}
	}

	public void OpenRoomCreate()
	{
		CloseMenus();

		createRoomScreen.SetActive(true);
	}

	public void CreateRoom()
	{
		if (!string.IsNullOrEmpty(roomNameInput.text))
		{
			RoomOptions options = new RoomOptions();

			options.MaxPlayers = 8;

			PhotonNetwork.CreateRoom(roomNameInput.text, options);

			CloseMenus();

			loadingText.text = "Creating Room...";

			loadingScreen.SetActive(true);
		}
	}

	public override void OnJoinedRoom()
	{
		CloseMenus();

		roomScreen.SetActive(true);

		roomName.text = PhotonNetwork.CurrentRoom.Name;

		ListAllPlayers();

		if (PhotonNetwork.IsMasterClient)
		{
			startButton.SetActive(true);
		}
		else
		{
			startButton.SetActive(false);
		}
	}

	public override void OnCreateRoomFailed(short returnCode, string message)
	{
		errorText.text = "Failed to create room: " + "[ " + returnCode + " ]" + message + ".";

		CloseMenus();

		errorScreen.SetActive(true);
	}

	public void CloseErrorScreen()
	{
		CloseMenus();

		menuButtons.SetActive(true);
	}

	public void LeaveRoom()
	{
		PhotonNetwork.LeaveRoom();

		CloseMenus();

		loadingText.text = "Leaving room...";

		loadingScreen.SetActive(true);
	}

	public override void OnLeftRoom()
	{
		CloseMenus();

		menuButtons.SetActive(true);
	}

	public void OpenRoomBrowser()
	{
		CloseMenus();

		roomBrowserScreen.SetActive(true);
	}

	public void CloseRoomBrowser()
	{
		CloseMenus();

		menuButtons.SetActive(true);
	}

	public override void OnRoomListUpdate(List<RoomInfo> roomList)
	{
		foreach (RoomButton rb in allRoomButtons)
		{
			Destroy(rb.gameObject);
		}

		allRoomButtons.Clear();

		roomButton.gameObject.SetActive(false);

		for (int i = 0; i < roomList.Count; i++)
		{
			if (roomList[i].PlayerCount != roomList[i].MaxPlayers && !roomList[i].RemovedFromList)
			{
				RoomButton newButton = Instantiate(roomButton, roomButton.transform.parent);

				newButton.SetButtonDetails(roomList[i]);

				newButton.gameObject.SetActive(true);

				allRoomButtons.Add(newButton);
			}
		}
	}

	public void JoinRoom(RoomInfo inputInfo)
	{
		PhotonNetwork.JoinRoom(inputInfo.Name);

		CloseMenus();

		loadingText.text = "Joining room...";

		loadingScreen.SetActive(true);
	}

	public override void OnPlayerEnteredRoom(Player newPlayer)
	{
		TMP_Text newPlayerLabel = Instantiate(playerNameLabel, playerNameLabel.transform.parent);

		newPlayerLabel.text = newPlayer.NickName;

		newPlayerLabel.gameObject.SetActive(true);

		allPlayerNames.Add(newPlayerLabel);
	}

	public override void OnPlayerLeftRoom(Player otherPlayer)
	{
		ListAllPlayers();
	}

	public void SetNickname()
	{
		if (!string.IsNullOrEmpty(nicknameInput.text))
		{
			PhotonNetwork.NickName = nicknameInput.text;

			PlayerPrefs.SetString("Nickname", nicknameInput.text);

			CloseMenus();

			menuButtons.SetActive(true);

			hasSetNickname = true;
		}
	}

	public void QuitGame()
	{
		Application.Quit();
	}

	public void StartGame()
	{
		// PhotonNetwork.LoadLevel(levelToPlay);

		PhotonNetwork.LoadLevel(allMaps[Random.Range(0, allMaps.Length)]);
	}

	public override void OnMasterClientSwitched(Player newMasterClient)
	{
		if (PhotonNetwork.IsMasterClient)
		{
			startButton.SetActive(true);
		}
		else
		{
			startButton.SetActive(false);
		}
	}

	public void QuickJoin()
	{
		RoomOptions options = new RoomOptions();

		options.MaxPlayers = 8;

		PhotonNetwork.CreateRoom("Test");

		CloseMenus();

		loadingText.text = "Creating room...";

		loadingScreen.SetActive(true);
	}

	private void CloseMenus()
	{
		loadingScreen.SetActive(false);

		menuButtons.SetActive(false);

		createRoomScreen.SetActive(false);

		roomScreen.SetActive(false);

		errorScreen.SetActive(false);

		roomBrowserScreen.SetActive(false);

		nicknameInputScreen.SetActive(false);
	}

}
