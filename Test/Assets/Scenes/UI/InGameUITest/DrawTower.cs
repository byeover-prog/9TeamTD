using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DrawTower : MonoBehaviour // 타워 배치 UI와 상호작용
{
    [SerializeField] GridSystem gridSystem;
    [SerializeField] GameObject[] TowerPrefabs;
    [HideInInspector] public int targetTower;

    Vector3 worldPoint; Cell cell;
    private void Update()
    {
        // 항상 플레이어의 위치에 worldPont를 설정
        worldPoint = transform.position;
        cell = gridSystem.WorldToCell(worldPoint);

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) // 타워 배치
        {
            if(gridSystem.TryPlaceTower(cell))
            {
                Instantiate(TowerPrefabs[targetTower], gridSystem.CellToWorld(cell, y: 0f), Quaternion.identity);
            }
        }
    }

}

