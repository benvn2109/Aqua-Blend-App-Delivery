using AquaBlend.Entities;

namespace AquaBlend.Data;

public static class SeedData
{
    public static void Initialize(AquaBlendDbContext context)
    {
        if (!context.WaterSources.Any())
        {
            context.WaterSources.AddRange(
                new WaterSource { Name = "Reservoir A", Type = "Surface"},
                new WaterSource { Name = "Bore Well 1", Type = "Groundwater"});

            context.SaveChanges();
        }

        var scenario = context.Scenarios.FirstOrDefault(s => s.ExternalId == "scenario_2026_07_17_001");

        if (scenario is null)
        {
            // Reuse an existing scenario row with a blank ExternalId if one exists
            // (e.g. from earlier Sprint 1 seeding), otherwise create a fresh one.
            scenario = context.Scenarios.FirstOrDefault(s => s.ExternalId == null);

            if (scenario is not null)
            {
                scenario.ExternalId = "scenario_2026_07_17_001";
            }
            else
            {
                scenario = new Scenario
                {
                    Name = "Drought Scenario",
                    Description = "Low rainfall projection",
                    ExternalId = "scenario_2026_07_17_001"
                };
                context.Scenarios.Add(scenario);
            }

            context.SaveChanges();
        }

        if (!context.OptimisationResults.Any(r => r.ScenarioId == scenario.Id))
        {
            var resultJsonPath = Path.Combine(
                AppContext.BaseDirectory, "Data", "SeedData", "sample_optimisation_result.json");

            context.OptimisationResults.Add(new OptimisationResult
            {
                ScenarioId = scenario.Id,
                Status = "OPTIMAL",
                SolvedAt = DateTime.Parse("2026-07-17T10:32:00Z").ToUniversalTime(),
                ReceivedAt = DateTime.UtcNow,
                ContractVersion = "1.0",
                TotalCost = 184150.00m,
                Currency = "AUD",
                ResultJson = File.ReadAllText(resultJsonPath)
            });

            context.SaveChanges();
        }
    }
}