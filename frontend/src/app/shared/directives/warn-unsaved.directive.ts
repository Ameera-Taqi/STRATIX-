import { Directive, HostListener, input } from '@angular/core';

/**
 * Warns on browser refresh / tab close when the bound form is dirty.
 * Route navigation is handled separately by unsavedChangesGuard.
 */
@Directive({
  selector: '[appWarnUnsaved]',
  standalone: true,
})
export class WarnUnsavedDirective {
  /** True when there are unsaved edits. */
  readonly appWarnUnsaved = input(false);

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (!this.appWarnUnsaved()) return;
    event.preventDefault();
    event.returnValue = '';
  }
}
