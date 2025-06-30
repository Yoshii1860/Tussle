using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class NetworkAudioManager : NetworkBehaviour
{
    public void PlaySFXAtPosition(string name, Vector3 position)
    {
        if (!IsServer)
        {
            RequestPlaySFXServerRpc(name, position);
            return;
        }

        PlaySFXForAllClientRpc(name, position);
    }

    [ClientRpc]
    private void PlaySFXForAllClientRpc(string name, Vector3 position)
    {
        SoundEffect sfx = AudioManager.Instance.soundEffects.Find(s => s.name == name);
        if (sfx == null) { return; }

        GameObject audioObject = new GameObject("TempAudio");
        AudioSource audioSource = audioObject.AddComponent<AudioSource>();

        audioSource.loop = false;
        audioSource.clip = sfx.clip;
        audioSource.volume = Random.Range(sfx.volumeMin, sfx.volumeMax);
        audioSource.pitch = Random.Range(sfx.pitchMin, sfx.pitchMax);
        audioSource.maxDistance = sfx.audioRange;

        if (position != Vector3.zero)
        {
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Custom;
            audioObject.transform.position = position;
        }

        audioSource.Play();
        StartCoroutine(DestroyOnCompletion(audioSource, audioObject));
    }

    private IEnumerator DestroyOnCompletion(AudioSource audioSource, GameObject audioObject)
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        Destroy(audioObject);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPlaySFXServerRpc(string name, Vector3 position)
    {
        PlaySFXForAllClientRpc(name, position);
    }
}