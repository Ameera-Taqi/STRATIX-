export interface ChartSegment {
  label: string;
  value: number;
  color: string;
}

export interface BarItem {
  label: string;
  value: number;
  color?: string;
  sublabel?: string;
}

export interface GroupedBarSeries {
  key: string;
  label: string;
  color: string;
}

export interface GroupedBarItem {
  label: string;
  values: Record<string, number>;
}
