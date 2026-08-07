import { ProjectHealthSnapshot } from '../../core/models/health-snapshot.model';

export interface HealthWeekDelta {
  points: number;
  direction: 'up' | 'down' | 'stable';
}

export interface HealthTrendPoint {
  score: number;
  capturedAt: string;
}

/** Chronological (oldest → newest) scores for sparkline. */
export function trendPointsFromSnapshots(
  snapshots: ProjectHealthSnapshot[],
  currentScore?: number,
): HealthTrendPoint[] {
  const chronological = snapshots
    .slice()
    .sort((a, b) => String(a.capturedAt).localeCompare(String(b.capturedAt)))
    .map((s) => ({
      score: Math.round(Number(s.score) || 0),
      capturedAt: s.capturedAt,
    }));

  if (chronological.length === 0 && currentScore != null) {
    return [{ score: Math.round(currentScore), capturedAt: new Date().toISOString() }];
  }

  // Ensure the live score is the tip of the trend when it differs from latest snapshot.
  if (currentScore != null && chronological.length > 0) {
    const tip = chronological[chronological.length - 1];
    const live = Math.round(currentScore);
    if (tip.score !== live) {
      chronological.push({ score: live, capturedAt: new Date().toISOString() });
    }
  }

  return chronological;
}

/**
 * Compare current score to the snapshot closest to ~7 days ago
 * (or the oldest point in the window if history is shorter).
 */
export function weekDeltaFromSnapshots(
  snapshots: ProjectHealthSnapshot[],
  currentScore: number,
): HealthWeekDelta | null {
  if (snapshots.length === 0) return null;

  const now = Date.now();
  const weekMs = 7 * 24 * 60 * 60 * 1000;
  const target = now - weekMs;

  let best: ProjectHealthSnapshot | null = null;
  let bestDist = Number.POSITIVE_INFINITY;

  for (const s of snapshots) {
    const t = new Date(s.capturedAt).getTime();
    if (Number.isNaN(t)) continue;
    const dist = Math.abs(t - target);
    // Prefer snapshots within 10 days of "a week ago", else oldest.
    if (dist < bestDist) {
      bestDist = dist;
      best = s;
    }
  }

  if (!best) return null;

  const past = Math.round(Number(best.score) || 0);
  const points = Math.round(currentScore) - past;
  if (points > 0) return { points, direction: 'up' };
  if (points < 0) return { points: Math.abs(points), direction: 'down' };
  return { points: 0, direction: 'stable' };
}

/** Build SVG polyline points for a simple sparkline (viewBox 0 0 200 64). */
export function sparklinePoints(
  scores: number[],
  width = 200,
  height = 64,
  pad = 6,
): string {
  if (scores.length === 0) return '';
  const min = Math.min(...scores, 0);
  const max = Math.max(...scores, 100);
  const span = Math.max(1, max - min);
  const usableW = width - pad * 2;
  const usableH = height - pad * 2;

  return scores
    .map((score, i) => {
      const x =
        scores.length === 1 ? width / 2 : pad + (i / (scores.length - 1)) * usableW;
      const y = pad + usableH - ((score - min) / span) * usableH;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(' ');
}
