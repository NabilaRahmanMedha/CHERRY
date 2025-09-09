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
		private readonly GooglePlacesService? _google;

		public NearbyPlacesService(HttpClient httpClient, GooglePlacesService? google = null)
		{
			_httpClient = httpClient;
			_google = google;
			// Overpass can be slow; set a sensible timeout
			_httpClient.Timeout = TimeSpan.FromSeconds(20);
		}

		public async Task<IReadOnlyList<PlaceResult>> GetNearbyPharmaciesAsync(double latitude, double longitude, int radiusMeters = 3000, CancellationToken cancellationToken = default)
		{
			// Try pharmacies; if none, widen radius progressively
			var results = new List<PlaceResult>();
			int[] radii = new[] { radiusMeters, Math.Min(10000, radiusMeters * 2), 20000, 30000 };
			foreach (var r in radii)
			{
				string q = BuildOverpassQuery(latitude, longitude, r, new[] { "amenity=pharmacy" });
				results = new List<PlaceResult>(await ExecuteOverpassAsync(q, cancellationToken));
				if (results.Count > 0) break;
			}
			if (results.Count == 0 && _google != null && _google.IsConfigured)
			{
				// Google fallback for pharmacies
				var googleRes = await _google.SearchNearbyAsync(latitude, longitude, "pharmacy", radii[^1], cancellationToken);
				return googleRes;
			}

			return results;
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

			// Include department tag and other potential flags
			string specialityConstraint = "[\"healthcare:speciality\"~\"gynaecology|gynecology|obstetrics|ob-gyn|obgyn|women'?s health\",i]";
			string departmentConstraint = "[department~\"gynaecology|gynecology|obstetrics\",i]";

			// Progressive widening and multiple strategies
			int[] radii = new[] { radiusMeters, Math.Min(15000, radiusMeters * 2), 30000, 50000 };
			foreach (var r in radii)
			{
				// 1) Speciality-tagged
				string q1 = BuildOverpassQuery(latitude, longitude, r, filters, specialityConstraint);
				var res1 = await ExecuteOverpassAsync(q1, cancellationToken);
				if (res1.Count > 0) return res1;

				// 1b) Department-tagged at hospitals/clinics
				string q1b = BuildOverpassQuery(latitude, longitude, r, filters, departmentConstraint);
				var res1b = await ExecuteOverpassAsync(q1b, cancellationToken);
				if (res1b.Count > 0) return res1b;

				// 2) Name contains gyn/obstet
				string nameConstraint = "[name~\"gyn|gyne|obstet|women's health|women health|OB\\/GYN|OBGYN|婦|妇|নারী|গাইন\",i]";
				string q2 = BuildOverpassQuery(latitude, longitude, r, filters, nameConstraint);
				var res2 = await ExecuteOverpassAsync(q2, cancellationToken);
				if (res2.Count > 0) return res2;

				// 3) General doctors/clinics filtered client-side
				string fallbackQuery = BuildOverpassQuery(latitude, longitude, r, new[] { "healthcare=doctor", "amenity=doctors", "healthcare=clinic" });
				var fallback = await ExecuteOverpassAsync(fallbackQuery, cancellationToken);
				var filtered = FilterByGynecologyKeywords(fallback);
				if (filtered.Count > 0) return filtered;
			}

			if (_google != null && _google.IsConfigured)
			{
				// Use common keywords for gynecology
				var googleRes = await _google.SearchNearbyAsync(latitude, longitude, "gynecologist|obgyn|women health clinic", 20000, cancellationToken);
				return googleRes;
			}

			return Array.Empty<PlaceResult>();
		}

		public async Task<IReadOnlyList<PlaceResult>> GetNearbyDoctorsAsync(double latitude, double longitude, int radiusMeters = 5000, CancellationToken cancellationToken = default)
		{
			var filters = new List<string>
			{
				"healthcare=doctor",
				"amenity=doctors",
				"healthcare=clinic",
				"amenity=hospital"
			};

			int[] radii = new[] { radiusMeters, Math.Min(15000, radiusMeters * 2), 30000, 50000 };
			var aggregated = new List<PlaceResult>();
			foreach (var r in radii)
			{
				string q = BuildOverpassQuery(latitude, longitude, r, filters);
				var res = await ExecuteOverpassAsync(q, cancellationToken);
				if (res.Count > 0)
				{
					aggregated.AddRange(res);
					break;
				}
			}

			return aggregated;
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
			try
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
			catch
			{
				// On network or service error, return empty set to allow fallbacks without crashing UI
				return Array.Empty<PlaceResult>();
			}
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


