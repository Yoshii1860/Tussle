using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class MerchantNPC : NetworkBehaviour
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

    [ServerRpc(RequireOwnership = false)]
    public void RequestMerchantInteractionServerRpc(ulong clientId)
    {
        Player player = GameManager.Instance.GetPlayer(clientId);
        if (player == null)
        {
            Debug.LogError($"MerchantNPC: Player with clientId {clientId} not found.");
            return;
        }

        CoinWallet coinWallet = player.GetComponent<CoinWallet>();
        if (coinWallet == null)
        {
            Debug.LogError($"MerchantNPC: CoinWallet component not found on player {player.name}.");
            return;
        }

        int coinCount = coinWallet.CoinCount.Value;

        if (player.DoesPlayerHaveKey())
        {
            GiveAdviceClientRpc(clientId);
        }
        else if (coinCount >= minCoinCount)
        {
            player.ReceiveKey();

            BuyFromMerchantClientRpc(clientId);

            player.GetComponent<PlayerUIManager>().SpawnBuffClientRpc(ObjectType.Key, 0);
        }
        else
        {
            TalkToMerchantClientRpc(clientId);
        }
    }

    [ClientRpc]
    private void GiveAdviceClientRpc(ulong clientId)
    {
        animator.SetTrigger("Talking");
        
        
        if (NetworkManager.Singleton.LocalClientId != clientId) { return; }

        symbol.SetActive(false);
        adviceMessage.SetActive(true);
        Invoke(nameof(HideMessage), 5f);
    }

    [ClientRpc]
    private void BuyFromMerchantClientRpc(ulong clientId)
    {
        animator.SetTrigger("Buying");

        if (NetworkManager.Singleton.LocalClientId != clientId) { return; }

        symbol.SetActive(false);
        buyMessage.SetActive(true);
        Invoke(nameof(HideMessage), 8f);
    }

    [ClientRpc]
    private void TalkToMerchantClientRpc(ulong clientId)
    {
        animator.SetTrigger("Talking");
        
        if (NetworkManager.Singleton.LocalClientId != clientId) { return; }

        symbol.SetActive(false);
        talkMessage.SetActive(true);
        Invoke(nameof(HideMessage), 5f);
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
