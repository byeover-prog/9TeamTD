using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TestUI : MonoBehaviour
{
    
    [SerializeField] TextMeshProUGUI goldText;
    [SerializeField] TextMeshProUGUI stageText;
    [SerializeField] TextMeshProUGUI itemListText;

    private void Awake()
    {
        UpdateGoldText(SaveManager.instance.nowPlayer.gold.Value);
        UpdateStageText(SaveManager.instance.nowPlayer.nowStage);
        UpdateItemListText(SaveManager.instance.nowPlayer.ownedItems);
    }

    private void OnEnable()
    {
        SaveManager.instance.nowPlayer.gold.AddListener(UpdateGoldText);
    }

    private void OnDisable()
    {
        SaveManager.instance.nowPlayer.gold.RemoveListener(UpdateGoldText);
    }

    public void GoldUp()// 버튼 연결
    {
        SaveManager.instance.nowPlayer.gold.Value += 10;
    }

    public void StageUp()// 버튼 연결
    {
        SaveManager.instance.nowPlayer.nowStage += 1;
        UpdateStageText(SaveManager.instance.nowPlayer.nowStage);
    }

    public void AddItem()// 버튼 연결
    {
        string newItem = "Item" + (SaveManager.instance.nowPlayer.ownedItems.Count + 1);
        SaveManager.instance.nowPlayer.ownedItems.Add(newItem);
        UpdateItemListText(SaveManager.instance.nowPlayer.ownedItems);
    }

    void UpdateGoldText(int newGold)
    {
        goldText.text = $"Gold: {newGold}";
    }
    void UpdateStageText(int newStage)
    {
        stageText.text = $"Stage: {newStage}";
    }
    void UpdateItemListText(List<string> newItemList)
    {
        itemListText.text = "Items: " + string.Join(", ", newItemList);
    }

    public void SaveGame()// 버튼 연결
    {
        SaveManager.instance.SaveData();
    }
}
