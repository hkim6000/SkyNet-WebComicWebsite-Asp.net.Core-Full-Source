using System.Globalization;
using System.Text;
using System.Text.Json;
using WebComic.Models;
using SkyNet;

namespace WebComic.codes
{
    public class Episode : WebPage
    {
        public override async Task OnInitialized()
        {
            HtmlDoc.SetTitle("Episode | Panelmoon");
            HtmlDoc.AddMetaElement("viewport", "width=device-width, initial-scale=1");
            HtmlDoc.AddMetaElement("description", "Read a Panelmoon episode in the page-flipping reader.");

            SiteData site = await LoadSite();
            DateTime today = DateTime.Today;
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == (QueryValue("s") ?? string.Empty).Trim().ToLowerInvariant());
            int no = Bound(QueryValue("e"), 1, 999);
            ComicEpisode? e = s == null ? null : s.Episodes.FirstOrDefault(x => x.No == no);
            if (s == null || e == null)
            {
                HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                    .Replace("{plhd_top}", string.Empty)
                    .Replace("{plhd_reader}", NotFound("That episode isn&rsquo;t available.", site, today))
                    .Replace("{plhd_after}", string.Empty);
                return;
            }
            HtmlDoc.SetTitle(s.Title + " Ep. " + e.No + ": " + e.Title + " | Panelmoon");
            Artist? a = ArtistOf(site, s);
            ComicEpisode? prev = s.Episodes.FirstOrDefault(x => x.No == e.No - 1);
            ComicEpisode? next = s.Episodes.FirstOrDefault(x => x.No == e.No + 1);
            List<string> pages = new List<string>();
            for (int i = 1; i <= e.Pages; i++)
            {
                pages.Add("images/pages/" + s.Key + "/" + e.No + "/" + i + ".webp");
            }
            StringBuilder top = new StringBuilder();
            top.Append("<div class=\"ep-rtop\" style=\"--ep-theme:" + s.Theme + "\"><div class=\"ep-wrap ep-rtop-in\"><a class=\"ep-rtop-s\" href=\"Series?s=" + s.Key + "\"><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\"><span><b>" + HtmlEncode(s.Title) + "</b><small>by " + HtmlEncode(a == null ? string.Empty : a.Name) + "</small></span></a>");
            top.Append("<div class=\"ep-rtop-ep\"><small>Episode " + e.No + " of " + s.Episodes.Count + "</small><b>" + HtmlEncode(e.Title) + "</b></div><div class=\"ep-rtop-nav\">");
            top.Append(prev == null ? "<span class=\"ep-pill ep-off\">&lsaquo; Prev</span>" : "<a class=\"ep-pill\" href=\"Episode?s=" + s.Key + "&e=" + prev.No + "\">&lsaquo; Prev</a>");
            top.Append("<a class=\"ep-pill\" href=\"Series?s=" + s.Key + "\">All</a>");
            top.Append(next == null ? "<span class=\"ep-pill ep-off\">Next &rsaquo;</span>" : "<a class=\"ep-pill\" href=\"Episode?s=" + s.Key + "&e=" + next.No + "\">Next &rsaquo;</a>");
            top.Append("</div></div></div>");
            string endTitle = next != null ? "Next: Episode " + next.No : s.Status == "completed" ? "The End" : "You&rsquo;re all caught up";
            string endSub = next != null ? HtmlEncode(next.Title) : s.Status == "completed" ? "Thanks for reading " + HtmlEncode(s.Title) + "." : NextShort(site, s, today);
            string endHref = next != null ? "Episode?s=" + s.Key + "&e=" + next.No : "Series?s=" + s.Key;
            string endBtn = next != null ? "Read Episode " + next.No + " &rarr;" : "Back to series";
            StringBuilder reader = new StringBuilder();
            reader.Append("<section class=\"ep-reader\" id=\"ep-reader\" data-pages=\"" + HtmlEncode(JsonSerializer.Serialize(pages)) + "\" data-theme=\"" + s.Theme + "\">");
            reader.Append("<div class=\"ep-stage\" id=\"ep-stage\"><div class=\"ep-book\" id=\"ep-book\"></div>");
            reader.Append("<button type=\"button\" class=\"ep-turn ep-turn-p\" id=\"ep-tprev\" aria-label=\"Previous page\" onclick=\"EpisodeJs.prev()\">&lsaquo;</button>");
            reader.Append("<button type=\"button\" class=\"ep-turn ep-turn-n\" id=\"ep-tnext\" aria-label=\"Next page\" onclick=\"EpisodeJs.next()\">&rsaquo;</button></div>");
            reader.Append("<template id=\"ep-endtpl\"><div class=\"ep-endcard\"><div class=\"ep-eyebrow\">End of Episode " + e.No + "</div><h3>" + endTitle + "</h3><p>" + endSub + "</p>");
            reader.Append("<a class=\"ep-btn\" href=\"" + endHref + "\">" + endBtn + "</a><button type=\"button\" class=\"ep-btn ep-btn-o\" onclick=\"EpisodeJs.toComments()\">Like &amp; comment</button></div></template>");
            reader.Append("<div class=\"ep-rbar\"><button type=\"button\" class=\"ep-rb\" onclick=\"EpisodeJs.first()\" aria-label=\"First page\">&laquo;</button><span class=\"ep-rcount\" id=\"ep-rcount\">1 / " + (e.Pages + 1) + "</span>");
            reader.Append("<div class=\"ep-rprog\"><i id=\"ep-rprog\"></i></div><button type=\"button\" class=\"ep-rb\" id=\"ep-rmode\" onclick=\"EpisodeJs.toggleMode()\">Book view</button>");
            reader.Append("<button type=\"button\" class=\"ep-rb\" onclick=\"EpisodeJs.toggleStrip()\">Pages</button><button type=\"button\" class=\"ep-rb\" onclick=\"EpisodeJs.zoom()\">Zoom</button><button type=\"button\" class=\"ep-rb ep-rb-fs\" onclick=\"EpisodeJs.fullscreen()\">Full screen</button></div>");
            reader.Append("<div class=\"ep-strip\" id=\"ep-strip\"></div><p class=\"ep-rhint\">Tap or click the edges, swipe, or use the arrow keys to turn the page.</p></section>");
            reader.Append("<div class=\"ep-zoom\" id=\"ep-zoom\" onclick=\"EpisodeJs.unzoom(event)\"><button type=\"button\" class=\"ep-zoom-x\" onclick=\"EpisodeJs.unzoom()\" aria-label=\"Close\">&times;</button><div class=\"ep-zoom-in\" id=\"ep-zoomin\"></div></div>");
            StringBuilder after = new StringBuilder();
            after.Append("<section class=\"ep-wrap ep-after\" id=\"ep-after\"><div class=\"ep-react\"><div class=\"ep-react-t\"><b>Enjoyed Episode " + e.No + "?</b><span>Let " + HtmlEncode(a == null ? "the artist" : a.Name) + " know.</span></div>");
            after.Append("<div class=\"ep-react-b\" id=\"ep-react\">" + ReactButtons(e.Likes, e.Dislikes, string.Empty) + "</div>");
            after.Append("<button type=\"button\" class=\"ep-btn ep-btn-o ep-btn-s\" onclick=\"EpisodeJs.share()\">Share</button></div>");
            after.Append("<div class=\"ep-after-grid\"><div class=\"ep-comments\"><h2 class=\"ep-h2\">Comments <small id=\"ep-ccount\">" + e.Comments.Count + "</small></h2>");
            after.Append("<div class=\"ep-cform\" id=\"ep-cform\"><div class=\"ep-cgate\" id=\"ep-cgate\"><p><b>Join the conversation.</b> Sign in to comment &mdash; any email works in this demo.</p><a class=\"ep-btn ep-btn-s\" href=\"Login?r=" + Uri.EscapeDataString("Episode?s=" + s.Key + "&e=" + e.No) + "\">Sign in</a></div>");
            after.Append("<div class=\"ep-cbox\" id=\"ep-cbox\"><textarea id=\"ep-ctext\" maxlength=\"500\" rows=\"3\" placeholder=\"Say something nice about this episode\" oninput=\"EpisodeJs.ccount(this)\"></textarea>");
            after.Append("<div class=\"ep-cbox-f\"><span class=\"ep-cname\" id=\"ep-cname\"></span><span class=\"ep-clen\" id=\"ep-clen\">0 / 500</span><button type=\"button\" class=\"ep-btn ep-btn-s\" onclick=\"EpisodeJs.comment('" + s.Key + "'," + e.No + ")\">Post</button></div><div class=\"ep-err\" id=\"ep-e-comment\"></div></div></div>");
            after.Append("<div class=\"ep-clist\" id=\"ep-clist\">" + CommentList(e) + "</div></div>");
            after.Append("<aside class=\"ep-side\"><h3>More episodes</h3>");
            foreach (ComicEpisode x in s.Episodes.Where(x => x.No != e.No).OrderBy(x => Math.Abs(x.No - e.No)).Take(4))
            {
                after.Append("<a class=\"ep-sidep\" href=\"Episode?s=" + s.Key + "&e=" + x.No + "\"><img src=\"images/thumbs/" + s.Key + "/" + x.No + ".webp\" alt=\"\" loading=\"lazy\"><span><small>Episode " + x.No + "</small><b>" + HtmlEncode(x.Title) + "</b></span></a>");
            }
            ComicSeries? rec = site.Series.Where(x => x.Key != s.Key).OrderBy(x => x.Genre == s.Genre ? 0 : 1).ThenByDescending(x => x.Views).FirstOrDefault();
            if (rec != null)
            {
                after.Append("<h3>Readers also like</h3>" + SeriesGrid(site, new List<ComicSeries> { rec }, today));
            }
            after.Append("</aside></div></section>");
            HtmlDoc.HtmlBodyText = HtmlDoc.HtmlBodyText
                .Replace("{plhd_top}", top.ToString())
                .Replace("{plhd_reader}", reader.ToString())
                .Replace("{plhd_after}", after.ToString());
        }

