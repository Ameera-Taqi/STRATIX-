import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { LanguageService } from '../i18n/language.service';
import { HasUnsavedChanges, allowLeaveIfClean } from '../unsaved/unsaved-changes';

/**
 * Blocks route deactivation when the component reports unsaved form edits.
 * Pair with HasUnsavedChanges + window:beforeunload in the component.
 */
export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) => {
  if (!component || typeof component.hasUnsavedChanges !== 'function') return true;
  const lang = inject(LanguageService);
  return allowLeaveIfClean(component.hasUnsavedChanges(), lang);
};
