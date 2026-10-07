// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Configuration.Tracking;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Mania.UI;

namespace osu.Game.Rulesets.Mania.Configuration
{
    public class ManiaRulesetConfigManager : RulesetConfigManager<ManiaRulesetSetting>
    {
        public ManiaRulesetConfigManager(SettingsStore? settings, RulesetInfo ruleset, int? variant = null)
            : base(settings, ruleset, variant)
        {
            Migrate();
        }

        protected override void InitialiseDefaults()
        {
            base.InitialiseDefaults();

            SetDefault(ManiaRulesetSetting.ScrollSpeed, 8.0, 1.0, 40.0, 0.1);
            SetDefault(ManiaRulesetSetting.ScrollDirection, ManiaScrollingDirection.Down);
            SetDefault(ManiaRulesetSetting.TimingBasedNoteColouring, false);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride1, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride2, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride3, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride4, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride5, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride6, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride7, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride8, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride9, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride12, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverride16, string.Empty);
            SetDefault(ManiaRulesetSetting.TimingBasedColourOverrideOther, string.Empty);
            SetDefault(ManiaRulesetSetting.MobileLayout, ManiaMobileLayout.Portrait);
            SetDefault(ManiaRulesetSetting.TouchOverlay, false);
        }

        public void Migrate()
        {
            var mobileLayout = GetBindable<ManiaMobileLayout>(ManiaRulesetSetting.MobileLayout);

#pragma warning disable CS0618 // Type or member is obsolete
            if (mobileLayout.Value == ManiaMobileLayout.LandscapeWithOverlay)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                mobileLayout.Value = ManiaMobileLayout.Landscape;
                SetValue(ManiaRulesetSetting.TouchOverlay, true);
            }
        }

        public override TrackedSettings CreateTrackedSettings() => new TrackedSettings
        {
            new TrackedSetting<double>(ManiaRulesetSetting.ScrollSpeed,
                speed => new SettingDescription(
                    rawValue: speed,
                    name: RulesetSettingsStrings.ScrollSpeed,
                    value: RulesetSettingsStrings.ScrollSpeedTooltip((int)DrawableManiaRuleset.ComputeScrollTime(speed), speed)
                )
            )
        };
    }

    /// <summary>
    /// Helpers to map beat divisors to their timing-based note colour override settings.
    /// </summary>
    public static class ManiaTimingColourDivisors
    {
        public static readonly int[] All = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 };

        public static ManiaRulesetSetting SettingFor(int divisor) => divisor switch
        {
            1 => ManiaRulesetSetting.TimingBasedColourOverride1,
            2 => ManiaRulesetSetting.TimingBasedColourOverride2,
            3 => ManiaRulesetSetting.TimingBasedColourOverride3,
            4 => ManiaRulesetSetting.TimingBasedColourOverride4,
            5 => ManiaRulesetSetting.TimingBasedColourOverride5,
            6 => ManiaRulesetSetting.TimingBasedColourOverride6,
            7 => ManiaRulesetSetting.TimingBasedColourOverride7,
            8 => ManiaRulesetSetting.TimingBasedColourOverride8,
            9 => ManiaRulesetSetting.TimingBasedColourOverride9,
            12 => ManiaRulesetSetting.TimingBasedColourOverride12,
            16 => ManiaRulesetSetting.TimingBasedColourOverride16,
            _ => throw new ArgumentOutOfRangeException(nameof(divisor), divisor, "Not a valid colour divisor."),
        };
    }

    public enum ManiaRulesetSetting
    {
        ScrollSpeed,
        ScrollDirection,
        TimingBasedNoteColouring,
        TimingBasedColourOverride1,
        TimingBasedColourOverride2,
        TimingBasedColourOverride3,
        TimingBasedColourOverride4,
        TimingBasedColourOverride5,
        TimingBasedColourOverride6,
        TimingBasedColourOverride7,
        TimingBasedColourOverride8,
        TimingBasedColourOverride9,
        TimingBasedColourOverride12,
        TimingBasedColourOverride16,
        TimingBasedColourOverrideOther,
        MobileLayout,
        TouchOverlay,
    }
}
