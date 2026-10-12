using System.Globalization;
using System.Text;
using System.Text.Json;
using WebComic.Models;
using SkyNet;

namespace WebComic.codes
{
    public class Series : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Series | Panelmoon");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Episodes, schedule and details of a Panelmoon series.");

            SiteData site = await LoadSite();
            DateTime today = DateTime.Today;
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == (QueryValue("s") ?? string.Empty).Trim().ToLowerInvariant());
            if (s == null)
            {
                HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText.Replace("{plhd_series}", NotFound("That series isn&rsquo;t on Panelmoon.", site, today)).Replace("{plhd_more}", string.Empty);
                return;
            }
            HtmlDoc.SetTitle(s.Title + " | Panelmoon");
            Artist? a = ArtistOf(site, s);
            long likes = s.Episodes.Sum(e => e.Likes);
            StringBuilder sb = new StringBuilder();
            sb.Append("<section class=\"sr-shero\" style=\"--sr-theme:" + s.Theme + "\"><img class=\"sr-shero-bg\" src=\"images/banners/" + s.Key + ".webp\" alt=\"\">");
            sb.Append("<div class=\"sr-wrap sr-shero-in\"><img class=\"sr-scover\" src=\"images/covers/" + s.Key + ".webp\" alt=\"" + HtmlEncode(s.Title) + " cover\">");
            sb.Append("<div class=\"sr-sinfo\"><div class=\"sr-chips\"><a class=\"sr-chip sr-chip-w\" href=\"Genres?g=" + s.Genre + "\">" + GenreLabel(site, s.Genre) + "</a><span class=\"sr-chip sr-chip-w\">" + ScheduleText(site, s) + "</span><span class=\"sr-chip sr-chip-w\">" + HtmlEncode(s.Rating) + "</span>" + (IsUp(s, today) ? "<span class=\"sr-up\">UP</span>" : string.Empty) + "</div>");
            sb.Append("<h1>" + HtmlEncode(s.Title) + "</h1><p class=\"sr-sby\">by <a href=\"Artists?a=" + s.Artist + "\">" + HtmlEncode(a == null ? string.Empty : a.Name) + "</a></p>");
            sb.Append("<p class=\"sr-ssyn\">" + HtmlEncode(s.Synopsis) + "</p>");
            sb.Append("<div class=\"sr-sstats\"><div><b>" + Compact(s.Views) + "</b><span>views</span></div><div><b id=\"sr-subs\">" + Compact(s.Subscribers) + "</b><span>subscribers</span></div><div><b>" + Compact(likes) + "</b><span>likes</span></div><div><b>" + s.Episodes.Count + "</b><span>episodes</span></div></div>");
            sb.Append("<div class=\"sr-sbtns\"><a class=\"sr-btn\" href=\"Episode?s=" + s.Key + "&e=1\">Read Episode 1</a>");
            if (s.Episodes.Count > 1)
            {
                sb.Append("<a class=\"sr-btn sr-btn-g\" href=\"Episode?s=" + s.Key + "&e=" + s.Episodes.Count + "\">Latest: Ep. " + s.Episodes.Count + "</a>");
            }
            sb.Append("<button type=\"button\" class=\"sr-btn sr-btn-g sr-subbtn\" id=\"sr-subbtn\" data-s=\"" + s.Key + "\" onclick=\"SeriesJs.subscribe(this)\">+ Subscribe</button></div>");
            sb.Append("<p class=\"sr-snext\">" + NextText(site, s, today) + "</p></div></div></section>");
            sb.Append("<section class=\"sr-wrap sr-eps\"><div class=\"sr-eps-h\"><h2 class=\"sr-h2\">Episodes <small>" + s.Episodes.Count + "</small></h2>");
            sb.Append("<div class=\"sr-seg\"><button type=\"button\" class=\"sr-seg-b sr-act\" data-o=\"new\" onclick=\"SeriesJs.epOrder('" + s.Key + "','new')\">Newest</button><button type=\"button\" class=\"sr-seg-b\" data-o=\"old\" onclick=\"SeriesJs.epOrder('" + s.Key + "','old')\">First</button></div></div>");
            sb.Append("<div class=\"sr-eplist\" id=\"sr-eplist\">" + EpisodeList(site, s, today, "new") + "</div>");
            sb.Append("<div class=\"sr-tags\">" + string.Join(string.Empty, s.Tags.Select(t => "<a class=\"sr-tag\" href=\"Search?q=" + Uri.EscapeDataString(t) + "\">#" + HtmlEncode(t) + "</a>")) + "</div></section>");
            List<ComicSeries> more = site.Series.Where(x => x.Key != s.Key).OrderBy(x => x.Genre == s.Genre ? 0 : 1).ThenByDescending(x => x.Views).Take(4).ToList();
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_series}", sb.ToString())
                .Replace("{plhd_more}", "<section class=\"sr-wrap sr-sec\"><h2 class=\"sr-h2\">You might also like</h2><div class=\"sr-cards sr-cards-4\">" + SeriesGrid(site, more, today) + "</div></section>");
        }

        public async Task<ApiResponse> Episodes()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == GetDataValue("s"));
            if (s == null)
            {
                return response;
            }
            string order = GetDataValue("order") == "old" ? "old" : "new";
            response.SetElementContents("sr-eplist", EpisodeList(site, s, DateTime.Today, order));
            response.ExecuteScript("SeriesJs.epOrdered('" + order + "');");
            return response;
        }

        public async Task<ApiResponse> Subscribe()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == GetDataValue("s"));
            if (s == null)
            {
                return response;
            }
            bool on = GetDataValue("on") == "1";
            response.SetElementContents("sr-subs", Compact(s.Subscribers + (on ? 1 : 0)));
            response.ExecuteScript("SeriesJs.subscribed(" + (on ? "true" : "false") + ",'" + JsText(s.Title) + "','" + JsText(NextShort(site, s, DateTime.Today)) + "');");
            return response;
        }

        private static string EpisodeList(SiteData site, ComicSeries s, DateTime today, string order)
        {
            IEnumerable<ComicEpisode> eps = order == "old" ? s.Episodes.OrderBy(e => e.No) : s.Episodes.OrderByDescending(e => e.No);
            StringBuilder sb = new StringBuilder();
            int latest = s.Episodes.Count == 0 ? 0 : s.Episodes.Max(e => e.No);
            foreach (ComicEpisode e in eps)
            {
                DateTime d = EpisodeDate(s, e.No, today);
                bool up = e.No == latest && IsUp(s, today);
                sb.Append("<a class=\"sr-ep\" href=\"Episode?s=" + s.Key + "&e=" + e.No + "\"><span class=\"sr-ep-th\"><img src=\"images/thumbs/" + s.Key + "/" + e.No + ".webp\" alt=\"\" loading=\"lazy\">" + (up ? "<i class=\"sr-up\">UP</i>" : string.Empty) + "</span>");
                sb.Append("<span class=\"sr-ep-t\"><small>Episode " + e.No + "</small><b>" + HtmlEncode(e.Title) + "</b><em>" + DateText(d, today) + " &middot; " + e.Pages + " pages</em></span>");
                sb.Append("<span class=\"sr-ep-s\"><span>&#9829; " + Compact(e.Likes) + "</span><span>&#128172; " + e.Comments.Count + "</span></span></a>");
            }
            if (s.Status == "ongoing")
            {
                sb.Append("<div class=\"sr-ep sr-ep-next\"><span class=\"sr-ep-th sr-ep-lock\">&#128339;</span><span class=\"sr-ep-t\"><small>Episode " + (latest + 1) + "</small><b>Coming soon</b><em>" + NextShort(site, s, today) + "</em></span></div>");
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
            string list = total == 0 ? "<div class=\"sr-sg-none\">Nothing matches &ldquo;" + HtmlEncode(q) + "&rdquo;</div>"
                : sb.ToString() + "<a class=\"sr-sg-foot\" href=\"Search?q=" + Uri.EscapeDataString(q) + "\">See all " + CountText(total, "result", "results") + " &rarr;</a>";
            response.SetElementContents("sr-sugg", list);
            response.ExecuteScript("SeriesJs.openSugg();");
            return response;
        }

        public async Task<ApiResponse> Newsletter()
        {
            ApiResponse response = new ApiResponse();
            string email = (GetDataValue("email") ?? string.Empty).Trim();
            if (!IsEmail(email))
            {
                response.SetElementContents("sr-nl-msg", "<span class=\"sr-err\">Please enter a valid email address.</span>");
                return response;
            }
            response.SetElementContents("sr-nl-msg", "<span class=\"sr-ok\">You&rsquo;re in! New-episode picks will go to " + HtmlEncode(email) + ". (Demo only &mdash; nothing was stored.)</span>");
            response.ExecuteScript("SeriesJs.newsletterDone();");
            return response;
        }

        private static string SeriesCard(SiteData site, ComicSeries s, DateTime today)
        {
            ComicEpisode? last = s.Episodes.OrderByDescending(e => e.No).FirstOrDefault();
            long likes = s.Episodes.Sum(e => e.Likes);
            Genre? g = site.Genres.FirstOrDefault(x => x.Key == s.Genre);
            return "<a class=\"sr-card\" href=\"Series?s=" + s.Key + "\"><span class=\"sr-card-img\"><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\">"
                + (IsUp(s, today) ? "<i class=\"sr-up\">UP</i>" : string.Empty) + (s.Status == "completed" ? "<i class=\"sr-done\">Completed</i>" : string.Empty) + "</span>"
                + "<span class=\"sr-card-g\" style=\"color:" + (g == null ? "#666" : g.Color) + "\">" + GenreLabel(site, s.Genre) + "</span><b class=\"sr-card-t\">" + HtmlEncode(s.Title) + "</b>"
                + "<span class=\"sr-card-a\">" + HtmlEncode(ArtistName(site, s)) + "</span><span class=\"sr-card-m\"><span>&#9829; " + Compact(likes) + "</span><span>" + ScheduleText(site, s) + "</span></span>"
                + (last == null ? string.Empty : "<span class=\"sr-card-ep\">Ep. " + last.No + " &middot; " + DateText(EpisodeDate(s, last.No, today), today) + "</span>") + "</a>";
        }

        private static string SeriesGrid(SiteData site, List<ComicSeries> list, DateTime today)
        {
            if (list.Count == 0)
            {
                return "<p class=\"sr-empty\">No series to show. <a href=\"Genres\">Browse all genres</a>.</p>";
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
                sb.Append("<a class=\"sr-new\" href=\"Episode?s=" + s.Key + "&e=" + e.No + "\"><span class=\"sr-new-th\"><img src=\"images/thumbs/" + s.Key + "/" + e.No + ".webp\" alt=\"\" loading=\"lazy\">" + (fresh ? "<i class=\"sr-up\">UP</i>" : string.Empty) + "</span>");
                sb.Append("<span class=\"sr-new-t\"><small>" + HtmlEncode(s.Title) + "</small><b>Ep. " + e.No + " &middot; " + HtmlEncode(e.Title) + "</b><em>" + DateText(d, today) + " &middot; &#9829; " + Compact(e.Likes) + "</em></span></a>");
            }
            return sb.ToString();
        }

        private static string DayTabs(SiteData site, string day, DateTime today)
        {
            StringBuilder sb = new StringBuilder();
            string now = DayKey(today);
            foreach (DayItem d in site.Days)
            {
                sb.Append("<button type=\"button\" role=\"tab\" class=\"sr-day" + (d.Key == day ? " sr-act" : string.Empty) + "\" aria-selected=\"" + (d.Key == day ? "true" : "false") + "\" onclick=\"SeriesJs.day('" + d.Key + "')\">" + d.Short + (d.Key == now ? "<i>Today</i>" : string.Empty) + "</button>");
            }
            sb.Append("<button type=\"button\" role=\"tab\" class=\"sr-day" + (day == "done" ? " sr-act" : string.Empty) + "\" aria-selected=\"" + (day == "done" ? "true" : "false") + "\" onclick=\"SeriesJs.day('done')\">Completed</button>");
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
                sb.Append("<a class=\"sr-rank\" href=\"Series?s=" + s.Key + "\"><b class=\"sr-rank-n\">" + i + "</b><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\"><span><b>" + HtmlEncode(s.Title) + "</b><small>" + GenreLabel(site, s.Genre) + " &middot; " + Compact(s.Views) + " views</small></span>" + (IsUp(s, today) ? "<i class=\"sr-up\">UP</i>" : string.Empty) + "</a>");
                i++;
            }
            return sb.ToString();
        }

        private static string ArtistMini(SiteData site, Artist a)
        {
            int n = site.Series.Count(s => s.Artist == a.Key);
            return "<a class=\"sr-amini\" href=\"Artists?a=" + a.Key + "\"><img src=\"images/artists/" + a.Key + ".webp\" alt=\"\" loading=\"lazy\"><b>" + HtmlEncode(a.Name) + "</b><small>" + Compact(a.Followers) + " followers &middot; " + CountText(n, "series", "series") + "</small></a>";
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
            return "<section class=\"sr-wrap sr-nf\"><h1>" + msg + "</h1><p>It may have moved, or the link is incomplete.</p><a class=\"sr-btn\" href=\"Genres\">Browse all series</a><h2 class=\"sr-h2\">Popular right now</h2><div class=\"sr-cards sr-cards-4\">"
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
            return "<a class=\"sr-sg\" href=\"" + href + "\"><img src=\"" + img + "\" alt=\"\"><span><b>" + HtmlEncode(title) + "</b><small>" + HtmlEncode(sub) + "</small></span></a>";
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
