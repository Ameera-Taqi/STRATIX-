import { Pipe, PipeTransform, inject } from '@angular/core';
import { LanguageService } from './language.service';
import { TranslationKey } from './translations';

@Pipe({ name: 't', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(LanguageService);

  transform(key: TranslationKey | string): string {
    this.i18n.lang();
    return this.i18n.t(key);
  }
}
