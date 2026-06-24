export function employeeInitials(name: string): string {
  return name
    .trim()
    .split(/\s+/)
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();
}

const DEPARTMENT_BADGE: Record<string, string> = {
  IT: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
  Product: 'bg-violet-100 text-violet-700 dark:bg-violet-900/40 dark:text-violet-300',
  Operations: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
  HR: 'bg-pink-100 text-pink-700 dark:bg-pink-900/40 dark:text-pink-300',
  Finance: 'bg-teal-100 text-teal-700 dark:bg-teal-900/40 dark:text-teal-300',
};

export function departmentBadgeClass(department: string): string {
  return DEPARTMENT_BADGE[department] ?? 'bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-300';
}
