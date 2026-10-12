using System.Globalization;
using System.Text;
using System.Text.Json;
using WebComic.Models;
using SkyNet;

namespace WebComic.codes
{
    public class Artists : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Artists | Panelmoon");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Meet the artists behind Panelmoon series.");

            SiteData site = await LoadSite();
            DateTime today = DateTime.Today;
            string key = Pick(QueryValue("a"), site.Artists.Select(a => a.Key));
            string genre = Pick(QueryValue("g"), site.Genres.Select(g => g.Key));
            Artist? cur = site.Artists.FirstOrDefault(a => a.Key == key);
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_profile}", cur == null ? string.Empty : Profile(site, cur, today))
                .Replace("{plhd_listtitle}", cur == null ? "Artists" : "More artists")
                .Replace("{plhd_achips}", ArtistChips(site, genre))
                .Replace("{plhd_agrid}", ArtistGrid(site, ArtistList(site, genre, key)))
                .Replace("{plhd_acount}", CountText(ArtistList(site, genre, key).Count, "artist", "artists"));
            if (cur != null)
            {
                HtmlDoc.SetTitle(cur.Name + " | Panelmoon artists");
            }
        }

        public async Task<ApiResponse> Filter()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            string genre = Pick(GetDataValue("genre"), site.Genres.Select(g => g.Key));
            string key = Pick(GetDataValue("artist"), site.Artists.Select(a => a.Key));
            List<Artist> list = ArtistList(site, genre, key);
            response.SetElementContents("ar-achips", ArtistChips(site, genre));
            response.SetElementContents("ar-agrid", ArtistGrid(site, list));
            response.SetElementContents("ar-acount", CountText(list.Count, "artist", "artists"));
            return response;
        }

        public async Task<ApiResponse> Follow()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            Artist? a = site.Artists.FirstOrDefault(x => x.Key == GetDataValue("artist"));
            if (a == null)
            {
                return response;
            }
            bool on = GetDataValue("on") == "1";
            long n = a.Followers + (on ? 1 : 0);
            response.SetElementContents("ar-fcount-" + a.Key, Compact(n));
            response.ExecuteScript("ArtistsJs.followed('" + a.Key + "'," + (on ? "true" : "false") + ");");
            return response;
        }

        private static List<Artist> ArtistList(SiteData site, string genre, string exclude)
        {
            return site.Artists.Where(a => a.Key != exclude && (genre == string.Empty || site.Series.Any(s => s.Artist == a.Key && s.Genre == genre)))
                .OrderByDescending(a => a.Followers).ToList();
        }

        private static string ArtistChips(SiteData site, string genre)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<button type=\"button\" class=\"ar-gchip" + (genre == string.Empty ? " ar-act" : string.Empty) + "\" onclick=\"ArtistsJs.artistGenre('')\">All</button>");
            foreach (Genre g in site.Genres)
            {
                sb.Append("<button type=\"button\" class=\"ar-gchip" + (genre == g.Key ? " ar-act" : string.Empty) + "\" style=\"--ar-g:" + g.Color + "\" onclick=\"ArtistsJs.artistGenre('" + g.Key + "')\">" + HtmlEncode(g.Label) + "</button>");
            }
            return sb.ToString();
        }

        private static string ArtistGrid(SiteData site, List<Artist> list)
        {
            if (list.Count == 0)
            {
                return "<p class=\"ar-empty\">No artists in this genre yet.</p>";
            }
            StringBuilder sb = new StringBuilder();
            foreach (Artist a in list)
            {
                List<ComicSeries> mine = site.Series.Where(s => s.Artist == a.Key).ToList();
                sb.Append("<article class=\"ar-acard\"><a class=\"ar-acard-top\" href=\"Artists?a=" + a.Key + "\"><img src=\"images/artists/" + a.Key + ".webp\" alt=\"\" loading=\"lazy\">");
                sb.Append("<span><b>" + HtmlEncode(a.Name) + "</b><small>" + HtmlEncode(a.Handle) + " &middot; " + HtmlEncode(a.Location) + "</small></span></a>");
                sb.Append("<p>" + HtmlEncode(a.Bio) + "</p><div class=\"ar-acard-series\">");
                foreach (ComicSeries s in mine)
                {
                    sb.Append("<a href=\"Series?s=" + s.Key + "\"><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\"><span>" + HtmlEncode(s.Title) + "<small>" + GenreLabel(site, s.Genre) + " &middot; " + ScheduleText(site, s) + "</small></span></a>");
                }
                sb.Append("</div><div class=\"ar-acard-foot\"><span><b id=\"ar-fcount-" + a.Key + "\">" + Compact(a.Followers) + "</b> followers</span>");
                sb.Append("<button type=\"button\" class=\"ar-btn ar-btn-s ar-btn-o ar-follow\" data-a=\"" + a.Key + "\" onclick=\"ArtistsJs.follow(this)\">Follow</button></div></article>");
            }
            return sb.ToString();
        }

        private static string Profile(SiteData site, Artist a, DateTime today)
        {
            List<ComicSeries> mine = site.Series.Where(s => s.Artist == a.Key).ToList();
            long views = mine.Sum(s => s.Views);
            long likes = mine.Sum(s => s.Episodes.Sum(e => e.Likes));
            StringBuilder sb = new StringBuilder();
            sb.Append("<section class=\"ar-prof\"><div class=\"ar-wrap ar-prof-in\"><img class=\"ar-prof-av\" src=\"images/artists/" + a.Key + ".webp\" alt=\"\">");
            sb.Append("<div class=\"ar-prof-t\"><div class=\"ar-eyebrow\">Artist</div><h1>" + HtmlEncode(a.Name) + "</h1><p class=\"ar-prof-h\">" + HtmlEncode(a.Handle) + " &middot; " + HtmlEncode(a.Location) + " &middot; on Panelmoon since " + a.Joined + "</p>");
            sb.Append("<p class=\"ar-prof-bio\">" + HtmlEncode(a.Bio) + "</p>");
            sb.Append("<div class=\"ar-prof-stats\"><div><b id=\"ar-fcount-" + a.Key + "\">" + Compact(a.Followers) + "</b><span>followers</span></div><div><b>" + Compact(views) + "</b><span>views</span></div><div><b>" + Compact(likes) + "</b><span>likes</span></div><div><b>" + mine.Count + "</b><span>" + (mine.Count == 1 ? "series" : "series") + "</span></div></div>");
            sb.Append("<button type=\"button\" class=\"ar-btn ar-follow\" data-a=\"" + a.Key + "\" onclick=\"ArtistsJs.follow(this)\">Follow</button></div></div>");
            sb.Append("<div class=\"ar-wrap\"><h2 class=\"ar-h2\">Series by " + HtmlEncode(a.Name) + "</h2><div class=\"ar-cards\">" + SeriesGrid(site, mine, today) + "</div></div></section>");
            return sb.ToString();
        }

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public async Task<ApiResponse> Suggest()
        {
            ApiResponse response = new ApiResponse();
            string q = Clip(GetDataValue("term"));
            SiteData site = await LoadSite();
            StringBuilder sb = new StringBuilder();
            int total = 0;
            if (q.Length >= 2)
            {
                foreach (ComicSeries s in site.Series.Where(s => Has(s.Title, q) || Has(GenreLabel(site, s.Genre), q) || s.Tags.Any(t => Has(t, q)) || Has(ArtistName(site, s), q)).OrderByDescending(s => s.Views))
                {
                    total++;
                    if (total <= 6)
                    {
                        sb.Append(Sugg("images/covers/" + s.Key + ".webp", s.Title, GenreLabel(site, s.Genre) + " · " + ArtistName(site, s) + " · " + s.Episodes.Count + " episodes", "Series?s=" + s.Key));
                    }
                }
                foreach (ComicSeries s in site.Series)
                {
                    foreach (ComicEpisode e in s.Episodes.Where(e => Has(e.Title, q)))
                    {
                        total++;
                        if (total <= 6)
                        {
                            sb.Append(Sugg("images/thumbs/" + s.Key + "/" + e.No + ".webp", e.Title, s.Title + " · Episode " + e.No, "Episode?s=" + s.Key + "&e=" + e.No));
                        }
                    }
                }
                foreach (Artist a in site.Artists.Where(a => Has(a.Name, q) || Has(a.Handle, q)))
                {
                    total++;
                    if (total <= 6)
                    {
                        sb.Append(Sugg("images/artists/" + a.Key + ".webp", a.Name, "Artist · " + a.Handle, "Artists?a=" + a.Key));
                    }
                }
            }
            string list = total == 0 ? "<div class=\"ar-sg-none\">Nothing matches &ldquo;" + HtmlEncode(q) + "&rdquo;</div>"
                : sb.ToString() + "<a class=\"ar-sg-foot\" href=\"Search?q=" + Uri.EscapeDataString(q) + "\">See all " + CountText(total, "result", "results") + " &rarr;</a>";
            response.SetElementContents("ar-sugg", list);
            response.ExecuteScript("ArtistsJs.openSugg();");
            return response;
        }

        public async Task<ApiResponse> Newsletter()
        {
            ApiResponse response = new ApiResponse();
            string email = (GetDataValue("email") ?? string.Empty).Trim();
            if (!IsEmail(email))
            {
                response.SetElementContents("ar-nl-msg", "<span class=\"ar-err\">Please enter a valid email address.</span>");
                return response;
            }
            response.SetElementContents("ar-nl-msg", "<span class=\"ar-ok\">You&rsquo;re in! New-episode picks will go to " + HtmlEncode(email) + ". (Demo only &mdash; nothing was stored.)</span>");
            response.ExecuteScript("ArtistsJs.newsletterDone();");
            return response;
        }

        private static string SeriesCard(SiteData site, ComicSeries s, DateTime today)
        {
            ComicEpisode? last = s.Episodes.OrderByDescending(e => e.No).FirstOrDefault();
            long likes = s.Episodes.Sum(e => e.Likes);
            Genre? g = site.Genres.FirstOrDefault(x => x.Key == s.Genre);
            return "<a class=\"ar-card\" href=\"Series?s=" + s.Key + "\"><span class=\"ar-card-img\"><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\">"
                + (IsUp(s, today) ? "<i class=\"ar-up\">UP</i>" : string.Empty) + (s.Status == "completed" ? "<i class=\"ar-done\">Completed</i>" : string.Empty) + "</span>"
                + "<span class=\"ar-card-g\" style=\"color:" + (g == null ? "#666" : g.Color) + "\">" + GenreLabel(site, s.Genre) + "</span><b class=\"ar-card-t\">" + HtmlEncode(s.Title) + "</b>"
                + "<span class=\"ar-card-a\">" + HtmlEncode(ArtistName(site, s)) + "</span><span class=\"ar-card-m\"><span>&#9829; " + Compact(likes) + "</span><span>" + ScheduleText(site, s) + "</span></span>"
                + (last == null ? string.Empty : "<span class=\"ar-card-ep\">Ep. " + last.No + " &middot; " + DateText(EpisodeDate(s, last.No, today), today) + "</span>") + "</a>";
        }

        private static string SeriesGrid(SiteData site, List<ComicSeries> list, DateTime today)
        {
            if (list.Count == 0)
            {
                return "<p class=\"ar-empty\">No series to show. <a href=\"Genres\">Browse all genres</a>.</p>";
            }
            StringBuilder sb = new StringBuilder();
            foreach (ComicSeries s in list)
            {
                sb.Append(SeriesCard(site, s, today));
            }
            return sb.ToString();
        }

        private static string NewArrivals(SiteData site, DateTime today, int take)
        {
            List<(ComicSeries s, ComicEpisode e, DateTime d)> all = new List<(ComicSeries, ComicEpisode, DateTime)>();
            foreach (ComicSeries s in site.Series)
            {
                foreach (ComicEpisode e in s.Episodes)
                {
                    all.Add((s, e, EpisodeDate(s, e.No, today)));
                }
            }
            StringBuilder sb = new StringBuilder();
            foreach ((ComicSeries s, ComicEpisode e, DateTime d) in all.Where(x => x.d <= today).OrderByDescending(x => x.d).ThenByDescending(x => x.s.Views).Take(take))
            {
                bool fresh = (today - d).TotalDays <= 1 && s.Status == "ongoing";
                sb.Append("<a class=\"ar-new\" href=\"Episode?s=" + s.Key + "&e=" + e.No + "\"><span class=\"ar-new-th\"><img src=\"images/thumbs/" + s.Key + "/" + e.No + ".webp\" alt=\"\" loading=\"lazy\">" + (fresh ? "<i class=\"ar-up\">UP</i>" : string.Empty) + "</span>");
                sb.Append("<span class=\"ar-new-t\"><small>" + HtmlEncode(s.Title) + "</small><b>Ep. " + e.No + " &middot; " + HtmlEncode(e.Title) + "</b><em>" + DateText(d, today) + " &middot; &#9829; " + Compact(e.Likes) + "</em></span></a>");
            }
            return sb.ToString();
        }

        private static string DayTabs(SiteData site, string day, DateTime today)
        {
            StringBuilder sb = new StringBuilder();
            string now = DayKey(today);
            foreach (DayItem d in site.Days)
            {
                sb.Append("<button type=\"button\" role=\"tab\" class=\"ar-day" + (d.Key == day ? " ar-act" : string.Empty) + "\" aria-selected=\"" + (d.Key == day ? "true" : "false") + "\" onclick=\"ArtistsJs.day('" + d.Key + "')\">" + d.Short + (d.Key == now ? "<i>Today</i>" : string.Empty) + "</button>");
            }
            sb.Append("<button type=\"button\" role=\"tab\" class=\"ar-day" + (day == "done" ? " ar-act" : string.Empty) + "\" aria-selected=\"" + (day == "done" ? "true" : "false") + "\" onclick=\"ArtistsJs.day('done')\">Completed</button>");
            return sb.ToString();
        }

        private static string DayTitle(SiteData site, string day, DateTime today)
        {
            if (day == "done")
            {
                return "Completed series &mdash; read them start to finish";
            }
            DayItem? d = site.Days.FirstOrDefault(x => x.Key == day);
            return (day == DayKey(today) ? "Updating today, " : "Updates every ") + (d == null ? string.Empty : d.Name) + " &mdash; daily series plus that day&rsquo;s weekly series";
        }

        private static string DayGrid(SiteData site, string day, DateTime today)
        {
            List<ComicSeries> list = day == "done"
                ? site.Series.Where(s => s.Status == "completed").ToList()
                : site.Series.Where(s => s.Status == "ongoing" && (s.Schedule == "daily" || s.Day == day)).OrderBy(s => s.Schedule == "daily" ? 1 : 0).ThenByDescending(s => s.Views).ToList();
            return SeriesGrid(site, list, today);
        }

        private static string TopList(SiteData site, DateTime today)
        {
            StringBuilder sb = new StringBuilder();
            int i = 1;
            foreach (ComicSeries s in site.Series.OrderByDescending(s => s.Views))
            {
                sb.Append("<a class=\"ar-rank\" href=\"Series?s=" + s.Key + "\"><b class=\"ar-rank-n\">" + i + "</b><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\"><span><b>" + HtmlEncode(s.Title) + "</b><small>" + GenreLabel(site, s.Genre) + " &middot; " + Compact(s.Views) + " views</small></span>" + (IsUp(s, today) ? "<i class=\"ar-up\">UP</i>" : string.Empty) + "</a>");
                i++;
            }
            return sb.ToString();
        }

        private static string ArtistMini(SiteData site, Artist a)
        {
            int n = site.Series.Count(s => s.Artist == a.Key);
            return "<a class=\"ar-amini\" href=\"Artists?a=" + a.Key + "\"><img src=\"images/artists/" + a.Key + ".webp\" alt=\"\" loading=\"lazy\"><b>" + HtmlEncode(a.Name) + "</b><small>" + Compact(a.Followers) + " followers &middot; " + CountText(n, "series", "series") + "</small></a>";
        }

        private static string CoverStack(SiteData site)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ComicSeries s in site.Series.OrderByDescending(s => s.Views).Take(5))
            {
                sb.Append("<img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\">");
            }
            return sb.ToString();
        }

        private static string NotFound(string msg, SiteData site, DateTime today)
        {
            return "<section class=\"ar-wrap ar-nf\"><h1>" + msg + "</h1><p>It may have moved, or the link is incomplete.</p><a class=\"ar-btn\" href=\"Genres\">Browse all series</a><h2 class=\"ar-h2\">Popular right now</h2><div class=\"ar-cards ar-cards-4\">"
                + SeriesGrid(site, site.Series.OrderByDescending(s => s.Views).Take(4).ToList(), today) + "</div></section>";
        }

        private static Artist? ArtistOf(SiteData site, ComicSeries s)
        {
            return site.Artists.FirstOrDefault(a => a.Key == s.Artist);
        }

        private static string ArtistName(SiteData site, ComicSeries s)
        {
            Artist? a = ArtistOf(site, s);
            return a == null ? string.Empty : a.Name;
        }

        private static string GenreLabel(SiteData site, string key)
        {
            return site.Genres.Where(g => g.Key == key).Select(g => g.Label).FirstOrDefault() ?? key;
        }

        private static string DayKey(DateTime d)
        {
            return new[] { "sun", "mon", "tue", "wed", "thu", "fri", "sat" }[(int)d.DayOfWeek];
        }

        private static int DayIndex(string key)
        {
            return Array.IndexOf(new[] { "sun", "mon", "tue", "wed", "thu", "fri", "sat" }, key);
        }

        private static string ScheduleText(SiteData site, ComicSeries s)
        {
            if (s.Status == "completed")
            {
                return "Completed";
            }
            if (s.Schedule == "daily")
            {
                return "Daily";
            }
            DayItem? d = site.Days.FirstOrDefault(x => x.Key == s.Day);
            return "Every " + (d == null ? s.Day : d.Short);
        }

        private static DateTime LatestDate(ComicSeries s, DateTime today)
        {
            DateTime d;
            if (s.Schedule == "daily")
            {
                d = today;
            }
            else
            {
                int back = ((int)today.DayOfWeek - Math.Max(0, DayIndex(s.Day)) + 7) % 7;
                d = today.AddDays(-back);
            }
            return s.Status == "completed" ? d.AddDays(-28) : d;
        }

        private static DateTime EpisodeDate(ComicSeries s, int no, DateTime today)
        {
            int count = s.Episodes.Count == 0 ? 0 : s.Episodes.Max(e => e.No);
            return LatestDate(s, today).AddDays(-(s.Schedule == "daily" ? 1 : 7) * (count - no));
        }

        private static DateTime NextDate(ComicSeries s, DateTime today)
        {
            return s.Schedule == "daily" ? today.AddDays(1) : LatestDate(s, today).AddDays(7);
        }

        private static bool IsUp(ComicSeries s, DateTime today)
        {
            return s.Status == "ongoing" && (today - LatestDate(s, today)).TotalDays <= 1;
        }

        private static string NextShort(SiteData site, ComicSeries s, DateTime today)
        {
            if (s.Status == "completed")
            {
                return "This series is complete";
            }
            DateTime n = NextDate(s, today);
            return "Next episode " + ((n - today).TotalDays <= 1 ? "tomorrow" : n.ToString("dddd, MMM d", Inv));
        }

        private static string NextText(SiteData site, ComicSeries s, DateTime today)
        {
            if (s.Status == "completed")
            {
                return "Completed in " + s.Episodes.Count + " episodes. Read it start to finish.";
            }
            return NextShort(site, s, today) + " &middot; " + (s.Schedule == "daily" ? "new episodes every day" : "new episodes every " + (site.Days.FirstOrDefault(d => d.Key == s.Day)?.Name ?? s.Day));
        }

        private static string DateText(DateTime d, DateTime today)
        {
            int days = (int)(today - d).TotalDays;
            if (days <= 0)
            {
                return "Today";
            }
            if (days == 1)
            {
                return "Yesterday";
            }
            if (days < 7)
            {
                return days + " days ago";
            }
            return d.ToString(d.Year == today.Year ? "MMM d" : "MMM d, yyyy", Inv);
        }

        private static string Compact(long n)
        {
            if (n >= 1000000)
            {
                return (n / 1000000d).ToString("0.#", Inv) + "M";
            }
            if (n >= 10000)
            {
                return (n / 1000d).ToString("0.#", Inv) + "K";
            }
            return n.ToString("#,0", Inv);
        }

        private static string CountText(int n, string one, string many)
        {
            return n.ToString("#,0", Inv) + " " + (n == 1 ? one : many);
        }

        private static string NameFrom(string email)
        {
            string local = email.Split('@')[0];
            string clean = new string(local.Where(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-').ToArray());
            if (clean.Length == 0)
            {
                clean = "reader";
            }
            return clean.Length > 24 ? clean.Substring(0, 24) : clean;
        }

        private static string JsText(string v)
        {
            return (v ?? string.Empty).Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("<", "\\u003c").Replace("\n", " ").Replace("\r", " ");
        }

        private static uint Hash(string s)
        {
            uint h = 2166136261;
            foreach (char c in s)
            {
                h ^= c;
                h *= 16777619;
            }
            return h;
        }

        private static string Code(string prefix, string seed)
        {
            uint h = Hash(seed);
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            StringBuilder sb = new StringBuilder(prefix + "-");
            for (int i = 0; i < 6; i++)
            {
                sb.Append(chars[(int)(h % (uint)chars.Length)]);
                h /= (uint)chars.Length;
                h ^= 2654435761u * (uint)(i + 1);
            }
            return sb.ToString();
        }

        private static string Sugg(string img, string title, string sub, string href)
        {
            return "<a class=\"ar-sg\" href=\"" + href + "\"><img src=\"" + img + "\" alt=\"\"><span><b>" + HtmlEncode(title) + "</b><small>" + HtmlEncode(sub) + "</small></span></a>";
        }

        private async Task<SiteData> LoadSite()
        {
            string file = Path.Combine(DataPath ?? string.Empty, "site.json");
            if (!File.Exists(file))
            {
                file = Path.Combine(Directory.GetCurrentDirectory(), "data", "site.json");
            }
            if (!File.Exists(file))
            {
                return new SiteData();
            }
            string json = await File.ReadAllTextAsync(file);
            JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<SiteData>(json, options) ?? new SiteData();
        }

        private static string Clip(string? value)
        {
            string q = (value ?? string.Empty).Trim();
            return q.Length > 40 ? q.Substring(0, 40) : q;
        }

        private static string Pick(string? value, IEnumerable<string> allowed)
        {
            string v = (value ?? string.Empty).Trim().ToLowerInvariant();
            return allowed.Contains(v) ? v : string.Empty;
        }

        private static int Bound(string? value, int min, int max)
        {
            int v;
            return int.TryParse((value ?? string.Empty).Trim(), NumberStyles.Integer, Inv, out v) && v >= min && v <= max ? v : 0;
        }

        private static bool Has(string text, string q)
        {
            return (text ?? string.Empty).Contains(q, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsEmail(string v)
        {
            if (v.Length < 5 || v.Length > 80 || v.Contains(' '))
            {
                return false;
            }
            int at = v.IndexOf('@');
            int dot = v.LastIndexOf('.');
            return at > 0 && at == v.LastIndexOf('@') && dot > at + 1 && dot < v.Length - 1;
        }
    }
}
