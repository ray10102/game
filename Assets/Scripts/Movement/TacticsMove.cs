﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsMove : MonoBehaviour
{

    List<Cell> selectableCells = new List<Cell>();

    [Header("Movement Attributes")]
    public float jumpHeight = 2f;
    public float moveSpeed = 2f;
    public int moveDistance = 5;


    [Header("Other")]
    public GameObject[] cells;
    public bool hasMoved = false;
    public bool isMoving = false;

    Stack<Cell> path = new Stack<Cell>(); //for the path
    public Cell currentCell;

    Vector3 velocity = new Vector3();
    Vector3 heading = new Vector3();

    float halfHeight = 0;
    public int xPositionCurrent, yPositionCurrent;

    protected void Init()
    {
        cells = GameObject.FindGameObjectsWithTag("Cell");
        halfHeight = GetComponent<Collider>().bounds.extents.y;
        Cell startingPlace = Grid.gameBoard[xPositionCurrent][yPositionCurrent];
        transform.position = startingPlace.transform.position;
        transform.position = new Vector3(
            transform.position.x, 
            transform.position.y + halfHeight + startingPlace.GetComponent<Collider>().bounds.extents.y, //adding halfHeights of unit and cell
            transform.position.z);
    }

    public void GetCurrentCell()
    {
        currentCell = Grid.gameBoard[xPositionCurrent][yPositionCurrent];
        //currentCell =  GetTargetCell(gameObject);
        currentCell.setIsCurrent(true);
    }

    public Cell GetTargetCell(GameObject target)
    {
        RaycastHit hit;
        if (Physics.Raycast(target.transform.position, -Vector3.up, out hit, 1)) {
            return hit.collider.GetComponent<Cell>();
        }
        return null;
    }

    public void ComputeAdjList()
    {
        foreach(GameObject cell in cells)
        {
            Cell c = cell.GetComponent<Cell>();
            c.FindNeighbors(jumpHeight);
           // Debug.Log("X " + c.adjacencyList[0].xCoordinate);
           // Debug.Log(c.adjacencyList[0].yCoordinate);
        }
    }

    public void FindSelectableCells()
    {
        ComputeAdjList();
        GetCurrentCell();

        Queue<Cell> process = new Queue<Cell>();

        process.Enqueue(currentCell);
        currentCell.visited = true;
        //currentCell.parent ignore

        while (process.Count > 0)
        {
            Cell c = process.Dequeue();
            selectableCells.Add(c);
            c.isSelectable = true;

            if (c.distance < moveDistance)
            {
                foreach (Cell cell in c.adjacencyList)
                {
                    if (!cell.visited)
                    {
                        cell.parent = c;
                        cell.visited = true;
                        cell.distance = 1 + c.distance;
                        process.Enqueue(cell);
                    }
                }
            }
        }
            
    }

    public void MoveToCell(Cell c)
    {

        path.Clear();

        Cell next = c;
        while (next != null)
        {
            path.Push(next);
            next = next.parent;
        }
        c.isTarget = true;
        isMoving = true;
    }

    public void Move()
    {
        Cell lastInPath = null;
        if (path.Count > 0)
        {
            Cell c = path.Peek();
            Vector3 target = c.transform.position;

            //unit's position on top of target tile
            target.y += halfHeight + c.GetComponent<Collider>().bounds.extents.y;

            if (Vector3.Distance(transform.position, target) >= 0.05f)
            {
                CalculateHeading(target);
                SetHorizontalVelocity();

                transform.forward = heading;
                transform.position += velocity * Time.deltaTime;
            } else
            {
                //reached goal
                xPositionCurrent = path.Peek().xCoordinate;
                yPositionCurrent = path.Peek().yCoordinate;
                lastInPath = path.Pop();
            }
        } else
        {
            RemoveSelectableCells();
            isMoving = false;
            hasMoved = true;
            GameStateManager.DeselectAllUnits();
            
        }
    }

    protected void RemoveSelectableCells()
    {
        if (currentCell != null)
        {
            currentCell.isCurrent = false;
            currentCell = null;
        }
        foreach(Cell cell in selectableCells)
        {
            cell.ResetVariables();
        }

        selectableCells.Clear();
    }

    void CalculateHeading(Vector3 target)
    {
        heading = target - transform.position;
        heading.Normalize();
    }

    void SetHorizontalVelocity()
    {
        velocity = heading * moveSpeed;
    }
}