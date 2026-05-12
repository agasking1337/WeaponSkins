using Microsoft.Extensions.DependencyInjection;

using WeaponSkins.Econ;
using WeaponSkins.Services;

namespace WeaponSkins.Injections;

public static class EconServiceInjection
{
    public static IServiceCollection AddEconService(this IServiceCollection services)
    {
        return services.AddSingleton<EconService>();
    }

    public static IServiceProvider UseEconService(this IServiceProvider provider)
    {
        var econ = provider.GetRequiredService<EconService>();
        var agentDataService = provider.GetRequiredService<AgentDataService>();
        foreach (var agent in econ.Agents.Values)
        {
            agentDataService.RegisterAgentModel(agent.ModelPath);
        }
        return provider;
    }
}