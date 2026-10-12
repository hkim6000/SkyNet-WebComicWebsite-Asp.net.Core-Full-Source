using System.Globalization;
using System.Text;
using System.Text.Json;
using WebComic.Models;
using SkyNet;

namespace WebComic.codes
{
    public class Studio : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Artist Studio | Panelmoon");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Publish a new episode: drag in your pages, set the order and pick a release time.");

            SiteData site = await LoadSite();
            DateTime today = DateTime.Today;
            StringBuilder opts = new StringBuilder("<option value=\"\">Choose a series</option>");
            foreach (ComicSeries s in site.Series.Where(s => s.Status == "ongoing").OrderBy(s => s.Title))
            {
                opts.Append("<option value=\"" + s.Key + "\">" + HtmlEncode(s.Title) + "</option>");
            }
            opts.Append("<option value=\"new\">+ Start a new series</option>");
            StringBuilder genres = new StringBuilder("<option value=\"\">Choose a genre</option>");
            foreach (Genre g in site.Genres)
            {
                genres.Append("<option value=\"" + g.Key + "\">" + HtmlEncode(g.Label) + "</option>");
            }
            StringBuilder days = new StringBuilder();
            foreach (DayItem d in site.Days)
            {
                days.Append("<option value=\"" + d.Key + "\">" + d.Name + "</option>");
            }
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_sopts}", opts.ToString())
                .Replace("{plhd_gopts}", genres.ToString())
                .Replace("{plhd_dopts}", days.ToString())
                .Replace("{plhd_today}", today.ToString("yyyy-MM-dd", Inv))
                .Replace("{plhd_max}", today.AddDays(90).ToString("yyyy-MM-dd", Inv))
                .Replace("{plhd_stats}", StudioStats(site, null, today));
        }

        public async Task<ApiResponse> Choose()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            DateTime today = DateTime.Today;
            string key = GetDataValue("s") ?? string.Empty;
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == key && x.Status == "ongoing");
            response.SetElementContents("st-stats", StudioStats(site, s, today));
            if (s == null)
            {
                response.ExecuteScript("StudioJs.picked(" + (key == "new" ? "1" : "0") + ",'','','','');");
                return response;
            }
            DateTime next = NextDate(s, today);
            response.ExecuteScript("StudioJs.picked(" + (s.Episodes.Count + 1) + ",'" + s.Schedule + "','" + s.Day + "','" + next.ToString("yyyy-MM-dd", Inv) + "','" + s.Key + "');");
            return response;
        }

        private static readonly string[] Types = { "image/jpeg", "image/png", "image/webp", "image/gif" };

        public async Task<ApiResponse> Publish()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            DateTime today = DateTime.Today;
            string key = GetDataValue("series") ?? string.Empty;
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == key && x.Status == "ongoing");
            bool isNew = key == "new";
            string newTitle = (GetDataValue("newtitle") ?? string.Empty).Trim();
            string genre = Pick(GetDataValue("genre"), site.Genres.Select(g => g.Key));
            string sched = Pick(GetDataValue("sched"), new[] { "daily", "weekly" });
            string day = Pick(GetDataValue("day"), site.Days.Select(d => d.Key));
            int no = Bound(GetDataValue("no"), 1, 999);
            string title = (GetDataValue("title") ?? string.Empty).Trim();
            string when = GetDataValue("when") == "later" ? "later" : "now";
            string time = (GetDataValue("time") ?? string.Empty).Trim();
            string note = (GetDataValue("note") ?? string.Empty).Trim();
            bool rights = GetDataValue("rights") == "1";
            string[] names = (GetDataValue("names") ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries);
            string[] types = (GetDataValue("types") ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries);
            long[] sizes = (GetDataValue("sizes") ?? string.Empty).Split('|', StringSplitOptions.RemoveEmptyEntries).Select(x => long.TryParse(x, NumberStyles.Integer, Inv, out long v) ? v : -1).ToArray();
            string eSeries = s == null && !isNew ? "Please choose a series." : string.Empty;
            string eNew = isNew && (newTitle.Length < 2 || newTitle.Length > 60) ? "Give your new series a title (2 to 60 characters)." : isNew && site.Series.Any(x => string.Equals(x.Title, newTitle, StringComparison.OrdinalIgnoreCase)) ? "A series with that title already exists." : string.Empty;
            string eGenre = isNew && genre == string.Empty ? "Please choose a genre." : string.Empty;
            string eSched = isNew && (sched == string.Empty || (sched == "weekly" && day == string.Empty)) ? "Choose daily, or weekly and a day." : string.Empty;
            string eNo = no == 0 ? "Enter an episode number." : s != null && no <= s.Episodes.Count ? "Episode " + no + " already exists. The next one is " + (s.Episodes.Count + 1) + "." : s != null && no > s.Episodes.Count + 1 ? "Episodes go in order. The next one is " + (s.Episodes.Count + 1) + "." : isNew && no != 1 ? "A new series starts at Episode 1." : string.Empty;
            string eTitle = title.Length < 2 || title.Length > 60 ? "Give the episode a title (2 to 60 characters)." : string.Empty;
            DateTime release = DateTime.Now;
            string eWhen = string.Empty;
            if (when == "later")
            {
                DateTime d;
                TimeSpan t;
                bool okDate = DateTime.TryParseExact(GetDataValue("date") ?? string.Empty, "yyyy-MM-dd", Inv, DateTimeStyles.None, out d);
                bool okTime = TimeSpan.TryParseExact(time, @"hh\:mm", Inv, out t);
                if (!okDate || !okTime)
                {
                    eWhen = "Pick a release date and time.";
                }
                else if (d.Add(t) <= DateTime.Now)
                {
                    eWhen = "The release time has to be in the future.";
                }
                else if (d > today.AddDays(90))
                {
                    eWhen = "You can schedule up to 90 days ahead.";
                }
                else
                {
                    release = d.Add(t);
                }
            }
            string ePages = names.Length == 0 ? "Add at least one page."
                : names.Length > 60 ? "An episode can have up to 60 pages."
                : names.Length != types.Length || names.Length != sizes.Length ? "Something went wrong reading the pages. Please add them again."
                : types.Any(t => !Types.Contains(t)) ? "Pages must be JPG, PNG, WEBP or GIF images."
                : sizes.Any(z => z < 0 || z > 10L * 1024 * 1024) ? "Each page can be up to 10 MB."
                : sizes.Sum() > 100L * 1024 * 1024 ? "An episode can be up to 100 MB in total."
                : string.Empty;
            string eNote = note.Length > 300 ? "Keep the note to readers under 300 characters." : string.Empty;
            string eRights = !rights ? "Please confirm you own the rights to this artwork." : string.Empty;
            response.SetElementContents("st-e-series", eSeries);
            response.SetElementContents("st-e-new", eNew);
            response.SetElementContents("st-e-genre", eGenre);
            response.SetElementContents("st-e-sched", eSched);
            response.SetElementContents("st-e-no", eNo);
            response.SetElementContents("st-e-title", eTitle);
            response.SetElementContents("st-e-when", eWhen);
            response.SetElementContents("st-e-pages", ePages);
            response.SetElementContents("st-e-note", eNote);
            response.SetElementContents("st-e-rights", eRights);
            if (eSeries + eNew + eGenre + eSched + eNo + eTitle + eWhen + ePages + eNote + eRights != string.Empty)
            {
                response.SetElementContents("st-pdone", string.Empty);
                response.ExecuteScript("StudioJs.publishFailed();");
                return response;
            }
            string seriesTitle = s != null ? s.Title : newTitle;
            string schedText = s != null ? ScheduleText(site, s) : sched == "daily" ? "Daily" : "Every " + site.Days.First(d => d.Key == day).Name;
            string mb = (sizes.Sum() / 1024d / 1024d).ToString("0.0", Inv);
            string code = Code("EP", seriesTitle + "|" + no + "|" + title + "|" + names.Length);
            string whenText = when == "now" ? "is live now" : "goes live " + release.ToString("dddd, MMM d", Inv) + " at " + release.ToString("h:mm tt", Inv).ToLowerInvariant();
            StringBuilder sb = new StringBuilder();
            sb.Append("<div class=\"st-pok\"><div class=\"st-pok-ic\">&#10003;</div><div><b>" + HtmlEncode(seriesTitle) + " &middot; Episode " + no + ": &ldquo;" + HtmlEncode(title) + "&rdquo; " + whenText + ".</b>");
            sb.Append("<span>" + CountText(names.Length, "page", "pages") + " &middot; " + mb + " MB &middot; " + schedText + " &middot; reference " + code + "</span>");
            if (isNew)
            {
                sb.Append("<span>New series in " + GenreLabel(site, genre) + ". Readers can subscribe as soon as Episode 1 is out.</span>");
            }
            sb.Append("<em>Demo: the server checked every field, but nothing was uploaded or saved. Your images never left your browser.</em></div></div>");
            response.SetElementContents("st-pdone", sb.ToString());
            response.ExecuteScript("StudioJs.published(" + JsonSerializer.Serialize(seriesTitle + " · Ep. " + no + " · " + title) + "," + JsonSerializer.Serialize(when == "now" ? "Live" : release.ToString("MMM d, h:mm tt", Inv)) + ");");
            return response;
        }

        private static string StudioStats(SiteData site, ComicSeries? s, DateTime today)
        {
            long views = s == null ? 1284500 : s.Views;
            long subs = s == null ? 96400 : s.Subscribers;
            long likes = s == null ? 211800 : s.Episodes.Sum(e => e.Likes);
            long comments = s == null ? 4120 : s.Episodes.Sum(e => e.Comments.Count) * 37;
            string next = s == null ? "Pick a series" : NextDate(s, today).ToString("ddd, MMM d", Inv);
            return "<div><b>" + Compact(views) + "</b><span>total views</span></div><div><b>" + Compact(subs) + "</b><span>subscribers</span></div><div><b>" + Compact(likes) + "</b><span>likes</span></div><div><b>" + Compact(comments) + "</b><span>comments</span></div><div><b>" + next + "</b><span>next release</span></div>";
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
            string list = total == 0 ? "<div class=\"st-sg-none\">Nothing matches &ldquo;" + HtmlEncode(q) + "&rdquo;</div>"
                : sb.ToString() + "<a class=\"st-sg-foot\" href=\"Search?q=" + Uri.EscapeDataString(q) + "\">See all " + CountText(total, "result", "results") + " &rarr;</a>";
            response.SetElementContents("st-sugg", list);
            response.ExecuteScript("StudioJs.openSugg();");
            return response;
        }

        public async Task<ApiResponse> Newsletter()
        {
            ApiResponse response = new ApiResponse();
            string email = (GetDataValue("email") ?? string.Empty).Trim();
            if (!IsEmail(email))
            {
                response.SetElementContents("st-nl-msg", "<span class=\"st-err\">Please enter a valid email address.</span>");
                return response;
            }
            response.SetElementContents("st-nl-msg", "<span class=\"st-ok\">You&rsquo;re in! New-episode picks will go to " + HtmlEncode(email) + ". (Demo only &mdash; nothing was stored.)</span>");
            response.ExecuteScript("StudioJs.newsletterDone();");
            return response;
        }

        private static string SeriesCard(SiteData site, ComicSeries s, DateTime today)
        {
            ComicEpisode? last = s.Episodes.OrderByDescending(e => e.No).FirstOrDefault();
            long likes = s.Episodes.Sum(e => e.Likes);
            Genre? g = site.Genres.FirstOrDefault(x => x.Key == s.Genre);
            return "<a class=\"st-card\" href=\"Series?s=" + s.Key + "\"><span class=\"st-card-img\"><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\">"
                + (IsUp(s, today) ? "<i class=\"st-up\">UP</i>" : string.Empty) + (s.Status == "completed" ? "<i class=\"st-done\">Completed</i>" : string.Empty) + "</span>"
                + "<span class=\"st-card-g\" style=\"color:" + (g == null ? "#666" : g.Color) + "\">" + GenreLabel(site, s.Genre) + "</span><b class=\"st-card-t\">" + HtmlEncode(s.Title) + "</b>"
                + "<span class=\"st-card-a\">" + HtmlEncode(ArtistName(site, s)) + "</span><span class=\"st-card-m\"><span>&#9829; " + Compact(likes) + "</span><span>" + ScheduleText(site, s) + "</span></span>"
                + (last == null ? string.Empty : "<span class=\"st-card-ep\">Ep. " + last.No + " &middot; " + DateText(EpisodeDate(s, last.No, today), today) + "</span>") + "</a>";
        }

        private static string SeriesGrid(SiteData site, List<ComicSeries> list, DateTime today)
        {
            if (list.Count == 0)
            {
                return "<p class=\"st-empty\">No series to show. <a href=\"Genres\">Browse all genres</a>.</p>";
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
                sb.Append("<a class=\"st-new\" href=\"Episode?s=" + s.Key + "&e=" + e.No + "\"><span class=\"st-new-th\"><img src=\"images/thumbs/" + s.Key + "/" + e.No + ".webp\" alt=\"\" loading=\"lazy\">" + (fresh ? "<i class=\"st-up\">UP</i>" : string.Empty) + "</span>");
                sb.Append("<span class=\"st-new-t\"><small>" + HtmlEncode(s.Title) + "</small><b>Ep. " + e.No + " &middot; " + HtmlEncode(e.Title) + "</b><em>" + DateText(d, today) + " &middot; &#9829; " + Compact(e.Likes) + "</em></span></a>");
            }
            return sb.ToString();
        }

        private static string DayTabs(SiteData site, string day, DateTime today)
        {
            StringBuilder sb = new StringBuilder();
            string now = DayKey(today);
            foreach (DayItem d in site.Days)
            {
                sb.Append("<button type=\"button\" role=\"tab\" class=\"st-day" + (d.Key == day ? " st-act" : string.Empty) + "\" aria-selected=\"" + (d.Key == day ? "true" : "false") + "\" onclick=\"StudioJs.day('" + d.Key + "')\">" + d.Short + (d.Key == now ? "<i>Today</i>" : string.Empty) + "</button>");
            }
            sb.Append("<button type=\"button\" role=\"tab\" class=\"st-day" + (day == "done" ? " st-act" : string.Empty) + "\" aria-selected=\"" + (day == "done" ? "true" : "false") + "\" onclick=\"StudioJs.day('done')\">Completed</button>");
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
                sb.Append("<a class=\"st-rank\" href=\"Series?s=" + s.Key + "\"><b class=\"st-rank-n\">" + i + "</b><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\"><span><b>" + HtmlEncode(s.Title) + "</b><small>" + GenreLabel(site, s.Genre) + " &middot; " + Compact(s.Views) + " views</small></span>" + (IsUp(s, today) ? "<i class=\"st-up\">UP</i>" : string.Empty) + "</a>");
                i++;
            }
            return sb.ToString();
        }

        private static string ArtistMini(SiteData site, Artist a)
        {
            int n = site.Series.Count(s => s.Artist == a.Key);
            return "<a class=\"st-amini\" href=\"Artists?a=" + a.Key + "\"><img src=\"images/artists/" + a.Key + ".webp\" alt=\"\" loading=\"lazy\"><b>" + HtmlEncode(a.Name) + "</b><small>" + Compact(a.Followers) + " followers &middot; " + CountText(n, "series", "series") + "</small></a>";
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
            return "<section class=\"st-wrap st-nf\"><h1>" + msg + "</h1><p>It may have moved, or the link is incomplete.</p><a class=\"st-btn\" href=\"Genres\">Browse all series</a><h2 class=\"st-h2\">Popular right now</h2><div class=\"st-cards st-cards-4\">"
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
            return "<a class=\"st-sg\" href=\"" + href + "\"><img src=\"" + img + "\" alt=\"\"><span><b>" + HtmlEncode(title) + "</b><small>" + HtmlEncode(sub) + "</small></span></a>";
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
