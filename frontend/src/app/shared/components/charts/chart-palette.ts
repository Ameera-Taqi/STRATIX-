/**
 * Chart palette sampled from reference dashboards.
 * Purple is slightly deeper than the screenshot swatch so it stays readable on light backgrounds.
 */
export const CHART_COLORS = {
  blue: '#3060D0',
  /** Periwinkle — readable on white, still soft on dark navy */
  purple: '#8B8BE8',
  teal: '#4E9B92',
  amber: '#E0B45C',
  coral: '#E07A7A',
  orange: '#D4925A',
  slate: '#8B95A8',
  /** Dark-mode donut track */
  track: '#2A3344',
  /** Light-mode donut track */
  trackLight: '#D8DEE8',
} as const;

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

export function chartTrackColor(isDark: boolean): string {
  return isDark ? CHART_COLORS.track : CHART_COLORS.trackLight;
}

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
