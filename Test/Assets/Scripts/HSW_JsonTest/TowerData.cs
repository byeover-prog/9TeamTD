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

// TowerDatas 형식의 리스트로 만들어 관리 
[Serializable]
public class TowerDataList
{
    public List<TowerDatas> towers;
}
