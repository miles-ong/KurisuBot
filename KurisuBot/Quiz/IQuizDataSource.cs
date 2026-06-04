namespace KurisuBot.Quiz
{
    internal interface IQuizDataSource
    {
        Task<QuizAnimePool> GetAnimePoolAsync(int pageIndex, string? genre = null);
    }
    
    internal sealed class QuizAnimePool
    {
        public List<QuizAnime> Animes { get; init; } = new();
    }

    internal sealed class QuizAnime
    {
        public string Title { get; init; } = "";
        public string? Description { get; init; }
        public string? ImageUrl { get; init; }
        public List<QuizCharacter> Characters { get; init; } = new();
    }

    internal sealed class QuizCharacter
    {
        public string Name { get; init; } = "";
        public string? ImageUrl { get; init; }
    }
}
