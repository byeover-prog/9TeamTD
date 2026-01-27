using UnityEngine;
using System;
using System.Collections.Generic; // 데이터에 리스트 사용시 필요

[Serializable]
public class TowerDatas // .json 파일과 이름과 겹치면 안 됨
{
    public int towerID;
    public string towerName;
    public int level;
    public int maxHP;
    public int attackValue;
}

[Serializable]
public class TowerDataList
{
    public List<TowerDatas> towers;
}
