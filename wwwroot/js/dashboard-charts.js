// ==========================================================================
// EventEase - Octo Code Dashboard & Reports Charts Script (Chart.js)
// Developer-centric dark palette, JetBrains Mono font, and high-contrast accents
// ==========================================================================

function initDashboardCharts(monthlyData, rsvpData, eventAttendanceData) {
    if (typeof Chart === 'undefined') return;

    // Chart.js Octo Code dark theme defaults
    Chart.defaults.font.family = "'Inter', sans-serif";
    Chart.defaults.color = "#8B949E"; // Octo text secondary
    Chart.defaults.borderColor = "#30363D"; // Octo border

    // 1. Monthly Events & Attendance Chart
    var monthlyCanvas = document.getElementById('monthlyEventsChart');
    if (monthlyCanvas && monthlyData) {
        var existingMonthly = Chart.getChart(monthlyCanvas);
        if (existingMonthly) existingMonthly.destroy();

        var labels = monthlyData.map(function (d) { return d.monthLabel; });
        var eventCounts = monthlyData.map(function (d) { return d.eventCount; });
        var attendanceCounts = monthlyData.map(function (d) { return d.attendanceCount; });

        new Chart(monthlyCanvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Events Created',
                        data: eventCounts,
                        backgroundColor: '#2F81F7', // Mona Blue
                        borderRadius: 3,
                        yAxisID: 'y'
                    },
                    {
                        label: 'Attendees Checked In',
                        data: attendanceCounts,
                        type: 'line',
                        borderColor: '#3FB950', // Growth Green
                        backgroundColor: 'rgba(63, 185, 80, 0.15)',
                        tension: 0.25,
                        fill: true,
                        pointBackgroundColor: '#3FB950',
                        pointBorderColor: '#161B22',
                        pointBorderWidth: 2,
                        pointRadius: 4,
                        yAxisID: 'y1'
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        labels: {
                            color: '#8B949E',
                            boxWidth: 12,
                            usePointStyle: true,
                            font: { size: 12, weight: 500 }
                        }
                    },
                    tooltip: {
                        backgroundColor: '#21262D',
                        borderColor: '#30363D',
                        borderWidth: 1,
                        titleColor: '#E6EDF3',
                        bodyColor: '#8B949E',
                        padding: 10,
                        cornerRadius: 6,
                        titleFont: { family: "'JetBrains Mono', monospace", size: 12 },
                        bodyFont: { family: "'JetBrains Mono', monospace", size: 12 }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: 'Events', color: '#8B949E', font: { size: 11, weight: 600 } },
                        grid: { color: '#30363D' },
                        ticks: { stepSize: 1, color: '#8B949E', font: { family: "'JetBrains Mono', monospace", size: 11 } }
                    },
                    y1: {
                        beginAtZero: true,
                        position: 'right',
                        title: { display: true, text: 'Attendees', color: '#8B949E', font: { size: 11, weight: 600 } },
                        grid: { drawOnChartArea: false },
                        ticks: { color: '#8B949E', font: { family: "'JetBrains Mono', monospace", size: 11 } }
                    },
                    x: {
                        grid: { color: '#30363D' },
                        ticks: { color: '#8B949E', font: { size: 12 } }
                    }
                }
            }
        });
    }

    // 2. RSVP Distribution Donut Chart
    var rsvpCanvas = document.getElementById('rsvpDistributionChart');
    if (rsvpCanvas && rsvpData) {
        var existingRsvp = Chart.getChart(rsvpCanvas);
        if (existingRsvp) existingRsvp.destroy();

        new Chart(rsvpCanvas, {
            type: 'doughnut',
            data: {
                labels: ['Going', 'Maybe', 'Not Going'],
                datasets: [{
                    data: [rsvpData.goingCount, rsvpData.maybeCount, rsvpData.notGoingCount],
                    backgroundColor: ['#3FB950', '#D29922', '#F85149'], // Success, Warning, Error
                    borderWidth: 2,
                    borderColor: '#161B22' // Surface background separation
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '72%',
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            color: '#8B949E',
                            boxWidth: 10,
                            padding: 14,
                            font: { size: 12, weight: 500 }
                        }
                    },
                    tooltip: {
                        backgroundColor: '#21262D',
                        borderColor: '#30363D',
                        borderWidth: 1,
                        titleColor: '#E6EDF3',
                        bodyColor: '#8B949E',
                        padding: 10,
                        cornerRadius: 6,
                        titleFont: { family: "'JetBrains Mono', monospace", size: 12 },
                        bodyFont: { family: "'JetBrains Mono', monospace", size: 12 }
                    }
                }
            }
        });
    }

    // 3. Attendance Rate per Event Bar Chart
    var rateCanvas = document.getElementById('eventAttendanceBarChart');
    if (rateCanvas && eventAttendanceData && eventAttendanceData.length > 0) {
        var existingRate = Chart.getChart(rateCanvas);
        if (existingRate) existingRate.destroy();

        var eventLabels = eventAttendanceData.map(function (e) { return e.eventTitle; });
        var rates = eventAttendanceData.map(function (e) { return e.percentage; });

        new Chart(rateCanvas, {
            type: 'bar',
            data: {
                labels: eventLabels,
                datasets: [{
                    label: 'Attendance Rate (%)',
                    data: rates,
                    backgroundColor: '#238636', // Growth Green
                    borderRadius: 3
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: '#21262D',
                        borderColor: '#30363D',
                        borderWidth: 1,
                        titleColor: '#E6EDF3',
                        bodyColor: '#8B949E',
                        cornerRadius: 6,
                        titleFont: { family: "'JetBrains Mono', monospace", size: 12 },
                        bodyFont: { family: "'JetBrains Mono', monospace", size: 12 },
                        callbacks: {
                            label: function (ctx) {
                                return ctx.raw + '% Attendance Rate';
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        beginAtZero: true,
                        max: 100,
                        ticks: {
                            callback: function (val) { return val + '%'; },
                            color: '#8B949E',
                            font: { family: "'JetBrains Mono', monospace", size: 11 }
                        },
                        grid: { color: '#30363D' }
                    },
                    y: {
                        grid: { display: false },
                        ticks: { color: '#8B949E', font: { size: 12 } }
                    }
                }
            }
        });
    }
}
