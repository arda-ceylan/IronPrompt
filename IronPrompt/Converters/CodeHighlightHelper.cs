// Copyright 2026 Arda Ceylan
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using System.Text.RegularExpressions;

namespace IronPrompt.Converters
{
    public class CodeHighlightHelper
    {
        public static readonly AttachedProperty<string> CodeTextProperty =
            AvaloniaProperty.RegisterAttached<SelectableTextBlock, string>("CodeText", typeof(CodeHighlightHelper), string.Empty);

        static CodeHighlightHelper()
        {
            CodeTextProperty.Changed.AddClassHandler<SelectableTextBlock>((tb, e) => OnCodeTextChanged(tb, e));
        }

        public static string GetCodeText(SelectableTextBlock element) => element.GetValue(CodeTextProperty);
        public static void SetCodeText(SelectableTextBlock element, string value) => element.SetValue(CodeTextProperty, value);

        private static void OnCodeTextChanged(SelectableTextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
        {
            var text = e.GetNewValue<string>();
            
            if (textBlock.Inlines == null) return;
            textBlock.Inlines.Clear();
            
            if (string.IsNullOrEmpty(text)) return;

            var keywordBrush = new SolidColorBrush(Color.Parse("#C678DD")); // Purple
            var typeBrush = new SolidColorBrush(Color.Parse("#E5C07B"));    // Yellow
            var methodBrush = new SolidColorBrush(Color.Parse("#61AFEF"));  // Blue
            var stringBrush = new SolidColorBrush(Color.Parse("#98C379"));  // Green
            var numberBrush = new SolidColorBrush(Color.Parse("#D19A66"));  // Orange
            var commentBrush = new SolidColorBrush(Color.Parse("#5C6370")); // Muted gray-green
            var defaultBrush = new SolidColorBrush(Color.Parse("#ABB2BF")); // Default text color

            var pattern = @"(?<comment>//.*|/\*[\s\S]*?\*/|#.*)" +
                          @"|(?<string>""[^""]*""|'[^']*')" +
                          @"|(?<number>\b\d+(\.\d+)?\b)" +
                          @"|(?<keyword>\b(using|namespace|class|struct|interface|enum|public|private|protected|internal|static|readonly|void|int|string|bool|double|float|char|var|new|if|else|switch|case|default|for|foreach|while|do|break|continue|return|async|await|true|false|null|try|catch|finally|throw|typeof|def|import|from|print|function|let|const)\b)" +
                          @"|(?<method>\b[a-zA-Z_][a-zA-Z0-9_]*(?=\s*\((?!comment|string)))" +
                          @"|(?<type>\b[A-Z][a-zA-Z0-9_]*\b)";

            var regex = new Regex(pattern, RegexOptions.Compiled);
            var matches = regex.Matches(text);
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Index > lastIndex)
                {
                    var normalText = text.Substring(lastIndex, match.Index - lastIndex);
                    textBlock.Inlines.Add(new Run(normalText) { Foreground = defaultBrush });
                }

                var tokenText = match.Value;
                IBrush brush = defaultBrush;

                if (match.Groups["comment"].Success) brush = commentBrush;
                else if (match.Groups["string"].Success) brush = stringBrush;
                else if (match.Groups["number"].Success) brush = numberBrush;
                else if (match.Groups["keyword"].Success) brush = keywordBrush;
                else if (match.Groups["method"].Success) brush = methodBrush;
                else if (match.Groups["type"].Success) brush = typeBrush;

                textBlock.Inlines.Add(new Run(tokenText) { Foreground = brush });

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < text.Length)
            {
                var remainingText = text.Substring(lastIndex);
                textBlock.Inlines.Add(new Run(remainingText) { Foreground = defaultBrush });
            }
        }
    }
}
