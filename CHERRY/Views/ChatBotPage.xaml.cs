using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using CHERRY.Services;
using CHERRY.Models;
using System.Collections.Generic;

namespace CHERRY.Views;

public partial class ChatBotPage : ContentPage
{
    private CancellationTokenSource typingCts;
    private readonly DatabaseService _db;
    private readonly GeminiService _gemini;
    private readonly AuthService _auth;
    private string _email = string.Empty;

    public ChatBotPage()
    {
        InitializeComponent();
        _db = ServiceHelper.GetService<DatabaseService>();
        _gemini = ServiceHelper.GetService<GeminiService>();
        _auth = ServiceHelper.GetService<AuthService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _email = await _auth.GetEmailAsync() ?? string.Empty;
        await LoadHistoryAsync();
    }

    private async Task LoadHistoryAsync()
    {
        MessagesStack.Children.Clear();
        if (string.IsNullOrEmpty(_email)) return;
        var messages = await _db.GetMessagesAsync(_email, 200);
        foreach (var m in messages)
        {
            AddMessage(m.Content, m.IsUser);
        }
        await ScrollToBottom();
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        string userMessage = UserInput.Text?.Trim();
        if (string.IsNullOrEmpty(userMessage))
            return;

        await HandleUserMessage(userMessage);
        UserInput.Text = string.Empty;
    }

    // When quick suggestion button is tapped
    private async void OnSuggestionClicked(object sender, EventArgs e)
    {
        if (sender is Button btn && !string.IsNullOrEmpty(btn.Text))
        {
            await HandleUserMessage(btn.Text);
        }
    }

    // Core message handler
    private async Task HandleUserMessage(string userMessage)
    {
        AddMessage(userMessage, isUser: true);
        if (!string.IsNullOrEmpty(_email))
        {
            await _db.AddMessageAsync(new ChatMessage
            {
                UserEmail = _email,
                IsUser = true,
                Content = userMessage,
                CreatedUtcTicks = DateTime.UtcNow.Ticks
            });
        }
        await ScrollToBottom();

        // Start typing animation
        var typingTask = ShowTypingIndicator();

        // Build chat history for Gemini
        var history = new List<(bool isUser, string content)>();
        if (!string.IsNullOrEmpty(_email))
        {
            var prior = await _db.GetMessagesAsync(_email, 50);
            foreach (var msg in prior)
                history.Add((msg.IsUser, msg.Content));
        }
        history.Add((true, userMessage));

        string botReply = string.Empty;
        // Detect Bangla quickly from latest message
        string? preferredLang = null;
        if (IsBangla(userMessage)) preferredLang = "Bangla";
        try
        {
            botReply = await _gemini.GetChatCompletionAsync(history, preferredLang);
        }
        catch (Exception ex)
        {
            botReply = "Sorry, I couldn't reach the AI service right now.";
        }

        // Stop typing indicator
        HideTypingIndicator();

        AddMessage(botReply, isUser: false);
        if (!string.IsNullOrEmpty(_email))
        {
            await _db.AddMessageAsync(new ChatMessage
            {
                UserEmail = _email,
                IsUser = false,
                Content = botReply,
                CreatedUtcTicks = DateTime.UtcNow.Ticks
            });
        }
        await ScrollToBottom();
    }

    private bool IsBangla(string text)
    {
        foreach (var ch in text)
        {
            if (ch >= '\u0980' && ch <= '\u09FF') return true; // Bengali block
        }
        return false;
    }

    // Show animated typing dots
    private async Task ShowTypingIndicator()
    {
        TypingIndicator.IsVisible = true;
        typingCts = new CancellationTokenSource();

        string baseText = "CherryMate is typing";
        int dotCount = 0;

        try
        {
            while (!typingCts.IsCancellationRequested)
            {
                dotCount = (dotCount + 1) % 4; // cycle through 0–3 dots
                TypingIndicator.Text = baseText + new string('.', dotCount);
                await Task.Delay(500, typingCts.Token);
            }
        }
        catch (TaskCanceledException)
        {
            // ignore when cancelled
        }
    }

    // Hide typing indicator
    private void HideTypingIndicator()
    {
        if (typingCts != null)
        {
            typingCts.Cancel();
            typingCts.Dispose();
            typingCts = null;
        }
        TypingIndicator.IsVisible = false;
    }

