using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [SerializeField] Text _userName;
    [SerializeField] Text _roomId;
    [SerializeField] Button _singleBtn;
    [SerializeField] Button _multiBtn;

    public static GameManager.Difficulty diff;
    public static bool isMulti;

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        isMulti = false;
        _singleBtn.onClick.AddListener(OnSingle);
        _multiBtn.onClick.AddListener(OnMulti);
        diff = GameManager.Difficulty.Medium;
    }

    void OnSingle()
    {
        isMulti = false;
        OnPlay();
    }

    void OnMulti()
    {
        isMulti = true;
        OnPlay();
    }

    void OnPlay()
    {
        PhotonNetwork.NickName = _userName.text;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        PhotonNetwork.JoinOrCreateRoom(_roomId.text, new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.Log("Room " + _roomId.text + " is full! You need to choose another room!");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Player " + _userName.text + " joined room " + _roomId.text + " has " + PhotonNetwork.CurrentRoom.PlayerCount);
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 || isMulti)
        {
            isMulti = true;
            SceneManager.LoadScene("Main");
        }
        else
        {
            SceneManager.LoadScene("Menu");
        }
    }
}
