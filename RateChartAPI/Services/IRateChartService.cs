using System.IO;

public interface IRateChartService
{
    void ImportRateChart(Stream csvStream, int clientId, string? password = null);
    decimal? GetRate(int clientId, decimal fat, decimal snf);
}