    // Add chat bubble
    // Add chat bubble with proper wrapping and max width
    private void AddMessage(string text, bool isUser)
    {
        // Container for the message (bot/user)
        var messageLayout = new HorizontalStackLayout
        {
            HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
            Spacing = 8
        };

        if (!isUser)
        {
            // Bot avatar
            messageLayout.Children.Add(new Image
            {
                Source = "cherrymate.png",
                WidthRequest = 30,
                HeightRequest = 30,
                Aspect = Aspect.AspectFit,
                VerticalOptions = LayoutOptions.Start
            });
        }

        // Message bubble
        var bubbleContent = isUser ? BuildUserLabel(text) : BuildFormattedLabel(text);

        var bubble = new Frame
        {
            BackgroundColor = isUser ? Color.FromArgb("#FF5C8A") : Color.FromArgb("#FFD1DC"),
            BorderColor = isUser ? Color.FromArgb("#FF2E63") : Color.FromArgb("#F8BBD0"),
            CornerRadius = 18,
            Padding = new Thickness(14, 12),
            HasShadow = true,
            Shadow = new Shadow { Brush = new SolidColorBrush(Color.FromArgb("#33000000")), Radius = 8, Offset = new Point(0, 3) },
            HorizontalOptions = LayoutOptions.Fill,
            Content = bubbleContent,
            MaximumWidthRequest = this.Width * 0.75
        };

        messageLayout.Children.Add(bubble);
        MessagesStack.Children.Add(messageLayout);
    }




    // Scroll to bottom of chat
    private async Task ScrollToBottom()
    {
        await Task.Delay(50);
        if (MessagesStack.Children.Any())
        {
            var last = MessagesStack.Children.Last() as Element;
            if (last != null)
                await MessagesScroll.ScrollToAsync(last, ScrollToPosition.End, true);
        }
    }

    // Placeholder removed; Gemini is used

    private Label BuildUserLabel(string text)
    {
        return new Label
        {
            Text = text,
            TextColor = Colors.White,
            FontSize = 16,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    // Minimal Markdown renderer for bold (**text**), bullets (- or *), and links [text](url)
    private Label BuildFormattedLabel(string markdown)
    {
        var label = new Label
        {
            TextColor = Colors.Black,
            FontSize = 16,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var fs = new FormattedString();
        foreach (var line in markdown.Replace("\r\n", "\n").Split('\n'))
        {
            bool isBullet = line.StartsWith("- ") || line.StartsWith("* ");
            string content = isBullet ? line.Substring(2) : line;

            if (isBullet)
            {
                fs.Spans.Add(new Span { Text = "• ", FontAttributes = FontAttributes.Bold });
            }

            int idx = 0;
            while (idx < content.Length)
            {
                // link [text](url)
                int linkStart = content.IndexOf('[', idx);
                int boldStart = content.IndexOf("**", idx, StringComparison.Ordinal);

                int next = MinPositive(linkStart, boldStart);
                if (next == -1)
                {
                    fs.Spans.Add(new Span { Text = content.Substring(idx) });
                    break;
                }

                if (next > idx)
                {
                    fs.Spans.Add(new Span { Text = content.Substring(idx, next - idx) });
                    idx = next;
                }

                if (next == linkStart)
                {
                    int closeBracket = content.IndexOf(']', linkStart + 1);
                    int openParen = closeBracket >= 0 ? content.IndexOf('(', closeBracket + 1) : -1;
                    int closeParen = openParen >= 0 ? content.IndexOf(')', openParen + 1) : -1;
                    if (closeBracket > 0 && openParen == closeBracket + 1 && closeParen > openParen)
                    {
                        string linkText = content.Substring(linkStart + 1, closeBracket - linkStart - 1);
                        string url = content.Substring(openParen + 1, closeParen - openParen - 1);
                        var linkSpan = new Span { Text = linkText, TextDecorations = TextDecorations.Underline, TextColor = Color.FromArgb("#1565C0") };
                        var tap = new TapGestureRecognizer();
                        tap.Tapped += async (_, __) =>
                        {
                            try { await Browser.OpenAsync(url); } catch { }
                        };
                        linkSpan.GestureRecognizers.Add(tap);
                        fs.Spans.Add(linkSpan);
                        idx = closeParen + 1;
                        continue;
                    }
                }

                if (next == boldStart)
                {
                    int end = content.IndexOf("**", boldStart + 2, StringComparison.Ordinal);
                    if (end > boldStart)
                    {
                        fs.Spans.Add(new Span { Text = content.Substring(boldStart + 2, end - boldStart - 2), FontAttributes = FontAttributes.Bold });
                        idx = end + 2;
                        continue;
                    }
                }

                // Fallback: append the current char and move on
                fs.Spans.Add(new Span { Text = content[idx].ToString() });
                idx++;
            }

            fs.Spans.Add(new Span { Text = "\n" });
        }

        label.FormattedText = fs;
        return label;
    }

    private int MinPositive(int a, int b)
    {
        if (a < 0) return b;
        if (b < 0) return a;
        return Math.Min(a, b);
    }
}
