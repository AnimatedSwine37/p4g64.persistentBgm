using System.ComponentModel;
using p4g64.persistentBgm.Template.Configuration;

namespace p4g64.persistentBgm.Configuration;

public class Config : Configurable<Config>
{
    [DisplayName("Always Play Boss Battle BGM")]
    [Description(
        "If enabled boss battle BGM will always play, regardless of the settings. Otherwise dungeon BGM can persist into boss battles.")]
    [DefaultValue(true)]
    public bool AlwaysPlayBossBgm { get; set; } = true;


    [DisplayName("Castle Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float CastleNormalChance { get; set; } = 1;

    [DisplayName("Bathhouse Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float BathhouseNormalChance { get; set; } = 1;

    [DisplayName("Striptease Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float StripteaseNormalChance { get; set; } = 1;

    [DisplayName("Void Quest Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float VoidQuestNormalChance { get; set; } = 1;

    [DisplayName("Secret Lab Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float LabNormalChance { get; set; } = 1;

    [DisplayName("Heaven Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float HeavenNormalChance { get; set; } = 1;

    [DisplayName("Magatsu Inaba Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float MagatsuInabaNormalChance { get; set; } = 1;

    [DisplayName("Hollow Forest Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float HollowForestNormalChance { get; set; } = 1;

    [DisplayName("Yomotsu Hirasaka Normal BGM Chance")]
    [Description(
        "The chance that the normal battle bgm is played from 0-1 with 0 meaning the dungeon bgm always persists and 1 meaning the dungeon bgm never persists (like vanilla).")]
    [DefaultValue(1f)]
    public float YomotsuHirasakaNormalChance { get; set; } = 1;

    [DisplayName("Debug Mode")]
    [Description("Logs additional information to the console that is useful for debugging.")]
    [DefaultValue(false)]
    public bool DebugEnabled { get; set; } = false;
}

/// <summary>
/// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
/// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
/// </summary>
public class ConfiguratorMixin : ConfiguratorMixinBase
{
    // 
}