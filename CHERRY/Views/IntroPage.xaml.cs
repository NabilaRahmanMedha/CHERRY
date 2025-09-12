using CHERRY.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

namespace CHERRY.Views
{
    public partial class IntroPage : ContentPage
    {

        private List<Border> bubbles = new List<Border>();
        private Random random = new Random();
        private System.Timers.Timer bubbleTimer;

        private readonly AuthService _auth;

        public IntroPage(AuthService auth)
        {
            InitializeComponent();
            _auth = auth;


            // Ensure the page is fully loaded before starting animation
            this.Loaded += (s, e) => StartBubbleAnimation();
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage(_auth));
        }

        private async void OnSignUpClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegistrationPage(_auth));
        }

        protected override void OnAppearing()
        {
            base.OnAppearing(); 
            StartBubbleAnimation();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            StopBubbleAnimation();
        }

        private void StartBubbleAnimation()
        {
            // Make sure we don't start multiple timers
            StopBubbleAnimation();

            // Create initial bubbles
            for (int i = 0; i < 15; i++)
            {
                AddBubble();
            }

            // Timer to add new bubbles periodically
            bubbleTimer = new System.Timers.Timer(1000);
            bubbleTimer.Elapsed += (s, e) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (bubbles.Count < 25) // Maximum bubbles
                    {
                        AddBubble();
                    }
                });
            };
            bubbleTimer.Start();
        }

        private void AddBubble()
        {
            // Check if the container is ready
            if (bubbleContainer.Width <= 0 || bubbleContainer.Height <= 0)
                return;

            // Create a circular border for the bubble
            var bubbleSize = random.Next(20, 60);
            var bubble = new Border
            {
                WidthRequest = bubbleSize,
                HeightRequest = bubbleSize,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(bubbleSize / 2) },
                Background = new SolidColorBrush(Color.FromRgba(255, 255, 255, random.Next(10, 40) / 100f)),
                Stroke = null
            };

            // Set initial position at the bottom of the screen
            double startX = random.NextDouble() * (bubbleContainer.Width - bubbleSize);
            double startY = bubbleContainer.Height;

            AbsoluteLayout.SetLayoutBounds(bubble,
                new Rect(startX, startY, bubbleSize, bubbleSize));

            bubbleContainer.Children.Add(bubble);
            bubbles.Add(bubble);

            // Animate the bubble
            AnimateBubble(bubble);
        }

        private async void AnimateBubble(Border bubble)
        {
            try
            {
                // Random horizontal movement
                double randomXMovement = (random.NextDouble() - 0.5) * 50;

                // Calculate animation duration based on size (smaller bubbles move slower)
                int baseDuration = 15000;
                int sizeFactor = (int)((60 - bubble.WidthRequest) * 100); // Smaller bubbles get longer duration
                uint duration = (uint)(baseDuration + sizeFactor);

                // Animate to top of screen with some horizontal movement
                await bubble.TranslateTo(randomXMovement, -bubbleContainer.Height - bubble.HeightRequest,
                    duration, Easing.Linear);

                // Remove bubble when animation completes
                if (bubbleContainer.Children.Contains(bubble))
                {
                    bubbleContainer.Children.Remove(bubble);
                    bubbles.Remove(bubble);
                }
            }
            catch (Exception ex)
            {
                // Handle any animation errors silently
                System.Diagnostics.Debug.WriteLine($"Animation error: {ex.Message}");
            }
        }

        private void StopBubbleAnimation()
        {
            bubbleTimer?.Stop();
            bubbleTimer?.Dispose();
            bubbleTimer = null;

            foreach (var bubble in bubbles.ToList())
            {
                Microsoft.Maui.Controls.ViewExtensions.CancelAnimations(bubble);
                if (bubbleContainer.Children.Contains(bubble))
                {
                    bubbleContainer.Children.Remove(bubble);
                }
            }
            bubbles.Clear();
        }
    }
}