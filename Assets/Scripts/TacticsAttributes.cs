﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsAttributes : MonoBehaviour
{
    public int actionPoints = 2;
    public int attackRange = 2;
    public Cell currentCell;

    [Header("Attack and Defense Attributes")]
    public float health = 10;
    public Status status;
    public GameObject burnDamagePrefab;

    [HideInInspector]
    public int actionPointsReset;
    public float healthReset;
    public List<Cell> selectableCells = new List<Cell>();
    public GameObject damageNumbers;
    public Animator anim;

    [Header("Positional and Movement Information")]
    public int xPositionCurrent;
    public int zPositionCurrent;
    public int yPositionCurrent;
    public int maximumTeamBounces;
    public bool canClimb = false;
    public Cell cell;

    public bool movementSelected = false;
    public bool attackingSelected = false;
    public bool shootSelected = false;
    public bool shootAbilitySelected = false;
    public bool isSelected = false;
    
    public bool hasBeenShotAt = false;
    // Start is called before the first frame update

    List<GameObject> players; 
    GameObject[] enemies; 
    void Start()
    {
        status = Status.None;
        anim = GetComponent<Animator>();
        players = convert(GameObject.FindGameObjectsWithTag("Player"));
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        cell = GameStateManager.FindCell(xPositionCurrent, zPositionCurrent);
        for (int i = 0; i < enemies.Length; i++)
        {
            players.Add(enemies[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 dir = (transform.position + Vector3.forward * 3) - transform.position;
        //Debug.DrawRay(transform.position, dir);
    }

    private void OnMouseEnter()
    {
        GameObject activeUnit = GameStateManager.activeUnit;
        if (activeUnit && activeUnit.name != gameObject.name && ReturnCurrentCell().isSelectable && !GameStateManager.isAnyoneAttacking && !GameStateManager.isAnyoneMoving)
        {
            if (GameStateManager.activeLaunchUnit)
            {
                return;
            }
            TacticsMove tm = activeUnit.GetComponent<TacticsMove>();
            if (tm.teamBounceCells.Count <= 0)
            {
                tm.MoveToCell(ReturnCurrentCell(), false);
            } else
            {
                tm.DrawBounceLine(cell.transform.position, false);
            }
            //GetComponent<PlayerMove>().MoveToCell(ReturnCurrentCell(), false);
        }
    }

    public void Deselect()
    {
        movementSelected = false;
        attackingSelected = false;
        shootSelected = false;
        isSelected = false;
        hasBeenShotAt = false;
    }

    public void GetCurrentCell()
    {
        currentCell = cell;
        //currentCell =  GetTargetCell(gameObject);
        currentCell.setIsCurrent(true);
    }

    public Cell ReturnCurrentCell()
    {
        return cell;
    }

    public void RemoveSelectableCells()
    {
        if (currentCell != null)
        {
            currentCell.isCurrent = false;
            currentCell = null;
        }
        foreach (Cell cell in selectableCells)
        {
            cell.ResetBFSVariables();
        }

        selectableCells.Clear();
    }

    public void TakeDamage(int damage, bool doAnim)
    {
        health -= damage;
        if (health <= 0 && doAnim)
        {
            KillUnit(doAnim);
        }
        else if (doAnim)
        {
            anim.SetTrigger("takeDamage");
        }
        ShowFloatingDamage(damage);
    }

    void KillUnit(bool doAnim)
    {
        if (doAnim)
        {
            anim.SetTrigger("Die");
        }
        Destroy(gameObject, 2);
        TurnManager.UpdateUnitCount();
    }

    public void CheckStatus()
    {
        if (status == Status.Burned)
        {
            TakeDamage(1, true);
            Instantiate(burnDamagePrefab, transform.position + Vector3.up, Quaternion.identity);
        }
    }
    
    void ShowFloatingDamage(int damage)
    {
        GameObject num = Instantiate(damageNumbers, transform.position + (Vector3.up), Quaternion.identity, transform);
        FaceTextMeshToCamera(num, Camera.main.transform);
        num.GetComponent<TextMesh>().text = damage.ToString();
    }

    public void DecrementActionPoints(int count)
    {
        actionPoints -= count;
    }

    private void OnMouseExit()
    {
        transform.GetChild(1).gameObject.SetActive(false);
        hasBeenShotAt = false;
    }

    List<GameObject> convert(GameObject[] arr)
    {
        List<GameObject> list = new List<GameObject>();
        for (int i = 0; i < arr.Length; i++)
        {
            list.Add(arr[i]);
        }
        return list;
    }

    public void FaceDirection(Directions dir)
    {
        Vector3 target = transform.position;
        if (dir == Directions.Up)
        {
            target += Vector3.forward;
        }
        else if (dir == Directions.Down)
        {
            target += Vector3.back;
        }
        else if (dir == Directions.Right)
        {
            target += Vector3.right;
        }
        else
        {
            target += Vector3.left;
        }
        StartCoroutine(TurnTowardsTarget(target));
    }

    public IEnumerator TurnTowardsTarget(Vector3 target)
    {
        Quaternion rotTarget = Quaternion.LookRotation(target - transform.position);
        rotTarget.x = transform.rotation.x;
        rotTarget.z = transform.rotation.z;
        float rotationValue = Mathf.Abs(rotTarget.eulerAngles.y - transform.eulerAngles.y);
        float turnSpeed = 130;
        if ((rotationValue >= 270 && rotationValue <= 360) ||
            rotationValue >= 0 && rotationValue <= 90)
        {
            turnSpeed = 90;
        }
        bool hasTriggeredAnim = false;
        bool isFullTurn = false;
        if (rotationValue > 100 && rotationValue < 260)
        {
            isFullTurn = true;
        }
        float firstFrameAngle = 500; //number irrelevant as long as its > 360
        float secondFrameAngle = 500;
        while (!(transform.eulerAngles.y >= rotTarget.eulerAngles.y - 0.05f && transform.eulerAngles.y <= rotTarget.eulerAngles.y + 0.05f))
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotTarget, Time.deltaTime * turnSpeed);
            if (!hasTriggeredAnim)
            {
                if (firstFrameAngle > 360)
                {
                    firstFrameAngle = transform.eulerAngles.y;
                    continue;
                } else if (secondFrameAngle > 360)
                {
                    secondFrameAngle = transform.eulerAngles.y;
                    continue;
                }
                if (secondFrameAngle > firstFrameAngle)
                {
                    if (isFullTurn)
                    {
                        anim.SetTrigger("halfTurnRight");
                    }
                    else
                    {
                        anim.SetTrigger("quarterTurnRight");
                    }
                }
                else
                {
                    if (isFullTurn)
                    {
                        anim.SetTrigger("halfTurnLeft");
                    }
                    else
                    {
                        anim.SetTrigger("quarterTurnLeft");
                    }
                }
                hasTriggeredAnim = true;
            }
            yield return null;
        }
    }

    public IEnumerator SideStep(Directions dir)
    {
        string animValue;
        Vector3 target;
        if (dir == Directions.Left)
        {
            target = transform.position + -transform.right * .75f;
            animValue = "StepLeft";
        } else 
        {
            target = transform.position + transform.right * .75f;
            animValue = "StepRight";
        } 
        anim.SetTrigger(animValue);
        while (Vector3.Distance(transform.position, target) > 0.05)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime *.6f);
            yield return null;
        }
        yield return new WaitForSeconds(0.3f);
    }

    public Directions GetSideStepDirection(Cell enemyLocation)
    {
        if (enemyLocation.xCoordinate > cell.xCoordinate + 1)
        {
            if (cell.GetNeighbor(Directions.Right) && (cell.GetNeighbor(Directions.Right).isCovered || cell.GetNeighbor(Directions.Right).yCoordinate > cell.yCoordinate))
            {
                if (enemyLocation.zCoordinate > cell.zCoordinate)
                {
                    return Directions.Left;
                } else
                {
                    return Directions.Right;
                }
            }
        }
        if (enemyLocation.xCoordinate < cell.xCoordinate - 1)
        {
            if (cell.GetNeighbor(Directions.Left) && (cell.GetNeighbor(Directions.Left).isCovered || cell.GetNeighbor(Directions.Left).yCoordinate > cell.yCoordinate))
            {
                if (enemyLocation.zCoordinate > cell.zCoordinate)
                {
                    return Directions.Right;
                }
                else
                {
                    return Directions.Left;
                }
            }
        }
        if (enemyLocation.zCoordinate > cell.zCoordinate + 1)
        {
            if (cell.GetNeighbor(Directions.Up) && (cell.GetNeighbor(Directions.Up).isCovered || cell.GetNeighbor(Directions.Up).yCoordinate > cell.yCoordinate))
            {
                if (enemyLocation.xCoordinate > cell.xCoordinate)
                {
                    return Directions.Right;
                } else
                {
                    return Directions.Left;
                }
            }
        }
        if (enemyLocation.zCoordinate < cell.zCoordinate - 1)
        {
            if (cell.GetNeighbor(Directions.Down) && (cell.GetNeighbor(Directions.Down).isCovered || cell.GetNeighbor(Directions.Down).yCoordinate > cell.yCoordinate))
            {
                if (enemyLocation.xCoordinate > cell.xCoordinate)
                {
                    return Directions.Left;
                }
                else
                {
                    return Directions.Right;
                }
            }
        }
        return Directions.Up; //Don't Turn
    }

    public static void FaceTextMeshToCamera(GameObject textMeshObject, Transform textLookTargetTransform)
    {
        textMeshObject.transform.LookAt(2 * textMeshObject.transform.position - Camera.main.transform.position);
    }
}
