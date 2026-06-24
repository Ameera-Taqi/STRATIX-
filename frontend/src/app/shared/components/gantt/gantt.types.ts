export interface GanttRow {
  id: number;
  label: string;
  start: string;
  end: string;
  progress: number;
  color?: string;
  sublabel?: string;
}

export interface GanttTaskMarker {
  id: number;
  rowId: number;
  label: string;
  date: string;
  color?: string;
}
