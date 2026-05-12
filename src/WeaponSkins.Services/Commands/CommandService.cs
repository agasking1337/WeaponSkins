using Microsoft.Extensions.Logging;

using System.Linq;

using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Players;
using WeaponSkins.Database;
using WeaponSkins.Extensions;
using WeaponSkins.Services;

namespace WeaponSkins;

public partial class CommandService
{

    private ISwiftlyCore Core { get; init; }
    private ILogger Logger { get; init; }
    private MenuService MenuService { get; init; }
    private IInventoryUpdateService InventoryUpdateService { get; init; }
    private WeaponSkinGetterAPI WeaponSkinGetterAPI { get; init; }
    private DataService DataService { get; init; }
    private InventoryService InventoryService { get; init; }
    private DatabaseService DatabaseService { get; init; }
    private WeaponSkinAPI WeaponSkinAPI { get; init; }

    public CommandService(ISwiftlyCore core,
        ILogger<CommandService> logger,
        MenuService menuService,
        IInventoryUpdateService inventoryUpdateService,
        WeaponSkinGetterAPI weaponSkinGetterAPI,
        DataService dataService,
        InventoryService inventoryService,
        DatabaseService databaseService,
        WeaponSkinAPI weaponSkinAPI)
    {
        Core = core;
        Logger = logger;
        MenuService = menuService;
        InventoryUpdateService = inventoryUpdateService;
        WeaponSkinGetterAPI = weaponSkinGetterAPI;
        DataService = dataService;
        InventoryService = inventoryService;
        DatabaseService = databaseService;
        WeaponSkinAPI = weaponSkinAPI;

        RegisterCommands();
    }
    public void RegisterCommands()
    {
        Core.Command.RegisterCommand("ws", CommandSkin);
        Core.Command.RegisterCommand("wp", CommandRefresh);
        Core.Command.RegisterCommand("unequip", CommandUnequip);
    }

    private void CommandSkin(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        MenuService.OpenMainMenu(context.Sender!);
    }

    private void CommandRefresh(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        var steamId = player.SteamID;


        Task.Run(async () =>
        {
            var dbSkins = await DatabaseService.GetSkinsAsync(steamId);
            var dbKnives = await DatabaseService.GetKnifesAsync(steamId);
            var dbGloves = await DatabaseService.GetGlovesAsync(steamId);

            var dbMusicKits = await DatabaseService.GetMusicKitsAsync(steamId);
            foreach (var mk in dbMusicKits)
                DataService.MusicKitDataService.SetMusicKit(mk.SteamID, mk.MusicKitIndex);

            var dbAgents = await DatabaseService.GetAgentsAsync(steamId);
            foreach (var agent in dbAgents)
                DataService.AgentDataService.SetAgent(agent.SteamID, agent.Team, agent.AgentIndex);

            var dbSkinsList = dbSkins.ToList();
            var dbKnivesList = dbKnives.ToList();
            var dbGlovesList = dbGloves.ToList();

            Core.Scheduler.NextWorldUpdate(() =>
            {
                try
                {
                    if (dbSkinsList.Count > 0)
                        InventoryUpdateService.UpdateWeaponSkins(dbSkinsList);

                    if (dbKnivesList.Count > 0)
                        InventoryUpdateService.UpdateKnifeSkins(dbKnivesList);

                    if (dbGlovesList.Count > 0)
                        InventoryUpdateService.UpdateGloveSkins(dbGlovesList);

                    if (DataService.MusicKitDataService.TryGetMusicKit(steamId, out var musicKitIndex))
                        InventoryService.UpdateMusicKit(steamId, musicKitIndex);
                }
                catch
                {
                    // ignore
                }
            });
        });

        context.Reply("Refreshing skins from database...");
    }

    private void CommandUnequip(ICommandContext context)
    {
        if (!context.IsSentByPlayer)
        {
            context.Reply("This command can only be used by players.");
            return;
        }

        var player = context.Sender!;
        var steamId = player.SteamID;

        if (WeaponSkinGetterAPI.TryGetWeaponSkins(steamId, out var weaponSkins))
        {
            foreach (var skin in weaponSkins)
            {
                WeaponSkinAPI.ResetWeaponSkin(steamId, skin.Team, skin.DefinitionIndex, true);
            }
        }

        foreach (var team in new[] { Team.T, Team.CT })
        {
            WeaponSkinAPI.ResetKnifeSkin(steamId, team, true);
            WeaponSkinAPI.ResetGloveSkin(steamId, team, true);
            WeaponSkinAPI.ResetAgentSkin(steamId, team, true);
        }

        WeaponSkinAPI.ResetMusicKit(steamId, true);

        context.Reply("All equipped items have been unequipped.");
    }
}