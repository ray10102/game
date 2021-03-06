﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticsShoot : MonoBehaviour
{
    public TacticsAttributes attributes;
    public bool checkedSelectableCells = false;
    public bool isShooting;
    public int shootRange = 5;
    public GameObject target;
    public GameObject projectilePrefab;
    public GameObject bigProjectilePrefab;
    public GameObject impactPrefab;
    public int damage = 150;
    public int shotCost = 2;
    public Material noLineOfSight;
    Vector3 velocity = new Vector3();
    Vector3 heading = new Vector3();

    public void Init()
    {
        attributes = GetComponent<TacticsAttributes>();
    }

    public void FindSelectableCells(Cell cellParam, bool usingShootAbility, int range)
    {
        GameStateManager.ResetCellBools();
        GameStateManager.ComputeAdjList();
        int fireRange = range;
        if (usingShootAbility)
        {
            fireRange = GetComponent<AbilityAttributes>().GetShootAbilityRange();
        }
        Queue<Cell> process = new Queue<Cell>();
        process.Enqueue(cellParam);
        if (cellParam)
        {
            cellParam.visited = true;
        }
        while (process.Count > 0)
        {
            Cell c = process.Dequeue();

            c.isInShootRange = true;
            if (usingShootAbility)
            {
                c.isInAbilityRange = true;
            }
            attributes.selectableCells.Add(c);

            if (c.distance < fireRange)
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

    GameObject projectile;
    public void SpawnBullet(GameObject enemy, bool isBigShot)
    {
        target = enemy;
        if (!isBigShot)
        {
            projectile = Instantiate(projectilePrefab, transform.position + (Vector3.up * .7f), Quaternion.identity);
        } else
        {
            projectile = Instantiate(bigProjectilePrefab, transform.position + (Vector3.up * .7f), Quaternion.identity);
        }
        ProjectileAttributes pa = projectile.GetComponent<ProjectileAttributes>();
        pa.target = enemy.transform;
        pa.velocity = velocity;
        pa.heading = heading;
        pa.damage = damage;
        pa.impactPrefab = impactPrefab;
        if (!target.GetComponent<TacticsAttributes>().ReturnCurrentCell().isSafeWhenShot(attributes.ReturnCurrentCell())) //if enemy is not behind cover
        {
            projectile.GetComponent<ProjectileAttributes>().willMiss = false;
        } else
        {
            Cell coverCell = target.GetComponent<TacticsAttributes>().ReturnCurrentCell().GetCoverCell(attributes.ReturnCurrentCell());
            pa.target = coverCell.transform;
            pa.willMiss = true;
        }
        
    }

    public void PerformShoot(Cell c, int howManyShots, bool isBigShot)
    {
        //Dynamic Dispatch wasn't working pain
        if (GetComponent<AbilityAttributes>() is AbilityAttributes)
        {
            GetComponent<AbilityAttributes>().PerformShoot(c, howManyShots, isBigShot);
            return;
        }
        StartCoroutine(ShootCoroutine(this, c, howManyShots, isBigShot));
    }

    IEnumerator ShootCoroutine(TacticsShoot ps, Cell c, int howManyShots, bool isBigShot)
    {
        int count = 0;
        if (GetComponent<AbilityAttributes>() != null)
        {
            AbilityAttributes aa = GetComponent<AbilityAttributes>();
            if (isBigShot)
            {
                shotCost = aa.GetBigShotCost();
            } else
            {
                shotCost = aa.GetStandardShotCost();
            }
        }
        while (count < howManyShots)
        {

            ps.SetUpShot(c.attachedUnit, isBigShot);
            count++;
            yield return new WaitForSeconds(.1f);
        }
    }

    public void SetUpShot(GameObject targetUnit, bool isBigShot)
    {
        SpawnBullet(targetUnit, isBigShot);
        isShooting = true;
        GameStateManager.isAnyoneAttacking = true;
        attributes.anim.SetTrigger("Attack");
        attributes.DecrementActionPoints(shotCost);
    }
    public bool HasLineOfSight(Cell target)
    {
        GameStateManager.ChangeUnitsRaycastLayer(false);
        GameStateManager.ChangeNeighboringCoverLayer(attributes.cell, false);
        GameStateManager.ChangeNeighboringCoverLayer(target, false);
        RaycastHit hit;
        Vector3 targetVec = new Vector3(target.transform.position.x, target.transform.position.y + .5f, target.transform.position.z);
        Vector3 rightVec = transform.position + Vector3.right + Vector3.up*.5f;
        Vector3 leftVec = transform.position + Vector3.left + Vector3.up * .5f;
        Vector3 upVec = transform.position + Vector3.forward + Vector3.up * .5f;
        Vector3 downVec = transform.position + Vector3.back + Vector3.up * .5f;
        Vector3 currentVec = transform.position + Vector3.up * .5f;
        bool final = false;
        if (!Physics.Raycast(currentVec, targetVec - currentVec, out hit, Vector3.Distance(currentVec, targetVec)))
        {
            print("current cell clear shot");
            final = true;
        }
        else if (!Physics.Raycast(rightVec, targetVec - rightVec, out hit, Vector3.Distance(rightVec, targetVec)))
        {
            print("right side clear shot");
            final = true;
        }
        else if (!Physics.Raycast(leftVec, targetVec - leftVec, out hit, Vector3.Distance(leftVec, targetVec)))
        {
            print("Left side clear shot");
            final = true;
        }
        else if (!Physics.Raycast(upVec, targetVec - upVec, out hit, Vector3.Distance(upVec, targetVec)))
        {
            print("up side clear shot");
            final = true;
        }
        else if (!Physics.Raycast(downVec, targetVec - downVec, out hit, Vector3.Distance(downVec, targetVec)))
        {
            print(downVec);
            print(targetVec);
            print("down side clear shot");
            final = true;
        }
        GameStateManager.ChangeNeighboringCoverLayer(attributes.cell, true);
        GameStateManager.ChangeNeighboringCoverLayer(target, true);
        GameStateManager.ChangeUnitsRaycastLayer(true);
        if (!final)
        {
            RaycastHit newHit;
            if (Physics.Raycast(currentVec, targetVec - currentVec, out newHit, Vector3.Distance(currentVec, targetVec)))
            {
                StartCoroutine(NoLineOfSightRoutine(currentVec, newHit.point, targetVec));
            }
        }
        return final;
    }

    IEnumerator NoLineOfSightRoutine(Vector3 lineStart, Vector3 lineEnd, Vector3 test)
    {
        GameStateManager.CreatePopupAlert("No line of sight");
        LineRenderer line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, lineStart);
        line.SetPosition(1, lineEnd);
        float ogStartWidth = line.startWidth;
        float ogEndWidth = line.endWidth;
        Material ogMaterial = line.material;
        
        line.startWidth = .07f;
        line.endWidth = .07f;
        line.enabled = true;
        line.material = noLineOfSight;
        yield return new WaitForSeconds(1);
        line.startWidth = ogStartWidth;
        line.endWidth = ogEndWidth;
        line.enabled = false;
        line.material = ogMaterial;
    }

    public void Shoot()
    {

        if (GameObject.FindGameObjectsWithTag("Projectile").Length == 0)
        {
            GameStateManager.isAnyoneAttacking = false;
            GameStateManager.DeselectAllUnits();
            isShooting = false;
            return;
        }
    }
}
