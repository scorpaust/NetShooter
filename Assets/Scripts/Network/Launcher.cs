using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;

	public GameObject loadingScreen;

	public TMP_Text loadingText;

	public GameObject menuButtons;

	public GameObject createRoomScreen;

	public TMP_InputField roomNameInput;

	public GameObject roomScreen;

	public TMP_Text roomName;

	public GameObject errorScreen;

	public TMP_Text errorText;

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
	}

	public override void OnConnectedToMaster()
	{
		base.OnConnectedToMaster();

		PhotonNetwork.JoinLobby();

		loadingText.text = "Joining lobby...";
	}

	public override void OnJoinedLobby()
	{
		base.OnJoinedLobby();

		CloseMenus();

		menuButtons.SetActive(true);
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

	private void CloseMenus()
	{
		loadingScreen.SetActive(false);

		menuButtons.SetActive(false);

		createRoomScreen.SetActive(false);

		roomScreen.SetActive(false);

		errorScreen.SetActive(false);
	}

}
