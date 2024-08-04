using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Logging;
using CsvHelper;

public class RateChartService : IRateChartService
{
    private readonly RateChartDbContext _context;
    private readonly ILogger<RateChartService> _logger;

    public RateChartService(RateChartDbContext context, ILogger<RateChartService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public void ImportRateChart(Stream csvStream, int clientId, string? password = null)
    {
        try
        {
            _logger.LogInformation("Starting ImportRateChart method.");
            var rateCharts = ReadCsvData(csvStream, clientId);

            foreach (var rateChart in rateCharts)
            {
                _context.RateCharts.Add(rateChart);
            }

            _context.SaveChanges();
            _logger.LogInformation("Data successfully saved to the database.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error importing rate chart: {ex.Message}");
            throw;
        }
    }

    public List<RateChart> ReadCsvData(Stream csvStream, int clientId)
    {
        List<RateChart> rateCharts = new List<RateChart>();

        try
        {
            _logger.LogInformation("Starting ReadCsvData method.");
            using (var reader = new StreamReader(csvStream))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                // Iterate through each record
                while (csv.Read())
                {
                    var record = csv.GetRecord<dynamic>() as IDictionary<string, object>;

                    if (record != null)
                    {
                        // Iterate through each field in the record
                        foreach (var kvp in record)
                        {
                            var fieldName = kvp.Key;
                            var fieldValue = kvp.Value;

                            if (fieldName.Equals("SNF/FAT", StringComparison.OrdinalIgnoreCase))
                            {
                                // Skip the first field which is the 'SNF/FAT' identifier
                                continue;
                            }

                            if (decimal.TryParse(fieldValue?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal rate))
                            {
                                if (decimal.TryParse(fieldName, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal fat))
                                {
                                    rateCharts.Add(new RateChart
                                    {
                                        ClientId = clientId,
                                        Fat = fat,
                                        Snf = 5.5m, // Assuming a default SNF value of 5.5 as per previous examples
                                        Rate = rate
                                    });

                                    _logger.LogInformation($"Added rate chart: ClientId={clientId}, Fat={fat}, Snf=5.5, Rate={rate}");
                                }
                            }
                        }
                    }
                }
            }

            _logger.LogInformation("Finished reading CSV data.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error reading CSV data: {ex.Message}");
            throw;
        }

        return rateCharts;
    }


    public decimal? GetRate(int clientId, decimal fat, decimal snf)
    {
        _logger.LogInformation($"Fetching rate for clientId: {clientId}, fat: {fat}, snf: {snf}");

        var rateChart = _context.RateCharts
            .FirstOrDefault(rc => rc.ClientId == clientId && rc.Fat == fat && rc.Snf == snf);

        if (rateChart == null)
        {
            _logger.LogWarning("Rate chart record not found.");
        }
        else
        {
            _logger.LogInformation($"Rate found: {rateChart.Rate}");
        }

        return rateChart?.Rate;
    }
}
