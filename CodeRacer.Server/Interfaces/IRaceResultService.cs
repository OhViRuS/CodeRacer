using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CodeRacer.Server.Models;

namespace CodeRacer.Server.Interfaces;

public interface IRaceResultService
{
    Task<RaceResultResponse> SaveResultAsync(CreateRaceResultRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceResultResponse>> GetHistoryAsync(Guid? userId = null, int limit = 20, CancellationToken cancellationToken = default);

    Task<UserStatsResponse?> GetUserStatsAsync(Guid userId, CancellationToken cancellationToken = default);
}
