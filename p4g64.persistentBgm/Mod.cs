using System.Diagnostics;
using Reloaded.Mod.Interfaces;
using p4g64.persistentBgm.Template;
using p4g64.persistentBgm.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using p4g64.persistentBgm.Model;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using static p4g64.persistentBgm.Utils;
using IReloadedHooks = Reloaded.Hooks.ReloadedII.Interfaces.IReloadedHooks;

namespace p4g64.persistentBgm;

/// <summary>
/// Your mod logic goes here.
/// </summary>
public unsafe class Mod : ModBase // <= Do not Remove.
{
    /// <summary>
    /// Provides access to the mod loader API.
    /// </summary>
    private readonly IModLoader _modLoader;

    /// <summary>
    /// Provides access to the Reloaded.Hooks API.
    /// </summary>
    /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
    private readonly IReloadedHooks? _hooks;

    /// <summary>
    /// Provides access to the Reloaded logger.
    /// </summary>
    private readonly ILogger _logger;

    /// <summary>
    /// Entry point into the mod, instance that created this class.
    /// </summary>
    private readonly IMod _owner;

    /// <summary>
    /// Provides access to this mod's configuration.
    /// </summary>
    private Config _configuration;

    /// <summary>
    /// The configuration of the currently executing mod.
    /// </summary>
    private readonly IModConfig _modConfig;

    private bool* _shouldPersistBgm;
    
    // The id of the current encounter. Only set when using the CALL_BATTLE flowscript function currently (used by boss battles which is all we actually care about)
    private int _encounterId;

    private List<IAsmHook> _asmHooks = new();
    private IHook<Action> _enteringBtlStopBgmHook;
    private Dungeon.AutomaticDungeonTask** _automaticDungeonTask;
    private IHook<FlowFunctionDelegate> _callBattleHook;
    
    private GetFlowInputDelegate _getFlowInput;

    public Mod(ModContext context)
    {
        _modLoader = context.ModLoader;
        _hooks = context.Hooks;
        _logger = context.Logger;
        _owner = context.Owner;
        _configuration = context.Configuration;
        _modConfig = context.ModConfig;

        if (!Utils.Initialise(_logger, _configuration, _modLoader))
        {
            return;
        }

        if (_hooks == null)
        {
            LogError("Failed to get Reloaded Hooks, nothing will work!");
            return;
        }

        _shouldPersistBgm = (bool*)Marshal.AllocHGlobal(sizeof(bool));
        *_shouldPersistBgm = false;

        SigScan("74 ?? 39 3D ?? ?? ?? ?? 74 ??", "AfterBattleStart", address =>
        {
            string[] function =
            {
                "use64",
                // If we should persist bgm go straight to the end. This comparison will cause the jz to always be true so it is skipped
                $"cmp byte [qword {(nuint)_shouldPersistBgm}], 1",
                "je endHook",
                // We don't want to force persisting bgm so run the normal test again so we get the normal result
                "test AL, 0x40",
                "label endHook"
            };

            _asmHooks.Add(_hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate());
        });


        SigScan("33 C9 E8 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 83 60 ?? FE", "AfterResultsStopBgm", address =>
        {
            string[] function =
            {
                "use64",
                // If we should persist bgm skip over the normal call so the bgm isn't stopped
                $"cmp byte [qword {(nuint)_shouldPersistBgm}], 1",
                "jne endHook",
                _hooks.Utilities.GetAbsoluteJumpMnemonics(address + 7, true),
                "label endHook"
            };

            _asmHooks.Add(_hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate());
        });

        SigScan("74 ?? 33 D2 33 C9 E8 ?? ?? ?? ?? 4C 8B 05 ?? ?? ?? ??", "EnteringBtlStopBgmSkip", address =>
        {
            string[] function =
            {
                "use64",
                // If we should persist bgm go straight to the end. This comparison will cause the jz to always be true so it is skipped
                $"cmp byte [qword {(nuint)_shouldPersistBgm}], 1",
                "je endHook",
                // We don't want to force persisting bgm so run the normal test again so we get the normal result
                "test byte [R8 + 0xc], 0x40",
                "label endHook"
            };

            _asmHooks.Add(_hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate());
        });

        SigScan("44 0F 29 B4 24 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 15 ?? ?? ?? ??", "PlayBattleBgm", address =>
        {
            string[] function =
            {
                "use64",
                // If we should persist bgm skip over the normal call so the bgm isn't stopped
                $"cmp byte [qword {(nuint)_shouldPersistBgm}], 1",
                "jne endHook",
                _hooks.Utilities.GetAbsoluteJumpMnemonics(address + 14, true),
                "label endHook"
            };

            _asmHooks.Add(_hooks.CreateAsmHook(function, address, AsmHookBehaviour.ExecuteFirst).Activate());
        });

        SigScan("40 53 48 83 EC 20 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 45 33 C0 41 8D 48 ??", "EnteringBtlStopBgm",
            address => { _enteringBtlStopBgmHook = _hooks.CreateHook(EnteringBtlStopBgmHook, address).Activate(); });

        SigScan("48 8B 05 ?? ?? ?? ?? 40 88 33", "AutomaticDungeonTaskPtr", address =>
        {
            _automaticDungeonTask = (Dungeon.AutomaticDungeonTask**)GetGlobalAddress(address + 3);
            LogDebug($"Found AutomaticDungeonTask at 0x{(nuint)_automaticDungeonTask:X}");
        });

        SigScan("41 56 41 57 48 83 EC 48 48 8D 0D ?? ?? ?? ??", "Flowscript::CALL_BATTLE",
            address =>
            {
                _callBattleHook = _hooks.CreateHook<FlowFunctionDelegate>(CallBattleHook, address).Activate();
            });
        
        SigScan("40 53 48 83 EC 20 8B D9 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 15 ?? ?? ?? ?? 8B 42 ??", "Flowscript::GetFlowInput",
            address =>
            {
                _getFlowInput = _hooks.CreateWrapper<GetFlowInputDelegate>(address, out _);
            });
    }

