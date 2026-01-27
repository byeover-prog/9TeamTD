using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveData
{
    public int nowStage = 1;
    public OP<int> gold = new();
    public List<string> ownedItems = new List<string>();
}

public class SaveManager : MonoBehaviour
{
    public SaveData nowPlayer = new SaveData();

    [HideInInspector] public int nowSlot;

    public static SaveManager instance;
    private void Awake()
    {
        #region 싱글톤
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        #endregion

        nowSlot = 0; // 현재 슬롯 0번만 명시적으로 사용하고 있음 (나중에 슬롯 추가하면 변경)
    }

    public string GetPath(int slotNum)
    {

        return Path.Combine(Application.persistentDataPath, $"save_{slotNum}.json");
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(nowPlayer);
        File.WriteAllText(GetPath(nowSlot), json);
    }

    public void LoadData()
    {
        if (File.Exists(GetPath(nowSlot)))
        {
            string json = File.ReadAllText(GetPath(nowSlot));
            
            JsonUtility.FromJsonOverwrite(json, nowPlayer);
            
            nowPlayer.gold ??= new OP<int>();
        }
        else
        {
            nowPlayer = new SaveData();
            SaveData();
        }
    }

    public bool LoadDataForPreview(int slotNum)
    {
        if (!File.Exists(GetPath(slotNum)))
        {
            return false;
        }
        return true;
    }
}
