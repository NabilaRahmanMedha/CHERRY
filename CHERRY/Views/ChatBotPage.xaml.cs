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
        try
        {
            botReply = await _gemini.GetChatCompletionAsync(history);
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
        var bubble = new Frame
        {
            BackgroundColor = isUser ? Colors.HotPink : Colors.LightPink,
            CornerRadius = 18,
            Padding = new Thickness(12),
            HasShadow = false,
            HorizontalOptions = LayoutOptions.Fill, // allow wrapping inside container
            Content = new Label
            {
                Text = text,
                TextColor = isUser ? Colors.White : Colors.Black,
                FontSize = 16,
                LineBreakMode = LineBreakMode.WordWrap,
            },
            MaximumWidthRequest = this.Width * 0.7 // limit bubble width to 70% of screen
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
}
