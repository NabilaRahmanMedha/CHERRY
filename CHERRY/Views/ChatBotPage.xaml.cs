namespace CHERRY.Views;

public partial class ChatBotPage : ContentPage
{
    public ChatBotPage()
    {
        InitializeComponent();
    }

    private void OnSendClicked(object sender, EventArgs e)
    {
        string userMessage = UserInput.Text?.Trim();
        if (string.IsNullOrEmpty(userMessage)) return;

        // Add user bubble
        AddMessage(userMessage, isUser: true);

        UserInput.Text = string.Empty;

        // ---- TODO: Call your backend API here ----
        string botReply = "This is a placeholder reply from the bot."; // TEMP
        AddMessage(botReply, isUser: false);
    }

    private void AddMessage(string text, bool isUser)
    {
        var bubble = new Frame
        {
            BackgroundColor = isUser ? Colors.Purple : Colors.LightGray,
            CornerRadius = 20,
            Padding = new Thickness(12, 8),
            Margin = new Thickness(5),
            HasShadow = false,
            HorizontalOptions = isUser ? LayoutOptions.End : LayoutOptions.Start,
            Content = new Label
            {
                Text = text,
                FontSize = 16,
                TextColor = isUser ? Colors.White : Colors.Black
            }
        };

        MessagesStack.Children.Add(bubble);
    }

    // Placeholder backend call (you can fill in later)
    private async Task<string> CallChatApi(string userMessage)
    {
        await Task.Delay(500); // simulate network delay
        return "Bot reply here...";
    }
}
