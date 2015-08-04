﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pawn : MonoBehaviour
{
	//public bool GetComponent<Piece>().wasMoved = false;

	//for En Passant move
	public uint? turnOfDoublePush = null;
	public bool canBePassed = false;
	public GameObject passablePawn;
	public List<Field> legalMoves = new List<Field>();

	Vector3 moveVector;
	Vector3[] captureVector = new Vector3[2];

	//for promotion
	public int promotionGoal;
	public GameObject[] promotionPieces = new GameObject[4];
	public bool canBePromoted = false;


	void Start()
	{
		if (GetComponent<Piece>().isWhite)
		{
			moveVector = new Vector3(0, 1, 0);
			captureVector[0] = new Vector3(1, 1, 0);
			captureVector[1] = new Vector3(-1, 1, 0);
			promotionGoal = 7;
		}
		else
		{
			moveVector = new Vector3(0, -1, 0);
			captureVector[0] = new Vector3(1, -1, 0);
			captureVector[1] = new Vector3(-1, -1, 0);
			promotionGoal = 0;
		}
		GetComponent<Piece>().isPawn = true;
	}

	void Update()
	{
		if (turnOfDoublePush != null)
		{
			if (Game.turnsTaken - turnOfDoublePush < 2)
				canBePassed = true;
			else
			{
				canBePassed = false;
				turnOfDoublePush = null;
			}
		}

	}

	void OnMouseDown()
	{
		if (!(Game.isWhitesTurn ^ GetComponent<Piece>().isWhite) && (!Game.isOnline || GetComponent<NetworkView>().isMine))
		{
			GetLegalMoves();
			AvoidCheck(legalMoves);
		}
	}

	public void GetLegalMoves()
	{
		legalMoves.Clear();
		Vector3 actualPosition = transform.position;
		actualPosition += moveVector;
		if (Game.isInRange(actualPosition))
		{
			Field field = Board.board[(int)actualPosition.x, (int)actualPosition.y].GetComponent<Field>();
			if (field.HoldedPiece == null)
			{
				legalMoves.Add(field);
				if (!GetComponent<Piece>().wasMoved)
				{
					actualPosition += moveVector;
					if (Game.isInRange(actualPosition))
					{
						field = Board.board[(int)actualPosition.x, (int)actualPosition.y].GetComponent<Field>();
						if (field.HoldedPiece == null)
							legalMoves.Add(field);
					}
				}
			}
		}

		foreach (Vector3 direction in captureVector)
		{
			actualPosition = transform.position;
			actualPosition += direction;

			CheckEnPassant(actualPosition);
			if (Game.isInRange(actualPosition))
			{
				Field field = Board.board[(int)actualPosition.x, (int)actualPosition.y].GetComponent<Field>();
				if (field.HoldedPiece != null)
					if (field.HoldedPiece.GetComponent<Piece>().isWhite ^ GetComponent<Piece>().isWhite)
						legalMoves.Add(field);

			}
		}
	}

	[RPC] void SetPassablePawn(Vector3 passablePosition)
	{
		Field passableField = Board.board[(int)passablePosition.x, (int)passablePosition.y].GetComponent<Field>();
		passablePawn = passableField.HoldedPiece;
	}

	bool CheckEnPassant(Vector3 targetPosition)
	{
		Vector3 passablePosition;
		if (GetComponent<Piece>().isWhite)
			passablePosition = new Vector3(targetPosition.x, targetPosition.y - 1, 0);
		else
			passablePosition = new Vector3(targetPosition.x, targetPosition.y + 1, 0);
		if (Game.isInRange(passablePosition))
		{
			Field passableField = Board.board[(int)passablePosition.x, (int)passablePosition.y].GetComponent<Field>();
			if (passableField.HoldedPiece != null)
				if (passableField.HoldedPiece.GetComponent<Piece>().isPawn)
					if (passableField.HoldedPiece.GetComponent<Pawn>().canBePassed)
					{
						//passablePawn = passableField.HoldedPiece;
						if (Game.isOnline)
							GetComponent<NetworkView>().RPC("SetPassablePawn", RPCMode.OthersBuffered, passablePosition);
						SetPassablePawn(passablePosition);

						legalMoves.Add(Board.board[(int)targetPosition.x, (int)targetPosition.y].GetComponent<Field>());
                        return true;
					}
		}
		return false;
	}

	public void UpdatePawnStatus(Vector3 targetPosition)
	{
		if(!GetComponent<Piece>().wasMoved && (Mathf.Abs(targetPosition.y - transform.position.y) == 2))
		{
			canBePassed = true;
			turnOfDoublePush = Game.turnsTaken;
		}

		if(passablePawn != null)
		{
			if( Mathf.Abs(targetPosition.y - passablePawn.transform.position.y) == 1)
			{
				Board.board[(int)passablePawn.transform.position.x, (int)passablePawn.transform.position.y].GetComponent<Field>().HoldedPiece = null;
				passablePawn.GetComponent<Piece>().isAlive = false;
				Destroy(passablePawn);
				passablePawn = null;
			}
		}
		Game.turnOfLastCapture = Game.turnsTaken;
	}

	public int AvoidCheck(List<Field> targetFields)
	{
		Field ownedField = Board.board[(int)transform.position.x, (int)transform.position.y].GetComponent<Field>();
		int availableMoves = 0;
		Vector3 originalPosition = transform.position;

		Field passableField;

		foreach (Field consideredField in targetFields)
		{
			GameObject capturedPiece = consideredField.HoldedPiece;

			consideredField.HoldedPiece = gameObject;
			ownedField.HoldedPiece = null;

			if (passablePawn != null)
				if (Mathf.Abs(consideredField.transform.position.y - passablePawn.transform.position.y) == 1)
				{
					passableField = Board.board[(int)passablePawn.transform.position.x, (int)passablePawn.transform.position.y].GetComponent<Field>();
					passableField.HoldedPiece = null;

					
				}
			
			if (!Game.CheckIfCheck())
			{
				consideredField.isLegal = true;
				++availableMoves;
			}

			consideredField.HoldedPiece = capturedPiece;
			ownedField.HoldedPiece = gameObject;
			transform.position = originalPosition;

			if (passablePawn != null)
				if (Mathf.Abs(consideredField.transform.position.y - passablePawn.transform.position.y) == 1)
				{
					passableField = Board.board[(int)passablePawn.transform.position.x, (int)passablePawn.transform.position.y].GetComponent<Field>();
					passableField.HoldedPiece = passablePawn;


				}
			
		}
		return availableMoves;
	}

	void OnGUI()
	{
		if (canBePromoted && (!Game.isOnline || GetComponent<NetworkView>().isMine))
		{
			if (GUI.Button(new Rect(100, 100, 50, 50), promotionPieces[0].GetComponent<SpriteRenderer>().sprite.texture))
			{
				InsertPromotedPiece(0);
			}
			else if (GUI.Button(new Rect(100, 200, 50, 50), promotionPieces[1].GetComponent<SpriteRenderer>().sprite.texture))
			{
				InsertPromotedPiece(1);
			}
			else if (GUI.Button(new Rect(100, 300, 50, 50), promotionPieces[2].GetComponent<SpriteRenderer>().sprite.texture))
			{
				InsertPromotedPiece(2);
			}
			else if (GUI.Button(new Rect(100, 400, 50, 50), promotionPieces[3].GetComponent<SpriteRenderer>().sprite.texture))
			{
				InsertPromotedPiece(3);
			}
		}
	}

	void InsertPromotedPiece(int index)
	{
		GameObject newPiece = new GameObject();
		if (Game.isOnline)
		{
			newPiece = Network.Instantiate(promotionPieces[index], transform.position, transform.rotation, 0) as GameObject;
			GetComponent<NetworkView>().RPC("OvertakeField", RPCMode.OthersBuffered, newPiece.GetComponent<NetworkView>().viewID);
		}
		else
			newPiece = Instantiate(promotionPieces[index], transform.position, transform.rotation) as GameObject;

		OvertakeField(newPiece.GetComponent<NetworkView>().viewID);
	}

	[RPC] void OvertakeField(NetworkViewID id)
	{
		Field ownedField = Board.board[(int)transform.position.x, (int)transform.position.y].GetComponent<Field>();
		ownedField.HoldedPiece = NetworkView.Find(id).gameObject;

		canBePromoted = false;
		Game.canSwitchTurns = true;
		GetComponent<Piece>().isAlive = false;
		Destroy(gameObject);
	}
}