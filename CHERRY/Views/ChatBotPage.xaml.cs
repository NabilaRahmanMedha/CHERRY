using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace CHERRY.Views;

public partial class ChatBotPage : ContentPage
{
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

        await Task.Delay(50);
        if (MessagesStack.Children.Any())
        {
            var last = MessagesStack.Children.Last() as Element;
            if (last != null)
                await MessagesScroll.ScrollToAsync(last, ScrollToPosition.End, true);
        }

        // ---- PLACEHOLDER: Call backend API ----
        string botReply = await CallChatApi(userMessage);

        AddMessage(botReply, isUser: false);

        await Task.Delay(50);
        if (MessagesStack.Children.Any())
        {
            var last = MessagesStack.Children.Last() as Element;
            if (last != null)
                await MessagesScroll.ScrollToAsync(last, ScrollToPosition.End, true);
        }
    }

    // Create a chat bubble
    private void AddMessage(string text, bool isUser)
    {
        var bubble = new Frame
        {
            BackgroundColor = isUser ? Colors.Purple : Colors.LightGray,
            CornerRadius = 18,
            Padding = new Thickness(12),
            Margin = new Thickness(5),
            HasShadow = false,
            HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
            Content = new Label
            {
                Text = text,
                TextColor = isUser ? Colors.White : Colors.Black,
                FontSize = 16
            }
        };

        MessagesStack.Children.Add(bubble);
    }

    // Placeholder backend method
    private async Task<string> CallChatApi(string userMessage)
    {
        await Task.Delay(400); // simulate delay
        return $"(Bot reply placeholder for: {userMessage})";
    }
}
