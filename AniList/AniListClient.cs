using System.Text;
using System.Text.Json;

namespace KurisuBot.AniList
{
    internal sealed class AniListClient
    {
        private readonly HttpClient _httpClient;
        private const string _endpoint = "https://graphql.anilist.co";

        public AniListClient(HttpClient? httpClient)
        {
            _httpClient = httpClient ?? new();
        }

        public async Task<List<AniListAnime>> GetAnimePageAsync(int pageNumber, string? genre, string sortFilter)
        {
            string query = BuildPageQuery(genre, sortFilter);
            var variables = new Dictionary<string, object> { { "page", pageNumber } };

            if (!string.IsNullOrWhiteSpace(genre)) variables["genre"] = genre;

            JsonElement media = await PostAsync(query, variables, "Page", "media");

            return ParseAnimeList(media);
        }

        private async Task<JsonElement> PostAsync(string query, Dictionary<string, object> variables, params string[] path)
        {
            var reqBody = new { query, variables };
            StringContent content = new(JsonSerializer.Serialize(reqBody), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(_endpoint, content);
            string jsonString = await response.Content.ReadAsStringAsync();

            using JsonDocument doc = JsonDocument.Parse(jsonString);
            JsonElement element = doc.RootElement.GetProperty("data");

            foreach (string key in path)
            {
                element = element.GetProperty(key);
            }

            return element.Clone();
        }

        private List<AniListAnime> ParseAnimeList(JsonElement mediaArray)
        {
            List<AniListAnime> result = new();

            foreach (JsonElement anime in mediaArray.EnumerateArray())
            {
                result.Add(ParseAnime(anime));
            }

            return result;
        }

        private AniListAnime ParseAnime(JsonElement info)
        {
            string[] genres = info.TryGetProperty("genres", out var g)
                ? g.EnumerateArray().Take(3).Select(x => x.GetString() ?? "").ToArray()
                : new string[3];

            List<AniListCharacter> characters = new();

            if (info.TryGetProperty("characters", out var chars) && chars.TryGetProperty("edges", out var edges))
            {
                foreach (JsonElement edge in edges.EnumerateArray())
                {
                    characters.Add(ParseCharacter(edge.GetProperty("node")));
                }
            }

            return new AniListAnime
            {
                Id = info.GetProperty("id").GetInt32(),
                Title = ParseTitle(info),
                Description = ParseDescription(info),
                ImageUrl = ParseImageUrl(info, "coverImage", "large", "medium"),
                Popularity = info.TryGetProperty("popularity", out var pop) && pop.ValueKind == JsonValueKind.Number ? pop.GetInt32() : 0,
                AverageScore = info.TryGetProperty("averageScore", out var score) && score.ValueKind == JsonValueKind.Number ? score.GetInt32() : 0,
                Genres = genres,
                Characters = characters
            };
        }

        private AniListCharacter ParseCharacter(JsonElement info)
        {
            return new AniListCharacter
            {
                Id = info.GetProperty("id").GetInt32(),
                Name = ParseCharacterName(info),
                ImageUrl = ParseImageUrl(info, "image", "large", "medium"),
                Favourites = info.TryGetProperty("favourites", out var fav) && fav.ValueKind == JsonValueKind.Number ? fav.GetInt32() : 0
            };
        }

        private static string ParseTitle(JsonElement anime)
        {
            if (!anime.TryGetProperty("title", out var title)) return "Unknown Anime";

            foreach (string key in new[] { "english", "romaji", "native" })
            {
                if (title.TryGetProperty(key, out var el) && el.GetString() is { Length: > 0 } val) return val;
            }

            return "Unknown Anime";
        }

        private static string ParseDescription(JsonElement anime)
        {
            if (anime.TryGetProperty("description", out var desc) && desc.GetString() is { Length: > 0 } raw)
                return System.Text.RegularExpressions.Regex.Replace(raw, "<.*?>", string.Empty);

            return "Description not found.";
        }

        private static string? ParseImageUrl(JsonElement element, string containerKey, params string[] preferenceOrder)
        {
            if (!element.TryGetProperty(containerKey, out var container)) return null;

            foreach (string key in preferenceOrder)
            {
                if (container.TryGetProperty(key, out var el) && el.GetString() is { Length: > 0 } val) return val;
            }

            return null;
        }
        
        private static string ParseCharacterName(JsonElement character)
        {
            if (!character.TryGetProperty("name", out var name)) return "Unknown Character";

            foreach (string key in new[] { "full", "native" })
            {
                if (name.TryGetProperty(key, out var el) && el.GetString() is { Length: > 0 } val) return val;
            }

            if (name.TryGetProperty("alternative", out var alt) &&
                alt.ValueKind == JsonValueKind.Array &&
                alt.GetArrayLength() > 0 &&
                alt[0].GetString() is { Length: > 0 } altVal)
                return altVal;

            return "Unknown Character";
        }

        private static string BuildPageQuery(string? genre, string sortFilter)
        {
            string mediaArgs = string.IsNullOrEmpty(genre)
                ? $"type: ANIME, sort: {sortFilter}, isAdult: false, format_in: [TV, ONA, MOVIE]"
                : $"type: ANIME, sort: {sortFilter}, isAdult: false, genre_in: [$genre], format_in: [TV, ONA, MOVIE]";

            return $$"""
                query ($page: Int{{(string.IsNullOrEmpty(genre) ? "" : ", $genre: String")}}) {
                    Page(page: $page, perPage: 50) {
                        media({{mediaArgs}}) {
                            id
                            title { romaji english native }
                            description(asHtml: false)
                            coverImage { large medium }
                            popularity
                            averageScore
                            genres
                            characters(page: 1, perPage: 50, sort: FAVOURITES_DESC) {
                                edges {
                                    node {
                                        id
                                        name { full native alternative }
                                        image { large medium }
                                        favourites
                                    }
                                }
                            }
                        }
                    }
                }
            """;
        }
    }
}
