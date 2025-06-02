using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public enum MatchmakerPollingResult
{
    Success,
    TicketCreationError,
    TicketCancellationError,
    TicketRetrievalError,
    MatchAssignmentError
}

public class MatchmakingResult
{
    public string ip;
    public int port;
    public MatchmakerPollingResult result;
    public string resultMessage;
}

public class MatchplayMatchmaker : IDisposable
{
    private string lastUsedTicket;
    private CancellationTokenSource cancelToken;
    private const int TicketCooldown = 1000;
    private const int MaxPollingAttempts = 60;
    public bool IsMatchmaking { get; private set; }

    private static MatchplayMatchmaker instance;
    public static MatchplayMatchmaker Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new MatchplayMatchmaker();
            }
            return instance;
        }
    }

    private MatchplayMatchmaker() { }

    public async Task<MatchmakingResult> Matchmake(UserData data)
    {
        cancelToken = new CancellationTokenSource();
        data.teamIndex = -1;
        
        string queueName = data.userGamePreferences.ToMultiplayQueue();
        CreateTicketOptions createTicketOptions = new CreateTicketOptions(queueName);
        Debug.Log($"MatchplayMatchmaker: Using matchmaking queue '{createTicketOptions.QueueName}'.");

        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player>
        {
            new Unity.Services.Matchmaker.Models.Player(data.userAuthId, data.userGamePreferences)
        };

        try
        {
            IsMatchmaking = true;
            CreateTicketResponse createResult = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);
            lastUsedTicket = createResult.Id;
            Debug.Log($"MatchplayMatchmaker: Created matchmaking ticket '{lastUsedTicket}'.");

            int attempts = 0;
            while (!cancelToken.IsCancellationRequested && attempts < MaxPollingAttempts)
            {
                try
                {
                    TicketStatusResponse checkTicket = await MatchmakerService.Instance.GetTicketAsync(lastUsedTicket);
                    if (checkTicket.Type == typeof(MultiplayAssignment))
                    {
                        MultiplayAssignment matchAssignment = (MultiplayAssignment)checkTicket.Value;
                        if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Found)
                        {
                            Debug.Log($"MatchplayMatchmaker: Match found for ticket '{lastUsedTicket}'.");
                            return ReturnMatchResult(MatchmakerPollingResult.Success, "", matchAssignment);
                        }
                        if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Timeout ||
                            matchAssignment.Status == MultiplayAssignment.StatusOptions.Failed)
                        {
                            Debug.LogWarning($"MatchplayMatchmaker: Matchmaking failed for ticket '{lastUsedTicket}': {matchAssignment.Status} - {matchAssignment.Message}");
                            return ReturnMatchResult(MatchmakerPollingResult.MatchAssignmentError,
                                $"Ticket: {lastUsedTicket} - {matchAssignment.Status} - {matchAssignment.Message}", null);
                        }
                        if (attempts % 5 == 0)
                        {
                            Debug.Log($"MatchplayMatchmaker: Polling ticket '{lastUsedTicket}' (Attempt {attempts + 1}) - Status: {matchAssignment.Status}");
                        }
                    }
                }
                catch (MatchmakerServiceException e)
                {
                    Debug.LogError($"MatchplayMatchmaker: Error retrieving ticket '{lastUsedTicket}': {e.Message}");
                    return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, e.ToString(), null);
                }

                attempts++;
                await Task.Delay(TicketCooldown);
            }

            Debug.LogWarning($"MatchplayMatchmaker: Timed out after {MaxPollingAttempts} attempts for ticket '{lastUsedTicket}'.");
            return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, "Matchmaking timed out", null);
        }
        catch (MatchmakerServiceException e)
        {
            Debug.LogError($"MatchplayMatchmaker: Failed to create matchmaking ticket: {e.Message}");
            return ReturnMatchResult(MatchmakerPollingResult.TicketCreationError, e.ToString(), null);
        }
    }

    public async Task CancelMatchmaking()
    {
        if (!IsMatchmaking) { return; }
        IsMatchmaking = false;
        if (cancelToken.Token.CanBeCanceled) { cancelToken.Cancel(); }
        if (!string.IsNullOrEmpty(lastUsedTicket))
        {
            Debug.Log($"MatchplayMatchmaker: Cancelling matchmaking ticket '{lastUsedTicket}'.");
            try
            {
                await MatchmakerService.Instance.DeleteTicketAsync(lastUsedTicket);
            }
            catch (MatchmakerServiceException e)
            {
                Debug.LogError($"MatchplayMatchmaker: Failed to cancel ticket '{lastUsedTicket}': {e.Message}");
            }
        }
    }

    private MatchmakingResult ReturnMatchResult(MatchmakerPollingResult resultErrorType, string message, MultiplayAssignment assignment)
    {
        IsMatchmaking = false;
        if (assignment != null)
        {
            string parsedIp = assignment.Ip;
            int? parsedPort = assignment.Port;
            if (parsedPort == null)
            {
                Debug.LogError($"MatchplayMatchmaker: Assignment missing port. Message: {assignment.Message}");
                return new MatchmakingResult
                {
                    result = MatchmakerPollingResult.MatchAssignmentError,
                    resultMessage = $"Port missing? - {assignment.Port}\n-{assignment.Message}"
                };
            }
            return new MatchmakingResult
            {
                result = MatchmakerPollingResult.Success,
                ip = parsedIp,
                port = (int)parsedPort,
                resultMessage = assignment.Message
            };
        }
        if (!string.IsNullOrEmpty(message))
        {
            Debug.LogWarning($"MatchplayMatchmaker: Returning error result: {resultErrorType} - {message}");
        }
        return new MatchmakingResult
        {
            result = resultErrorType,
            resultMessage = message
        };
    }

    public void Dispose()
    {
        _ = CancelMatchmaking();
        cancelToken?.Dispose();
        Debug.Log("MatchplayMatchmaker: Disposed and cancelled any ongoing matchmaking.");
    }
}