    // Set whether we persist bgm or not before entering each battle
    private void EnteringBtlStopBgmHook()
    {
        *_shouldPersistBgm = !ShouldSwitchBgm();
        _encounterId = 0; // Reset so we don't get stuck thinking we're in a boss battle as this only changes when calling a battle through the CALL_BATTLE flow function
        LogDebug($"{(*_shouldPersistBgm ? "Persisting" : "Not persisting")} dungeon BGM into battle.");
        
        _enteringBtlStopBgmHook.OriginalFunction();
    }

    // Hook for the CALL_BATTLE function which just grabs the encounter id from it and stores for later so we can see if we're entering a boss battle
    private nuint CallBattleHook()
    {
        _encounterId = _getFlowInput(0);
        return _callBattleHook.OriginalFunction();
    }

    private readonly List<Dungeon> _dungeons = new()
    {
        new Dungeon("Castle", 5, 14),
        new Dungeon("Bathhouse", 20, 32),
        new Dungeon("Striptease", 40, 52),
        new Dungeon("VoidQuest", 60, 71),
        new Dungeon("Lab", 80, 90),
        new Dungeon("Heaven", 100, 111),
        new Dungeon("MagatsuInaba", 120, 130),
        new Dungeon("YomotsuHirasaka", 140, 149),
        new Dungeon("HollowForest", 160, 170)
    };

    /// <summary>
    /// Checks if we should switch the bgm
    /// </summary>
    /// <returns>True if we should switch it, false if the current bgm should persist</returns>
    private bool ShouldSwitchBgm()
    {
        // We're not in a dungeon so just let the music through
        if (*_automaticDungeonTask == null)
        {
            return true;
        }

        int floorId = (*_automaticDungeonTask)->Info->CurrentFloor;
        Dungeon? dungeon = _dungeons.FirstOrDefault(x => floorId > x.StartFloor && floorId <= x.EndFloor);

        // This shouldn't happen, if it does then idk just give up
        if (dungeon == null)
        {
            LogError($"Failed to get dungeon for floor {floorId}. This shouldn't happen!");
            return true;
        }

        LogDebug($"The floor id is {floorId} in the dungeon {dungeon.Name}");

        if (_configuration.AlwaysPlayBossBgm && IsBossBattle())
        {
            return true;
        }

        Random random = new Random();
        double randomNum = random.NextDouble();
        float normalChance =
            (float)_configuration.GetType().GetProperty($"{dungeon.Name}NormalChance")!.GetValue(_configuration)!;
        bool switchBgm;
        // We should let the bgm persist
        if (randomNum > normalChance)
            switchBgm = false;
        // Let the bgm change
        else
            switchBgm = true;
        
        return switchBgm;
    }


    /// <summary>
    /// Checks if the current encounter is a boss battle
    /// </summary>
    /// <returns>True if it is a boss battle, false otherwise</returns>
    private bool IsBossBattle()
    {
        LogDebug($"The current encounter is id {_encounterId}");
        return _encounterId is >= 512 and <= 535 or >= 801 and <= 820 or 938 or 939;
    }

    private delegate nuint FlowFunctionDelegate();

    private delegate int GetFlowInputDelegate(int argNum);
    

    #region Standard Overrides

    public override void ConfigurationUpdated(Config configuration)
    {
        // Apply settings from configuration.
        // ... your code here.
        _configuration = configuration;
        _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
    }

    #endregion

    #region For Exports, Serialization etc.

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public Mod()
    {
    }
#pragma warning restore CS8618

    #endregion
}