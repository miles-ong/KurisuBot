namespace KurisuBot.AniList
{
    internal sealed class AniListAnimePage
    {
        public string Genre { get; set; } = "all";
        public int PageNumber { get; set; }
        public string SortFilter { get; set; } = "POPULARITY_DESC";
        public List<AniListAnime> Animes { get; set; } = new();
    }

    internal sealed class AniListAnime
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int Popularity { get; set; }
        public int AverageScore { get; set; }
        public string[] Genres { get; set; } = new string[3];
        public List<AniListCharacter> Characters { get; set; } = new();
    }

    internal sealed class AniListCharacter
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? ImageUrl { get; set; }
        public int Favourites { get; set; }
    }
}
