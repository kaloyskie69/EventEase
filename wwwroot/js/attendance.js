// ==========================================================================
// EventEase - Event-Day Live Attendance & Check-In Script
// ==========================================================================

document.addEventListener('DOMContentLoaded', function () {
    var searchInput = document.getElementById('attendeeSearchInput');
    var filterSelect = document.getElementById('attendeeStatusFilter');
    var attendeeTable = document.getElementById('attendeeRosterTable');

    // 1. Audio feedback using Web Audio API
    // Caches AudioContext to prevent memory leaks from multiple context creation.
    // Reuses single context across multiple calls, checking for 'closed' state.
    // Browser policy: Audio context requires user interaction before first playback.
    var audioCtx = null;
    function playCheckInChime() {
        try {
            var AudioContextClass = window.AudioContext || window.webkitAudioContext;
            if (!AudioContextClass) return;

            // Reuse existing context if active, or create new if closed
            if (!audioCtx || audioCtx.state === 'closed') {
                audioCtx = new AudioContextClass();
            }

            // Resume context if suspended (e.g., from browser autoplay policy)
            if (audioCtx.state === 'suspended') {
                audioCtx.resume();
            }

            // Create temporary oscillator and gain nodes for this chime
            var osc = audioCtx.createOscillator();
            var gain = audioCtx.createGain();

            // Connect nodes: osc -> gain -> speakers
            osc.connect(gain);
            gain.connect(audioCtx.destination);

            // D5 to A5 ascending note sequence
            osc.type = 'sine';
            osc.frequency.setValueAtTime(587.33, audioCtx.currentTime); // D5 note
            osc.frequency.setValueAtTime(880.00, audioCtx.currentTime + 0.1); // A5 note

            // Fade out over 350ms
            gain.gain.setValueAtTime(0.15, audioCtx.currentTime);
            gain.gain.exponentialRampToValueAtTime(0.001, audioCtx.currentTime + 0.35);

            // Play tone and stop
            osc.start();
            osc.stop(audioCtx.currentTime + 0.35);

            // Note: Nodes are automatically garbage collected after playback completes
        } catch (e) {
            // Audio features may be blocked by browser, disabled, or unavailable
            // Gracefully continue without audio feedback
        }
    }

    // 2. Real-time Instant Filter & Search Table Rows
    function filterTableRows() {
        if (!attendeeTable) return;
        var query = (searchInput ? searchInput.value : '').toLowerCase().trim();
        var filterVal = (filterSelect ? filterSelect.value : 'All');

        var rows = attendeeTable.querySelectorAll('tbody tr.attendee-row');
        var visibleCount = 0;

        rows.forEach(function (row) {
            var name = row.getAttribute('data-name') || '';
            var email = row.getAttribute('data-email') || '';
            var rsvpStatus = row.getAttribute('data-rsvp-status') || '';
            var isCheckedIn = row.getAttribute('data-checked-in') === 'true';

            var matchesSearch = query === '' || name.toLowerCase().includes(query) || email.toLowerCase().includes(query);

            var matchesFilter = true;
            if (filterVal === 'CheckedIn') {
                matchesFilter = isCheckedIn;
            } else if (filterVal === 'NotCheckedIn') {
                matchesFilter = !isCheckedIn;
            } else if (filterVal !== 'All') {
                matchesFilter = (rsvpStatus.toLowerCase() === filterVal.toLowerCase());
            }

            if (matchesSearch && matchesFilter) {
                row.style.display = '';
                visibleCount++;
            } else {
                row.style.display = 'none';
            }
        });

        var noResultsRow = document.getElementById('noAttendeesMatchRow');
        if (noResultsRow) {
            noResultsRow.style.display = (visibleCount === 0) ? '' : 'none';
        }

        var visibleCountEl = document.getElementById('visibleAttendeeCount');
        if (visibleCountEl) visibleCountEl.innerText = visibleCount;
    }

    if (searchInput) {
        searchInput.addEventListener('input', filterTableRows);
    }
    if (filterSelect) {
        filterSelect.addEventListener('change', filterTableRows);
    }

    // 3. AJAX Check-In / Undo Toggle Handler
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.btn-toggle-checkin');
        if (!btn) return;

        var rsvpId = parseInt(btn.getAttribute('data-rsvp-id'), 10);
        var isUndo = btn.getAttribute('data-undo') === 'true';
        var row = document.getElementById('attendee-row-' + rsvpId);

        // UI loading state
        var originalHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>`;

        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        fetch('/Attendance/ToggleCheckIn', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'RequestVerificationToken': token ? token.value : ''
            },
            body: JSON.stringify({ rsvpId: rsvpId, undo: isUndo })
        })
        .then(function (res) { return res.json(); })
        .then(function (data) {
            btn.disabled = false;

            if (data.success) {
                if (data.checkedIn) {
                    playCheckInChime();
                    showToast("Checked In", data.message, false);

                    // Update Row
                    if (row) {
                        row.setAttribute('data-checked-in', 'true');
                        row.classList.add('checked-in-row');
                        
                        var statusBadge = row.querySelector('.checkin-status-badge');
                        if (statusBadge) {
                            statusBadge.className = 'badge badge-checkedin font-mono checkin-status-badge';
                            statusBadge.innerHTML = `<i class="bi bi-check-circle-fill me-1"></i> checked_in`;
                        }

                        var timeEl = row.querySelector('.checkin-time-text');
                        if (timeEl) {
                            timeEl.innerText = data.checkedInTime || "Just now";
                        }
                    }

                    // Switch button to Undo
                    btn.setAttribute('data-undo', 'true');
                    btn.className = 'btn btn-sm btn-outline-custom btn-toggle-checkin';
                    btn.innerHTML = `<i class="bi bi-arrow-counterclockwise"></i> Undo`;
                } else {
                    showToast("Status Updated", data.message, false);

                    // Update Row
                    if (row) {
                        row.setAttribute('data-checked-in', 'false');
                        row.classList.remove('checked-in-row');
                        
                        var statusBadge = row.querySelector('.checkin-status-badge');
                        if (statusBadge) {
                            statusBadge.className = 'badge badge-neutral font-mono checkin-status-badge';
                            statusBadge.innerHTML = `<i class="bi bi-clock me-1"></i> pending`;
                        }

                        var timeEl = row.querySelector('.checkin-time-text');
                        if (timeEl) {
                            timeEl.innerText = "—";
                        }
                    }

                    // Switch button to Check In
                    btn.setAttribute('data-undo', 'false');
                    btn.className = 'btn btn-sm btn-primary btn-toggle-checkin';
                    btn.innerHTML = `<i class="bi bi-check2"></i> Check In`;
                }

                // Update Stats Banner Elements
                var checkedCountEl = document.getElementById('statTotalCheckedIn');
                if (checkedCountEl) checkedCountEl.innerText = data.totalCheckedIn;

                var percentEl = document.getElementById('statAttendancePercent');
                if (percentEl) percentEl.innerText = data.attendancePercentage + "%";

                var progressBar = document.getElementById('statProgressBar');
                if (progressBar) {
                    progressBar.style.width = data.attendancePercentage + "%";
                    progressBar.setAttribute('aria-valuenow', data.attendancePercentage);
                }

                // Re-evaluate filters if currently viewing filtered list
                if (filterSelect && filterSelect.value !== 'All') {
                    filterTableRows();
                }
            } else {
                btn.innerHTML = originalHtml;
                showToast("Error", data.message || "Failed to update check-in.", true);
            }
        })
        .catch(function (err) {
            btn.disabled = false;
            btn.innerHTML = originalHtml;
            console.error('Check-in error', err);
            showToast("Network Error", "Unable to reach server. Please try again.", true);
        });
    });

    // Initial filter run
    filterTableRows();
});
