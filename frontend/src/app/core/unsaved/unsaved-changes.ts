import { LanguageService } from '../i18n/language.service';

/** Components that may block navigation when a form is dirty. */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

export function formSnapshot<T>(value: T): string {
  return JSON.stringify(value);
}

export function isFormDirty(current: unknown, baseline: string | null): boolean {
  if (baseline == null) return false;
  return formSnapshot(current) !== baseline;
}

export function confirmLeaveUnsaved(lang: LanguageService): boolean {
  return window.confirm(lang.t('unsaved.confirm'));
}

/** Returns true when navigation / dismiss may proceed. */
export function allowLeaveIfClean(
  dirty: boolean,
  lang: LanguageService,
): boolean {
  if (!dirty) return true;
  return confirmLeaveUnsaved(lang);
}
