/**
 * Exact chart palette sampled from the reference dashboards.
 * Blue + periwinkle lavender + muted teal (not generic Tailwind defaults).
 */
export const CHART_COLORS = {
  /** Comments / primary series — royal blue from bar chart */
  blue: '#3060D0',
  /** Posts series — soft periwinkle (R≈B), not violet */
  purple: '#ACACF4',
  /** Total / success series — muted teal from area chart */
  teal: '#4E9B92',
  /** Soft semantic accents that still fit the dark dashboard */
  amber: '#E0B45C',
  coral: '#E07A7A',
  orange: '#D4925A',
  slate: '#8B95A8',
  track: '#2A3344',
  trackLight: '#D8DEE8',
} as const;

/** Default multi-series cycle matching the reference triad. */
export const CHART_SERIES = [
  CHART_COLORS.blue,
  CHART_COLORS.purple,
  CHART_COLORS.teal,
  CHART_COLORS.amber,
  CHART_COLORS.coral,
] as const;

export function chartSeriesColor(index: number): string {
  return CHART_SERIES[index % CHART_SERIES.length];
}

/** Solid fills like the reference bars (slight top highlight only). */
export function chartBarFill(color: string): string {
  return `linear-gradient(180deg, color-mix(in srgb, ${color} 92%, white) 0%, ${color} 38%, color-mix(in srgb, ${color} 82%, black) 100%)`;
}

export const CHART_TASK_STATUS = {
  TODO: CHART_COLORS.slate,
  IN_PROGRESS: CHART_COLORS.blue,
  REVIEW: CHART_COLORS.purple,
  DONE: CHART_COLORS.teal,
} as const;

export const CHART_HEALTH = {
  HEALTHY: CHART_COLORS.teal,
  WARNING: CHART_COLORS.amber,
  CRITICAL: CHART_COLORS.coral,
} as const;

export const CHART_PRIORITY = {
  URGENT: CHART_COLORS.coral,
  HIGH: CHART_COLORS.orange,
  MEDIUM: CHART_COLORS.amber,
  LOW: CHART_COLORS.blue,
} as const;

export const CHART_LOAD = {
  NORMAL: CHART_COLORS.teal,
  HIGH: CHART_COLORS.amber,
  OVERLOADED: CHART_COLORS.coral,
} as const;

export const CHART_RISK = {
  open: CHART_COLORS.coral,
  critical: CHART_COLORS.orange,
  closed: CHART_COLORS.teal,
  empty: CHART_COLORS.trackLight,
} as const;

export const CHART_BUDGET_STATUS = {
  OK: CHART_COLORS.teal,
  WARNING: CHART_COLORS.amber,
  OVER_BUDGET: CHART_COLORS.coral,
} as const;
