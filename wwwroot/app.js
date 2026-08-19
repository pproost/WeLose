async function shareCanvas(canvasId, title, text) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) {
        return;
    }

    const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/png'));
    if (!blob) {
        return;
    }

    const file = new File([blob], 'voortgang.png', { type: 'image/png' });

    if (navigator.canShare && navigator.canShare({ files: [file] })) {
        try {
            await navigator.share({ files: [file], title: title, text: text });
        } catch (err) {
            if (err.name !== 'AbortError') {
                console.error('Share failed', err);
            }
        }
    } else {
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = 'voortgang.png';
        link.click();
        URL.revokeObjectURL(link.href);
    }
}

function roundRect(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
}

window.weightChart = (function () {
    const charts = {};

    function render(canvasId, label, labels, values, color) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const existing = charts[canvasId];
        if (existing && existing.canvas === canvas) {
            existing.data.labels = labels;
            existing.data.datasets[0].label = label;
            existing.data.datasets[0].data = values;
            existing.data.datasets[0].borderColor = color;
            existing.update();
            return;
        }

        if (existing) {
            existing.destroy();
        }

        charts[canvasId] = new Chart(canvas.getContext('2d'), {
            type: 'line',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: values,
                    borderColor: color,
                    backgroundColor: color,
                    tension: 0.3,
                    pointRadius: 3,
                    fill: false
                }]
            },
            options: {
                responsive: true,
                plugins: {
                    legend: { display: true }
                },
                scales: {
                    y: { beginAtZero: false }
                }
            }
        });
    }

    return { render, share: shareCanvas };
})();

window.achievementCard = (function () {
    function render(canvasId, opts) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            return;
        }

        const dpr = window.devicePixelRatio || 1;
        const width = canvas.clientWidth || 320;
        const height = canvas.clientHeight || 400;
        canvas.width = width * dpr;
        canvas.height = height * dpr;

        const ctx = canvas.getContext('2d');
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        ctx.clearRect(0, 0, width, height);

        const gradient = ctx.createLinearGradient(0, 0, width, height);
        gradient.addColorStop(0, '#594AE2');
        gradient.addColorStop(1, '#2CA58D');
        ctx.fillStyle = gradient;
        roundRect(ctx, 0, 0, width, height, 28);
        ctx.fill();

        ctx.save();
        roundRect(ctx, 0, 0, width, height, 28);
        ctx.clip();

        if (opts.trendLast && opts.trendOverall) {
            const badgeY = height * 0.11;
            drawTrendBadge(ctx, width * 0.27, badgeY, opts.trendLast);
            drawTrendBadge(ctx, width * 0.73, badgeY, opts.trendOverall);
        }

        const cx = width / 2;
        const cy = height * 0.46;
        const radius = width / 2 - 50;

        if (opts.percent !== null && opts.percent !== undefined) {
            ctx.beginPath();
            ctx.arc(cx, cy, radius, 0, Math.PI * 2);
            ctx.lineWidth = 16;
            ctx.strokeStyle = 'rgba(255,255,255,0.25)';
            ctx.stroke();

            const startAngle = -Math.PI / 2;
            const endAngle = startAngle + (Math.PI * 2 * Math.max(0.02, opts.percent / 100));
            ctx.beginPath();
            ctx.arc(cx, cy, radius, startAngle, endAngle);
            ctx.lineWidth = 16;
            ctx.lineCap = 'round';
            ctx.strokeStyle = '#ffffff';
            ctx.stroke();
        }

        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.font = `${Math.round(radius * 0.8)}px "Segoe UI Emoji", "Apple Color Emoji", "Noto Color Emoji", sans-serif`;
        ctx.fillStyle = '#ffffff';
        ctx.fillText(opts.emoji, cx, cy);

        ctx.font = 'bold 26px "Segoe UI", Roboto, sans-serif';
        ctx.fillText(opts.title, cx, cy + radius + 44);

        if (opts.subtitle) {
            ctx.font = '16px "Segoe UI", Roboto, sans-serif';
            ctx.fillStyle = 'rgba(255,255,255,0.85)';
            ctx.fillText(opts.subtitle, cx, cy + radius + 74);
        }

        ctx.restore();
    }

    function drawTrendBadge(ctx, cx, cy, badge) {
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';

        ctx.font = 'bold 24px "Segoe UI", Roboto, sans-serif';
        ctx.fillStyle = badge.color;
        ctx.fillText(badge.symbol, cx, cy);

        ctx.font = '13px "Segoe UI", Roboto, sans-serif';
        ctx.fillStyle = 'rgba(255,255,255,0.85)';
        ctx.fillText(badge.label, cx, cy + 21);
    }

    return { render, share: shareCanvas };
})();
