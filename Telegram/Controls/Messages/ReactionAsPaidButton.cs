﻿//
// Copyright Fela Ameghino 2015-2024
//
// Distributed under the GNU General Public License v3.0. (See accompanying
// file LICENSE or copy at https://www.gnu.org/licenses/gpl-3.0.txt)
//
using System;
using Telegram.Common;
using Telegram.Navigation;
using Telegram.Td;
using Telegram.Td.Api;
using Telegram.ViewModels;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Telegram.Controls.Messages
{
    public class ReactionAsPaidButton : ReactionButton
    {
        public ReactionAsPaidButton()
        {
            DefaultStyleKey = typeof(ReactionAsPaidButton);
        }

        private int _pendingCount;
        private int _pendingTime;
        private ToastPopup _pendingToast;
        private DispatcherTimer _pendingTimer;

        protected override void OnClick(MessageViewModel message, MessageReaction chosen)
        {
            Animate();
            message.ClientService.Send(new AddPendingPaidMessageReaction(message.ChatId, message.Id, 1, true, true));

            ShowOrUpdateToast(message);
        }

        private void ShowOrUpdateToast(MessageViewModel message)
        {
            _pendingCount++;

            var title = message.ClientService.Options.IsPaidReactionAnonymous
                ? Strings.StarsSentAnonymouslyTitle
                : Strings.StarsSentTitle;

            var text = string.Format("**{0}**\n{1}", title, Locale.Declension(Strings.R.StarsSentText, _pendingCount));
            var formatted = ClientEx.ParseMarkdown(text);

            if (_pendingToast?.Content is Grid)
            {
                var content = _pendingToast.Content as Grid;
                var textBlock = content.Children[0] as TextBlock;

                TextBlockHelper.SetFormattedText(textBlock, formatted);

                var animated = content.Children[2] as Grid;
                var slice = animated.Children[0] as SelfDestructTimer;
                var value = animated.Children[1] as AnimatedTextBlock;

                _pendingTime = 5;
                _pendingTimer.Stop();
                _pendingTimer.Start();

                slice.Maximum = _pendingTime;
                slice.Value = DateTime.Now.AddSeconds(_pendingTime);

                value.Text = _pendingTime.ToString();
            }
            else
            {
                var toast = ToastPopup.Show(XamlRoot, formatted, ToastPopupIcon.StarsSent, dismissAfter: TimeSpan.Zero);
                var content = toast.Content as Grid;

                toast.MaxWidth = 500;
                toast.MinWidth = 336;

                var undo = new Button()
                {
                    Content = Strings.StarsSentUndo,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Style = BootStrapper.Current.Resources["AccentTextButtonStyle"] as Style,
                    Margin = new Thickness(8, -4, -4, -4),
                    Padding = new Thickness(4, 5, 4, 6)
                };

                var animated = new Grid
                {
                    Height = 32,
                    Margin = new Thickness(8, -12, -4, -12)
                };

                animated.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                animated.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32, GridUnitType.Pixel) });

                var slice = new SelfDestructTimer
                {
                    Background = new SolidColorBrush(Colors.Transparent),
                    Foreground = undo.Foreground,
                    //Width = 24,
                    //Height = 24,
                    //Center = 12,
                    //Radius = 10.5
                    Width = 22,
                    Height = 22,
                    Center = 11,
                    Radius = 9.5
                };

                _pendingTime = 5;

                slice.Maximum = _pendingTime;
                slice.Value = DateTime.Now.AddSeconds(_pendingTime);

                var value = new AnimatedTextBlock
                {
                    Foreground = undo.Foreground,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 1, 0, 1),
                    TextStyle = BootStrapper.Current.Resources["CaptionTextBlockStyle"] as Style,
                    Text = _pendingTime.ToString()
                };

                _pendingTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(1),
                };

                void tick(object sender, object e)
                {
                    _pendingTime--;

                    value.Text = _pendingTime.ToString();

                    if (_pendingTime == 0)
                    {
                        Logger.Info("expired");

                        message.ClientService.Send(new CommitPendingPaidMessageReactions(message.ChatId, message.Id));
                        undo.Click -= handler;

                        _pendingTimer.Tick -= tick;
                        _pendingTimer.Stop();
                        _pendingTimer = null;

                        _pendingToast.IsOpen = false;
                        _pendingToast = null;

                        _pendingCount = 0;
                    }
                }

                void handler(object sender, RoutedEventArgs e)
                {
                    Logger.Info("closed");

                    message.ClientService.Send(new RemovePendingPaidMessageReactions(message.ChatId, message.Id));
                    undo.Click -= handler;

                    _pendingTimer.Tick -= tick;
                    _pendingTimer.Stop();
                    _pendingTimer = null;

                    _pendingToast.IsOpen = false;
                    _pendingToast = null;

                    _pendingCount = 0;
                }

                undo.Click += handler;

                _pendingToast = toast;

                _pendingTimer.Tick += tick;
                _pendingTimer.Start();

                Grid.SetColumn(slice, 1);
                Grid.SetColumn(value, 1);

                animated.Children.Add(slice);
                animated.Children.Add(value);
                animated.Children.Add(undo);

                Grid.SetColumn(animated, 2);
                content.Children.Add(animated);
            }
        }
    }
}