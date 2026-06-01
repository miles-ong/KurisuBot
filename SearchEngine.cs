//using DSharpPlus.Commands.Processors.SlashCommands;
//using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
//using DSharpPlus.Entities;
//using Meilisearch;
//using System.Diagnostics;

//namespace KurisuBot
//{
//    internal static class SearchEngine
//    {
//        private static MeilisearchClient? _client;

//        private static Meilisearch.Index? _animeIndex;
//        private static Meilisearch.Index? _characterIndex;

//        private static string _debugTag = "[SearchEngine]";

//        internal static async Task InitializeAsync(MeilisearchConfig config)
//        {
//            _client = new MeilisearchClient(config.Url, config.Key);
//            await _client.HealthAsync();

//            _animeIndex = _client.Index("anime");
//            _characterIndex = _client.Index("character");

//            await UpdateIndexSettings(_animeIndex, ["title", "description"]);
//            await UpdateIndexSettings(_characterIndex, ["name"]);

//            Console.WriteLine($"[SearchEngine] Initialized");
//        }

//        internal static async Task<IReadOnlyList<KurisuAnime>> SearchAnimeAsync(string query, int limit = 5)
//        {
//            if(_animeIndex is null)
//            {
//                Console.WriteLine($"{_debugTag} Anime index is unintialized, exiting...");
//                throw new Exception();
//            }

//            if (string.IsNullOrWhiteSpace(query)) return [];

//            Stopwatch sw = Stopwatch.StartNew();
//            var result = await _animeIndex.SearchAsync<AnimeSearchDocument>(query, new() { Limit = limit });
//            Console.WriteLine($"{_debugTag} Anime search took {sw.ElapsedMilliseconds}ms");
//            List<KurisuAnime> animes = new();

//            foreach(var hit in result.Hits)
//            {
//                var cacheResult = KurisuAnimeCache.SearchByID(hit.Id);

//                if (cacheResult.Success) animes.Add(cacheResult.Value);
//            }

//            return animes;
//        }

//        internal static async Task AddAnimeAsync(KurisuAnime anime)
//        {
//            AnimeSearchDocument document = new AnimeSearchDocument
//            {
//                Id = anime.Id,
//                Title = anime.Title,
//                Description = anime.Description,
//            };

//            await IndexAnimeDocumentAsync(document);
//        }

//        internal static async Task DeleteAnimeAsync(KurisuAnime anime)
//        {
//            await DeleteDocumentAsync(anime.Id, _animeIndex);
//        }

//        internal static async Task<IReadOnlyList<KurisuCharacter>> SearchCharacterAsync(string query, int limit = 5)
//        {
//            if (_characterIndex is null)
//            {
//                Console.WriteLine($"{_debugTag} Character index is unintialized, exiting...");
//                throw new Exception();
//            }

//            if (string.IsNullOrWhiteSpace(query)) return [];

//            Stopwatch sw = Stopwatch.StartNew();
//            var result = await _characterIndex.SearchAsync<CharacterSearchDocument>(query, new() { Limit = limit });
//            Console.WriteLine($"{_debugTag} Character search took {sw.ElapsedMilliseconds}ms");
//            List<KurisuCharacter> characters = new();

//            foreach(var hit in result.Hits)
//            {
//                var cacheResult = KurisuCharacterCache.SearchByID(hit.Id);

//                if (cacheResult.Success) characters.Add(cacheResult.Value);
//            }

//            return characters;
//        }

//        internal static async Task AddCharacterAsync(KurisuCharacter character)
//        {
//            CharacterSearchDocument document = new CharacterSearchDocument
//            {
//                Id = character.Id,
//                Name = character.Name
//            };

//            await IndexCharacterDocumentAsync(document);
//        }

//        internal static async Task DeleteCharacterAsync(KurisuCharacter character)
//        {
//            await DeleteDocumentAsync(character.Id, _characterIndex);
//        }

//        private static async Task IndexAnimeDocumentAsync(AnimeSearchDocument document)
//        {
//            try
//            {
//                if (_animeIndex is null)
//                {
//                    Console.WriteLine($"{_debugTag} Anime index is unintialized, exiting...");
//                    throw new Exception();
//                }

//                await _animeIndex.AddDocumentsAsync([document]);
//                Console.WriteLine($"{_debugTag} Successfully indexed anime '{document.Title}' (ID: {document.Id})");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"{_debugTag} Failed to index anime '{document.Title}' (ID: {document.Id}): {ex.Message}");
//            }
//        }

//        private static async Task IndexCharacterDocumentAsync(CharacterSearchDocument document)
//        {
//            try
//            {
//                if (_characterIndex is null)
//                {
//                    Console.WriteLine($"{_debugTag} Character index is unintialized, exiting...");
//                    throw new Exception();
//                }

//                await _characterIndex.AddDocumentsAsync([document]);
//                Console.WriteLine($"{_debugTag} Successfully indexed character '{document.Name}' (ID: {document.Id})");
//            }
//            catch(Exception ex)
//            {
//                Console.WriteLine($"{_debugTag} Failed to index character '{document.Name}' (ID: {document.Id}): {ex.Message}");
//            }
//        }

//        private static async Task DeleteDocumentAsync(int id, Meilisearch.Index? index)
//        {
//            try
//            {
//                if (index is null)
//                {
//                    throw new Exception("Index uninitialized");
//                }

//                await index.DeleteDocumentsAsync([id]);
//                Console.WriteLine("[SearchEngine] Successfully deleted document");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[SearchEngine] Failed to delete document: {ex.Message}");
//            }
//        }

//        private static async Task UpdateIndexSettings(Meilisearch.Index index, string[] searchable, string[]? filterable = null)
//        {
//            await index.UpdateSettingsAsync(new()
//            {
//                SearchableAttributes = searchable,
//                FilterableAttributes = filterable ?? ["id"],
//                RankingRules = ["words", "typo", "proximity", "attribute", "exactness"]
//            });
//        }

//        private sealed class AnimeSearchDocument
//        {
//            public int Id { get; set; }
//            public string Title { get; set; } = "";
//            public string Description { get; set; } = "";
//        }

//        private sealed class CharacterSearchDocument
//        {
//            public int Id { get; set; }
//            public string Name { get; set; } = "";
//        }
//    }

//    public class KurisuAnimeAutoComplete : IAutoCompleteProvider
//    {
//        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
//        {
//            string query = context.UserInput ?? "";

//            if (string.IsNullOrWhiteSpace(query)) return [];

//            var results = await SearchEngine.SearchAnimeAsync(query, 10);

//            return results.Select(a => new DiscordAutoCompleteChoice(a.Title, a.Id));
//        }
//    }

//    public class KurisuCharacterAutoComplete : IAutoCompleteProvider
//    {
//        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
//        {
//            string query = context.UserInput ?? "";

//            if (string.IsNullOrWhiteSpace(query)) return [];

//            var results = await SearchEngine.SearchCharacterAsync(query, 10);

//            return results.Select(c => new DiscordAutoCompleteChoice(c.Name, c.Id));
//        }
//    }
//}
