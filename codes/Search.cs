using System.Globalization;
using System.Text;
using System.Text.Json;
using WebComic.Models;
using SkyNet;

namespace WebComic.codes
{
    public class Search : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Search | Panelmoon");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Search series, episodes and artists on Panelmoon.");

            SiteData site = await LoadSite();
            DateTime today = DateTime.Today;
            string q = Clip(QueryValue("q"));
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_q}", HtmlEncode(q))
                .Replace("{plhd_results}", Results(site, q, today));
        }

        public async Task<ApiResponse> Find()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            string q = Clip(GetDataValue("term"));
            response.SetElementContents("se-results", Results(site, q, DateTime.Today));
            response.ExecuteScript("SearchJs.found('" + JsText(q) + "');");
            return response;
        }

        private static string Results(SiteData site, string q, DateTime today)
        {
            StringBuilder sb = new StringBuilder();
            if (q.Length < 2)
            {
                sb.Append("<p class=\"se-rsum\">Type at least 2 characters to search series, episodes and artists.</p><h2 class=\"se-h2\">Popular right now</h2><div class=\"se-cards se-cards-4\">" + SeriesGrid(site, site.Series.OrderByDescending(s => s.Views).Take(4).ToList(), today) + "</div>");
                return sb.ToString();
            }
            List<ComicSeries> series = site.Series.Where(s => Has(s.Title, q) || Has(s.Synopsis, q) || Has(GenreLabel(site, s.Genre), q) || s.Tags.Any(t => Has(t, q)) || Has(ArtistName(site, s), q)).OrderByDescending(s => s.Views).ToList();
            List<(ComicSeries s, ComicEpisode e)> eps = new List<(ComicSeries, ComicEpisode)>();
            foreach (ComicSeries s in site.Series)
            {
                foreach (ComicEpisode e in s.Episodes.Where(e => Has(e.Title, q)))
                {
                    eps.Add((s, e));
                }
            }
            List<Artist> artists = site.Artists.Where(a => Has(a.Name, q) || Has(a.Handle, q) || Has(a.Location, q)).ToList();
            int total = series.Count + eps.Count + artists.Count;
            sb.Append("<p class=\"se-rsum\">" + (total == 0 ? "No results for &ldquo;" + HtmlEncode(q) + "&rdquo;. Try a genre like <a href=\"Search?q=Fantasy\">Fantasy</a> or a tag like <a href=\"Search?q=Cozy\">Cozy</a>." : CountText(total, "result", "results") + " for &ldquo;" + HtmlEncode(q) + "&rdquo;") + "</p>");
            if (series.Count > 0)
            {
                sb.Append("<h2 class=\"se-h2\">Series <small>" + series.Count + "</small></h2><div class=\"se-cards se-cards-4\">" + SeriesGrid(site, series, today) + "</div>");
            }
            if (eps.Count > 0)
            {
                sb.Append("<h2 class=\"se-h2\">Episodes <small>" + eps.Count + "</small></h2><div class=\"se-eplist\">");
                foreach ((ComicSeries s, ComicEpisode e) in eps)
                {
                    sb.Append("<a class=\"se-ep\" href=\"Episode?s=" + s.Key + "&e=" + e.No + "\"><span class=\"se-ep-th\"><img src=\"images/thumbs/" + s.Key + "/" + e.No + ".webp\" alt=\"\" loading=\"lazy\"></span><span class=\"se-ep-t\"><small>" + HtmlEncode(s.Title) + " &middot; Episode " + e.No + "</small><b>" + HtmlEncode(e.Title) + "</b><em>" + DateText(EpisodeDate(s, e.No, today), today) + "</em></span><span class=\"se-ep-s\"><span>&#9829; " + Compact(e.Likes) + "</span></span></a>");
                }
                sb.Append("</div>");
            }
            if (artists.Count > 0)
            {
                sb.Append("<h2 class=\"se-h2\">Artists <small>" + artists.Count + "</small></h2><div class=\"se-amini-row\">");
                foreach (Artist a in artists)
                {
                    sb.Append(ArtistMini(site, a));
                }
                sb.Append("</div>");
            }
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
            string list = total == 0 ? "<div class=\"se-sg-none\">Nothing matches &ldquo;" + HtmlEncode(q) + "&rdquo;</div>"
                : sb.ToString() + "<a class=\"se-sg-foot\" href=\"Search?q=" + Uri.EscapeDataString(q) + "\">See all " + CountText(total, "result", "results") + " &rarr;</a>";
            response.SetElementContents("se-sugg", list);
            response.ExecuteScript("SearchJs.openSugg();");
            return response;
        }

        public async Task<ApiResponse> Newsletter()
        {
            ApiResponse response = new ApiResponse();
            string email = (GetDataValue("email") ?? string.Empty).Trim();
            if (!IsEmail(email))
            {
                response.SetElementContents("se-nl-msg", "<span class=\"se-err\">Please enter a valid email address.</span>");
                return response;
            }
            response.SetElementContents("se-nl-msg", "<span class=\"se-ok\">You&rsquo;re in! New-episode picks will go to " + HtmlEncode(email) + ". (Demo only &mdash; nothing was stored.)</span>");
            response.ExecuteScript("SearchJs.newsletterDone();");
            return response;
        }

        private static string SeriesCard(SiteData site, ComicSeries s, DateTime today)
        {
            ComicEpisode? last = s.Episodes.OrderByDescending(e => e.No).FirstOrDefault();
            long likes = s.Episodes.Sum(e => e.Likes);
            Genre? g = site.Genres.FirstOrDefault(x => x.Key == s.Genre);
            return "<a class=\"se-card\" href=\"Series?s=" + s.Key + "\"><span class=\"se-card-img\"><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\">"
                + (IsUp(s, today) ? "<i class=\"se-up\">UP</i>" : string.Empty) + (s.Status == "completed" ? "<i class=\"se-done\">Completed</i>" : string.Empty) + "</span>"
                + "<span class=\"se-card-g\" style=\"color:" + (g == null ? "#666" : g.Color) + "\">" + GenreLabel(site, s.Genre) + "</span><b class=\"se-card-t\">" + HtmlEncode(s.Title) + "</b>"
                + "<span class=\"se-card-a\">" + HtmlEncode(ArtistName(site, s)) + "</span><span class=\"se-card-m\"><span>&#9829; " + Compact(likes) + "</span><span>" + ScheduleText(site, s) + "</span></span>"
                + (last == null ? string.Empty : "<span class=\"se-card-ep\">Ep. " + last.No + " &middot; " + DateText(EpisodeDate(s, last.No, today), today) + "</span>") + "</a>";
        }

        private static string SeriesGrid(SiteData site, List<ComicSeries> list, DateTime today)
        {
            if (list.Count == 0)
            {
                return "<p class=\"se-empty\">No series to show. <a href=\"Genres\">Browse all genres</a>.</p>";
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
                sb.Append("<a class=\"se-new\" href=\"Episode?s=" + s.Key + "&e=" + e.No + "\"><span class=\"se-new-th\"><img src=\"images/thumbs/" + s.Key + "/" + e.No + ".webp\" alt=\"\" loading=\"lazy\">" + (fresh ? "<i class=\"se-up\">UP</i>" : string.Empty) + "</span>");
                sb.Append("<span class=\"se-new-t\"><small>" + HtmlEncode(s.Title) + "</small><b>Ep. " + e.No + " &middot; " + HtmlEncode(e.Title) + "</b><em>" + DateText(d, today) + " &middot; &#9829; " + Compact(e.Likes) + "</em></span></a>");
            }
            return sb.ToString();
        }

        private static string DayTabs(SiteData site, string day, DateTime today)
        {
            StringBuilder sb = new StringBuilder();
            string now = DayKey(today);
            foreach (DayItem d in site.Days)
            {
                sb.Append("<button type=\"button\" role=\"tab\" class=\"se-day" + (d.Key == day ? " se-act" : string.Empty) + "\" aria-selected=\"" + (d.Key == day ? "true" : "false") + "\" onclick=\"SearchJs.day('" + d.Key + "')\">" + d.Short + (d.Key == now ? "<i>Today</i>" : string.Empty) + "</button>");
            }
            sb.Append("<button type=\"button\" role=\"tab\" class=\"se-day" + (day == "done" ? " se-act" : string.Empty) + "\" aria-selected=\"" + (day == "done" ? "true" : "false") + "\" onclick=\"SearchJs.day('done')\">Completed</button>");
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
                sb.Append("<a class=\"se-rank\" href=\"Series?s=" + s.Key + "\"><b class=\"se-rank-n\">" + i + "</b><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\"><span><b>" + HtmlEncode(s.Title) + "</b><small>" + GenreLabel(site, s.Genre) + " &middot; " + Compact(s.Views) + " views</small></span>" + (IsUp(s, today) ? "<i class=\"se-up\">UP</i>" : string.Empty) + "</a>");
                i++;
            }
            return sb.ToString();
        }

        private static string ArtistMini(SiteData site, Artist a)
        {
            int n = site.Series.Count(s => s.Artist == a.Key);
            return "<a class=\"se-amini\" href=\"Artists?a=" + a.Key + "\"><img src=\"images/artists/" + a.Key + ".webp\" alt=\"\" loading=\"lazy\"><b>" + HtmlEncode(a.Name) + "</b><small>" + Compact(a.Followers) + " followers &middot; " + CountText(n, "series", "series") + "</small></a>";
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
            return "<section class=\"se-wrap se-nf\"><h1>" + msg + "</h1><p>It may have moved, or the link is incomplete.</p><a class=\"se-btn\" href=\"Genres\">Browse all series</a><h2 class=\"se-h2\">Popular right now</h2><div class=\"se-cards se-cards-4\">"
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
            return "<a class=\"se-sg\" href=\"" + href + "\"><img src=\"" + img + "\" alt=\"\"><span><b>" + HtmlEncode(title) + "</b><small>" + HtmlEncode(sub) + "</small></span></a>";
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
