using UnityEngine;
using System.Collections.Generic;

public class PrefabManager : MonoBehaviour
{
    [System.Serializable]
    public struct CharacterPrefab
    {
        public int characterId; // Enum or int representing the character type
        public GameObject prefab; // The prefab associated with this character
    }

    [SerializeField] private List<CharacterPrefab> characterPrefabs = new List<CharacterPrefab>();

    private Dictionary<int, GameObject> prefabDictionary;

    public static PrefabManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize the dictionary for quick lookup
        prefabDictionary = new Dictionary<int, GameObject>();
        foreach (var characterPrefab in characterPrefabs)
        {
            prefabDictionary[characterPrefab.characterId] = characterPrefab.prefab;
        }
    }

    public GameObject GetPrefabByCharacterId(int characterId)
    {
        if (prefabDictionary.TryGetValue(characterId, out GameObject prefab))
        {
            return prefab;
        }

        Debug.LogError($"PrefabManager: No prefab found for CharacterId {characterId}. Returning null.");
        return null;
    }
}
