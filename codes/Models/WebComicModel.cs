namespace WebComic.Models
{
    public class SiteData
    {
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public List<DayItem> Days { get; set; } = new List<DayItem>();
        public List<Artist> Artists { get; set; } = new List<Artist>();
        public List<ComicSeries> Series { get; set; } = new List<ComicSeries>();
        public List<Plan> Plans { get; set; } = new List<Plan>();
        public List<Faq> Faqs { get; set; } = new List<Faq>();
    }

    public class Genre
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Blurb { get; set; } = string.Empty;
    }

    public class DayItem
    {
        public string Key { get; set; } = string.Empty;
        public string Short { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class Artist
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Joined { get; set; }
        public long Followers { get; set; }
        public string Bio { get; set; } = string.Empty;
    }

    public class ComicSeries
    {
        public string Key { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Schedule { get; set; } = string.Empty;
        public string Day { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long Views { get; set; }
        public long Subscribers { get; set; }
        public string Rating { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();
        public string Theme { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Synopsis { get; set; } = string.Empty;
        public List<ComicEpisode> Episodes { get; set; } = new List<ComicEpisode>();
    }

    public class ComicEpisode
    {
        public int No { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Pages { get; set; }
        public long Views { get; set; }
        public long Likes { get; set; }
        public long Dislikes { get; set; }
        public List<ReaderComment> Comments { get; set; } = new List<ReaderComment>();
    }

    public class ReaderComment
    {
        public string User { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public long Likes { get; set; }
        public long Mins { get; set; }
    }

    public class Plan
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string Period { get; set; } = string.Empty;
        public string Lead { get; set; } = string.Empty;
        public List<string> Features { get; set; } = new List<string>();
        public string Cta { get; set; } = string.Empty;
        public bool Best { get; set; }
    }

    public class Faq
    {
        public string Q { get; set; } = string.Empty;
        public string A { get; set; } = string.Empty;
    }
}
