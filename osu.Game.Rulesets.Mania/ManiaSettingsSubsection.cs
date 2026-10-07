// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Rulesets.Mania.Configuration;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Screens.Edit;
using osuTK;

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

            // Colour pickers to customise the timing-based note colouring per beat divisor.
            foreach (int divisor in ManiaTimingColourDivisors.All)
            {
                var colour = BindableBeatDivisor.GetColourFor(divisor, colours);
                var current = config.GetBindable<string>(ManiaTimingColourDivisors.SettingFor(divisor));

                Add(new SettingsItemV2(new TimingColourPicker(colour)
                {
                    Caption = RulesetSettingsStrings.TimingBasedColourOverride(divisor),
                    Current = current,
                })
                {
                    Keywords = new[] { "color", "colour" },
                });
            }

            // Colour used for any divisor without its own override.
            var otherCurrent = config.GetBindable<string>(ManiaRulesetSetting.TimingBasedColourOverrideOther);

            Add(new SettingsItemV2(new TimingColourPicker(new Colour4(1f, 0f, 0f, 1f))
            {
                Caption = RulesetSettingsStrings.TimingBasedColourOther,
                Current = otherCurrent,
            })
            {
                Keywords = new[] { "color", "colour" },
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

        /// <summary>
        /// A form control which opens a colour picker. The value is stored as a hex string (compatible with
        /// the underlying ruleset configuration), with a fallback colour used while the string is empty or invalid.
        /// </summary>
        private partial class TimingColourPicker : OsuClickableContainer, IHasCurrentValue<string>, IFormControl, IHasPopover
        {
            public Bindable<string> Current
            {
                get => current.Current;
                set => current.Current = value;
            }

            private readonly BindableWithCurrent<string> current = new BindableWithCurrent<string>();
            private readonly Bindable<Colour4> pickerCurrent = new Bindable<Colour4>(Colour4.White);

            private bool updatingFromCurrent;

            /// <summary>
            /// A frame-delayed mirror of the current popover's visibility. This lets <see cref="OnMouseDown" />
            /// know whether a popover was visible just before the current mousedown, as the hosting
            /// <see cref="PopoverContainer" /> dismisses popovers on mouse down (before this control's click fires).
            /// </summary>
            private bool popoverVisible;

            /// <summary>
            /// True when the next click is for hiding the popover (i.e. the popover was visible at mousedown
            /// and has been dismissed by the hosting <see cref="PopoverContainer" /> already).
            /// </summary>
            private bool hidingFromClick;

            /// <summary>
            /// Caption describing this control, displayed above the control.
            /// </summary>
            public LocalisableString Caption { get; init; }

            private readonly Colour4 defaultColour;

            private FormControlBackground background = null!;
            private FormFieldCaption caption = null!;
            private Circle swatch = null!;
            private OsuSpriteText hexCodeText = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public TimingColourPicker(Colour4 defaultColour)
            {
                this.defaultColour = defaultColour;
                Action = () =>
                {
                    // If the popover was visible at mousedown, the hosting PopoverContainer has already
                    // dismissed it on mouse down. Re-showing here would cause a close/re-open flicker.
                    if (hidingFromClick)
                        hidingFromClick = false;
                    else
                        this.ShowPopover();
                };
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (popoverVisible)
                    hidingFromClick = true;

                return base.OnMouseDown(e);
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                AddRangeInternal(new Drawable[]
                {
                    background = new FormControlBackground(),
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Padding = new MarginPadding(9),
                        Spacing = new Vector2(0, 4),
                        Children = new Drawable[]
                        {
                            caption = new FormFieldCaption
                            {
                                Anchor = Anchor.TopLeft,
                                Origin = Anchor.TopLeft,
                                Caption = Caption,
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 20,
                                Children = new Drawable[]
                                {
                                    swatch = new Circle
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Size = new Vector2(20),
                                    },
                                    hexCodeText = new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Margin = new MarginPadding { Left = 30 },
                                    },
                                },
                            },
                        },
                    },
                });
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                current.BindValueChanged(_ =>
                {
                    updateDisplay();
                    ValueChanged?.Invoke();
                }, true);

                pickerCurrent.BindValueChanged(e =>
                {
                    if (updatingFromCurrent || current.Disabled)
                        return;

                    current.Value = e.NewValue.ToHex();
                });

                // Ensure the popover doesn't linger when the settings panel itself is hidden.
                // (The hosting PopoverContainer's auto-hide cannot fire once the panel is faded out,
                // as its children stop updating and the row's own alpha never reaches zero.)
                this.FindClosestParent<SettingsPanel>()?.State.BindValueChanged(s =>
                {
                    if (s.NewValue == Visibility.Hidden)
                        this.HidePopover();
                });
            }

            private void updateDisplay()
            {
                updatingFromCurrent = true;

                Colour4 colour = Colour4.TryParseHex(current.Value, out Colour4 parsed) ? parsed : defaultColour;

                swatch.Colour = colour;
                hexCodeText.Text = colour.ToHex();
                pickerCurrent.Value = colour;

                updatingFromCurrent = false;

                updateState();
            }

            protected override bool OnHover(HoverEvent e)
            {
                base.OnHover(e);
                updateState();
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                base.OnHoverLost(e);
                updateState();
            }

            private void updateState()
            {
                caption.Colour = Current.Disabled ? colourProvider.Background1 : colourProvider.Content2;
                hexCodeText.Colour = Current.Disabled ? colourProvider.Foreground1 : colourProvider.Content1;

                if (Current.Disabled)
                    background.VisualStyle = VisualStyle.Disabled;
                else if (IsHovered)
                    background.VisualStyle = VisualStyle.Hovered;
                else
                    background.VisualStyle = VisualStyle.Normal;
            }

            public Popover GetPopover()
            {
                var popover = new OsuPopover(false)
                {
                    Child = new OsuColourPicker
                    {
                        Current = { BindTarget = pickerCurrent }
                    }
                };

                // Update the visibility mirror on the frame *after* any visibility change, so that
                // OnMouseDown observes the state from before the current mousedown's dismissal.
                popover.State.BindValueChanged(s => Schedule(() => popoverVisible = s.NewValue == Visibility.Visible), true);

                return popover;
            }

            public event Action? ValueChanged;

            public bool IsDefault => current.IsDefault;

            public void SetDefault() => current.SetDefault();

            public bool IsDisabled => current.Disabled;

            public IEnumerable<LocalisableString> FilterTerms => new[] { Caption };

            public float MainDrawHeight => DrawHeight;
        }
    }
}
