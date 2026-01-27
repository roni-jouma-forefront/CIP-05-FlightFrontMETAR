using Flightfront.Domain.Models;

namespace Flightfront.Application.Interfaces;

public interface IMetarService
{
    Task<MetarData?> GetMetarByIcaoAsync(string icaoCode);
    MetarData? ParseMetarString(string metarString);
}
