using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using Windows.Foundation;
using System.Reflection;
using Windows.ApplicationModel.VoiceCommands;
using System.Diagnostics;
using Windows.Security.Cryptography.Core;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;
using Windows.UI.Composition;
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AnimatedTextView
{
    public sealed class TextView : Control
    {
        private Grid _textContainer;
        private Grid _rootGrid;
        private string _lastText = string.Empty;
        private string _calculatedText = string.Empty;

        private List<ColumnDefinition> _columns = new();
        private List<TextBlock> _textBlocks = new();
        private List<ModifyAction> _actionsQueue = new();

        public TextView()
        {
            this.DefaultStyleKey = typeof(TextView);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextView),
            new PropertyMetadata(string.Empty, OnTextChanged));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextView control)
            {
                control.UpdateText((string)e.NewValue);
            }
        }

        public static readonly DependencyProperty TextBlockStyleProperty = 
            DependencyProperty.Register(nameof(Style), typeof(Style), typeof(TextView),
            new PropertyMetadata(null, OnTextBlockStyleChanged));

        private static void OnTextBlockStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // there's nothing here
        }

        public new Style Style
        {
            get => (Style)GetValue(TextBlockStyleProperty);
            set => SetValue(TextBlockStyleProperty, value);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textContainer = GetTemplateChild("TextContainer") as Grid;
            _rootGrid = GetTemplateChild("RootGrid") as Grid;
            UpdateText(Text);
        }

        private void UpdateText(string newText)
        {
            string oldText = _calculatedText;
            int m = oldText.Length;
            int n = newText.Length;

            // Build LCS DP matrix.
            int[,] dp = new int[m + 1, n + 1];
            for (int i = 0; i <= m; i++)
            {
                for (int j = 0; j <= n; j++)
                {
                    if (i == 0 || j == 0)
                        dp[i, j] = 0;
                    else if (oldText[i - 1] == newText[j - 1])
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                    else
                        dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }

            // Backtrack to determine the diff operations.
            // We'll build a list of ModifyAction objects.
            List<ModifyAction> actions = new List<ModifyAction>();
            int x = m, y = n;
            while (x > 0 || y > 0)
            {
                // If both characters match, no operation is needed.
                if (x > 0 && y > 0 && oldText[x - 1] == newText[y - 1])
                {
                    x--;
                    y--;
                }
                // If insertion is the better option (or deletion isn't available), schedule an insertion.
                else if (y > 0 && (x == 0 || dp[x, y - 1] >= dp[x - 1, y]))
                {
                    // Insert newText[y-1] at position x.
                    actions.Add(new ModifyAction
                    {
                        IsAdding = true,
                        Char = newText[y - 1],
                        Index = x
                    });
                    y--;
                }
                // Otherwise, schedule a deletion.
                else if (x > 0 && (y == 0 || dp[x, y - 1] < dp[x - 1, y]))
                {
                    actions.Add(new ModifyAction
                    {
                        IsAdding = false,
                        Char = oldText[x - 1],
                        Index = x - 1
                    });
                    x--;
                }
            }

            // Reverse the list to get left-to-right order.
            actions.Reverse();
            // Sort to get a left-to-right animation
            actions = actions
                .OrderBy(a => a.Index)
                .ThenBy(a => a.IsAdding ? 1 : 0) // deletions (false) come before additions (true)
                .ToList();

            // Process the actions with an offset to adjust indices.
            int offset = 0;
            foreach (var action in actions)
            {
                // Adjust the action index to the current state.
                int effectiveIndex = action.Index + offset;
                if (action.IsAdding)
                {
                    ScheduleForAddition(action.Char, effectiveIndex);
                    offset++; // insertion increases the string length.
                }
                else
                {
                    ScheduleForRemoval(effectiveIndex);
                    offset--; // removal decreases the string length.
                }
            }
            _lastText = newText;
            if (_textContainer != null)
            {
                startAnimation();
            }
        }


        private async void startAnimation()
        {
            while (_actionsQueue.Count > 0)
            {
                var action = _actionsQueue[0];
                if (action.IsAdding) InsertAt(action.Char, action.Index);
                else RemoveAt(action.Index);

                _actionsQueue.RemoveAt(0);
                await Task.Delay(5);
            }
            trimText();
        }

        private void trimText()
        {
            if (_textContainer.ActualWidth > _rootGrid.ActualWidth)
            {
                double totalColumnsWidth = 0;
                int firstOverlappingColumn = -1;
                for (int i = 0; i < _columns.Count; i++)
                {
                    totalColumnsWidth += _columns[i].ActualWidth;
                    if (totalColumnsWidth > _rootGrid.ActualWidth)
                    {
                        firstOverlappingColumn = i;
                        break;
                    }
                }
                if (firstOverlappingColumn > 2)
                {
                    Debug.WriteLine($"Found overlap at position {firstOverlappingColumn}");
                    RemoveAt(firstOverlappingColumn - 3);
                    InsertAt('\u2026', firstOverlappingColumn - 3); // insert the ellipsis character ("…")
                    while (_columns.Count > firstOverlappingColumn - 2)
                    {
                        RemoveAt(firstOverlappingColumn - 2);
                    }

                    StringBuilder sb = new StringBuilder(_calculatedText);
                    sb[firstOverlappingColumn - 1] = '\u2026';
                    _calculatedText = sb.ToString().Substring(0, _columns.Count);
                    _lastText = _calculatedText;
                    Debug.WriteLine($"Displayed string is now {_calculatedText}");
                }
            }
        }

        private void ScheduleForAddition(char letter, int index)
        {
            if (index < 0 || index > _calculatedText.Length)
            {
                return;
            }

            _calculatedText = _calculatedText.Insert(index, letter.ToString());

            _actionsQueue.Add(new ModifyAction()
            {
                IsAdding = true,
                Char = letter,
                Index = index
            });
        }

        private void ScheduleForRemoval(int index)
        {
            if (index < 0 || index >= _calculatedText.Length)
            {
                return;
            }

            char removedChar = _calculatedText[index];
            _calculatedText = _calculatedText.Remove(index, 1);

            _actionsQueue.Add(new ModifyAction()
            {
                IsAdding = false,
                Char = 'e',
                Index = index
            });
        }

        private void InsertAt(char letter, int index)
        {
            var insertIndex = InsertColumnAt(index);

            var charBlock = new TextBlock
            {
                Text = letter.ToString()
            };

            if (Style != null) charBlock.Style = Style;
            else charBlock.FontSize = 12;

            if (letter == ' ') charBlock.MinWidth = _textContainer.ActualHeight / 6;

            Grid.SetColumn(charBlock, insertIndex);
            _textBlocks.Insert(index, charBlock);
            _textContainer.Children.Add(charBlock);
            AnimateIn(charBlock);
        }

        private async void RemoveAt(int index)
        {
            var textBlock = _textBlocks[index];
            var column = _columns[index];

            _textBlocks.RemoveAt(index);
            _columns.RemoveAt(index);

            for (int i = index; i < _textBlocks.Count; i++)
            {
                Grid.SetColumn(_textBlocks[i], i);
            }

            await AnimateOut(textBlock);

            _textContainer.ColumnDefinitions.Remove(column);
            _textContainer.Children.Remove(textBlock);
        }

        private void AnimateIn(TextBlock text)
        {
            UISettings settings = new UISettings();
            if (settings.AnimationsEnabled)
            {
                var visual = ElementCompositionPreview.GetElementVisual(text);
                var compositor = visual.Compositor;
                var milliseconds = 500;

                // Set Transform Center to Middle
                visual.CenterPoint = new System.Numerics.Vector3(
                    (float)text.ActualWidth / 2,
                    (float)text.ActualHeight,
                    0
                );

                // Start values: Fully shrunk & transparent
                visual.Opacity = 0f;
                visual.Scale = new System.Numerics.Vector3(0, 0, 1);

                // Opacity Animation
                var fadeIn = compositor.CreateScalarKeyFrameAnimation();
                fadeIn.InsertKeyFrame(1f, 1f);
                fadeIn.Duration = TimeSpan.FromMilliseconds(milliseconds);

                // Scale Animation (expands to full size)
                var scaleUp = compositor.CreateVector3KeyFrameAnimation();
                scaleUp.InsertKeyFrame(1f, new System.Numerics.Vector3(1f, 1f, 1f));
                scaleUp.Duration = TimeSpan.FromMilliseconds(milliseconds);

                // Start the animations
                visual.StartAnimation("Opacity", fadeIn);
                visual.StartAnimation("Scale", scaleUp);
            }
            else
            {
                var visual = ElementCompositionPreview.GetElementVisual(text);
                visual.Opacity = 1f;
                visual.Scale = new System.Numerics.Vector3(1, 1, 1);
            }
        }


        private async Task AnimateOut(TextBlock text)
        {
            var visual = ElementCompositionPreview.GetElementVisual(text);
            var compositor = visual.Compositor;
            var milliseconds = 300;

            UISettings settings = new UISettings();
            if (settings.AnimationsEnabled)
            {
                // start values
                visual.Opacity = 1f;
                visual.Scale = new System.Numerics.Vector3(1f, 1f, 1f);

                visual.CenterPoint = new System.Numerics.Vector3(
                    (float)text.ActualWidth / 2,
                    (float)(text.ActualHeight * 0.75),
                    0
                );

                // opacity animation
                var fadeIn = compositor.CreateScalarKeyFrameAnimation();
                fadeIn.InsertKeyFrame(1f, 0f);
                fadeIn.Duration = TimeSpan.FromMilliseconds(milliseconds);

                // scale animation
                var scaleUp = compositor.CreateVector3KeyFrameAnimation();
                scaleUp.InsertKeyFrame(1f, new System.Numerics.Vector3(0f, 0f, 1f)); // Normal size
                scaleUp.Duration = TimeSpan.FromMilliseconds(milliseconds);

                visual.StartAnimation("Opacity", fadeIn);
                visual.StartAnimation("Scale", scaleUp);

                await Task.Delay(milliseconds);
            }
            else
            {
                visual.Opacity = 0f;
                visual.Scale = new System.Numerics.Vector3(0f, 0f, 1f);
            }
            return;
        }

        private int InsertColumnAt(int index)
        {
            var column = new ColumnDefinition { Width = GridLength.Auto };
            _columns.Insert(index, column);
            _textContainer.ColumnDefinitions.Insert(index, column);

            // Shift columns only for text blocks at or after 'index'
            for (int i = index; i < _textBlocks.Count; i++)
            {
                var oldColumn = Grid.GetColumn(_textBlocks[i]);
                Grid.SetColumn(_textBlocks[i], oldColumn + 1);
            }

            return index;
        }
    }

    class ModifyAction
    {
        public bool IsAdding { get; set; }
        public char Char { get; set; }
        public int Index { get; set; }
        public new string ToString()
        {
            return $"IsAdding: {IsAdding} the character {Char} at {Index}";
        }
    }
}
