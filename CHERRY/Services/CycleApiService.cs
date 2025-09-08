using System.Net.Http.Json;

namespace CHERRY.Services
{
	public class CycleApiService
	{
		private readonly HttpClient _httpClient;
		private readonly AuthService _authService;

		public CycleApiService(HttpClient httpClient, AuthService authService)
		{
			_httpClient = httpClient;
			_authService = authService;
		}

		public async Task<List<CycleDto>> GetHistoryAsync()
		{
			if (!await _authService.EnsureAuthHeaderAsync()) return new List<CycleDto>();
			var res = await _httpClient.GetAsync("api/cycles/history");
			if (!res.IsSuccessStatusCode) return new List<CycleDto>();
			var data = await res.Content.ReadFromJsonAsync<List<CycleDto>>();
			return data ?? new List<CycleDto>();
		}

		public async Task<bool> CreateAsync(DateTime startDate, DateTime endDate)
		{
			if (!await _authService.EnsureAuthHeaderAsync()) return false;
			var payload = new CreateCycleDto
			{
				StartDate = DateOnly.FromDateTime(startDate.Date),
				EndDate = DateOnly.FromDateTime(endDate.Date)
			};
			var res = await _httpClient.PostAsJsonAsync("api/cycles", payload);
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> UpdateAsync(int id, DateTime startDate, DateTime endDate)
		{
			if (!await _authService.EnsureAuthHeaderAsync()) return false;
			var payload = new UpdateCycleDto
			{
				StartDate = DateOnly.FromDateTime(startDate.Date),
				EndDate = DateOnly.FromDateTime(endDate.Date)
			};
			var res = await _httpClient.PutAsJsonAsync($"api/cycles/{id}", payload);
			return res.IsSuccessStatusCode;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			if (!await _authService.EnsureAuthHeaderAsync()) return false;
			var res = await _httpClient.DeleteAsync($"api/cycles/{id}");
			return res.IsSuccessStatusCode;
		}

		public class CycleDto
		{
			public int Id { get; set; }
			public DateOnly StartDate { get; set; }
			public DateOnly EndDate { get; set; }
		}

		private class CreateCycleDto
		{
			public DateOnly StartDate { get; set; }
			public DateOnly EndDate { get; set; }
		}

		private class UpdateCycleDto
		{
			public DateOnly StartDate { get; set; }
			public DateOnly EndDate { get; set; }
		}
	}
}


