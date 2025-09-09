using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Devices.Sensors;
using CHERRY.Services;

namespace CHERRY.Views
{
    public partial class EmergencyPage : ContentPage
    {
        private readonly NearbyPlacesService _nearbyPlacesService;
        private Location _lastLocation;

        public EmergencyPage()
        {
            InitializeComponent();
            _nearbyPlacesService = ServiceHelper.GetService<NearbyPlacesService>();

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

        private async void OnFindPharmaciesClicked(object sender, EventArgs e)
        {
            try
            {
                var location = await EnsureLocationAsync();
                if (location == null)
                {
                    await DisplayAlert("Location Required", "Enable location or enter your address.", "OK");
                    return;
                }

                var places = await _nearbyPlacesService.GetNearbyPharmaciesAsync(location.Latitude, location.Longitude, 3000);
                var uiItems = places.Select(p => new Pharmacy
                {
                    Name = p.Name,
                    Address = p.Address,
                    Distance = CalculateDistanceLabel(location, p.Latitude, p.Longitude),
                    Hours = string.Empty
                }).ToList();
                PharmacyList.ItemsSource = uiItems;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Failed to fetch nearby pharmacies.", "OK");
                System.Diagnostics.Debug.WriteLine(ex);
            }
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

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
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
                    _lastLocation = location;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", "Unable to get current location. Please enter your address manually.", "OK");
                Console.WriteLine($"Location error: {ex.Message}");
            }
        }

        private async Task<Location> EnsureLocationAsync()
        {
            if (_lastLocation != null)
            {
                return _lastLocation;
            }

            var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                return null;
            }

            try
            {
                var cached = await Geolocation.GetLastKnownLocationAsync();
                if (cached != null)
                {
                    _lastLocation = cached;
                    return cached;
                }

                var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                var location = await Geolocation.GetLocationAsync(request);
                _lastLocation = location;
                return location;
            }
            catch
            {
                return null;
            }
        }

        private string CalculateDistanceLabel(Location from, double lat, double lon)
        {
            try
            {
                var to = new Location(lat, lon);
                var meters = Location.CalculateDistance(from, to, DistanceUnits.Kilometers) * 1000.0;
                if (meters < 1000) return $"{Math.Round(meters)} m away";
                return $"{(meters / 1000.0).ToString("0.0")} km away";
            }
            catch
            {
                return string.Empty;
            }
        }

        private async void OnEmergencyCallClicked(object sender, EventArgs e)
        {
            bool proceed = await DisplayAlert("Emergency Call",
                "Are you sure you want to call emergency services (999)?", "Call", "Cancel");

            if (proceed)
            {
                try
                {
                    PhoneDialer.Open("999");
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
                "Call Women's Health Hotline at 16263?", "Call", "Cancel");

            if (proceed)
            {
                try
                {
                    PhoneDialer.Open("16263");
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
                    // If Pharmacy has coordinates, use them; otherwise fallback to last location
                    var location = _lastLocation ?? new Location(23.8103, 90.4125);
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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Try to pre-warm location so first search is quick
            _ = EnsureLocationAsync();
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
            await Navigation.PushAsync(new ChatBotPage());
        }

        private async void OnResourcesClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new WomensHealthPage());
        }

        private async void OnUrgentCareClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new UrgentCarePage());
        }

        private async void OnHealthInfoClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SexualHealthPage());
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