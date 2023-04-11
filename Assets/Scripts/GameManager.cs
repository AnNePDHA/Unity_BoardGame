using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class GameManager : MonoSingleton<GameManager>
{
    public Board board;

    public GameObject whiteKing;
    public GameObject whiteQueen;
    public GameObject whiteBishop;
    public GameObject whiteKnight;
    public GameObject whiteRook;
    public GameObject whitePawn;

    public GameObject blackKing;
    public GameObject blackQueen;
    public GameObject blackBishop;
    public GameObject blackKnight;
    public GameObject blackRook;
    public GameObject blackPawn;

    public GameObject[,] pieces;
    public List<GameObject> movedPawns;

    public Player white;
    public Player black;
    public Player currentPlayer;
    public Player otherPlayer;

    public string mine;

    //Luke Guest
    MiniMax miniMaxAI;

    GameObject[] buttons;
    bool gameOver = false;

    public PhotonView photonView;

    public enum Difficulty
    {
        Easy,
        Medium,
        Hard,
    };

    public Difficulty currentDifficulty;
    public bool isMulti;

    public Text uiPromptText;
    
    public Text winText;
    public GameObject gameOverPanel;

    int depth = 4;

    bool firstMoveMade;
    

    void Start ()
    {
        photonView = GetComponent<PhotonView>();
        pieces = new GameObject[8, 8];
        movedPawns = new List<GameObject>();

        white = new Player("white", true);
        black = new Player("black", false);

        if (PhotonNetwork.IsMasterClient)
        {
            mine = "white";
        }
        else
        {
            mine = "black";
        }

        currentPlayer = white;
        otherPlayer = black;

        InitialSetup();

        //Luke Guest
        buttons = GameObject.FindGameObjectsWithTag("Button");

        isMulti = NetworkManager.isMulti;
        
        miniMaxAI = new MiniMax();
        currentDifficulty = Difficulty.Medium;
        ChangeDifficulty(NetworkManager.diff);
    }

    private void InitialSetup()
    {
        AddPiece(whiteRook, white, 0, 0);
        AddPiece(whiteKnight, white, 1, 0);
        AddPiece(whiteBishop, white, 2, 0);
        AddPiece(whiteQueen, white, 3, 0);
        AddPiece(whiteKing, white, 4, 0);
        AddPiece(whiteBishop, white, 5, 0);
        AddPiece(whiteKnight, white, 6, 0);
        AddPiece(whiteRook, white, 7, 0);

        for (int i = 0; i < 8; i++)
        {
            AddPiece(whitePawn, white, i, 1);
        }

        AddPiece(blackRook, black, 0, 7);
        AddPiece(blackKnight, black, 1, 7);
        AddPiece(blackBishop, black, 2, 7);
        AddPiece(blackQueen, black, 3, 7);
        AddPiece(blackKing, black, 4, 7);
        AddPiece(blackBishop, black, 5, 7);
        AddPiece(blackKnight, black, 6, 7);
        AddPiece(blackRook, black, 7, 7);

        for (int i = 0; i < 8; i++)
        {
            AddPiece(blackPawn, black, i, 6);
        }
    }

    public void AddPiece(GameObject prefab, Player player, int col, int row)
    {
        GameObject pieceObject = board.AddPiece(prefab, col, row);
        player.pieces.Add(pieceObject);
        pieces[col, row] = pieceObject;
    }

    public void SelectPieceAtGrid(Vector2Int gridPoint)
    {
        GameObject selectedPiece = pieces[gridPoint.x, gridPoint.y];
        if (selectedPiece)
        {
            board.SelectPiece(selectedPiece);
        }
    }

    public List<Vector2Int> MovesForPiece(GameObject pieceObject, bool realMove = true, bool maximisingPlayer = true)
    {
        Piece piece = pieceObject.GetComponent<Piece>();
        Vector2Int gridPoint = GridForPiece(pieceObject);
        List<Vector2Int> locations = piece.MoveLocations(gridPoint);

        // filter out offboard locations
        locations.RemoveAll(gp => gp.x < 0 || gp.x > 7 || gp.y < 0 || gp.y > 7);

        if (realMove)
        {
            // filter out locations with friendly piece
            locations.RemoveAll(gp => FriendlyPieceAt(gp));
        }
        else
        {
            locations.RemoveAll(gp => FriendlyPieceAt(gp, false, maximisingPlayer ? white : black));
        }
        
        //if (NetworkManager.isMulti)
        //{
        //    photonView.RPC("MovesForPieceRPC", RpcTarget.Others, gridPoint.x, gridPoint.y, realMove, maximisingPlayer);
        //}
        return locations;
    }

    //[PunRPC]
    //public void MovesForPieceRPC(int gridPointX, int gridPointY, bool realMove, bool maximisingPlayer)
    //{
    //    Piece piece = pieces[gridPointX, gridPointY].GetComponent<Piece>();
    //    List<Vector2Int> locations = piece.MoveLocations(new Vector2Int(gridPointX, gridPointY));

    //    // filter out offboard locations
    //    locations.RemoveAll(gp => gp.x < 0 || gp.x > 7 || gp.y < 0 || gp.y > 7);

    //    if (realMove)
    //    {
    //        // filter out locations with friendly piece
    //        locations.RemoveAll(gp => FriendlyPieceAt(gp));
    //    }
    //    else
    //    {
    //        locations.RemoveAll(gp => FriendlyPieceAt(gp, false, maximisingPlayer ? white : black));
    //    }
    //}

    //[PunRPC]
    public void Move(GameObject piece, Vector2Int gridPoint, bool realMove = true)
    {
        //if (!photonView.IsMine) return;
        Piece pieceComponent = piece.GetComponent<Piece>();
        if (pieceComponent.type == PieceType.Pawn && !HasPawnMoved(piece) && realMove)
        {
            movedPawns.Add(piece);
        }

        Vector2Int startGridPoint = GridForPiece(piece);

        pieces[startGridPoint.x, startGridPoint.y] = null;
        pieces[gridPoint.x, gridPoint.y] = piece;
        if (realMove)
        {
            board.MovePiece(piece, gridPoint);
        }
        if (NetworkManager.isMulti)
            photonView.RPC("MovePiece", RpcTarget.Others, startGridPoint.x, startGridPoint.y, gridPoint.x, gridPoint.y, realMove);
    }

    [PunRPC]
    public void MovePiece(int startGridPointX, int startGridPointY, int gridPointX, int gridPointY, bool realMove)
    {
        GameObject piece = pieces[startGridPointX, startGridPointY];
        //Debug.LogError(piece.name);
        Piece pieceComponent = piece.GetComponent<Piece>();
        if (pieceComponent.type == PieceType.Pawn && !HasPawnMoved(piece) && realMove)
        {
            movedPawns.Add(piece);
        }

        pieces[startGridPointX, startGridPointY] = null;
        pieces[gridPointX, gridPointY] = piece;
        if (realMove)
        {
            board.MovePiece(piece, new Vector2Int(gridPointX, gridPointY));
        }
    }

    public void PawnMoved(GameObject pawn)
    {
        movedPawns.Add(pawn);
    }

    public bool HasPawnMoved(GameObject pawn)
    {
        return movedPawns.Contains(pawn);
    }

    /*
     * Modified by Luke Guest.
     * 
     * Ability to capture piece in a 'virtual' board - if not a real move, just moves pieces within 2d array 'pieces'.
     * 
     * realMove - whether piece needs to be deleted, or an operation of MiniMax algo.
     * player - only needed if realMove = false
     */
    //[PunRPC]
    public void CapturePieceAt(Vector2Int gridPoint, bool realMove = true, Player player = null)
    {
        //if (photonView.IsMine) return;
        GameObject pieceToCapture = PieceAtGrid(gridPoint);
        if (pieceToCapture.GetComponent<Piece>().type == PieceType.King && realMove)
        {
            Debug.Log(currentPlayer.name + " wins!");
            Destroy(board.GetComponent<TileSelector>());
            Destroy(board.GetComponent<MoveSelector>());
            GameOver(currentPlayer);
        }

        //Luke Guest
        if (realMove)
        {
            currentPlayer.capturedPieces.Add(pieceToCapture);
        }
        else
        {
            player.capturedPieces.Add(pieceToCapture);
        }
        //

        pieces[gridPoint.x, gridPoint.y] = null;

        //Luke Guest
        if (realMove)
        {
            Destroy(pieceToCapture);
        }
        if (NetworkManager.isMulti)
        {
            if (player == null)
            {
                player = currentPlayer;
            }
            photonView.RPC("CapturePiece", RpcTarget.Others, gridPoint.x, gridPoint.y, realMove, player.name);
        }
        //return pieceToCapture.gameObject;
    }

    [PunRPC]
    public void CapturePiece(int gridPointX, int gridPointY, bool realMove, string player)
    {
        GameObject pieceToCapture = PieceAtGrid(new Vector2Int(gridPointX, gridPointY));
        if (pieceToCapture.GetComponent<Piece>().type == PieceType.King && realMove)
        {
            Debug.Log(currentPlayer.name + " wins!");
            Destroy(board.GetComponent<TileSelector>());
            Destroy(board.GetComponent<MoveSelector>());
            GameOver(currentPlayer);
        }

        Player _player;

        if (player == "white")
        {
            _player = white;
        }
        else
        {
            _player = black;
        }

        //Luke Guest
        if (realMove)
        {
            currentPlayer.capturedPieces.Add(pieceToCapture);
        }
        else
        {
            _player.capturedPieces.Add(pieceToCapture);
        }
        //

        pieces[gridPointX, gridPointY] = null;

        //Luke Guest
        if (realMove)
        {
            Destroy(pieceToCapture);
        }
    }

    public void SelectPiece(GameObject piece)
    {
        board.SelectPiece(piece);
    }

    public void DeselectPiece(GameObject piece)
    {
        board.DeselectPiece(piece);
    }

    public bool DoesPieceBelongToCurrentPlayer(GameObject piece)
    {
        return currentPlayer.pieces.Contains(piece);
    }

    public GameObject PieceAtGrid(Vector2Int gridPoint)
    {
        if (gridPoint.x > 7 || gridPoint.y > 7 || gridPoint.x < 0 || gridPoint.y < 0)
        {
            return null;
        }
        return pieces[gridPoint.x, gridPoint.y];
    }

    public Vector2Int GridForPiece(GameObject piece)
    {
        for (int i = 0; i < 8; i++) 
        {
            for (int j = 0; j < 8; j++)
            {
                if (pieces[i, j] == piece)
                {
                    return new Vector2Int(i, j);
                }
            }
        }

        return new Vector2Int(-1, -1);
    }

    public bool FriendlyPieceAt(Vector2Int gridPoint, bool realMove = true, Player other = null)
    {
        GameObject piece = PieceAtGrid(gridPoint);

        if (realMove)
        {
            if (piece == null)
            {
                return false;
            }

            if (otherPlayer.pieces.Contains(piece))
            {
                return false;
            }

            return true;
        }
        else
        {
            if(piece == null)
            {
                return false;
            }

            if (other.pieces.Contains(piece))
            {
                return false;
            }

            return true;
        }
    }

    [PunRPC]
    public void UpdateCurrentPlayer(string currentPlayer)
    {
        if (currentPlayer == "white")
        {
            this.currentPlayer = white;
            this.otherPlayer = black;
        }
        else
        {
            this.currentPlayer = black;
            this.otherPlayer = white;
        }
    }

    public void NextPlayer()
    {
        if (NetworkManager.isMulti)
        {
            if (currentPlayer == white)
            {
                currentPlayer = black;
                otherPlayer = white;
                if (!firstMoveMade)
                {
                    DisableButtons();
                    firstMoveMade = true;
                    uiPromptText.text = "";
                }
            }
            else
            {
                currentPlayer = white;
                otherPlayer = black;
            }
            photonView.RPC("UpdateCurrentPlayer", RpcTarget.Others, currentPlayer.name);
        } else
        {
            if (currentPlayer == white)
            {
                currentPlayer = black;
                otherPlayer = white;
            }

            if (currentPlayer.name == "black" && !gameOver)
            {
                if (currentDifficulty == Difficulty.Easy)
                {
                    depth = 1;
                }
                else if (currentDifficulty == Difficulty.Medium)
                {
                    depth = 3;
                }
                else
                {
                    depth = 4;
                }

                StartCoroutine(AITurn());
                CheckmateCheck();
            }
            else
            {
                if (!firstMoveMade)
                {
                    DisableButtons();
                    firstMoveMade = true;
                    uiPromptText.text = "";
                }
            }
        }
    }

    IEnumerator AITurn()
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log(depth);

        (float, Move) miniMaxCall = miniMaxAI.MinMax(depth, true);
        if (miniMaxCall.Item2 == null)
        {
            GameOver(currentPlayer);
        }

        Debug.Log("---------------------------");

        Move moveToMake = miniMaxCall.Item2;

        miniMaxAI.EvaluatedMovesCount();
        Debug.Log("Best Score: " + miniMaxCall.Item1);
        Debug.Log("Piece: " + moveToMake.piece.name + " Current Pos: " + moveToMake.source + " Dest Pos: " + moveToMake.dest);

        if (PieceAtGrid(moveToMake.dest) == null)
        {
            Move(moveToMake.piece, moveToMake.dest);
        }
        else
        {
            CapturePieceAt(moveToMake.dest);
            Move(moveToMake.piece, moveToMake.dest);
        }

        currentPlayer = white;
        otherPlayer = black;
    }

    //Luke Guest
    public List<Move> GenerateAllMoves(Player player)
    {
        string playerStr;

        if (player.name == "white")
        {
            playerStr = "White";
        }
        else {
            playerStr = "Black";
        }

        List<Move> moves = new List<Move>();

        for (int i = 0; i < pieces.GetLength(0); i++)
        {
            for(int j = 0; j < pieces.GetLength(1); j++)
            {
                if(pieces[i,j] != null)
                {
                    if (pieces[i, j].name.Contains(playerStr))
                    {
                        Vector2Int gridPos = GridForPiece(pieces[i, j]);

                        bool maximising = false;
                        if(player.name == "black")
                        {
                            maximising = true;
                        }

                        List<Vector2Int> movePos = MovesForPiece(pieces[i, j], false, maximising);

                        foreach(Vector2Int pos in movePos)
                        {
                            moves.Add(new Move(gridPos, pos, pieces[i, j]));
                        }
                    }
                }
            }
        }

        return moves;
    }

    public void UndoDelete(GameObject piece, Vector2Int position, Player player)
    {
        GameObject test = player.capturedPieces[player.capturedPieces.Count - 1];
        player.capturedPieces.Remove(test);
        
        pieces[position.x, position.y] = test;
    }

    public void ChangeDifficulty(Difficulty diff)
    {
        if (!NetworkManager.isMulti)
        {
            currentDifficulty = diff;
            uiPromptText.text = "Current Difficulty: " + diff.ToString();
        }
    }

    private void DisableButtons()
    {
        foreach(GameObject buttonObj in buttons)
        {
            buttonObj.SetActive(false);
        }
    }

    public void CheckmateCheck()
    {
        bool checkMate = false;

        List<List<Vector2Int>> gameMoves = new List<List<Vector2Int>>();
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (pieces[i, j] != null)
                {
                    GameObject piece = pieces[i, j];

                    if (piece.name.Contains("Black"))
                    {
                        gameMoves.Add(MovesForPiece(piece));
                    }
                }
            }
        }

        foreach(List<Vector2Int> moveList in gameMoves)
        {
            foreach(Vector2Int move in moveList)
            {
                GameObject piece = PieceAtGrid(move);
                if(piece != null)
                {
                    if(piece.GetComponent<Piece>().type == PieceType.King)
                    {
                        Debug.Log("King Found");
                        checkMate = true;
                    }
                }
            }
        }

        if (checkMate)
        {
            uiPromptText.text = "Checkmate!";
        }
        else
        {
            uiPromptText.text = "";
        }
    }

    private void GameOver(Player player)
    {
        gameOverPanel.SetActive(true);

        gameOver = true;

        if(player.name == "white")
        {
            winText.text = "White Wins!";
        }
        else
        {
            winText.text = "Black Wins!";
        }
    }
}