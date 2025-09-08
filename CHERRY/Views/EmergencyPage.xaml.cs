using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;

namespace CHERRY.Views
{
    public partial class EmergencyPage : ContentPage
    {
        public EmergencyPage()
        {
            InitializeComponent();

            // Static gynecologist contacts with more details
            DoctorList.ItemsSource = new List<DoctorContact>
            {
                new DoctorContact
                {
                    Name = "Dr. Ayesha Rahman",
                    Phone = "01234 567890",
                    Specialty = "Gynecologist & Obstetrician",
                    Address = "123 Medical Street, Dhaka",
                    Availability = "Available Today"
                },
                new DoctorContact
                {
                    Name = "Dr. Nabila Chowdhury",
                    Phone = "01711 223344",
                    Specialty = "Reproductive Health Specialist",
                    Address = "456 Health Avenue, Dhaka",
                    Availability = "Available Tomorrow"
                },
                new DoctorContact
                {
                    Name = "Dr. Sharmeen Akter",
                    Phone = "01922 334455",
                    Specialty = "Women's Health Specialist",
                    Address = "789 Care Road, Dhaka",
                    Availability = "Available Now"
                },
                new DoctorContact
                {
                    Name = "Dr. Nusrat Jahan",
                    Phone = "01555 667788",
                    Specialty = "Gynecologic Surgeon",
                    Address = "321 Wellness Lane, Dhaka",
                    Availability = "Available in 2 hours"
                },
                new DoctorContact
                {
                    Name = "Dr. Tasnim Hossain",
                    Phone = "01888 445566",
                    Specialty = "Endocrinology & Fertility",
                    Address = "654 Treatment Blvd, Dhaka",
                    Availability = "Available Monday"
                }
            };
        }

        private void OnFindPharmaciesClicked(object sender, EventArgs e)
        {
            // Example mock results with more details (replace with API later)
            PharmacyList.ItemsSource = new List<Pharmacy>
            {
                new Pharmacy
                {
                    Name = "City Pharmacy",
                    Distance = "500m away",
                    Address = "123 Main Street, Dhaka",
                    Hours = "Open until 10 PM"
                },
                new Pharmacy
                {
                    Name = "HealthPlus Pharmacy",
                    Distance = "700m away",
                    Address = "456 Health Avenue, Dhaka",
                    Hours = "Open 24/7"
                },
                new Pharmacy
                {
                    Name = "MediCare Pharmacy",
                    Distance = "1.2km away",
                    Address = "789 Care Road, Dhaka",
                    Hours = "Open until 11 PM"
                },
                new Pharmacy
                {
                    Name = "LifeCare Pharmacy",
                    Distance = "1.5km away",
                    Address = "321 Wellness Lane, Dhaka",
                    Hours = "Open until 9 PM"
                }
            };
        }

