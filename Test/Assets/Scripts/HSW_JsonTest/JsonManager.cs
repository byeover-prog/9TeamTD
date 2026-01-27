using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static TowerStats;
using static UnityEngine.GraphicsBuffer;

// https://3dperson1.tistory.com/117 스크립트 기반 -> 추후 수정 예정
public class JsonManager : MonoBehaviour
{
    public static JsonManager instanceJsonManger { get; private set; }

    [SerializeField] private int targetID = 0;
    private TowerDataList _towerData;   // TowerStats 의 데이터를 불러오면 됨

    private void Awake()
    {
        // 이 매니저가 2개 이상 있으면 1개는 삭제
        #region 싱글톤
        if (instanceJsonManger == null)
        {
            instanceJsonManger = this;
            DontDestroyOnLoad(gameObject);
            LoadJsonFile(); // 파일 불러오기
        }
        else
        {
            Destroy(gameObject);
        }
        #endregion
    }

    public void Start()
    {
        ChangeID(targetID);

        /*
        // 1. Resources 폴더에서 JSON 파일 로드
        // Assets 폴더 밑에 "Resources" 라는 이름의 폴더를 만들고, 그 안에 PlayerData.json 파일을 넣어주세요.
        TextAsset jsonDataFile = Resources.Load<TextAsset>("TowerData"); // 파일 이름만 (확장자 제외)

        if (jsonDataFile != null)
        {
            towerDataList dataContainer = JsonUtility.FromJson<towerDataList>(jsonDataFile.text);


            string jsonString = jsonDataFile.text;
            Debug.Log("로드된 JSON 원본: " + jsonString);

            // 2. JSON 문자열을 PlayerData 객체로 변환 (역직렬화)
            TowerStats towerData = JsonUtility.FromJson<TowerStats>(jsonString);

            // 3. 데이터 활용
            if (towerData != null)
            {
                // 하이어라키에 있는 PlayerStats 스크립트를 가진 오브젝트를 찾습니다.
                // (현업에서는 보통 싱글톤이나 인스펙터 할당 방식을 사용하지만, 여기서는 직관적인 Get/Find를 사용합니다.)
                TowerStats towerData = FindFirstObjectByType<TowerStats>();

                if (towerData != null)
                {
                    // 로드한 데이터를 플레이어 실제 스텟 스크립트에 전달합니다.
                    towerData.Setup(towerData);
                }
                else
                {
                    Debug.LogWarning("씬 내에 PlayerStats 스크립트를 가진 오브젝트가 없습니다.");
                }
            }
            else
            {
                Debug.LogError("JSON 파싱 실패!");
            }

            // (선택) PlayerData 객체를 다시 JSON 문자열로 변환 (직렬화)
            // true를 넣으면 예쁘게 정렬된 형태로 출력됩니다.
            string newJsonString = JsonUtility.ToJson(towerData, true);
            Debug.Log("다시 직렬화된 JSON: " + newJsonString);

        }
        else
        {
            Debug.LogError("Resources 폴더에서 PlayerData.json 파일을 찾을 수 없습니다!");
        }
        */
    }

    private void LoadJsonFile()
    {
        TextAsset jsonDataFile = Resources.Load<TextAsset>("Datas/TowerData");    // TextAsset(텍스트 파일 형식) 으로 리소스 폴더 하위 경로에서 TowerData 파일을 불러옴
        if (jsonDataFile != null)
        {
            _towerData = JsonUtility.FromJson<TowerDataList>(jsonDataFile.text);    // JsonUtility 활용해 TowerDataList 역직렬화
        }
        else
        {
            Debug.LogError("파일 없음");
        }
    }

    // 원하는 ID를 타워에 전달하는 역할
    public void ChangeID(int id)
    {
        if (_towerData == null) return; // 타워 데이터에 없으면 리턴

        // TowerDataList에서 해당 ID 찾기
        TowerDatas foundData = _towerData.towers.Find(t => t.towerID == id);

        if (foundData != null)
        {
            TowerStats tower = FindFirstObjectByType<TowerStats>(); // 현재 씬에서 배치된 오브젝트중 TowerStats 붙은 오브젝트 1개만 찾기
            if (tower != null)
            {
                tower.SetupValue(foundData); // TowerStats 에서 받은 능력치로 설정
            }
        }
        else
        {
            Debug.LogWarning($"ID {id}에 해당하는 데이터 없음");
        }
    }
}
