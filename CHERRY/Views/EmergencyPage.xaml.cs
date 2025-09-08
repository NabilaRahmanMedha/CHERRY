using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;

namespace CHERRY.Views
{
    public partial class EmergencyPage : ContentPage
    {
        public EmergencyPage()
        {
            InitializeComponent();

            // Static gynecologist contacts
            DoctorList.ItemsSource = new List<string>
            {
                "Dr. Ayesha Rahman - 01234 567890",
                "Dr. Nabila Chowdhury - 01711 223344",
                "Dr. Sharmeen Akter - 01922 334455",
                "Dr. Nusrat Jahan - 01555 667788",
                "Dr. Tasnim Hossain - 01888 445566",
                "Dr. Fariha Karim - 01612 778899",
                "Dr. Laila Ahmed - 01333 112233",
                "Dr. Maria Sultana - 01444 556677",
                "Dr. Samira Khan - 01777 889900",
                "Dr. Rukhsana Haque - 01999 223355"
            };
        }

        private void OnFindPharmaciesClicked(object sender, EventArgs e)
        {
            // Example mock results (replace with API later)
            PharmacyList.ItemsSource = new List<string>
            {
                "City Pharmacy - 500m away",
                "HealthPlus Pharmacy - 700m away",
                "MediCare Pharmacy - 1.2km away",
                "LifeCare Pharmacy - 1.5km away"
            };
        }
    }
}
