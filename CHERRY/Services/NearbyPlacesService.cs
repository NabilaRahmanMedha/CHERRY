using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CHERRY.Services
{
	public class NearbyPlacesService
	{
		private readonly HttpClient _httpClient;

		public NearbyPlacesService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			// Overpass can be slow; set a sensible timeout
			_httpClient.Timeout = TimeSpan.FromSeconds(20);
		}

		public async Task<IReadOnlyList<PlaceResult>> GetNearbyPharmaciesAsync(double latitude, double longitude, int radiusMeters = 3000, CancellationToken cancellationToken = default)
		{
			string query = BuildOverpassQuery(latitude, longitude, radiusMeters, new[] { "amenity=pharmacy" });
			return await ExecuteOverpassAsync(query, cancellationToken);
		}

		public async Task<IReadOnlyList<PlaceResult>> GetNearbyGynecologistsAsync(double latitude, double longitude, int radiusMeters = 5000, CancellationToken cancellationToken = default)
		{
			// Try common OSM tagging variants for gynecology
			var filters = new List<string>
			{
				"healthcare=doctor",
				"amenity=doctors",
				"healthcare=clinic",
				"amenity=hospital"
			};

			string specialityConstraint = "[\"healthcare:speciality\"~\"gynaecology|gynecology|obstetrics\",i]";

			string query = BuildOverpassQuery(latitude, longitude, radiusMeters, filters, specialityConstraint);
			var results = await ExecuteOverpassAsync(query, cancellationToken);

			// If no results, fall back to general doctors/clinics and filter client-side by name
			if (results.Count == 0)
			{
				string fallbackQuery = BuildOverpassQuery(latitude, longitude, radiusMeters, new[] { "healthcare=doctor", "amenity=doctors", "healthcare=clinic" });
				var fallback = await ExecuteOverpassAsync(fallbackQuery, cancellationToken);
				return FilterByGynecologyKeywords(fallback);
			}

			return results;
		}

		private static string BuildOverpassQuery(double lat, double lon, int radius, IEnumerable<string> keyEqualsFilters, string extraConstraint = "")
		{
			var sb = new StringBuilder();
			sb.Append("[out:json][timeout:25];");
			sb.Append("(");
			foreach (var f in keyEqualsFilters)
			{
				// node/way/relation within radius
				sb.Append($"node[\"{f.Split('=')[0]}\"=\"{f.Split('=')[1]}\"]{extraConstraint}(around:{radius},{lat},{lon});");
				sb.Append($"way[\"{f.Split('=')[0]}\"=\"{f.Split('=')[1]}\"]{extraConstraint}(around:{radius},{lat},{lon});");
				sb.Append($"relation[\"{f.Split('=')[0]}\"=\"{f.Split('=')[1]}\"]{extraConstraint}(around:{radius},{lat},{lon});");
			}
			sb.Append(");out center 50;\n");
			return sb.ToString();
		}

		private async Task<IReadOnlyList<PlaceResult>> ExecuteOverpassAsync(string query, CancellationToken cancellationToken)
		{
			var form = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("data", query) });
			// Use a public Overpass instance. Consider rotating if throttled.
			var response = await _httpClient.PostAsync("https://overpass-api.de/api/interpreter", form, cancellationToken);
			response.EnsureSuccessStatusCode();
			var json = await response.Content.ReadAsStringAsync(cancellationToken);

			var doc = JsonSerializer.Deserialize<OverpassResponse>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			var results = new List<PlaceResult>();
			if (doc?.Elements == null) return results;

			foreach (var el in doc.Elements)
			{
				string name = el.Tags?.GetValueOrDefault("name") ?? "Unnamed";
				string address = BuildAddress(el.Tags);
				double lat = el.Lat ?? el.Center?.Lat ?? 0;
				double lon = el.Lon ?? el.Center?.Lon ?? 0;

				results.Add(new PlaceResult
				{
					Name = name,
					Address = address,
					Latitude = lat,
					Longitude = lon
				});
			}

			return results;
		}

		private static string BuildAddress(Dictionary<string, string>? tags)
		{
			if (tags == null) return string.Empty;
			tags.TryGetValue("addr:street", out var street);
			tags.TryGetValue("addr:housenumber", out var house);
			tags.TryGetValue("addr:city", out var city);
			tags.TryGetValue("addr:postcode", out var postcode);
			var parts = new List<string>();
			if (!string.IsNullOrWhiteSpace(house) || !string.IsNullOrWhiteSpace(street))
			{
				parts.Add($"{house} {street}".Trim());
			}
			if (!string.IsNullOrWhiteSpace(city)) parts.Add(city);
			if (!string.IsNullOrWhiteSpace(postcode)) parts.Add(postcode);
			return string.Join(", ", parts);
		}

		private static IReadOnlyList<PlaceResult> FilterByGynecologyKeywords(IReadOnlyList<PlaceResult> input)
		{
			var results = new List<PlaceResult>();
			foreach (var r in input)
			{
				var text = ($"{r.Name} {r.Address}").ToLowerInvariant();
				if (text.Contains("gyn") || text.Contains("obstet") || text.Contains("妇") || text.Contains("妇科"))
				{
					results.Add(r);
				}
			}
			return results;
		}
	}

	public class PlaceResult
	{
		public string Name { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;
		public double Latitude { get; set; }
		public double Longitude { get; set; }
	}

	internal sealed class OverpassResponse
	{
		[JsonPropertyName("elements")] public List<OverpassElement>? Elements { get; set; }
	}

	internal sealed class OverpassElement
	{
		[JsonPropertyName("type")] public string? Type { get; set; }
		[JsonPropertyName("lat")] public double? Lat { get; set; }
		[JsonPropertyName("lon")] public double? Lon { get; set; }
		[JsonPropertyName("center")] public OverpassCenter? Center { get; set; }
		[JsonPropertyName("tags")] public Dictionary<string, string>? Tags { get; set; }
	}

	internal sealed class OverpassCenter
	{
		[JsonPropertyName("lat")] public double Lat { get; set; }
		[JsonPropertyName("lon")] public double Lon { get; set; }
	}
}