        private async void OnUseCurrentLocationClicked(object sender, EventArgs e)
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Permission Required",
                        "Location permission is needed to find nearby help.", "OK");
                    return;
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Medium);
                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    // Reverse geocoding to get address from coordinates
                    var placemarks = await Geocoding.GetPlacemarksAsync(location.Latitude, location.Longitude);
                    var placemark = placemarks?.FirstOrDefault();

                    if (placemark != null)
                    {
                        var address = $"{placemark.FeatureName}, {placemark.Locality}, {placemark.AdminArea}";
                        AddressEntry.Text = address;

                        // Automatically search for nearby help
                        OnFindPharmaciesClicked(sender, e);
                    }
                    else
                    {
                        AddressEntry.Text = $"Lat: {location.Latitude}, Long: {location.Longitude}";
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Unable to get current location. Please enter your address manually.", "OK");
                Console.WriteLine($"Location error: {ex.Message}");
            }
        }

        private async void OnEmergencyCallClicked(object sender, EventArgs e)
        {
            bool proceed = await DisplayAlert("Emergency Call",
                "Are you sure you want to call emergency services (911)?", "Call", "Cancel");

            if (proceed)
            {
                try
                {
                    PhoneDialer.Open("911");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Could not make the call. Please check your device.", "OK");
                    Console.WriteLine($"Call error: {ex.Message}");
                }
            }
        }

        private async void OnHealthHotlineClicked(object sender, EventArgs e)
        {
            bool proceed = await DisplayAlert("Health Hotline",
                "Call Women's Health Hotline at 1-800-XXX-XXXX?", "Call", "Cancel");

            if (proceed)
            {
                try
                {
                    PhoneDialer.Open("1800XXXXXXX");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Could not make the call. Please check your device.", "OK");
                    Console.WriteLine($"Call error: {ex.Message}");
                }
            }
        }

        private async void OnGetDirectionsClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var pharmacy = button?.BindingContext as Pharmacy;

            if (pharmacy != null)
            {
                try
                {
                    var location = new Location(23.8103, 90.4125); // Default to Dhaka coordinates
                    var options = new MapLaunchOptions { Name = pharmacy.Name };

                    await Map.OpenAsync(location, options);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Error", "Could not open maps. Please check your device.", "OK");
                    Console.WriteLine($"Maps error: {ex.Message}");
                }
            }
        }

        private async void OnCallDoctorClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var doctor = button?.BindingContext as DoctorContact;

            if (doctor != null)
            {
                bool proceed = await DisplayAlert("Call Doctor",
                    $"Call {doctor.Name} at {doctor.Phone}?", "Call", "Cancel");

                if (proceed)
                {
                    try
                    {
                        PhoneDialer.Open(doctor.Phone);
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Error", "Could not make the call. Please check your device.", "OK");
                        Console.WriteLine($"Call error: {ex.Message}");
                    }
                }
            }
        }

        private async void OnBookAppointmentClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var doctor = button?.BindingContext as DoctorContact;

            if (doctor != null)
            {
                await DisplayAlert("Appointment",
                    $"Appointment booking feature for {doctor.Name} would open here.", "OK");
            }
        }

        private void OnRefreshPharmaciesClicked(object sender, EventArgs e)
        {
            OnFindPharmaciesClicked(sender, e);
            DisplayToast("Pharmacies list refreshed");
        }

        private void OnRefreshDoctorsClicked(object sender, EventArgs e)
        {
            // Simulate refresh by reinitializing
            DoctorList.ItemsSource = new List<DoctorContact>
            {
                new DoctorContact
                {
                    Name = "Dr. Ayesha Rahman",
                    Phone = "01234 567890",
                    Specialty = "Gynecologist & Obstetrician",
                    Address = "123 Medical Street, Dhaka",
                    Availability = "Available Today"
                },
                new DoctorContact
                {
                    Name = "Dr. Fatima Khan (New)",
                    Phone = "01666 999888",
                    Specialty = "Emergency Gynecology",
                    Address = "555 Urgent Care Plaza, Dhaka",
                    Availability = "Available Now"
                },
                new DoctorContact
                {
                    Name = "Dr. Nabila Chowdhury",
                    Phone = "01711 223344",
                    Specialty = "Reproductive Health Specialist",
                    Address = "456 Health Avenue, Dhaka",
                    Availability = "Available Tomorrow"
                }
            };

            DisplayToast("Doctors list refreshed");
        }

        private async void OnAiHealthAssistantClicked(object sender, EventArgs e)
        {
            await DisplayAlert("ChatBot","Navigate to Ai Chatbot", "OK");
        }

        private async void OnResourcesClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Resources", "Women's health resources would be shown here.", "OK");
        }

        private async void OnUrgentCareClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Urgent Care", "Urgent care centers would be shown here.", "OK");
        }

        private async void OnHealthInfoClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Health Information", "Sexual health information would be shown here.", "OK");
        }

        private void DisplayToast(string message)
        {
            // In a real app, you would use a toast notification library
            // This is a simple implementation that could be enhanced
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await DisplayAlert("Info", message, "OK");
            });
        }
    }

    public class DoctorContact
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Specialty { get; set; }
        public string Address { get; set; }
        public string Availability { get; set; }

        public override string ToString() => $"{Name} - {Phone}";
    }

    public class Pharmacy
    {
        public string Name { get; set; }
        public string Distance { get; set; }
        public string Address { get; set; }
        public string Hours { get; set; }

        public override string ToString() => $"{Name} - {Distance}";
    }
}