        private static readonly string[] Votes = { "like", "dislike" };

        public async Task<ApiResponse> React()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == GetDataValue("s"));
            ComicEpisode? e = s == null ? null : s.Episodes.FirstOrDefault(x => x.No == Bound(GetDataValue("e"), 1, 999));
            if (e == null)
            {
                return response;
            }
            string prev = Pick(GetDataValue("prev"), Votes);
            string vote = Pick(GetDataValue("vote"), Votes);
            string now = vote == prev ? string.Empty : vote;
            long likes = e.Likes + (now == "like" ? 1 : 0);
            long dislikes = e.Dislikes + (now == "dislike" ? 1 : 0);
            response.SetElementContents("ep-react", ReactButtons(likes, dislikes, now));
            response.ExecuteScript("EpisodeJs.reacted('" + now + "');");
            return response;
        }

        public async Task<ApiResponse> Comment()
        {
            ApiResponse response = new ApiResponse();
            SiteData site = await LoadSite();
            ComicSeries? s = site.Series.FirstOrDefault(x => x.Key == GetDataValue("s"));
            ComicEpisode? e = s == null ? null : s.Episodes.FirstOrDefault(x => x.No == Bound(GetDataValue("e"), 1, 999));
            if (e == null)
            {
                return response;
            }
            string name = (GetDataValue("name") ?? string.Empty).Trim();
            string text = (GetDataValue("text") ?? string.Empty).Trim();
            int mine = Bound(GetDataValue("mine"), 0, 999);
            string err = name.Length == 0 || name.Length > 40 ? "Please sign in to comment."
                : text.Length < 2 ? "Write at least a couple of characters."
                : text.Length > 500 ? "Comments can be up to 500 characters."
                : text.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Any(w => w.Length > 60) ? "That comment has a very long word. Please add some spaces."
                : string.Empty;
            response.SetElementContents("ep-e-comment", err);
            if (err != string.Empty)
            {
                return response;
            }
            string html = CommentHtml(name, text, 0, "Just now", true);
            response.SetElementContents("ep-ccount", (e.Comments.Count + mine + 1).ToString(Inv));
            response.ExecuteScript("EpisodeJs.commented(" + JsonSerializer.Serialize(html) + ");");
            return response;
        }

        private static string ReactButtons(long likes, long dislikes, string vote)
        {
            return "<button type=\"button\" class=\"ep-vote" + (vote == "like" ? " ep-on" : string.Empty) + "\" aria-pressed=\"" + (vote == "like" ? "true" : "false") + "\" onclick=\"EpisodeJs.react('like')\">&#128077; <b>" + likes.ToString("#,0", Inv) + "</b><span>Like</span></button>"
                + "<button type=\"button\" class=\"ep-vote ep-vote-d" + (vote == "dislike" ? " ep-on" : string.Empty) + "\" aria-pressed=\"" + (vote == "dislike" ? "true" : "false") + "\" onclick=\"EpisodeJs.react('dislike')\">&#128078; <b>" + dislikes.ToString("#,0", Inv) + "</b><span>Dislike</span></button>";
        }

        private static string CommentList(ComicEpisode e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ReaderComment c in e.Comments.OrderByDescending(c => c.Likes))
            {
                sb.Append(CommentHtml(c.User, c.Text, c.Likes, AgoText(c.Mins), false));
            }
            return sb.ToString();
        }

        private static string CommentHtml(string user, string text, long likes, string ago, bool mine)
        {
            string initial = user.Length > 0 ? user.Substring(0, 1).ToUpperInvariant() : "?";
            int hue = (int)(Hash(user) % 360);
            return "<article class=\"ep-cm" + (mine ? " ep-cm-mine" : string.Empty) + "\"><span class=\"ep-cm-av\" style=\"background:hsl(" + hue + ",55%,46%)\">" + HtmlEncode(initial) + "</span><div class=\"ep-cm-b\"><div class=\"ep-cm-h\"><b>" + HtmlEncode(user) + "</b>" + (mine ? "<i>You</i>" : string.Empty) + "<span>" + ago + "</span></div><p>" + HtmlEncode(text) + "</p>"
                + "<button type=\"button\" class=\"ep-cm-like\" onclick=\"EpisodeJs.likeComment(this)\" data-n=\"" + likes + "\">&#9829; <span>" + likes + "</span></button></div></article>";
        }

        private static string AgoText(long mins)
        {
            if (mins < 60)
            {
                return mins + "m ago";
            }
            if (mins < 1440)
            {
                return (mins / 60) + "h ago";
            }
            if (mins < 43200)
            {
                return (mins / 1440) + "d ago";
            }
            return (mins / 43200) + "mo ago";
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
            string list = total == 0 ? "<div class=\"ep-sg-none\">Nothing matches &ldquo;" + HtmlEncode(q) + "&rdquo;</div>"
                : sb.ToString() + "<a class=\"ep-sg-foot\" href=\"Search?q=" + Uri.EscapeDataString(q) + "\">See all " + CountText(total, "result", "results") + " &rarr;</a>";
            response.SetElementContents("ep-sugg", list);
            response.ExecuteScript("EpisodeJs.openSugg();");
            return response;
        }

        public async Task<ApiResponse> Newsletter()
        {
            ApiResponse response = new ApiResponse();
            string email = (GetDataValue("email") ?? string.Empty).Trim();
            if (!IsEmail(email))
            {
                response.SetElementContents("ep-nl-msg", "<span class=\"ep-err\">Please enter a valid email address.</span>");
                return response;
            }
            response.SetElementContents("ep-nl-msg", "<span class=\"ep-ok\">You&rsquo;re in! New-episode picks will go to " + HtmlEncode(email) + ". (Demo only &mdash; nothing was stored.)</span>");
            response.ExecuteScript("EpisodeJs.newsletterDone();");
            return response;
        }

        private static string SeriesCard(SiteData site, ComicSeries s, DateTime today)
        {
            ComicEpisode? last = s.Episodes.OrderByDescending(e => e.No).FirstOrDefault();
            long likes = s.Episodes.Sum(e => e.Likes);
            Genre? g = site.Genres.FirstOrDefault(x => x.Key == s.Genre);
            return "<a class=\"ep-card\" href=\"Series?s=" + s.Key + "\"><span class=\"ep-card-img\"><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\">"
                + (IsUp(s, today) ? "<i class=\"ep-up\">UP</i>" : string.Empty) + (s.Status == "completed" ? "<i class=\"ep-done\">Completed</i>" : string.Empty) + "</span>"
                + "<span class=\"ep-card-g\" style=\"color:" + (g == null ? "#666" : g.Color) + "\">" + GenreLabel(site, s.Genre) + "</span><b class=\"ep-card-t\">" + HtmlEncode(s.Title) + "</b>"
                + "<span class=\"ep-card-a\">" + HtmlEncode(ArtistName(site, s)) + "</span><span class=\"ep-card-m\"><span>&#9829; " + Compact(likes) + "</span><span>" + ScheduleText(site, s) + "</span></span>"
                + (last == null ? string.Empty : "<span class=\"ep-card-ep\">Ep. " + last.No + " &middot; " + DateText(EpisodeDate(s, last.No, today), today) + "</span>") + "</a>";
        }

        private static string SeriesGrid(SiteData site, List<ComicSeries> list, DateTime today)
        {
            if (list.Count == 0)
            {
                return "<p class=\"ep-empty\">No series to show. <a href=\"Genres\">Browse all genres</a>.</p>";
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
                sb.Append("<a class=\"ep-new\" href=\"Episode?s=" + s.Key + "&e=" + e.No + "\"><span class=\"ep-new-th\"><img src=\"images/thumbs/" + s.Key + "/" + e.No + ".webp\" alt=\"\" loading=\"lazy\">" + (fresh ? "<i class=\"ep-up\">UP</i>" : string.Empty) + "</span>");
                sb.Append("<span class=\"ep-new-t\"><small>" + HtmlEncode(s.Title) + "</small><b>Ep. " + e.No + " &middot; " + HtmlEncode(e.Title) + "</b><em>" + DateText(d, today) + " &middot; &#9829; " + Compact(e.Likes) + "</em></span></a>");
            }
            return sb.ToString();
        }

        private static string DayTabs(SiteData site, string day, DateTime today)
        {
            StringBuilder sb = new StringBuilder();
            string now = DayKey(today);
            foreach (DayItem d in site.Days)
            {
                sb.Append("<button type=\"button\" role=\"tab\" class=\"ep-day" + (d.Key == day ? " ep-act" : string.Empty) + "\" aria-selected=\"" + (d.Key == day ? "true" : "false") + "\" onclick=\"EpisodeJs.day('" + d.Key + "')\">" + d.Short + (d.Key == now ? "<i>Today</i>" : string.Empty) + "</button>");
            }
            sb.Append("<button type=\"button\" role=\"tab\" class=\"ep-day" + (day == "done" ? " ep-act" : string.Empty) + "\" aria-selected=\"" + (day == "done" ? "true" : "false") + "\" onclick=\"EpisodeJs.day('done')\">Completed</button>");
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
                sb.Append("<a class=\"ep-rank\" href=\"Series?s=" + s.Key + "\"><b class=\"ep-rank-n\">" + i + "</b><img src=\"images/covers/" + s.Key + ".webp\" alt=\"\" loading=\"lazy\"><span><b>" + HtmlEncode(s.Title) + "</b><small>" + GenreLabel(site, s.Genre) + " &middot; " + Compact(s.Views) + " views</small></span>" + (IsUp(s, today) ? "<i class=\"ep-up\">UP</i>" : string.Empty) + "</a>");
                i++;
            }
            return sb.ToString();
        }

        private static string ArtistMini(SiteData site, Artist a)
        {
            int n = site.Series.Count(s => s.Artist == a.Key);
            return "<a class=\"ep-amini\" href=\"Artists?a=" + a.Key + "\"><img src=\"images/artists/" + a.Key + ".webp\" alt=\"\" loading=\"lazy\"><b>" + HtmlEncode(a.Name) + "</b><small>" + Compact(a.Followers) + " followers &middot; " + CountText(n, "series", "series") + "</small></a>";
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
            return "<section class=\"ep-wrap ep-nf\"><h1>" + msg + "</h1><p>It may have moved, or the link is incomplete.</p><a class=\"ep-btn\" href=\"Genres\">Browse all series</a><h2 class=\"ep-h2\">Popular right now</h2><div class=\"ep-cards ep-cards-4\">"
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
            return "<a class=\"ep-sg\" href=\"" + href + "\"><img src=\"" + img + "\" alt=\"\"><span><b>" + HtmlEncode(title) + "</b><small>" + HtmlEncode(sub) + "</small></span></a>";
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
