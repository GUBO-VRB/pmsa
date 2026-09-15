// Progressive enhancement for the project report (spec 012).
//
// Two things it buys, neither of which the page depends on — without it every control is still an
// ordinary GET form submission and every requirement below is still met by the server:
//
//   NFR-002 — a filter change re-renders the result without a full page reload.
//   SC-005  — a backwards range is refused before anything is fetched, so the result already on
//             screen stays there. The server refuses it too; it just has no previous result to keep.
(function () {
    'use strict';

    var MAX_DAYS = 366;

    var form = document.getElementById('report-filter');
    if (!form || !window.fetch || !window.history || !window.history.pushState || !window.DOMParser) {
        return;
    }

    var results = document.getElementById('report-result');
    if (!results) {
        return;
    }

    function dayNumber(value) {
        var parts = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value || '');
        return parts ? Date.UTC(+parts[1], +parts[2] - 1, +parts[3]) / 86400000 : null;
    }

    // The same two refusals as DateRange.TryCreate, worded the same way. They are duplicated here
    // rather than round-tripped precisely so that the result on screen survives the refusal.
    function rangeProblem(from, to) {
        var begin = dayNumber(from);
        var end = dayNumber(to);
        if (begin === null || end === null) {
            return null; // Let the server answer for anything this cannot parse (EC-4).
        }
        if (end < begin) {
            return 'The end date must not precede the begin date.';
        }
        if (end - begin + 1 > MAX_DAYS) {
            return 'A report covers at most ' + MAX_DAYS + ' days.';
        }
        return null;
    }

    function clearProblem() {
        var existing = document.getElementById('range-problem');
        if (existing) {
            existing.remove();
        }
    }

    function showProblem(message) {
        clearProblem();
        var alert = document.createElement('div');
        alert.id = 'range-problem';
        alert.className = 'alert alert-danger';
        alert.setAttribute('role', 'alert');
        alert.textContent = message;
        results.parentNode.insertBefore(alert, results);
    }

    function swap(html, url) {
        var incoming = new DOMParser().parseFromString(html, 'text/html');
        var incomingResults = incoming.getElementById('report-result');
        var incomingForm = incoming.getElementById('report-filter');
        if (!incomingResults || !incomingForm) {
            window.location.assign(url);
            return;
        }

        // The form comes back too: a preset changes the dates, and the range the server settled on
        // decides which preset button reads as pressed.
        form.innerHTML = incomingForm.innerHTML;
        results.innerHTML = incomingResults.innerHTML;

        clearProblem();
        var incomingProblem = incoming.getElementById('range-problem');
        if (incomingProblem) {
            showProblem(incomingProblem.textContent.trim());
        }

        // FR-018: the address always spells out the report on screen, so it stays copyable.
        window.history.pushState({ report: true }, '', url);
    }

    function load(url) {
        form.setAttribute('aria-busy', 'true');
        return fetch(url, { headers: { 'Accept': 'text/html' }, credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) {
                    // A sign-in redirect or an error page is the server's to present (EC-16).
                    window.location.assign(url);
                    return null;
                }
                return response.text().then(function (html) {
                    // response.url is the address after the canonicalising redirect, which is the
                    // one worth pushing.
                    swap(html, response.url || url);
                });
            })
            .catch(function () {
                window.location.assign(url);
            })
            .then(function () {
                form.removeAttribute('aria-busy');
            });
    }

    form.addEventListener('submit', function (event) {
        var params = new URLSearchParams(new FormData(form));

        // A submit button's own name/value is not part of the form data, and for the preset
        // buttons that value is the whole message.
        var submitter = event.submitter;
        if (submitter && submitter.name) {
            params.append(submitter.name, submitter.value);
        }

        // A preset is always well formed, so only a typed range can be refused here.
        if (!params.has('preset')) {
            var problem = rangeProblem(params.get('from'), params.get('to'));
            if (problem) {
                event.preventDefault();
                showProblem(problem);
                return;
            }
        }

        event.preventDefault();
        load(form.action.split('?')[0] + '?' + params.toString());
    });

    // Back and forward should move between reports, not out of the screen.
    window.addEventListener('popstate', function (event) {
        if (event.state && event.state.report) {
            load(window.location.href);
        }
    });
})();
