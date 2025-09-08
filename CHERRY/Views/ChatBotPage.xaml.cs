using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CHERRY.Views;

public partial class ChatBotPage : ContentPage
{
    private CancellationTokenSource typingCts;

    public ChatBotPage()
    {
        InitializeComponent();
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
        await ScrollToBottom();

        // Start typing animation
        var typingTask = ShowTypingIndicator();

        // Simulate backend API call (replace with real one later)
        string botReply = await CallChatApi(userMessage);

        // Stop typing indicator
        HideTypingIndicator();

        AddMessage(botReply, isUser: false);
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

    // Placeholder backend method
    private async Task<string> CallChatApi(string userMessage)
    {
        await Task.Delay(1200); // simulate API delay
        return $"Here’s my friendly reply to \"{userMessage}\" 💕";
    }
}
