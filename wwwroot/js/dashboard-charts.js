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
                        label: 'Events',
                        data: eventCounts,
                        backgroundColor: '#2F81F7', // Mona Blue
                        borderRadius: 3,
                        yAxisID: 'y'
                    },
                    {
                        label: 'Guests checked in',
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
                        titleFont: { family: "'Inter', sans-serif", size: 12 },
                        bodyFont: { family: "'Inter', sans-serif", size: 12 }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        title: { display: true, text: 'Events', color: '#8B949E', font: { size: 11, weight: 600 } },
                        grid: { color: '#30363D' },
                        ticks: { stepSize: 1, precision: 0, color: '#8B949E', font: { family: "'Inter', sans-serif", size: 11 } }
                    },
                    y1: {
                        beginAtZero: true,
                        position: 'right',
                        title: { display: true, text: 'Guests checked in', color: '#8B949E', font: { size: 11, weight: 600 } },
                        grid: { drawOnChartArea: false },
                        ticks: { stepSize: 1, precision: 0, color: '#8B949E', font: { family: "'Inter', sans-serif", size: 11 } }
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
                labels: ['Going', 'Maybe', 'Not going'],
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
                        titleFont: { family: "'Inter', sans-serif", size: 12 },
                        bodyFont: { family: "'Inter', sans-serif", size: 12 }
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
                        titleFont: { family: "'Inter', sans-serif", size: 12 },
                        bodyFont: { family: "'Inter', sans-serif", size: 12 },
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
                            font: { family: "'Inter', sans-serif", size: 11 }
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

// ==========================================================================
// Print Lifecycle Management for Charts
// Synchronously updates Chart.js instances to print palette (#111 text, #ddd grid)
// and disables animation so the canvas prints fully rendered.
// ==========================================================================

function prepareChartsForPrint() {
    if (typeof Chart === 'undefined') return;

    var monthlyCanvas = document.getElementById('monthlyEventsChart');
    var rsvpCanvas = document.getElementById('rsvpDistributionChart');

    var monthlyChart = monthlyCanvas ? Chart.getChart(monthlyCanvas) : null;
    var rsvpChart = rsvpCanvas ? Chart.getChart(rsvpCanvas) : null;

    if (monthlyChart) {
        monthlyChart._origAnimation = monthlyChart.options.animation;
        monthlyChart.options.animation = false;

        if (monthlyChart.options.plugins && monthlyChart.options.plugins.legend && monthlyChart.options.plugins.legend.labels) {
            monthlyChart.options.plugins.legend.labels.color = '#111111';
        }
        if (monthlyChart.options.scales && monthlyChart.options.scales.x) {
            if (monthlyChart.options.scales.x.ticks) monthlyChart.options.scales.x.ticks.color = '#111111';
            if (monthlyChart.options.scales.x.grid) monthlyChart.options.scales.x.grid.color = '#dddddd';
        }
        if (monthlyChart.options.scales && monthlyChart.options.scales.y) {
            if (monthlyChart.options.scales.y.title) {
                monthlyChart.options.scales.y.title.color = '#111111';
                monthlyChart.options.scales.y.title.text = 'Events';
            }
            if (monthlyChart.options.scales.y.ticks) {
                monthlyChart.options.scales.y.ticks.color = '#111111';
                monthlyChart.options.scales.y.ticks.precision = 0;
                monthlyChart.options.scales.y.ticks.stepSize = 1;
            }
            if (monthlyChart.options.scales.y.grid) monthlyChart.options.scales.y.grid.color = '#dddddd';
        }
        if (monthlyChart.options.scales && monthlyChart.options.scales.y1) {
            if (monthlyChart.options.scales.y1.title) {
                monthlyChart.options.scales.y1.title.color = '#111111';
                monthlyChart.options.scales.y1.title.text = 'Guests checked in';
            }
            if (monthlyChart.options.scales.y1.ticks) {
                monthlyChart.options.scales.y1.ticks.color = '#111111';
                monthlyChart.options.scales.y1.ticks.precision = 0;
                monthlyChart.options.scales.y1.ticks.stepSize = 1;
            }
        }

        monthlyChart.resize();
        monthlyChart.update('none');
    }

    if (rsvpChart) {
        rsvpChart._origAnimation = rsvpChart.options.animation;
        rsvpChart.options.animation = false;

        if (rsvpChart.options.plugins && rsvpChart.options.plugins.legend && rsvpChart.options.plugins.legend.labels) {
            rsvpChart.options.plugins.legend.labels.color = '#111111';
        }

        rsvpChart.resize();
        rsvpChart.update('none');
    }
}

function restoreChartsAfterPrint() {
    if (typeof Chart === 'undefined') return;

    var monthlyCanvas = document.getElementById('monthlyEventsChart');
    var rsvpCanvas = document.getElementById('rsvpDistributionChart');

    var monthlyChart = monthlyCanvas ? Chart.getChart(monthlyCanvas) : null;
    var rsvpChart = rsvpCanvas ? Chart.getChart(rsvpCanvas) : null;

    if (monthlyChart) {
        monthlyChart.options.animation = monthlyChart._origAnimation !== undefined ? monthlyChart._origAnimation : true;

        if (monthlyChart.options.plugins && monthlyChart.options.plugins.legend && monthlyChart.options.plugins.legend.labels) {
            monthlyChart.options.plugins.legend.labels.color = '#8B949E';
        }
        if (monthlyChart.options.scales && monthlyChart.options.scales.x) {
            if (monthlyChart.options.scales.x.ticks) monthlyChart.options.scales.x.ticks.color = '#8B949E';
            if (monthlyChart.options.scales.x.grid) monthlyChart.options.scales.x.grid.color = '#30363D';
        }
        if (monthlyChart.options.scales && monthlyChart.options.scales.y) {
            if (monthlyChart.options.scales.y.title) {
                monthlyChart.options.scales.y.title.color = '#8B949E';
                monthlyChart.options.scales.y.title.text = 'Events';
            }
            if (monthlyChart.options.scales.y.ticks) monthlyChart.options.scales.y.ticks.color = '#8B949E';
            if (monthlyChart.options.scales.y.grid) monthlyChart.options.scales.y.grid.color = '#30363D';
        }
        if (monthlyChart.options.scales && monthlyChart.options.scales.y1) {
            if (monthlyChart.options.scales.y1.title) {
                monthlyChart.options.scales.y1.title.color = '#8B949E';
                monthlyChart.options.scales.y1.title.text = 'Guests checked in';
            }
            if (monthlyChart.options.scales.y1.ticks) monthlyChart.options.scales.y1.ticks.color = '#8B949E';
        }

        monthlyChart.resize();
        monthlyChart.update();
    }

    if (rsvpChart) {
        rsvpChart.options.animation = rsvpChart._origAnimation !== undefined ? rsvpChart._origAnimation : true;

        if (rsvpChart.options.plugins && rsvpChart.options.plugins.legend && rsvpChart.options.plugins.legend.labels) {
            rsvpChart.options.plugins.legend.labels.color = '#8B949E';
        }

        rsvpChart.resize();
        rsvpChart.update();
    }
}

if (!window._dashboardChartsPrintBound) {
    window._dashboardChartsPrintBound = true;
    window.addEventListener('beforeprint', prepareChartsForPrint);
    window.addEventListener('afterprint', restoreChartsAfterPrint);
}
