export function priorityClass(priority: string): string {
  const map: Record<string, string> = {
    LOW: 'text-slate-600 bg-slate-100',
    MEDIUM: 'text-blue-700 bg-blue-100',
    HIGH: 'text-amber-800 bg-amber-100',
    URGENT: 'text-red-800 bg-red-100',
  };
  return map[priority] ?? 'bg-slate-100 text-slate-600';
}
