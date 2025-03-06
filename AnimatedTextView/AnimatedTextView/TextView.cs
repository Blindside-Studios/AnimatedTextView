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

        private void UpdateText(string newText)
        {
            if (_textContainer == null) return;

            _textContainer.Children.Clear();
            _textContainer.ColumnDefinitions.Clear();

            for (int i = 0; i < newText.Length; i++)
            {
                // Add a column for each character
                _textContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var charBlock = new TextBlock
                {
                    Text = newText[i].ToString(),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(charBlock, i);
                _textContainer.Children.Add(charBlock);
            }
        }
    }
}
