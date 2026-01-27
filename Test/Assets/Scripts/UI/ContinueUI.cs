using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinueUI : MonoBehaviour
{
    [SerializeField] GameObject continueButton;

    void Start()
    {
        bool hasSave = SaveManager.instance.LoadDataForPreview(SaveManager.instance.nowSlot);
        if (hasSave)
        {
            continueButton.SetActive(true);
        }
    }
}
