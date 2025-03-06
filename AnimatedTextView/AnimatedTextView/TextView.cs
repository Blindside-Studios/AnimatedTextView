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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AnimatedTextView
{
    public sealed class TextView : Control
    {
        private Grid _textContainer;

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

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _textContainer = GetTemplateChild("TextContainer") as Grid;
            UpdateText(Text);
        }

        private async void UpdateText(string newText)
        {
            if (_textContainer == null) return;

            //foreach (TextBlock text in _textContainer.Children.OfType<TextBlock>()) AnimateOut(text);

            _textContainer.Children.Clear();
            _textContainer.ColumnDefinitions.Clear();

            for (int i = 0; i < newText.Length; i++)
            {
                // Add a column for each character
                _textContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                InsertAt(newText[i], i);

                await Task.Delay(3);
            }
        }

        private async void InsertAt(char letter, int index)
        {
            var charBlock = new TextBlock
            {
                Text = letter.ToString(),
                FontSize = 30,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(charBlock, index);
            _textContainer.Children.Add(charBlock);
            await AnimateIn(charBlock);
        }

        private void ReplaceAt(char letter, int index)
        {

        }

        private async void RemoveAt(int index)
        {
            var textBlock = _textContainer.Children
                                .OfType<TextBlock>()
                                .FirstOrDefault(tb => Grid.GetColumn(tb) == index);

            if (textBlock != null)
            {
                await AnimateOut(textBlock);
                _textContainer.Children.Remove(textBlock);
            }
        }

        private async Task AnimateIn(TextBlock text)
        {
            var visual = ElementCompositionPreview.GetElementVisual(text);
            var compositor = visual.Compositor;
            var milliseconds = 300;

            // start values
            visual.Opacity = 0f;
            visual.Scale = new System.Numerics.Vector3(0.5f, 0.5f, 1f);
            visual.Offset = new System.Numerics.Vector3(0, -5, 0);

            // opacity animation
            var fadeIn = compositor.CreateScalarKeyFrameAnimation();
            fadeIn.InsertKeyFrame(1f, 1f);
            fadeIn.Duration = TimeSpan.FromMilliseconds(milliseconds);

            // scale animation
            var scaleUp = compositor.CreateVector3KeyFrameAnimation();
            scaleUp.InsertKeyFrame(1f, new System.Numerics.Vector3(1f, 1f, 1f)); // Normal size
            scaleUp.Duration = TimeSpan.FromMilliseconds(milliseconds);

            // translation animation
            var moveDown = compositor.CreateVector3KeyFrameAnimation();
            moveDown.InsertKeyFrame(1f, new System.Numerics.Vector3(0, 0, 0)); // Move to final position
            moveDown.Duration = TimeSpan.FromMilliseconds(milliseconds);

            visual.StartAnimation("Opacity", fadeIn);
            visual.StartAnimation("Scale", scaleUp);
            visual.StartAnimation("Offset", moveDown);

            await Task.Delay(milliseconds);
            return;
        }

        private async Task AnimateOut(TextBlock text)
        {
            var visual = ElementCompositionPreview.GetElementVisual(text);
            var compositor = visual.Compositor;
            var milliseconds = 300;

            // start values
            visual.Opacity = 1f;
            visual.Scale = new System.Numerics.Vector3(1f, 1f, 1f);
            visual.Offset = new System.Numerics.Vector3(0, 0, 0);

            // opacity animation
            var fadeIn = compositor.CreateScalarKeyFrameAnimation();
            fadeIn.InsertKeyFrame(0f, 0f);
            fadeIn.Duration = TimeSpan.FromMilliseconds(milliseconds);

            // scale animation
            var scaleUp = compositor.CreateVector3KeyFrameAnimation();
            scaleUp.InsertKeyFrame(1f, new System.Numerics.Vector3(0f, 0f, 1f)); // Normal size
            scaleUp.Duration = TimeSpan.FromMilliseconds(milliseconds);

            // translation animation
            var moveDown = compositor.CreateVector3KeyFrameAnimation();
            moveDown.InsertKeyFrame(1f, new System.Numerics.Vector3(0, 0, 0)); // Move to final position
            moveDown.Duration = TimeSpan.FromMilliseconds(milliseconds);

            visual.StartAnimation("Opacity", fadeIn);
            visual.StartAnimation("Scale", scaleUp);
            visual.StartAnimation("Offset", moveDown);

            await Task.Delay(milliseconds);
            return;
        }
    }
}
