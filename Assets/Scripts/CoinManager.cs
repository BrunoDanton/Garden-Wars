using UnityEngine;
using TMPro;

public class CoinManager : MonoBehaviour
{
    public static int totalMoney = 0;
    public TextMeshProUGUI totalMoneyText;

    void Update()
    {
        totalMoneyText.text = totalMoney.ToString();
    }

}