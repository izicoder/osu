// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mania.Configuration;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Mania
{
    public partial class ManiaSettingsSubsection : RulesetSettingsSubsection
    {
        public ManiaSettingsSubsection(ManiaRuleset ruleset)
            : base(ruleset)
        {
        }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            var config = (ManiaRulesetConfigManager)Config;

            Children = new Drawable[]
            {
                new SettingsItemV2(new FormEnumDropdown<ManiaScrollingDirection>
                {
                    Caption = RulesetSettingsStrings.ScrollingDirection,
                    Current = config.GetBindable<ManiaScrollingDirection>(ManiaRulesetSetting.ScrollDirection)
                }),
                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = RulesetSettingsStrings.ScrollSpeed,
                    Current = config.GetBindable<double>(ManiaRulesetSetting.ScrollSpeed),
                    KeyboardStep = 1,
                    LabelFormat = v => RulesetSettingsStrings.ScrollSpeedTooltip((int)DrawableManiaRuleset.ComputeScrollTime(v), v),
                }),
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = RulesetSettingsStrings.TimingBasedColouring,
                    Current = config.GetBindable<bool>(ManiaRulesetSetting.TimingBasedNoteColouring),
                })
                {
                    Keywords = new[] { "color" },
                },
            };

            // Input boxes to customise the timing-based note colouring per beat divisor.
            foreach (int divisor in ManiaTimingColourDivisors.All)
            {
                var colour = BindableBeatDivisor.GetColourFor(divisor, colours);
                var current = config.GetBindable<string>(ManiaTimingColourDivisors.SettingFor(divisor));

                var box = new ColourPreviewTextBox(colour)
                {
                    Caption = RulesetSettingsStrings.TimingBasedColourOverride(divisor),
                    PlaceholderText = new Colour4(colour.R, colour.G, colour.B, colour.A).ToHex(),
                    Current = current,
                };

                current.BindValueChanged(v => box.UpdateSwatch(v.NewValue), true);

                Add(new SettingsItemV2(box)
                {
                    Keywords = new[] { "color", "colour", "hex" },
                });
            }

            // Colour used for any divisor without its own override.
            var otherCurrent = config.GetBindable<string>(ManiaRulesetSetting.TimingBasedColourOverrideOther);

            var otherBox = new ColourPreviewTextBox(new Colour4(1f, 0f, 0f, 1f))
            {
                Caption = RulesetSettingsStrings.TimingBasedColourOther,
                PlaceholderText = @"#RRGGBB",
                Current = otherCurrent,
            };

            otherCurrent.BindValueChanged(v => otherBox.UpdateSwatch(v.NewValue), true);

            Add(new SettingsItemV2(otherBox)
            {
                Keywords = new[] { "color", "colour", "hex" },
            });

            Add(new SettingsItemV2(new FormCheckBox
            {
                Caption = RulesetSettingsStrings.TouchOverlay,
                Current = config.GetBindable<bool>(ManiaRulesetSetting.TouchOverlay)
            }));

            if (RuntimeInfo.IsMobile)
            {
                Add(new SettingsItemV2(new FormEnumDropdown<ManiaMobileLayout>
                {
                    Caption = RulesetSettingsStrings.MobileLayout,
                    Current = config.GetBindable<ManiaMobileLayout>(ManiaRulesetSetting.MobileLayout),
#pragma warning disable CS0618 // Type or member is obsolete
                    Items = Enum.GetValues<ManiaMobileLayout>().Where(l => l != ManiaMobileLayout.LandscapeWithOverlay),
#pragma warning restore CS0618 // Type or member is obsolete
                }));
            }
        }
        private partial class ColourPreviewTextBox : FormTextBox
        {
            private readonly Circle swatch;
            private readonly Colour4 defaultColour;

            public ColourPreviewTextBox(Colour4 defaultColour)
            {
                this.defaultColour = defaultColour;

                swatch = new Circle
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Size = new osuTK.Vector2(14),
                    Margin = new MarginPadding { Left = 5 },
                };
            }

            [BackgroundDependencyLoader]
            private void load() => CaptionContainer.Add(swatch);

            public void UpdateSwatch(string value) =>
                swatch.Colour = Colour4.TryParseHex(value, out Colour4 colour) ? colour : defaultColour;
        }
    }
}
