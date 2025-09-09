using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CHERRY.Services
{
	public sealed class GooglePlacesOptions
	{
		public string ApiKey { get; set; } = string.Empty;
	}

	public class GooglePlacesService
	{
		private readonly HttpClient _httpClient;
		private readonly GooglePlacesOptions _options;

		public GooglePlacesService(HttpClient httpClient, GooglePlacesOptions options)
		{
			_httpClient = httpClient;
			_options = options;
		}

		public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

		public async Task<IReadOnlyList<PlaceResult>> SearchNearbyAsync(double latitude, double longitude, string keywordOrType, int radiusMeters, CancellationToken cancellationToken = default)
		{
			if (!IsConfigured) return Array.Empty<PlaceResult>();

			var url = $"https://maps.googleapis.com/maps/api/place/nearbysearch/json?location={latitude},{longitude}&radius={radiusMeters}&keyword={Uri.EscapeDataString(keywordOrType)}&key={_options.ApiKey}";
			var response = await _httpClient.GetAsync(url, cancellationToken);
			response.EnsureSuccessStatusCode();
			var json = await response.Content.ReadAsStringAsync(cancellationToken);
			var doc = JsonSerializer.Deserialize<GoogleNearbyResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

			var results = new List<PlaceResult>();
			if (doc?.Results == null) return results;

			foreach (var r in doc.Results)
			{
				results.Add(new PlaceResult
				{
					Name = r.Name ?? "Unnamed",
					Address = r.Vicinity ?? r.FormattedAddress ?? string.Empty,
					Latitude = r.Geometry?.Location?.Lat ?? 0,
					Longitude = r.Geometry?.Location?.Lng ?? 0
				});
			}

			return results;
		}

		private sealed class GoogleNearbyResponse
		{
			[JsonPropertyName("results")] public List<Result>? Results { get; set; }
		}

		private sealed class Result
		{
			[JsonPropertyName("name")] public string? Name { get; set; }
			[JsonPropertyName("vicinity")] public string? Vicinity { get; set; }
			[JsonPropertyName("formatted_address")] public string? FormattedAddress { get; set; }
			[JsonPropertyName("geometry")] public Geometry? Geometry { get; set; }
		}

		private sealed class Geometry
		{
			[JsonPropertyName("location")] public LatLng? Location { get; set; }
		}

		private sealed class LatLng
		{
			[JsonPropertyName("lat")] public double Lat { get; set; }
			[JsonPropertyName("lng")] public double Lng { get; set; }
		}
	}
}


