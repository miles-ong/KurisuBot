using KurisuBot.AniList;

namespace KurisuBot.Core.Quiz.DataSources
{
    internal sealed class AniListQuizDataSource : IQuizDataSource
    {
        private readonly AniListService _anilistService;

        public AniListQuizDataSource(AniListService anilist)
        {
            _anilistService = anilist;
        }

        public async Task<QuizAnimePool> GetAnimePoolAsync(int pageIndex, string? genre = null)
        {
            AniListAnimePage page = await _anilistService.GetAnimePageAsync(pageIndex, genre);

            return new QuizAnimePool
            {
                Animes = page.Animes.Select(a => new QuizAnime
                {
                    Title = a.Title,
                    Description = a.Description,
                    ImageUrl = a.ImageUrl,
                    Characters = a.Characters.Select(c => new QuizCharacter
                    {
                        Name = c.Name,
                        ImageUrl = c.ImageUrl
                    })
                    .ToList()
                })
                .ToList()
            };
        }
    }
}
