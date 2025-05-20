using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class MatchplayBackfiller : IDisposable
{
    private CreateBackfillTicketOptions createBackfillOptions;
    private BackfillTicket localBackfillTicket;
    private bool localDataDirty;
    private int maxPlayers;
    private const int TicketCheckMs = 1000;

    private MatchProperties MatchProperties => localBackfillTicket.Properties.MatchProperties;
    public bool IsBackfilling { get; private set; }
    public int MatchPlayerCount => localBackfillTicket?.Properties.MatchProperties.Players.Count ?? 0;

    public MatchplayBackfiller(string connection, string queueName, MatchProperties matchmakerPayloadProperties, int maxPlayers)
    {
        this.maxPlayers = maxPlayers;
        BackfillTicketProperties backfillProperties = new BackfillTicketProperties(matchmakerPayloadProperties);
        localBackfillTicket = new BackfillTicket
        {
            Id = matchmakerPayloadProperties.BackfillTicketId,
            Properties = backfillProperties
        };

        createBackfillOptions = new CreateBackfillTicketOptions
        {
            Connection = connection,
            QueueName = queueName,
            Properties = backfillProperties
        };
    }

    public async Task BeginBackfilling()
    {
        if (IsBackfilling)
        {
            Debug.LogWarning("Already backfilling, no need to start another.");
            return;
        }

        Debug.Log($"Starting backfill Server: {MatchPlayerCount}/{maxPlayers}");

        try
        {
            if (string.IsNullOrEmpty(localBackfillTicket.Id))
            {
                localBackfillTicket.Id = await MatchmakerService.Instance.CreateBackfillTicketAsync(createBackfillOptions);
                Debug.Log($"Backfill ticket created with ID: {localBackfillTicket.Id}");
            }

            IsBackfilling = true;
            BackfillLoop();
        }
        catch (MatchmakerServiceException e)
        {
            Debug.LogError($"Failed to start backfilling: {e.Message}");
            IsBackfilling = false;
        }
    }

    public async Task AddPlayerToMatch(UserData userData)
    {
        if (!IsBackfilling)
        {
            Debug.LogWarning("Can't add users to the backfill ticket before it's been created");
            return;
        }

        if (GetPlayerById(userData.userAuthId) != null)
        {
            Debug.LogWarning($"User: {userData.userName} - {userData.userAuthId} already in Match. Ignoring add.");
            return;
        }

        Unity.Services.Matchmaker.Models.Player matchmakerPlayer = new Unity.Services.Matchmaker.Models.Player(userData.userAuthId, userData.userGamePreferences);
        MatchProperties.Players.Add(matchmakerPlayer);
        MatchProperties.Teams[0].PlayerIds.Add(matchmakerPlayer.Id);
        localDataDirty = true;
        if (IsBackfilling)
        {
            await UpdateBackfillTicket();
        }
        Debug.Log($"Added player {userData.userAuthId} to backfill ticket. Current players: {MatchPlayerCount}/{maxPlayers}");
    }

    private async Task UpdateBackfillTicket()
    {
        try
        {
            await MatchmakerService.Instance.UpdateBackfillTicketAsync(localBackfillTicket.Id, localBackfillTicket);
            Debug.Log($"Backfill ticket {localBackfillTicket.Id} updated. Players: {MatchPlayerCount}/{maxPlayers}");
            localDataDirty = false;
        }
        catch (MatchmakerServiceException e)
        {
            Debug.LogError($"Failed to update backfill ticket: {e.Message}");
        }
    }

    public async Task<int> RemovePlayerFromMatch(string userId)
    {
        Unity.Services.Matchmaker.Models.Player playerToRemove = GetPlayerById(userId);
        if (playerToRemove == null)
        {
            Debug.LogWarning($"No user by the ID: {userId} in local backfill Data.");
            return MatchPlayerCount;
        }

        MatchProperties.Players.Remove(playerToRemove);
        MatchProperties.Teams[0].PlayerIds.Remove(userId);
        localDataDirty = true;
        Debug.Log($"Removed player {userId} from backfill ticket. Current players: {MatchPlayerCount}/{maxPlayers}");

        if (IsBackfilling)
        {
            await UpdateBackfillTicket();
        }
        
        return MatchPlayerCount;
    }

    public bool NeedsPlayers()
    {
        return MatchPlayerCount < maxPlayers;
    }

    private Unity.Services.Matchmaker.Models.Player GetPlayerById(string userId)
    {
        return MatchProperties.Players.FirstOrDefault(p => p.Id == userId);
    }

    public async Task StopBackfill()
    {
        if (!IsBackfilling)
        {
            Debug.LogWarning("Can't stop backfilling before we start.");
            return;
        }

        try
        {
            await MatchmakerService.Instance.DeleteBackfillTicketAsync(localBackfillTicket.Id);
            Debug.Log($"Backfill ticket {localBackfillTicket.Id} deleted.");
        }
        catch (MatchmakerServiceException e)
        {
            Debug.LogError($"Failed to stop backfilling: {e.Message}");
        }

        IsBackfilling = false;
        localBackfillTicket.Id = null;
    }

    private async void BackfillLoop()
    {
        while (IsBackfilling)
        {
            try
            {
                if (localDataDirty)
                {
                    await MatchmakerService.Instance.UpdateBackfillTicketAsync(localBackfillTicket.Id, localBackfillTicket);
                    Debug.Log($"Backfill ticket {localBackfillTicket.Id} updated. Players: {MatchPlayerCount}/{maxPlayers}");
                    localDataDirty = false;
                }
                else
                {
                    localBackfillTicket = await MatchmakerService.Instance.ApproveBackfillTicketAsync(localBackfillTicket.Id);
                    Debug.Log($"Backfill ticket {localBackfillTicket.Id} approved. Players: {MatchPlayerCount}/{maxPlayers}");
                }

                if (!NeedsPlayers())
                {
                    Debug.Log($"Backfill ticket {localBackfillTicket.Id} is full. Stopping backfill.");
                    await StopBackfill();
                    break;
                }
            }
            catch (MatchmakerServiceException e)
            {
                Debug.LogError($"Backfill loop error: {e.Message}. Stopping backfill.");
                await StopBackfill();
                break;
            }

            await Task.Delay(TicketCheckMs);
        }
    }

    public void Dispose()
    {
        _ = StopBackfill();
    }
}