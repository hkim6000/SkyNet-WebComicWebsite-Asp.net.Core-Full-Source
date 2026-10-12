var LoginJs = (function () {

    var timer = null;
    var ftimer = null;
    var ttimer = null;
    var stimer = null;
    var rtimer = null;
    var lastQ = '';
    var slideIx = 0;
    var paused = false;
    var vote = '';
    var mine = 0;
    var follows = {};
    var R = null;
    var pages = [];
    var nextSlot = '';
    var drag = null;
    var touchX = 0;
    var touchY = 0;
    var swiped = false;

    function el(id) {
        return document.getElementById(id);
    }

    function val(id) {
        var e = el(id);
        return e ? (e.value || '').trim() : '';
    }

    function checked(id) {
        var e = el(id);
        return e && e.checked ? '1' : '';
    }

    function radio(name) {
        var r = document.querySelector('input[name="' + name + '"]:checked');
        return r ? r.value : '';
    }

    function site() {
        return document.querySelector('.lg-site');
    }

    function page() {
        var s = site();
        return s ? s.getAttribute('data-page') : '';
    }

    function param(k) {
        return new URLSearchParams(location.search).get(k) || '';
    }

    function esc(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function call(method, list) {
        $ApiRequest('Login/' + method, JSON.stringify(list || []));
    }

    function base() {
        return location.pathname.replace(/[^\/]*$/, '') || '/';
    }

    function getCookie(k) {
        var m = document.cookie.match(new RegExp('(?:^|; )' + k + '=([^;]*)'));
        return m ? decodeURIComponent(m[1]) : '';
    }

    function setCookie(k, v, days) {
        var c = k + '=' + encodeURIComponent(v) + '; path=' + base() + '; SameSite=Lax';
        if (days) {
            c += '; max-age=' + (days * 86400);
        }
        document.cookie = c;
    }

    function delCookie(k) {
        document.cookie = k + '=; path=' + base() + '; max-age=0; SameSite=Lax';
    }

    function member() {
        return getCookie('pm_member');
    }

    function artist() {
        return getCookie('pm_artist');
    }

    function toast(msg) {
        var t = el('lg-toast');
        if (!t) {
            return;
        }
        t.textContent = msg;
        t.classList.add('lg-open');
        clearTimeout(ttimer);
        ttimer = setTimeout(function () {
            t.classList.remove('lg-open');
        }, 3200);
    }

    function renderAcct() {
        var m = member();
        var a = artist();
        site().setAttribute('data-auth', a ? 'artist' : (m ? 'member' : 'none'));
        var name = m || a;
        if (!name) {
            return;
        }
        var who = m ? 'Signed in as ' + esc(m) + (a ? ' &middot; artist ' + esc(a) : '') : 'Signed in as artist ' + esc(a);
        var html = '<div class="lg-me" id="lg-me"><button type="button" class="lg-me-b" aria-haspopup="true" onclick="LoginJs.meMenu(event)">'
            + '<span class="lg-me-av' + (!m ? ' lg-me-av-art' : '') + '">' + esc(name.charAt(0).toUpperCase()) + '</span><span class="lg-me-n">' + esc(name) + '</span></button>'
            + '<div class="lg-me-m"><small>' + who + '</small>'
            + (a ? '<a href="Studio">Artist Studio</a>' : '<a href="ArtistLogin">Artist login</a>')
            + (m ? '' : '<a href="Login">Member login</a>')
            + '<a href="Membership">Membership</a><button type="button" onclick="LoginJs.signOut()">Sign out</button></div></div>';
        el('lg-acct').innerHTML = html;
        var nav = el('lg-nav-acct');
        if (nav) {
            nav.innerHTML = (a ? '<a class="lg-btn" href="Studio">Artist Studio</a>' : '<a class="lg-btn" href="ArtistLogin">Artist login</a>')
                + '<button type="button" class="lg-btn lg-btn-o" onclick="LoginJs.signOut()">Sign out ' + esc(name) + '</button>';
        }
    }

    function meMenu(e) {
        e.stopPropagation();
        el('lg-me').classList.toggle('lg-open');
    }

    function signOut() {
        delCookie('pm_member');
        delCookie('pm_artist');
        toast('Signed out');
        setTimeout(function () {
            location.reload();
        }, 500);
    }

    function signIn() {
        var list = [
            { key: 'email', vlu: val('lg-email') },
            { key: 'password', vlu: el('lg-pass').value }
        ];
        if (el('lg-pen')) {
            list.push({ key: 'pen', vlu: val('lg-pen') });
        }
        call('SignIn', list);
        return false;
    }

    function safeReturn() {
        var r = param('r');
        return /^[A-Za-z]+(\?[A-Za-z0-9=&%._-]*)?$/.test(r) ? r : '';
    }

    function signedIn(name, role) {
        setCookie(role === 'artist' ? 'pm_artist' : 'pm_member', name, checked('lg-remember') ? 30 : 0);
        toast('Welcome, ' + name + '!');
        setTimeout(function () {
            location.href = safeReturn() || (role === 'artist' ? 'Studio' : 'Home');
        }, 600);
    }

    function togglePass() {
        var p = el('lg-pass');
        p.type = p.type === 'password' ? 'text' : 'password';
    }

    function showReset(on) {
        el('lg-signin').parentElement.classList.toggle('lg-resetting', on);
        if (on && el('lg-remail')) {
            el('lg-remail').value = val('lg-email');
        }
    }

    function reset() {
        call('Reset', [{ key: 'email', vlu: val('lg-remail') }]);
        return false;
    }

    function openNav() {
        el('lg-nav').classList.add('lg-open');
        el('lg-scrim').classList.add('lg-open');
        document.body.style.overflow = 'hidden';
    }

    function closeNav() {
        el('lg-nav').classList.remove('lg-open');
        el('lg-scrim').classList.remove('lg-open');
        document.body.style.overflow = '';
    }

    function toggleSearch() {
        var s = el('lg-search');
        s.classList.toggle('lg-open');
        if (s.classList.contains('lg-open')) {
            el('lg-q').focus();
        } else {
            closeSugg();
        }
    }

    function search(value) {
        var q = (value || '').trim();
        clearTimeout(timer);
        if (q.length < 2) {
            closeSugg();
            lastQ = '';
            return;
        }
        timer = setTimeout(function () {
            if (q === lastQ && el('lg-sugg').innerHTML !== '') {
                openSugg();
                return;
            }
            lastQ = q;
            call('Suggest', [{ key: 'term', vlu: q }]);
        }, 250);
    }

    function searchKey(e) {
        if (e.key === 'Enter') {
            var q = val('lg-q');
            if (q.length > 0) {
                location.href = 'Search?q=' + encodeURIComponent(q);
            }
        } else if (e.key === 'Escape') {
            closeSugg();
        }
    }

    function openSugg() {
        el('lg-sugg').classList.add('lg-open');
    }

    function closeSugg() {
        var s = el('lg-sugg');
        if (s) {
            s.classList.remove('lg-open');
        }
    }

    function newsletter() {
        call('Newsletter', [{ key: 'email', vlu: val('lg-nl-email') }]);
        return false;
    }

    function newsletterDone() {
        el('lg-nl-email').value = '';
    }

    function slide(i) {
        var s = document.querySelectorAll('.lg-slide');
        var d = document.querySelectorAll('.lg-dot');
        if (!s.length) {
            return;
        }
        slideIx = (i + s.length) % s.length;
        for (var k = 0; k < s.length; k++) {
            s[k].classList.toggle('lg-act', k === slideIx);
            if (d[k]) {
                d[k].classList.toggle('lg-act', k === slideIx);
            }
        }
    }

    function slideBy(n) {
        slide(slideIx + n);
        startSlides();
    }

    function pause(p) {
        paused = p;
    }

    function startSlides() {
        clearInterval(stimer);
        stimer = setInterval(function () {
            if (!paused && !document.hidden) {
                slide(slideIx + 1);
            }
        }, 6500);
    }

    function day(d) {
        call('Schedule', [{ key: 'day', vlu: d }]);
    }

    function genre(g) {
        el('lg-genre').value = g;
        filter();
    }

    function filter() {
        call('Filter', [
            { key: 'genre', vlu: val('lg-genre') },
            { key: 'sch', vlu: radio('lg-sch') },
            { key: 'st', vlu: radio('lg-st') },
            { key: 'sort', vlu: val('lg-sort') }
        ]);
    }

    function genreSet(g, color) {
        var p = new URLSearchParams();
        if (g) {
            p.set('g', g);
        }
        if (radio('lg-sch')) {
            p.set('sch', radio('lg-sch'));
        }
        if (radio('lg-st')) {
            p.set('st', radio('lg-st'));
        }
        if (val('lg-sort')) {
            p.set('sort', val('lg-sort'));
        }
        var qs = p.toString();
        history.replaceState(null, '', 'Genres' + (qs ? '?' + qs : ''));
        el('lg-ghead').style.setProperty('--lg-g', color);
    }

    function artistGenre(g) {
        call('Filter', [{ key: 'genre', vlu: g }, { key: 'artist', vlu: param('a') }]);
    }

    function follow(btn) {
        var key = btn.getAttribute('data-a');
        call('Follow', [{ key: 'artist', vlu: key }, { key: 'on', vlu: follows[key] ? '' : '1' }]);
    }

    function followed(key, on) {
        follows[key] = on;
        var b = document.querySelectorAll('.lg-follow[data-a="' + key + '"]');
        for (var i = 0; i < b.length; i++) {
            b[i].classList.toggle('lg-on', on);
            b[i].textContent = on ? 'Following ✓' : 'Follow';
        }
        toast(on ? 'Following! New episodes will show in your feed.' : 'Unfollowed');
    }

    function subscribe(btn) {
        call('Subscribe', [{ key: 's', vlu: btn.getAttribute('data-s') }, { key: 'on', vlu: btn.classList.contains('lg-on') ? '' : '1' }]);
    }

    function subscribed(on, title, next) {
        var b = el('lg-subbtn');
        b.classList.toggle('lg-on', on);
        b.textContent = on ? '✓ Subscribed' : '+ Subscribe';
        toast(on ? 'Subscribed to ' + title + '. ' + next + '.' : 'Unsubscribed from ' + title);
    }

    function epOrder(s, o) {
        call('Episodes', [{ key: 's', vlu: s }, { key: 'order', vlu: o }]);
    }

    function epOrdered(o) {
        var b = document.querySelectorAll('.lg-seg-b[data-o]');
        for (var i = 0; i < b.length; i++) {
            b[i].classList.toggle('lg-act', b[i].getAttribute('data-o') === o);
        }
    }

    function react(v) {
        call('React', [{ key: 's', vlu: param('s') }, { key: 'e', vlu: param('e') }, { key: 'vote', vlu: v }, { key: 'prev', vlu: vote }]);
    }

    function reacted(now) {
        var msg = now === 'like' ? 'Liked! The artist will love that.' : now === 'dislike' ? 'Thanks for the honest feedback.' : 'Reaction removed';
        vote = now;
        toast(msg);
    }

    function initComments() {
        var f = el('lg-cform');
        if (!f) {
            return;
        }
        if (member()) {
            f.classList.add('lg-in');
            el('lg-cname').textContent = 'Commenting as ' + member();
        }
    }

    function ccount(t) {
        el('lg-clen').textContent = t.value.length + ' / 500';
    }

    function comment(s, e) {
        call('Comment', [{ key: 's', vlu: s }, { key: 'e', vlu: String(e) }, { key: 'name', vlu: member() }, { key: 'text', vlu: val('lg-ctext') }, { key: 'mine', vlu: String(mine) }]);
    }

    function commented(html) {
        el('lg-clist').insertAdjacentHTML('afterbegin', html);
        el('lg-ctext').value = '';
        ccount(el('lg-ctext'));
        mine++;
        toast('Comment posted');
    }

    function likeComment(b) {
        var on = !b.classList.contains('lg-on');
        b.classList.toggle('lg-on', on);
        b.querySelector('span').textContent = parseInt(b.getAttribute('data-n'), 10) + (on ? 1 : 0);
    }

    function toComments() {
        var a = el('lg-after');
        if (a) {
            a.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }
    }

    function share() {
        var url = location.href;
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(url).then(function () {
                toast('Link copied');
            }, function () {
                toast(url);
            });
        } else {
            toast(url);
        }
    }

    function readerStart(list) {
        R = { pages: list, n: list.length + 1, t: 0, L: 0, single: false, forced: R ? R.forced : '', zt: null };
        build(0);
    }

    function wantSingle() {
        if (R.forced) {
            return R.forced === 'single';
        }
        return window.innerWidth < 900 || window.innerWidth < window.innerHeight * 1.08;
    }

    function maxT() {
        return R.single ? R.n - 1 : Math.ceil((R.n - 1) / 2);
    }

    function curPage() {
        if (!R) {
            return 0;
        }
        return R.single ? R.t : Math.max(0, Math.min(R.n - 1, R.t * 2 - (R.t * 2 >= R.n ? 1 : 0)));
    }

    function face(ix, cls) {
        var f = document.createElement('div');
        f.className = 'lg-face ' + cls;
        if (ix < R.pages.length) {
            f.appendChild(pic(R.pages[ix], 'Page ' + (ix + 1), 0));
        } else if (ix === R.pages.length) {
            var t = el('lg-endtpl');
            if (t) {
                f.appendChild(t.content.cloneNode(true));
            }
        } else {
            f.classList.add('lg-face-blank');
        }
        return f;
    }

    function pic(src, alt, max) {
        if (typeof src === 'string') {
            var img = document.createElement('img');
            img.src = src;
            img.alt = alt;
            img.draggable = false;
            return img;
        }
        var bmp = src.bmp;
        var k = max ? Math.min(1, max / bmp.width) : 1;
        var c = document.createElement('canvas');
        c.width = Math.max(1, Math.round(bmp.width * k));
        c.height = Math.max(1, Math.round(bmp.height * k));
        c.getContext('2d').drawImage(bmp, 0, 0, c.width, c.height);
        c.setAttribute('role', 'img');
        c.setAttribute('aria-label', alt);
        return c;
    }

    function build(p) {
        var book = el('lg-book');
        R.single = wantSingle();
        book.className = 'lg-book lg-nofx' + (R.single ? ' lg-single' : '');
        book.innerHTML = '';
        var per = R.single ? 1 : 2;
        R.L = Math.ceil(R.n / per);
        for (var i = 0; i < R.L; i++) {
            var leaf = document.createElement('div');
            leaf.className = 'lg-leaf';
            leaf.appendChild(face(i * per, 'lg-front'));
            if (!R.single) {
                leaf.appendChild(face(i * 2 + 1, 'lg-back'));
            }
            book.appendChild(leaf);
        }
        size();
        R.t = Math.min(maxT(), R.single ? p : Math.ceil(p / 2));
        apply();
        fixZ();
        strip();
        var m = el('lg-rmode');
        if (m) {
            m.textContent = R.single ? 'Book view' : 'Single page';
        }
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                book.classList.remove('lg-nofx');
            });
        });
    }

    function size() {
        var stage = el('lg-stage');
        var book = el('lg-book');
        if (!stage || !R) {
            return;
        }
        var cs = getComputedStyle(stage);
        var W = stage.clientWidth - parseFloat(cs.paddingLeft) - parseFloat(cs.paddingRight);
        var modal = stage.closest('.lg-pv') !== null;
        var H = window.innerHeight - (document.fullscreenElement ? 110 : modal ? 190 : 200);
        H = Math.max(H, 300);
        var ratio = 1217 / 860;
        var pw = R.single ? Math.min(W, H / ratio, 660) : Math.min(W / 2, H / ratio, 640);
        pw = Math.max(120, Math.floor(pw));
        book.style.width = (R.single ? pw : pw * 2) + 'px';
        book.style.height = Math.round(pw * ratio) + 'px';
    }

    function apply() {
        var book = el('lg-book');
        var leaves = book.children;
        for (var i = 0; i < leaves.length; i++) {
            leaves[i].classList.toggle('lg-turned', i < R.t);
        }
        book.classList.toggle('lg-start', !R.single && R.t === 0);
        book.classList.toggle('lg-end', !R.single && R.t === R.L);
        var total = R.pages.length;
        var label;
        if (R.single) {
            label = R.t < total ? (R.t + 1) + ' / ' + total : 'End';
        } else {
            var a = R.t * 2 - 1;
            var b = R.t * 2;
            var shown = [];
            if (a >= 0 && a < total) {
                shown.push(a + 1);
            }
            if (b < total) {
                shown.push(b + 1);
            }
            label = shown.length ? shown.join('–') + ' / ' + total : 'End';
        }
        var c = el('lg-rcount');
        if (c) {
            c.textContent = label;
        }
        var seen = R.single ? R.t + 1 : Math.min(R.n, R.t * 2 + 1);
        var pr = el('lg-rprog');
        if (pr) {
            pr.style.width = Math.round(seen / R.n * 100) + '%';
        }
        var pv = el('lg-tprev');
        var nx = el('lg-tnext');
        if (pv) {
            pv.disabled = R.t <= 0;
        }
        if (nx) {
            nx.disabled = R.t >= maxT();
        }
        var sb = document.querySelectorAll('#lg-strip button');
        var cur = curPage();
        for (var k = 0; k < sb.length; k++) {
            var on = R.single ? k === R.t : (k === R.t * 2 - 1 || k === R.t * 2);
            sb[k].classList.toggle('lg-act', on);
            if (on && k === cur && sb[k].scrollIntoView && el('lg-strip').classList.contains('lg-open')) {
                sb[k].scrollIntoView({ block: 'nearest', inline: 'center' });
            }
        }
    }

    function fixZ() {
        var leaves = el('lg-book').children;
        for (var i = 0; i < leaves.length; i++) {
            leaves[i].style.zIndex = i < R.t ? (i + 1) : (leaves.length - i + 1);
        }
    }

    function go(t) {
        if (!R) {
            return;
        }
        t = Math.max(0, Math.min(maxT(), t));
        if (t === R.t) {
            return;
        }
        var book = el('lg-book');
        var leaves = book.children;
        var jump = Math.abs(t - R.t) > 1;
        if (jump) {
            book.classList.add('lg-nofx');
        } else {
            var i = Math.min(t, R.t);
            leaves[i].style.zIndex = 500;
        }
        R.t = t;
        apply();
        clearTimeout(R.zt);
        if (jump) {
            fixZ();
            requestAnimationFrame(function () {
                requestAnimationFrame(function () {
                    book.classList.remove('lg-nofx');
                });
            });
        } else {
            R.zt = setTimeout(fixZ, 780);
        }
    }

    function next() {
        if (R) {
            go(R.t + 1);
        }
    }

    function prev() {
        if (R) {
            go(R.t - 1);
        }
    }

    function first() {
        if (R) {
            go(0);
        }
    }

    function goPage(ix) {
        go(R.single ? ix : Math.ceil(ix / 2));
    }

    function strip() {
        var s = el('lg-strip');
        if (!s) {
            return;
        }
        s.innerHTML = '';
        for (var i = 0; i < R.pages.length; i++) {
            var b = document.createElement('button');
            b.type = 'button';
            b.setAttribute('aria-label', 'Page ' + (i + 1));
            b.setAttribute('onclick', 'LoginJs.goPage(' + i + ')');
            b.appendChild(pic(R.pages[i], '', 120));
            var n = document.createElement('span');
            n.textContent = i + 1;
            b.appendChild(n);
            s.appendChild(b);
        }
        s.insertAdjacentHTML('beforeend', '<button type="button" class="lg-strip-end" onclick="LoginJs.goPage(' + R.pages.length + ')">End</button>');
        apply();
    }

    function toggleStrip() {
        el('lg-strip').classList.toggle('lg-open');
        apply();
    }

    function toggleMode() {
        var p = curPage();
        R.forced = R.single ? 'spread' : 'single';
        build(p);
    }

    function zoom() {
        if (!R) {
            return;
        }
        var ix = Math.min(curPage(), R.pages.length - 1);
        if (!R.single && R.t * 2 < R.pages.length) {
            ix = R.t * 2;
        }
        var z = el('lg-zoomin');
        z.innerHTML = '';
        z.appendChild(pic(R.pages[ix], 'Page ' + (ix + 1) + ', zoomed', 0));
        el('lg-zoom').classList.add('lg-open');
        document.body.style.overflow = 'hidden';
    }

    function unzoom(e) {
        if (e && e.target && (e.target.tagName === 'IMG' || e.target.tagName === 'CANVAS')) {
            return;
        }
        el('lg-zoom').classList.remove('lg-open');
        document.body.style.overflow = '';
    }

    function fullscreen() {
        var r = el('lg-reader');
        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else if (r.requestFullscreen) {
            r.requestFullscreen().catch(function () {
                toast('Full screen is not available here');
            });
        } else {
            toast('Full screen is not available here');
        }
    }

    function bindReader() {
        var stage = el('lg-stage');
        if (!stage) {
            return;
        }
        stage.addEventListener('click', function (e) {
            if (swiped || !R || e.target.closest('a, button')) {
                return;
            }
            var b = el('lg-book').getBoundingClientRect();
            var mid = R.single ? b.left + b.width * 0.35 : b.left + b.width / 2;
            if (e.clientX < mid) {
                prev();
            } else {
                next();
            }
        });
        stage.addEventListener('touchstart', function (e) {
            touchX = e.touches[0].clientX;
            touchY = e.touches[0].clientY;
            swiped = false;
        }, { passive: true });
        stage.addEventListener('touchend', function (e) {
            var dx = e.changedTouches[0].clientX - touchX;
            var dy = e.changedTouches[0].clientY - touchY;
            if (Math.abs(dx) > 40 && Math.abs(dx) > Math.abs(dy)) {
                swiped = true;
                if (dx < 0) {
                    next();
                } else {
                    prev();
                }
                setTimeout(function () {
                    swiped = false;
                }, 350);
            }
        }, { passive: true });
        document.addEventListener('fullscreenchange', function () {
            if (R) {
                size();
            }
        });
    }

    function onResize() {
        clearTimeout(rtimer);
        rtimer = setTimeout(function () {
            if (!R || !el('lg-book') || el('lg-book').children.length === 0) {
                return;
            }
            if (wantSingle() !== R.single) {
                build(curPage());
            } else {
                size();
            }
        }, 180);
    }

    function onKey(e) {
        var t = e.target;
        if (t && (t.tagName === 'INPUT' || t.tagName === 'TEXTAREA' || t.tagName === 'SELECT')) {
            return;
        }
        if (e.key === 'Escape') {
            if (el('lg-zoom') && el('lg-zoom').classList.contains('lg-open')) {
                unzoom();
            } else if (el('lg-pv') && el('lg-pv').classList.contains('lg-open')) {
                closePreview();
            }
            closeSugg();
            return;
        }
        if (!R || !el('lg-book') || el('lg-book').children.length === 0) {
            return;
        }
        if (el('lg-pv') && !el('lg-pv').classList.contains('lg-open')) {
            return;
        }
        if (e.key === 'ArrowRight' || e.key === 'PageDown') {
            next();
            e.preventDefault();
        } else if (e.key === 'ArrowLeft' || e.key === 'PageUp') {
            prev();
            e.preventDefault();
        } else if (e.key === 'Home') {
            first();
        }
    }

    function find(q) {
        clearTimeout(ftimer);
        ftimer = setTimeout(function () {
            call('Find', [{ key: 'term', vlu: (q || '').trim() }]);
        }, 300);
    }

    function found(q) {
        history.replaceState(null, '', q ? 'Search?q=' + encodeURIComponent(q) : 'Search');
    }

    function pickPlan(p) {
        var r = document.querySelector('input[name="lg-plan"][value="' + p + '"]');
        if (r) {
            r.checked = true;
        }
    }

    function join() {
        call('Join', [
            { key: 'name', vlu: val('lg-jname') },
            { key: 'email', vlu: val('lg-jemail') },
            { key: 'password', vlu: el('lg-jpass').value },
            { key: 'pick', vlu: radio('lg-plan') },
            { key: 'age', vlu: checked('lg-jage') },
            { key: 'terms', vlu: checked('lg-jterms') }
        ]);
        return false;
    }

    function joined(name) {
        setCookie('pm_member', name, 30);
        renderAcct();
        toast('Welcome to Panelmoon, ' + name + '!');
    }

    function send() {
        call('Send', [
            { key: 'name', vlu: val('lg-cname') },
            { key: 'email', vlu: val('lg-cemail') },
            { key: 'topic', vlu: val('lg-ctopic') },
            { key: 'message', vlu: val('lg-cmsg') }
        ]);
        return false;
    }

    function sent() {
        el('lg-cmsg').value = '';
        el('lg-ctopic').value = '';
        toast('Message sent');
    }

    function seriesPick(v) {
        el('lg-newseries').classList.toggle('lg-open', v === 'new');
        call('Choose', [{ key: 's', vlu: v }]);
    }

    function picked(no, sched, dayKey, next, key) {
        el('lg-no').value = no || 1;
        nextSlot = next;
        var hint = el('lg-schedhint');
        if (key) {
            var d = new Date(next + 'T09:00:00');
            hint.textContent = (sched === 'daily' ? 'Daily series' : 'Weekly series') + ' · next regular slot ' + d.toLocaleDateString(undefined, { weekday: 'long', month: 'short', day: 'numeric' }) + ', 9:00 am';
            if (radio('lg-when') === 'later') {
                el('lg-date').value = next;
            }
        } else {
            hint.textContent = '';
        }
    }

    function schedSet() {
        el('lg-day').classList.toggle('lg-hide', radio('lg-sched') === 'daily');
    }

    function whenSet() {
        var later = radio('lg-when') === 'later';
        el('lg-whenrow').classList.toggle('lg-open', later);
        if (later && nextSlot) {
            el('lg-date').value = nextSlot;
        }
    }

    function fmtSize(n) {
        return n >= 1048576 ? (n / 1048576).toFixed(1) + ' MB' : Math.max(1, Math.round(n / 1024)) + ' KB';
    }

    function addFiles(list) {
        var jobs = [];
        var skipped = 0;
        for (var i = 0; i < list.length; i++) {
            var f = list[i];
            if (!/^image\/(jpeg|png|webp|gif)$/.test(f.type)) {
                skipped++;
                continue;
            }
            jobs.push((function (file) {
                return createImageBitmap(file).then(function (bmp) {
                    return { bmp: bmp, url: '', name: file.name, size: file.size, type: file.type };
                }, function () {
                    return null;
                });
            })(f));
        }
        var inp = el('lg-files');
        if (inp) {
            inp.value = '';
        }
        Promise.all(jobs).then(function (res) {
            var added = res.filter(function (x) {
                return x !== null;
            });
            skipped += res.length - added.length;
            added.sort(function (a, b) {
                return a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: 'base' });
            });
            pages = pages.concat(added);
            renderThumbs();
            if (added.length) {
                toast(added.length + (added.length === 1 ? ' page added' : ' pages added') + (skipped ? ' \u00b7 ' + skipped + ' skipped (not an image)' : ''));
            } else if (skipped) {
                toast('Only JPG, PNG, WEBP or GIF images can be added');
            }
        });
    }

    function samplePages() {
        var k = val('lg-series');
        if (!k || k === 'new') {
            k = 'lantern-keeper';
        }
        var order = [1, 2, 4, 3, 5, 6];
        for (var i = 0; i < order.length; i++) {
            pages.push({ bmp: null, url: 'images/pages/' + k + '/1/' + order[i] + '.webp', name: k + '-page-' + order[i] + '.webp', size: 61440, type: 'image/webp' });
        }
        renderThumbs();
        toast('6 sample pages added — pages 3 and 4 are swapped. Drag to fix them!');
    }

    function renderThumbs() {
        var h = '';
        for (var i = 0; i < pages.length; i++) {
            var p = pages[i];
            h += '<li class="lg-th" data-i="' + i + '"><span class="lg-th-n">' + (i + 1) + '</span>' + (p.bmp ? '<span class="lg-th-c"></span>' : '<img src="' + esc(p.url) + '" alt="Page ' + (i + 1) + '">')
                + '<button type="button" class="lg-th-x" aria-label="Remove page ' + (i + 1) + '" onclick="LoginJs.removePage(' + i + ')">&times;</button>'
                + '<div class="lg-th-f"><small title="' + esc(p.name) + '">' + esc(p.name) + ' · ' + fmtSize(p.size) + '</small><span class="lg-th-m">'
                + '<button type="button" aria-label="Move earlier" onclick="LoginJs.movePage(' + i + ',-1)"' + (i === 0 ? ' disabled' : '') + '>&larr;</button>'
                + '<button type="button" aria-label="Move later" onclick="LoginJs.movePage(' + i + ',1)"' + (i === pages.length - 1 ? ' disabled' : '') + '>&rarr;</button></span></div></li>';
        }
        el('lg-thumbs').innerHTML = h;
        var slots = el('lg-thumbs').querySelectorAll('.lg-th');
        for (var k = 0; k < slots.length; k++) {
            var c = slots[k].querySelector('.lg-th-c');
            if (c) {
                c.replaceWith(pic(pages[k], 'Page ' + (k + 1), 240));
            }
        }
        el('lg-pcount').textContent = pages.length;
    }

    function removePage(i) {
        var p = pages[i];
        if (p && p.bmp) {
            p.bmp.close();
        }
        pages.splice(i, 1);
        renderThumbs();
    }

    function movePage(i, d) {
        var j = i + d;
        if (j < 0 || j >= pages.length) {
            return;
        }
        var t = pages[i];
        pages[i] = pages[j];
        pages[j] = t;
        renderThumbs();
    }

    function clearMarks() {
        var m = document.querySelectorAll('.lg-target, .lg-target-after');
        for (var i = 0; i < m.length; i++) {
            m[i].classList.remove('lg-target', 'lg-target-after');
        }
    }

    function bindStudio() {
        var drop = el('lg-drop');
        var list = el('lg-thumbs');
        if (!drop || !list) {
            return;
        }
        ['dragenter', 'dragover'].forEach(function (n) {
            drop.addEventListener(n, function (e) {
                e.preventDefault();
                drop.classList.add('lg-over');
            });
        });
        ['dragleave', 'drop'].forEach(function (n) {
            drop.addEventListener(n, function (e) {
                e.preventDefault();
                drop.classList.remove('lg-over');
            });
        });
        drop.addEventListener('drop', function (e) {
            if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files.length) {
                addFiles(e.dataTransfer.files);
            }
        });
        window.addEventListener('dragover', function (e) {
            e.preventDefault();
        });
        window.addEventListener('drop', function (e) {
            e.preventDefault();
        });
        list.addEventListener('pointerdown', function (e) {
            var li = e.target.closest('.lg-th');
            if (!li || e.target.closest('button') || e.pointerType === 'touch' || e.button !== 0) {
                return;
            }
            drag = { i: parseInt(li.getAttribute('data-i'), 10), x: e.clientX, y: e.clientY, li: li, on: false, ghost: null, to: -1 };
            li.setPointerCapture(e.pointerId);
        });
        list.addEventListener('pointermove', function (e) {
            if (!drag) {
                return;
            }
            if (!drag.on) {
                if (Math.abs(e.clientX - drag.x) + Math.abs(e.clientY - drag.y) < 6) {
                    return;
                }
                drag.on = true;
                drag.li.classList.add('lg-dragging');
                var g = pic(pages[drag.i].bmp ? pages[drag.i] : pages[drag.i].url, '', 200);
                g.className = 'lg-th-ghost';
                document.body.appendChild(g);
                drag.ghost = g;
            }
            drag.ghost.style.left = (e.clientX - 50) + 'px';
            drag.ghost.style.top = (e.clientY - 70) + 'px';
            clearMarks();
            drag.to = -1;
            var items = list.querySelectorAll('.lg-th');
            for (var k = 0; k < items.length; k++) {
                var r = items[k].getBoundingClientRect();
                if (e.clientX >= r.left && e.clientX <= r.right && e.clientY >= r.top && e.clientY <= r.bottom) {
                    var after = e.clientX > r.left + r.width / 2;
                    items[k].classList.add(after ? 'lg-target-after' : 'lg-target');
                    drag.to = k + (after ? 1 : 0);
                }
            }
        });
        function end() {
            if (!drag) {
                return;
            }
            if (drag.ghost) {
                drag.ghost.remove();
            }
            clearMarks();
            if (drag.on && drag.to >= 0) {
                var from = drag.i;
                var to = drag.to > from ? drag.to - 1 : drag.to;
                if (to !== from) {
                    var item = pages.splice(from, 1)[0];
                    pages.splice(to, 0, item);
                    toast('Page ' + (from + 1) + ' moved to position ' + (to + 1));
                }
            }
            drag = null;
            renderThumbs();
        }
        list.addEventListener('pointerup', end);
        list.addEventListener('pointercancel', end);
    }

    function preview() {
        if (!pages.length) {
            toast('Add some pages first — or try the sample pages');
            return;
        }
        var t = val('lg-title');
        el('lg-pvtitle').textContent = 'Preview' + (t ? ': ' + t : '') + ' · ' + pages.length + (pages.length === 1 ? ' page' : ' pages');
        el('lg-pv').classList.add('lg-open');
        document.body.style.overflow = 'hidden';
        readerStart(pages.map(function (p) {
            return p.bmp ? p : p.url;
        }));
    }

    function closePreview() {
        el('lg-pv').classList.remove('lg-open');
        document.body.style.overflow = '';
    }

    function publish() {
        call('Publish', [
            { key: 'series', vlu: val('lg-series') },
            { key: 'newtitle', vlu: val('lg-newtitle') },
            { key: 'genre', vlu: val('lg-genre') },
            { key: 'sched', vlu: radio('lg-sched') },
            { key: 'day', vlu: val('lg-day') },
            { key: 'no', vlu: val('lg-no') },
            { key: 'title', vlu: val('lg-title') },
            { key: 'when', vlu: radio('lg-when') },
            { key: 'date', vlu: val('lg-date') },
            { key: 'time', vlu: val('lg-time') },
            { key: 'note', vlu: val('lg-note') },
            { key: 'rights', vlu: checked('lg-rights') },
            { key: 'names', vlu: pages.map(function (p) { return p.name.replace(/\|/g, '_'); }).join('|') },
            { key: 'types', vlu: pages.map(function (p) { return p.type; }).join('|') },
            { key: 'sizes', vlu: pages.map(function (p) { return String(p.size); }).join('|') }
        ]);
        return false;
    }

    function published(text, when) {
        var li = document.createElement('li');
        li.innerHTML = esc(text) + ' <span>· ' + esc(when) + '</span>';
        el('lg-recent').appendChild(li);
        el('lg-recentbox').classList.add('lg-open');
        var n = parseInt(val('lg-no'), 10) || 1;
        el('lg-no').value = n + 1;
        el('lg-title').value = '';
        toast('Episode published (demo)');
        el('lg-pdone').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function publishFailed() {
        toast('Please fix the highlighted fields');
        var errs = document.querySelectorAll('.lg-pub .lg-err');
        for (var i = 0; i < errs.length; i++) {
            if (errs[i].textContent !== '') {
                errs[i].scrollIntoView({ behavior: 'smooth', block: 'center' });
                break;
            }
        }
    }

    function initStudio() {
        var a = artist();
        if (a && el('lg-aname')) {
            el('lg-aname').textContent = a;
        }
        bindStudio();
        schedSet();
    }

    function reveal() {
        renderAcct();
        var head = el('lg-head');
        window.addEventListener('scroll', function () {
            head.classList.toggle('lg-scrolled', window.scrollY > 8);
        }, { passive: true });
        document.addEventListener('click', function (e) {
            if (!e.target.closest('#lg-search')) {
                closeSugg();
            }
            var me = el('lg-me');
            if (me && !me.contains(e.target)) {
                me.classList.remove('lg-open');
            }
        });
        document.addEventListener('keydown', onKey);
        window.addEventListener('resize', onResize);
        var p = page();
        if (p === 'Home') {
            startSlides();
            var hero = el('lg-hero');
            var hx = 0;
            hero.addEventListener('touchstart', function (e) {
                hx = e.touches[0].clientX;
            }, { passive: true });
            hero.addEventListener('touchend', function (e) {
                var dx = e.changedTouches[0].clientX - hx;
                if (Math.abs(dx) > 50) {
                    slideBy(dx < 0 ? 1 : -1);
                }
            }, { passive: true });
        }
        if (p === 'Episode') {
            var r = el('lg-reader');
            if (r) {
                bindReader();
                readerStart(JSON.parse(r.getAttribute('data-pages') || '[]'));
            }
            initComments();
        }
        if (p === 'Studio') {
            bindReader();
            initStudio();
        }
        if (p === 'Login' && member() && !safeReturn()) {
            toast('You are signed in as ' + member());
        }
    }

    return {
        reveal: reveal,
        openNav: openNav,
        closeNav: closeNav,
        toggleSearch: toggleSearch,
        search: search,
        searchKey: searchKey,
        openSugg: openSugg,
        newsletter: newsletter,
        newsletterDone: newsletterDone,
        meMenu: meMenu,
        signOut: signOut,
        signIn: signIn,
        signedIn: signedIn,
        togglePass: togglePass,
        showReset: showReset,
        reset: reset,
        slide: slide,
        slideBy: slideBy,
        pause: pause,
        day: day,
        genre: genre,
        filter: filter,
        genreSet: genreSet,
        artistGenre: artistGenre,
        follow: follow,
        followed: followed,
        subscribe: subscribe,
        subscribed: subscribed,
        epOrder: epOrder,
        epOrdered: epOrdered,
        react: react,
        reacted: reacted,
        ccount: ccount,
        comment: comment,
        commented: commented,
        likeComment: likeComment,
        toComments: toComments,
        share: share,
        next: next,
        prev: prev,
        first: first,
        goPage: goPage,
        toggleStrip: toggleStrip,
        toggleMode: toggleMode,
        zoom: zoom,
        unzoom: unzoom,
        fullscreen: fullscreen,
        find: find,
        found: found,
        pickPlan: pickPlan,
        join: join,
        joined: joined,
        send: send,
        sent: sent,
        seriesPick: seriesPick,
        picked: picked,
        schedSet: schedSet,
        whenSet: whenSet,
        addFiles: addFiles,
        samplePages: samplePages,
        removePage: removePage,
        movePage: movePage,
        preview: preview,
        closePreview: closePreview,
        publish: publish,
        published: published,
        publishFailed: publishFailed
    };
})();

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', LoginJs.reveal);
} else {
    LoginJs.reveal();
}
