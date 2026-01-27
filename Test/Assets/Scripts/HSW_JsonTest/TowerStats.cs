using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerStats : MonoBehaviour
{
    // TowerData.cs 참고하여 모두 설정해줌
    [Header("Tower Status")]
    public int towerID;  // 식별자
    public string towerName;
    public int level;
    public int maxHP;
    public int attackValue;


    // 호출 받으면 TowerData.cs 참고하여 모두 설정해줌
    public void SetupValue(TowerDatas data)
    {
        if (data == null) return;

        // json과 동일해야 함
        towerID = data.towerID;
        towerName = data.towerName;
        level = data.level;
        maxHP = data.maxHP;
        attackValue = data.attackValue;

        Debug.Log($"{towerName}의 능력치 설정 완료");
    }
}
