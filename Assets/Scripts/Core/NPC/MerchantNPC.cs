using UnityEngine;

public class MerchantNPC : MonoBehaviour
{
    [SerializeField] private GameObject symbol;
    [SerializeField] private GameObject talkMessage;
    [SerializeField] private GameObject buyMessage;
    [SerializeField] private GameObject adviceMessage;
    [SerializeField] private int minCoinCount = 1000;
    [SerializeField] private ObjectType objectType = ObjectType.Key;

    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void InteractWithMerchant(ulong clientId)
    {
        Player player = GameManager.Instance.GetPlayer(clientId);
        int coinCount = player.GetComponent<CoinWallet>().CoinCount.Value;

        if (player.HasKey())
        {
            GiveAdvice();
        }
        else if (coinCount >= minCoinCount)
        {
            BuyFromMerchant(player);
        }
        else
        {
            TalkToMerchant();
        }
    }

    private void GiveAdvice()
    {
        animator.SetTrigger("Talking");
        Debug.Log("Giving advice to player...");
        symbol.SetActive(false);
        adviceMessage.SetActive(true);
        Invoke(nameof(HideMessage), 5f); // Hide message after 5 seconds
    }

    private void BuyFromMerchant(Player player)
    {
        animator.SetTrigger("Buying");
        Debug.Log($"Buying from merchant with {player.GetComponent<CoinWallet>().CoinCount.Value} coins.");
        symbol.SetActive(false);
        buyMessage.SetActive(true);
        player.GetComponent<PlayerUIManager>().SpawnBuffClientRpc(objectType, 0);
        player.ReceiveKey();
        Invoke(nameof(HideMessage), 8f); // Hide message after 5 seconds
    }

    private void TalkToMerchant()
    {
        animator.SetTrigger("Talking");
        Debug.Log("Talking to merchant...");
        symbol.SetActive(false);
        talkMessage.SetActive(true);
        Invoke(nameof(HideMessage), 5f); // Hide message after 3 seconds
    }

    private void HideMessage()
    {
        talkMessage.SetActive(false);
        buyMessage.SetActive(false);
        adviceMessage.SetActive(false);
        symbol.SetActive(true);
        Debug.Log("Finished talking to merchant.");
    }
}
